# PostgreSQL logical backup — `tandoor` had no coverage, and the job never said so

Found while planning the CNPG 17 → 18 upgrade
([david-driscoll/vault#114](https://github.com/david-driscoll/vault/issues/114),
blocking prerequisite 0a). Two independent defects; the second is why the first
went unnoticed.

> **Status: both resolved, 2026-08-02.**
>
> - The silent-success bug was fixed in code by
>   [#2984](https://github.com/david-driscoll/equestria-cluster/pull/2984) (§4).
> - The schema permissions were applied to the live cluster at ~01:20 UTC on
>   2026-08-02, to `tandoor` **and** to the four databases carrying the same
>   latent pattern (§5). §3 records exactly what ran.
>
> Kept as the record of why this happened and what to check if it recurs. The
> stale `tandoor` schema itself has **not** been dropped — see §3B.

---

## 1. What was broken

`postgres-backup` (`kubernetes/apps/database/postgres/backups`, daily at 02:00 UTC)
iterates every non-template database and shells out to `pg_dump`. For `tandoor`
it had been failing for as long as the current script had run:

```text
Backing up database: tandoor
Error backing up database tandoor: pg_dump failed: pg_dump: last built-in OID is 16383
...
pg_dump: error: query failed: ERROR:  permission denied for schema tandoor
```

(`kubectl --context admin@equestria logs -n database postgres-backup-29759160-8hfpr`,
read 2026-08-01.)

`tandoor` carries two parallel schemas. `pg_dump` runs as the unprivileged
`tandoor` login role, reached the second one, tried to `LOCK` its tables, and
aborted the whole dump — including the half it *could* read.

The job still reported `Complete`. See §4.

**`tandoor` therefore had no logical backup at all** until 2026-08-02. How far
back is not knowable from the cluster — `successfulJobsHistory: 3` means only
the last three runs are retained, and all three failed this way. Physical
coverage was never affected: barman-cloud WAL archiving and PITR were enabled in
[#2981](https://github.com/david-driscoll/equestria-cluster/pull/2981) and back
up the whole cluster at the storage layer.

---

## 2. Which schema is authoritative

`public`. Not close:

| | `public` | `tandoor` |
|---|---|---|
| owner | `pg_database_owner` | `postgres` |
| tables | 99 | 95 |
| total size | 4560 kB | 5600 kB |
| `django_migrations` rows | 297 | 290 |
| latest migration applied | **2026-04-03 21:08 UTC** | 2026-01-30 22:51 UTC |
| scans since stats reset | **426,270** | 0 |
| `tandoor` role has USAGE | yes | **no** |

The `tandoor` schema is a snapshot that stopped receiving migrations on
2026-01-30, has never been read, and is 7 migrations behind. Tandoor is a Django
app and sets no `search_path`, so it gets the PostgreSQL default `"$user",
public` — and `"$user"` resolves to the schema named `tandoor`, which would
normally shadow `public`. It does not, purely because the role lacks USAGE and
PostgreSQL silently skips search-path entries it cannot access. The missing
grant is simultaneously what breaks the backup and what keeps the app pointed at
the right data.

### How it got there

`kubernetes/components/postgres/database.yaml` declares a per-app schema for
every database using the `postgres` component. On 2025-07-28, commit `dff50f6d6`
briefly set that schema's `owner` to `${CLUSTER_CNAME}` (= `postgres`);
`667097a8e` restored `owner: ${APP}` the same afternoon. Schemas created inside
that window kept `postgres` as owner, and **CNPG does not reconcile owner drift
on a schema that already exists** — every `Database` resource reports
`status.applied: true` today while the live owner disagrees with the declared
one. The declared GitOps state is already correct; the live cluster is what
drifted.

---

## 3. Remediation — applied 2026-08-02

### A. Pin the search path, then grant — applied

The 95 tables and 91 sequences in `tandoor.tandoor` are all owned by `postgres`,
so schema USAGE alone is not enough: `pg_dump` takes `ACCESS SHARE` locks on
every table and reads sequence values.

There is a trap in granting that access. Tandoor is Django and sets no
`search_path`, so it uses the PostgreSQL default `"$user", public`. Today
`"$user"` — the schema named `tandoor` — is skipped only because the role cannot
see it. **Granting USAGE makes it resolvable and moves it ahead of `public`**,
so a restarted Tandoor would silently start reading the January data.

Pinning the search path first removes the trap outright, which is better than
sequencing around it:

```sql
-- ran against admin@equestria, database tandoor, as postgres
BEGIN;
ALTER ROLE tandoor IN DATABASE tandoor SET search_path = public;  -- do this FIRST
ALTER SCHEMA tandoor OWNER TO tandoor;
GRANT USAGE ON SCHEMA tandoor TO tandoor;
GRANT SELECT ON ALL TABLES IN SCHEMA tandoor TO tandoor;
GRANT SELECT ON ALL SEQUENCES IN SCHEMA tandoor TO tandoor;
COMMIT;
```

With `search_path` pinned, `"$user"` can no longer shadow `public`, the grant is
safe across restarts, and step B becomes optional cleanup rather than a
prerequisite. `ALTER SCHEMA ... OWNER` converges the live cluster with the
ownership already declared in `components/postgres/database.yaml`; the `GRANT`s
cover the objects inside, which that declaration does not reach.

The same `ALTER SCHEMA` + `GRANT` block (**without** the `search_path` pin — see
§5) was applied to `immich`, `n8n`, `retrom`, and `vikunja`.

### B. Drop the stale schema — still outstanding, optional

Once a `tandoor.sql.gz` has been restored and checked:

```sql
\c tandoor
DROP SCHEMA tandoor CASCADE;   -- 95 tables, last written 2026-01-30
```

No longer urgent — the search-path hazard is gone and the schema is now captured
in the nightly dump. This is housekeeping to stop backing up a dead 95-table
copy indefinitely.

### Verify

Rehearse exactly what `pg_dump` does — take its locks as the app role, inside a
transaction that rolls back. Read-only, safe at any time:

```sql
BEGIN;
SET ROLE tandoor;
DO $$
DECLARE r record; n int := 0;
BEGIN
  FOR r IN SELECT c.relname FROM pg_class c JOIN pg_namespace ns ON ns.oid = c.relnamespace
           WHERE ns.nspname = 'tandoor' AND c.relkind = 'r'
  LOOP
    EXECUTE format('LOCK TABLE tandoor.%I IN ACCESS SHARE MODE', r.relname);
    n := n + 1;
  END LOOP;
  RAISE NOTICE 'locked % tables as role %', n, current_user;
END $$;
ROLLBACK;
```

Confirmed `locked 95 tables in schema tandoor as role tandoor` on 2026-08-02.

> **Do not verify with `kubectl create job --from=cronjob/postgres-backup`.**
> `concurrencyPolicy: Forbid` governs only CronJob-created Jobs, so a manual Job
> can run alongside the 02:00 scheduled one. Both would write the same
> `<db>.sql.gz.tmp` staging paths on the NFS share and race each other. Either
> use the lock rehearsal above, or run a manual Job well clear of 02:00 UTC.

---

## 4. Why nobody noticed: the job could not fail

`kubernetes/apps/database/postgres/backups/resources/App.cs` wrapped each
per-database dump in `try`/`catch`, logged the exception, continued, then
unconditionally printed `PostgreSQL backup completed successfully` and fell off
the end of the program with exit 0. The CronJob reported `Complete`, so the
existing Job-failure alerting had nothing to fire on.

Fixed in [#2984](https://github.com/david-driscoll/equestria-cluster/pull/2984):

- failed databases are collected; a non-empty list logs to **stderr** and exits **1**
- each dump is written to `<db>.sql.gz.tmp` and moved into place only after
  `pg_dump` exits 0. Previously `File.Create` truncated the previous good backup
  *before* the new dump was known to be valid, so a failure left a plausible-looking
  file where a working one used to be
- `pg_dump`'s stderr is drained concurrently with stdout instead of after
  `WaitForExit`. `--verbose` writes progress to stderr throughout; once that pipe
  buffer fills, `pg_dump` blocks and the job hangs

The container command already runs under `set -eux` and `dotnet run` propagates
the exit code, so a non-zero exit fails the Job with no manifest change.

---

## 5. The same pattern elsewhere

Every database using the `postgres` component gets an `<app>` schema. Five had
one owned by `postgres` rather than the app role. All five were fixed on
2026-08-02; state before → after:

| database | schema tables | app role had USAGE | dump status before | now |
|---|---|---|---|---|
| **tandoor** | 95 | no | **failing** | owner `tandoor`, USAGE + SELECT on all 95 tables / 91 sequences |
| immich | 18 | yes — **only because `immich` is a superuser** | passing, latently fragile | owner `immich`, USAGE + SELECT, no longer superuser-dependent *for the dump* |
| n8n | 0 | no | passing (nothing to lock) | owner `n8n` |
| retrom | 0 | no | passing (nothing to lock) | owner `retrom` |
| vikunja | 0 | no | passing (nothing to lock) | owner `vikunja` |

Transferring schema ownership is what closes this permanently for the empty
ones: tables the app role creates in a schema it owns are owned by that role, so
a future `CREATE TABLE` cannot reopen the gap.

Two caveats that survive the fix:

- **The `search_path` pin from §3A applies to `tandoor` only.** It was correct
  there because `public` is authoritative and the `tandoor` schema is a dead
  copy. **`immich` is the opposite** — its schema is live data, `geodata_places`
  alone holds 227,901 rows read on 2026-08-01, because recent Immich versions
  genuinely use a dedicated schema. Pinning `search_path = public` on `immich`
  would break the app. Do not copy §3A wholesale.
- **`immich`'s grant covers the backup, not the application.** `pg_dump` needs
  `SELECT`; Immich itself needs write access, which it currently gets from
  `rolsuper = t`. Revoking that superuser bit — reasonable hardening, and
  something a CNPG major-version upgrade may prompt — would leave the nightly
  dump working but break the app. Transfer ownership of the 18 tables and their
  sequences to the `immich` role first.

Since [#2984](https://github.com/david-driscoll/equestria-cluster/pull/2984),
none of these can fail silently again — they fail the whole nightly job instead.

---

## 6. Scope note

`david-driscoll/stargate-command-cluster` carries a byte-identical `App.cs` and
has the same exit-code bug. Its three databases all dump cleanly today, so only
the code fix applies there; it ships as a separate PR.
