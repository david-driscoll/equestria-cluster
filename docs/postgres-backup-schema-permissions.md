# PostgreSQL logical backup — `tandoor` has no coverage, and the job never said so

Found while planning the CNPG 17 → 18 upgrade
([david-driscoll/vault#114](https://github.com/david-driscoll/vault/issues/114),
blocking prerequisite 0a). Two independent defects; the second is why the first
went unnoticed.

The PR that adds this file fixes **only the second one** (the silent-success
bug, in code). The first needs one-time SQL against the live cluster and is
written out below — **nothing here has been applied.**

---

## 1. What is broken

`postgres-backup` (`kubernetes/apps/database/postgres/backups`, daily at 02:00)
iterates every non-template database and shells out to `pg_dump`. For `tandoor`
it has been failing for as long as the current script has run:

```text
Backing up database: tandoor
Error backing up database tandoor: pg_dump failed: pg_dump: last built-in OID is 16383
...
pg_dump: error: query failed: ERROR:  permission denied for schema tandoor
```

(`kubectl --context admin@equestria logs -n database postgres-backup-29759160-8hfpr`,
read 2026-08-01.)

`tandoor` carries two parallel schemas. `pg_dump` runs as the unprivileged
`tandoor` login role, reaches the second one, tries to `LOCK` its tables, and
aborts the whole dump — including the half it *could* read.

The job still reported `Complete`. See §4.

**`tandoor` therefore has no logical backup at all.** Physical coverage is
unaffected: barman-cloud WAL archiving and PITR were enabled in
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

## 3. Remediation — not applied

Two-step, deliberately. Step A is non-destructive and restores coverage
immediately; it also pulls the stale schema *into* the backup, so step B stops
being irreversible.

### A. Grant, so `pg_dump` completes

The 95 tables and 91 sequences in `tandoor.tandoor` are all owned by `postgres`,
so schema USAGE alone is not enough — `pg_dump` takes `ACCESS SHARE` locks and
reads sequence values.

```sql
\c tandoor
ALTER SCHEMA tandoor OWNER TO tandoor;
GRANT USAGE ON SCHEMA tandoor TO tandoor;
GRANT SELECT ON ALL TABLES IN SCHEMA tandoor TO tandoor;
GRANT SELECT ON ALL SEQUENCES IN SCHEMA tandoor TO tandoor;
```

`ALTER SCHEMA ... OWNER` is what converges the live cluster with the ownership
already declared in `components/postgres/database.yaml`. The `GRANT`s cover the
objects inside, whose ownership that declaration does not reach.

> Granting `tandoor` access to the `tandoor` schema makes `"$user"` resolvable
> and therefore **puts it ahead of `public` in the app's search path**. Restart
> the Tandoor deployment only after step B, or verify the app still reads the
> `public` tables. This is the one sharp edge in this document.

### B. Drop the stale schema — only after a verified restore

Once the next nightly run produces a `tandoor.sql.gz` that restores cleanly:

```sql
\c tandoor
DROP SCHEMA tandoor CASCADE;   -- 95 tables, last written 2026-01-30
```

This also removes the search-path hazard from step A permanently.

### Verify

```bash
kubectl --context admin@equestria create job -n database \
  --from=cronjob/postgres-backup postgres-backup-verify
```

Expect exit 0 and `Successfully created backup: /backups/tandoor.sql.gz`.

---

## 4. Why nobody noticed: the job could not fail

`kubernetes/apps/database/postgres/backups/resources/App.cs` wrapped each
per-database dump in `try`/`catch`, logged the exception, continued, then
unconditionally printed `PostgreSQL backup completed successfully` and fell off
the end of the program with exit 0. The CronJob reported `Complete`, so the
existing Job-failure alerting had nothing to fire on.

Fixed in this PR:

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

Every database using the `postgres` component gets an `<app>` schema. Five have
one owned by `postgres` rather than the app role:

| database | schema tables | app role has USAGE | dump status |
|---|---|---|---|
| **tandoor** | 95 | no | **failing** |
| immich | 18 | yes — **only because `immich` is a superuser** | passing, latently fragile |
| n8n | 0 | no | passing (nothing to lock) |
| retrom | 0 | no | passing (nothing to lock) |
| vikunja | 0 | no | passing (nothing to lock) |

Two things worth acting on separately from this PR:

- **`immich` is live data, not a stale copy** — `geodata_places` alone holds
  227,901 rows and was read on 2026-08-01. Recent Immich versions genuinely use
  a dedicated schema. It dumps today purely because `rolsuper = t` on the
  `immich` role bypasses the ACL check. Revoking that superuser bit — reasonable
  hardening, and something a CNPG major-version upgrade may prompt — would break
  Immich's backup exactly the way Tandoor's is broken now. Apply the same
  `ALTER SCHEMA` / `GRANT` before touching that role.
- **`n8n`, `retrom`, `vikunja`** are one `CREATE TABLE` away from the same
  failure. Applying step A's grants pre-emptively costs nothing.

After this PR, none of these can fail silently again — but they will fail the
whole nightly job, so fixing them is not optional once the exit code is honest.

---

## 6. Scope note

`david-driscoll/stargate-command-cluster` carries a byte-identical `App.cs` and
has the same exit-code bug. Its three databases all dump cleanly today, so only
the code fix applies there; it ships as a separate PR.
