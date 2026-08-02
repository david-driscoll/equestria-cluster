# CrowdSec enforcement rollout — equestria

Companion to [david-driscoll/vault#111](https://github.com/david-driscoll/vault/issues/111).
The PR that added this file ships CrowdSec **wired up and switched off**. This
document is the ordered procedure for switching it on, and the runbook for when
it misbehaves.

Target state is enforcement **everywhere**, per David's answer to Q2. The ladder
below reaches that destination in tranches that each fail small.

---

## 0. Break glass — read this first

`kubectl` does not traverse Traefik. The API server is reached directly over the
LAN or Tailscale with a kubeconfig. So even in the worst case — you banned
yourself, `authentik` is behind the bouncer, every web app returns 403 — this
still works:

```bash
# unban one address
kubectl exec -n network deploy/crowdsec-lapi -- cscli decisions delete --ip <your-ip>

# nuclear: drop every decision
kubectl exec -n network deploy/crowdsec-lapi -- cscli decisions delete --all
```

That is the *only* reason enforcing on `authentik` is survivable. **Rehearse it
before stage 6, not during the incident.**

The declarative kill switch is one line in
`kubernetes/apps/network/traefik/middleware/crowdsec.yaml`:

```yaml
spec:
  plugin:
    crowdsec-bouncer-traefik-plugin:
      enabled: false   # <- pass every request through
```

This works regardless of how many routes reference the middleware, which
matters: middlewares attach per-`HTTPRoute` via `ExtensionRef` filters and this
repo has 74 `HTTPRoute` definitions across 69 files. "Detach the middleware from
the routes" is not a rollback at that scale. This is.

`clientTrustedIPs` in the same file is the second lever: listed CIDRs bypass all
plugin checks without the LAPI needing to be healthy. LAN and the Tailscale
CGNAT range are already listed.

---

## Prerequisites (must be true before stage 1)

| # | Prerequisite | How to check |
|---|---|---|
| P1 | 1Password item `Crowdsec ApiKey` (vault `Eris`) has field `apikey_bouncer` | `kubectl get secret crowdsec-secret -n network -o json \| jq '.data \| keys'` — done, present |
| P2 | ~~The same item has field `credential` holding a **currently valid** console enrollment key~~ **DROPPED 2026-08-02** — console enrolment is off (Option B) after the stale `credential` key crash-looped the LAPI. Not a prerequisite for any stage | n/a. See §Enrollment and §Re-enabling below if you ever want the console back |
| P3 | A **separate** item `Crowdsec UI` (vault `Eris`) has field `password` (a generated password, ≥ 24 chars) | needed by `crowdsec-ui`; provisioned 2026-08-01. Deliberately *not* a field on `Crowdsec ApiKey` — `crowdsec-ui`'s ExternalSecret extracts this item itself and prefixes its keys to `webui_*`, so the template key stays `webui_password` |
| P4 | Authentik OIDC app for `crowdsec-ui` provisioned, `crowdsec-ui-oidc-credentials` present in the `cluster` store | runs automatically once `definition.yaml` lands and the Pulumi authentik stack runs |

Checking a secret's shape without reading its value:

```bash
kubectl get secret crowdsec-secret -n network -o json \
  | jq -r '.data | to_entries | map("\(.key): \((.value|@base64d)|length) chars") | .[]'
```

A console enrollment key is 40 characters; a bouncer key is 32. Right shape is a
useful negative test only — it proves nothing about validity.

### Enrollment (`ENROLL_KEY`) is a crash-loop risk, not a log line

**This fired on 2026-08-02. Console enrolment is now OFF (Option B).** P2 above
is therefore not a prerequisite for anything — leave it unchecked.

The entrypoint is the *chart's* `files/docker-start-custom.sh`, not the image's:
the chart embeds it via `.Files.Get` into `crowdsec-docker-start-script-configmap`
and mounts it at `/docker_start.sh`. It runs under `set -e` (line 9) and calls,
at line 277 of chart 0.24.0:

```bash
cscli console enroll $enroll_args "$ENROLL_KEY"
```

unguarded. `cscli console enroll` returns non-zero on a rejected or expired key,
so **a dead key crash-loops the LAPI**. An *already enrolled* instance returns
success, so restarts after a good first enrollment are safe.

Two details that make it worse than "no console":

* **Bouncer registration is downstream.** Enrolment is line 277; the
  `BOUNCER_KEY_*` loop is line 310. A dead enrolment key means the `traefik`
  bouncer is never registered either, so Stage 2's proof below cannot pass.
* **Flux uninstalls the release.** The HelmRelease has `timeout: 10m`; when the
  LAPI never goes Ready the install fails and install remediation *uninstalls*
  CrowdSec entirely, then retries. The symptom cycles rather than sitting still.

There is no chart value for this and no fixed chart to upgrade to (0.24.0 is the
newest published, and the call is still unguarded on the chart's main branch).
`helmrelease.yaml` therefore carries a `postRenderers` patch that rewrites that
one line to append `|| echo CROWDSEC_CONSOLE_ENROLL_FAILED_NONFATAL`. Grep the
LAPI log for that string to see it catch. It fails open: if a chart bump changes
the line, the `sed` no-ops and behaviour returns to the unguarded default.

**Enrollment is optional garnish and must never gate the security engine.**

### Re-enabling console enrolment

Only worth doing for the hosted console UI and CAPI signal-sharing; `crowdsec-ui`
already replaces the dashboard, and the community blocklist works without any of
this.

1. Sign in to `app.crowdsec.net` → **Security Engines → Enroll**, and copy the
   enrollment key.
2. Put it on the 1Password item **`Crowdsec ApiKey`** (vault `Eris`), field
   **`credential`**. Nothing else needs rewiring — `externalsecret.yaml` still
   copies that field into the `crowdsec-secrets` Secret verbatim, and was left
   untouched when enrolment was switched off.
3. Paste this block back into `lapi.env` in
   `kubernetes/apps/network/crowdsec/values.yaml`, where the
   `---- console enrolment: OFF ----` comment is:

   ```yaml
   - name: ENROLL_KEY
     valueFrom:
       secretKeyRef:
         name: ${APP}-secrets
         key: credential
   - name: ENROLL_INSTANCE_NAME
     value: '${CLUSTER_CNAME}'
   - name: ENROLL_TAGS
     value: 'k8s ${CLUSTER_CNAME}'
   ```

The block lives here rather than commented out in `values.yaml` because a
dollar-brace sequence in a *comment* in that file is not inert — Flux `postBuild`
envsubst expands comments too, which is the trap that broke #2977 and #2980, and
`scripts/eso-values-lint` fails CI for it. In this Markdown file it is inert.

Verify after re-enabling: `cscli console status` in the LAPI pod, and confirm the
log has no `CROWDSEC_CONSOLE_ENROLL_FAILED_NONFATAL` line (which would mean the
new key was rejected too — the guard caught it and the LAPI stayed up).

---

## The ladder

Every stage's rollback is `enabled: false`.

### Stage 1 — detection

Already wired by this PR. On merge, Flux creates the `crowdsec` Kustomization,
the LAPI, the agent DaemonSet and the `crowdsec-bouncer` Middleware. Nothing
enforces: the plugin is disabled and only `whoami` references the middleware.

**Gate to advance:**

- [ ] `flux get kustomization -n network crowdsec` Ready
- [ ] LAPI **Ready regardless of enrollment outcome** — if it is crash-looping
      on `ENROLL_KEY`, drop the `ENROLL_*` block and re-check
- [ ] `kubectl exec -n network deploy/crowdsec-lapi -- cscli collections list`
      shows traefik / base-http-scenarios / http-cve installed
- [ ] the `crowdsec` database now has application tables (it held only system
      catalogs before — this proves the DB wiring)
- [ ] both ServiceMonitors scraping in Prometheus
- [ ] `whoami.${CLUSTER_DOMAIN}` still serves normally — this proves the plugin
      loaded and read `crowdsecLapiKeyFile`, because the plugin validates its
      config even when disabled

### Stage 2 — read, and prove the bouncer key actually works

Stand up `crowdsec-ui` (also in this PR) and watch for at least 72h.

**The load-bearing check — a Secret existing is not proof the plugin reads it:**

```bash
# 1. the file is where the plugin looks
kubectl exec -n network deploy/traefik -- \
  sh -c 'wc -c < /etc/secrets/crowdsec/apikey_bouncer'      # expect 32

# 2. THE PROOF: the traefik bouncer authenticated against the LAPI.
#    "Last API pull" only advances when a request carrying that exact key
#    reaches the LAPI. A row with a never/stale pull means the key does not
#    match, even though everything else looks healthy.
kubectl exec -n network deploy/crowdsec-lapi -- cscli bouncers list
```

> **Why this is a hard gate.** `updateMaxFailure: -1` (deliberate, see the
> middleware file) means LAPI failures never block traffic. A *wrong* bouncer
> key therefore looks exactly like "no decisions to apply": requests pass,
> nothing errors, and the deployment reads as protecting you while it blocks
> nothing. A *missing* key fails loudly (the plugin refuses to build); a wrong
> one does not. `cscli bouncers list` is the only positive signal.

**Client-IP gate — do not skip:**

```bash
kubectl exec -n network deploy/crowdsec-lapi -- cscli alerts list
kubectl exec -n network deploy/crowdsec-lapi -- cscli decisions list
```

- [ ] parsed source IPs are **real public addresses**, not a single repeated pod
      or `cloudflared` IP
- [ ] no `10.x` / `172.16.x` / `192.168.x` address appears in
      `cscli decisions list`

If every alert shows the same internal source IP, the forwarded-header chain is
wrong. Under enforcement-everywhere, banning that address bans the entire
internet at once. Fix the chain before going further. The `s01-whitelist`
postoverflow makes this survivable rather than instant, but it is a safety net,
not a substitute for the check.

### Stage 3 — enforce on `whoami` only

Flip `enabled: false` → `true`. `whoami` is already the only route attached.

- [ ] `cscli decisions add --ip <a test address>` → 403 on
      `whoami.${CLUSTER_DOMAIN}`
- [ ] **provably nothing else affected** — every other Gatus endpoint green
- [ ] `cscli decisions delete --ip <test address>` → 200 again

### Stage 4 — external tranches

Widen across externally-reachable routes by adding the `ExtensionRef` filter,
**one namespace per commit**. Not one commit for 69 files.

Each tranche soaks ≥24h with Gatus green and no step change in the Traefik
`403` rate on `websecure` before the next.

> An entrypoint-level middleware
> (`--entryPoints.websecure.http.middlewares=...`) would attach the bouncer to
> every router in one line. **Unverified** whether Traefik v3.7 applies
> entrypoint middlewares to Gateway-API-derived routers, and `websecure` is
> shared by the external Gateway *and* the internal Traefik service, so it would
> collapse stages 4 and 5 into one commit. Right end state, wrong migration
> path. Consolidate afterwards if desired.

### Stage 5 — internal / Tailscale routes

Lower value — most of this traffic is `clientTrustedIPs`-exempt already — but it
completes "everywhere". Same soak rule.

### Stage 6 — `authentik`, last and alone

Its own commit, its own day, **after** rehearsing §0 break-glass end to end: ban
a throwaway address, confirm the 403, clear it via `kubectl exec … cscli
decisions delete`, without touching a browser.

---

## How a false positive shows up

The likeliest failure is not "banned a real attacker who turned out to be a
friend". It is the trusted-IP chain being wrong.

| Shape | Symptom | Consequence |
|---|---|---|
| XFF not trusted / not parsed | every alert shows the *same* source IP (a `cloudflared` or Traefik pod IP) | banning it bans the entire internet at once |
| XFF trusted too broadly | source IPs look plausible but are attacker-controlled | an attacker gets a third party banned, or evades bans; silent |

Detection, fastest first:

1. **Gatus** — a bouncer false positive shows as a synchronous cluster of
   endpoints going red. No new work; just read it that way.
2. **Traefik access-log 403 rate** — a step change on `websecure` with no
   corresponding traffic change. Worth a `PrometheusRule` before stage 4.
3. **`cscli decisions list`** — an internal address appearing in that list is
   baseline-free and should be an alert in its own right.
4. **The UI** — best for reading *why*, worst for noticing. Not a detector.

---

## Rotation gotchas

Both consumers register on first sight and are **not** updated by changing the
1Password value alone:

```bash
# bouncer key (register_bouncer only registers a name that is not already present)
kubectl exec -n network deploy/crowdsec-lapi -- cscli bouncers delete traefik
# then restart the LAPI so BOUNCER_KEY_traefik re-registers

# crowdsec-ui watcher (re-registration of an existing machine is refused)
kubectl exec -n network deploy/crowdsec-lapi -- cscli machines delete crowdsec-web-ui
# then restart crowdsec-ui so its init container re-registers
```

---

## Deliberately out of scope

- **UniFi syslog integration** (`crowdsecurity/unifi`). Separate PR. Notes for
  whoever picks it up: `sdks/unifi/setting.ts` in `home-operations` models
  `syslog` with `contents[]`, but `Setting` is monolithic, so the `Network`
  clobber precedent applies — snapshot `GET /proxy/network/api/s/default/rest/setting`
  before the first `pulumi up` and diff after. The syslog listener is
  unauthenticated UDP and trivially spoofable, so under enforcement-everywhere
  a `CiliumNetworkPolicy` restricting it to the gateway's IP is mandatory,
  not optional. Sequence it after stage 3.
- **Renovate annotations** on the Traefik plugin pins. Separate PR.
  Dependabot cannot do this — it has no generic/regex manager and no Helm-values
  manager. Renovate already has a `customManagers` regex over `**/*.yaml` in
  `.github/renovate.json5` that matches `# renovate:` + `key: "value"`, so all
  three plugins need is an annotation comment. Do it **after** the `-beta1`
  pin is gone (done in this PR) — Renovate treats the current value's stability
  as its floor.
- **Enforcement at the router.** Blocking on UniFi OS needs a remediation
  component that is not a supported CrowdSec target, and in practice means an
  on-boot-script persistence hack. A failed boot script on the gateway is an
  internet outage for the house, not a 403. Its own issue if wanted.
- **`stargate-command-cluster`.** Has the byte-identical orphaned app directory.
  Enabling it there would recreate the `crowdsec` database name collision that
  vault#84 Q5 exists to remove. equestria is the live one; SGC's duplicate
  database is the one to drop.
