# Codebase Concerns

> Last updated: 2026-05-24

## Core Sections (Required)

### 1) Top Risks (Prioritized)

| Severity | Concern | Evidence | Impact | Suggested action |
|----------|---------|----------|--------|------------------|
| High | Enormous etcd snapshots in repo | `talos/etcd-pre-upgrade-*.db` (439MB + 439MB + 340MB = ~1.2GB total) | Git clone is huge; CI is slow; disk waste | Move DB snapshots out of repo to external storage (Backblaze/NFS); add to `.gitignore` |
| High | `age.key` / `eq.age.key` in repo root | `.gitignore` excludes them, but their presence on disk means any process with repo access can decrypt all secrets | If leaked, all cluster secrets exposed | Verify `.gitignore` is enforced; consider storing age keys only in a password manager |
| High | Single SOPS age key set for all secrets | `.sops.yaml` — same 3 recipients for everything | Compromise of any recipient key = full secret exposure | [ASK USER] Consider separate keys per sensitivity tier |
| Medium | Generated files can drift from source | `Update.cs` generates files committed to Git; devs may hand-edit | `task update` overwrites manual edits silently | CI could diff generated output to detect drift before merge |
| Medium | No automated post-deploy health verification | CI only validates rendering, not live cluster | Broken apps can be deployed without automated detection | Gatus health checks partially cover this; no CI gate |
| Medium | 1Password Connect is a single point of failure | All ExternalSecrets pull from one Connect instance | Connect outage = no new secrets; existing pods survive but pod restarts will fail | [ASK USER] Is there a Connect HA setup? Document recovery procedure |
| Low | `xcproxy/resources/tmdb_cache.db` (19MB) in Git | `kubernetes/apps/equestria/pvr/xcproxy/resources/tmdb_cache.db` | Bloats repo; binary cache not appropriate for Git | Move to persistent volume; remove from Git |
| Low | `kubernetes/apps/equestria/home/vikunja/resources/Tasks.cs` (220KB) | Largest C# script in repo | Large auto-generated file committed to Git | Confirm whether this is generated or hand-authored; if generated, consider generating on-the-fly |

### 2) Technical Debt

| Debt item | Why it exists | Where | Risk if ignored | Suggested fix |
|-----------|---------------|-------|-----------------|---------------|
| Etcd DB snapshots in repo | Left after Talos upgrade operations | `talos/etcd-pre-upgrade-*.db` | Repo grows unboundedly with each upgrade | Archive to external storage; add post-upgrade cleanup to task |
| Binary/large cache files in repo | Convenience (xcproxy TMDB cache) | `kubernetes/apps/equestria/pvr/xcproxy/resources/tmdb_cache.db` | Slow clone/checkout; poor Git practice | Use PVC or ConfigMap with external URL |
| `.mise.toml` high churn (75 commits/90d) | Frequent tool version updates (Renovate + manual) | `.mise.toml` | Not debt per se, but signals frequent friction in tool versioning | Accept as normal for homelab; Renovate handles most of it |
| `postgres/app/passwords.sops.yaml` high churn (19 commits/90d) | Password rotation or new app additions | `kubernetes/apps/database/postgres/app/passwords.sops.yaml` | [ASK USER] Is rotation manual or automated? | If manual, consider automating via ExternalSecret |

### 3) Security Concerns

| Risk | OWASP category | Evidence | Current mitigation | Gap |
|------|----------------|----------|--------------------|-----|
| Secrets management | A02: Cryptographic Failures | `.sops.yaml`, `*.sops.yaml` | SOPS + age encryption; ExternalSecret pulls from 1Password | Age key files on disk; if workstation compromised, all secrets exposed |
| Supply chain (container images) | A06: Vulnerable Components | All `helmrelease.yaml` files | Renovate updates images; image digests pinned (`@sha256:...`) in many places | Not all images are digest-pinned; `flux-local` does not do CVE scanning |
| Cluster API exposure | A01: Broken Access Control | `talos/talconfig.yaml` | API server behind VIP `10.10.206.201`; cert SANs include internal domains | `apiserver.equestria.driscoll.tech` DNS entry exists — verify firewall rules |
| GitHub deploy key | A07: Auth Failures | `github-deploy.key` | Read-only SSH deploy key for Flux | Key rotation cadence [ASK USER] |
| Pod security | A01: Broken Access Control | `helmrelease.yaml` files | `securityContext` set on most pods; `runAsNonRoot`, `readOnlyRootFilesystem`, `drop ALL` caps are common | Some pods (e.g., n8n) run as root for permission fixes; `runAsNonRoot: false` seen |
| Network policies | A01: Broken Access Control | CrowdSec, Cilium | Cilium enforces network policies; CrowdSec provides IDS | [ASK USER] Are default-deny NetworkPolicies in place? |
| Cloudflare tunnel | External access | `kubernetes/apps/network/cloudflare-tunnel/` | No open inbound ports; traffic via Cloudflare | Cloudflare as trust boundary; any Cloudflare outage = external services down |

### 4) Performance and Scaling Concerns

| Concern | Evidence | Current symptom | Scaling risk | Suggested improvement |
|---------|----------|-----------------|-------------|-----------------------|
| Longhorn storage replication | `kubernetes/apps/longhorn-system/` | Unknown | Longhorn requires 3+ nodes for 3-replica volumes; cluster currently has limited nodes per `talconfig.yaml` | Verify replica count vs node count; check Longhorn dashboard |
| Flux reconciliation interval | All `ks.yaml` — `interval: 1h` | 1h max delay for auto-sync | Not a performance issue; intentional | Webhook push (already configured via `flux-system`) gives instant sync on push |
| CNPG single-node PostgreSQL | `kubernetes/apps/database/postgres/app/resources/values.yaml` — `mode: standalone` | No HA for postgres | Primary DB for all apps; postgres node failure = all DB-backed apps down | `mode: standalone` for homelab; acceptable risk |

### 5) Fragile/High-Churn Areas

| Area | Why fragile | Churn signal | Safe change strategy |
|------|-------------|-------------|----------------------|
| `.mise.toml` | All tool versions here; Renovate bumps frequently | 75 commits/90d | Always run `mise install` after pull; check `task update` still passes |
| `kubernetes/apps/equestria/games/questarr/helmrelease.yaml` | Highest-churn app manifest (55 commits) | 55 commits/90d | Likely actively developed; always run `flux-local test` after changes |
| `kubernetes/apps/equestria/media/tdarr/helmrelease.yaml` | Media processing app; frequent updates | 41 commits/90d | Test rendering; verify resource limits after changes |
| `kubernetes/apps/observability/grafana/helmrelease.yaml` | Dashboard changes + chart updates | 40 commits/90d | Grafana dashboard changes are additive; schema-validate |
| `kubernetes/apps/equestria/home/vikunja/helmrelease.yaml` | Task management app with large Tasks.cs | 35 commits/90d | Confirm whether Tasks.cs is generated; check before editing |
| `kubernetes/apps/database/postgres/app/passwords.sops.yaml` | SOPS-encrypted; every app addition/rotation touches it | 19 commits/90d | Always decrypt with `sops`, edit, re-encrypt; never edit encrypted bytes |
| `bootstrap/helmfile.yaml` | Core platform; version bumps here affect entire cluster bootstrap | 18 commits/90d | Only touch during planned upgrades; test in staging first [ASK USER: is there a staging cluster?] |

### 6) `[ASK USER]` Questions

1. **[ASK USER]** The etcd snapshot files (`talos/etcd-pre-upgrade-*.db`) total ~1.2GB in the Git repo. Are these kept intentionally for disaster recovery, or can they be removed and stored externally?

2. **[ASK USER]** `kubernetes/apps/database/postgres/app/passwords.sops.yaml` has high churn (19 commits/90d). Is password rotation manual or automated? If manual, what's the rotation policy?

3. **[ASK USER]** Is there a staging/test cluster separate from the production `equestria` cluster, or is everything tested on production with `flux-local` as the only gate?

4. **[ASK USER]** The SOPS age key setup uses 3 recipients for all secrets. Are these 3 individual developer keys, or shared infrastructure keys? What is the key rotation procedure?

5. **[ASK USER]** 1Password Connect is used as the sole secret backend. Is there a secondary Connect instance for HA, or a documented recovery procedure if Connect goes down?

6. **[ASK USER]** `kubernetes/apps/equestria/home/vikunja/resources/Tasks.cs` is 220KB — unusually large for a C# script. Is this hand-authored or generated? If generated, from what source?

7. **[ASK USER]** GitHub deploy key (`github-deploy.key`) is in the repo root. What is the rotation cadence for this key?

8. **[ASK USER]** Are there default-deny Cilium NetworkPolicies in place, or is all pod-to-pod traffic allowed by default within the cluster?

### 7) Evidence

- `.codebase-scan.txt` — HIGH-CHURN FILES section
- `talos/` — etcd snapshot files
- `kubernetes/apps/equestria/pvr/xcproxy/resources/tmdb_cache.db`
- `kubernetes/apps/equestria/home/vikunja/resources/Tasks.cs`
- `.sops.yaml`
- `kubernetes/apps/database/postgres/app/passwords.sops.yaml`
- `kubernetes/apps/equestria/home/n8n/helmrelease.yaml` (securityContext examples)
- `bootstrap/helmfile.yaml`
