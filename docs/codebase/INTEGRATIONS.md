# External Integrations

> Last updated: 2026-05-24

## Core Sections (Required)

### 1) Integration Inventory

| System | Type | Purpose | Auth model | Criticality | Evidence |
|--------|------|---------|------------|-------------|----------|
| GitHub (this repo) | Git source | Flux GitOps source of truth | SSH deploy key (`github-deploy.key`) | High | `kubernetes/flux/meta/`, `github-deploy.key` |
| 1Password Connect | Secret manager API | All app secrets pulled at runtime | Bearer token (`op://Eris/Eris 1Password Connect Access Token/credential`) | High | `kubernetes/apps/kube-system/external-secrets/`, `.mise.toml` |
| Cloudflare | DNS + Tunnel | External DNS records + secure ingress tunnel (no open ports) | API token (ExternalSecret) | High | `kubernetes/apps/network/cloudflare-tunnel/`, `kubernetes/apps/network/external-dns/` |
| Backblaze B2 | Object storage | PostgreSQL WAL backups + Volsync restic offsite backup | S3-compatible credentials (ExternalSecret) | High | `kubernetes/apps/database/postgres/app/resources/values.yaml` |
| Tailscale | VPN mesh / overlay network | Service exposure on tailnet for both k8s services and external nodes (Proxmox, dockge); IDP integration | OAuth client (`.mise.toml` `TAILSCALE_CLIENT_ID/SECRET`) | High | `kubernetes/apps/tailscale-system/` |
| Authentik | SSO / IdP | Forward auth for protected apps, OAuth2/OIDC provider | Internal (Authentik API via Pulumi) | High | `kubernetes/apps/equestria/idp/authentik/` |
| TrueNAS | NFS file server | Media/shared persistent storage via NFS provisioner | Network access (no auth) | Medium | `kubernetes/apps/nfs-system/` |
| Pulumi Cloud | State backend | Pulumi stack state storage | [TODO] access token | Medium | `kubernetes/apps/pulumi/` |
| GitHub Actions | CI | Flux local test/diff on PRs; ARC self-hosted runners on cluster | GitHub App (ARC controller) | Medium | `.github/workflows/`, `kubernetes/apps/github-actions/` |
| Longhorn | Block storage | PVC storage for stateful apps (primary) | In-cluster (no external) | High | `kubernetes/apps/longhorn-system/` |
| OpenEBS (hostpath) | Block storage | Supplemental local PVC storage | In-cluster (no external) | Medium | `kubernetes/apps/openebs-system/` |

### 2) Data Stores

| Store | Role | Access layer | Key risk | Evidence |
|-------|------|--------------|----------|----------|
| CloudNative-PG (PostgreSQL 17.5) | Primary relational DB for most stateful apps | ExternalSecret → app pod env vars | Single cluster; CNPG handles HA and WAL backups to Backblaze | `kubernetes/apps/database/postgres/app/resources/values.yaml` |
| Valkey (Redis-compatible) | Cache/queue for apps requiring it (n8n, etc.) | Direct pod connection | In-cluster only | `kubernetes/apps/database/` |
| Neo4j | Graph database (knowledge graph use case) | Direct pod connection | Single instance, not HA | `kubernetes/apps/database/neo4j/helmrelease.yaml` |
| Longhorn | Distributed block storage (PVCs) | Kubernetes StorageClass `longhorn-snapshot` | Requires 3+ nodes for replication | `kubernetes/apps/longhorn-system/` |
| Backblaze B2 | Offsite backup storage | Via Volsync (restic) + CNPG WAL shipping | External dependency; outage = no new backups | `kubernetes/components/volsync/replicationsource.yaml` |
| TrueNAS (NFS) | Shared file storage (media, etc.) | `truenas-media` PVC (NFS provisioner) | Single NAS; no HA | `kubernetes/apps/nfs-system/` |

### 3) Secrets and Credentials Handling

**Three-layer secret model:**

1. **SOPS-encrypted static secrets** (`*.sops.yaml`): cluster infrastructure credentials (SOPS age key, GitHub deploy key, Talos secrets). Encrypted in Git using `age` keys. `sops` CLI + `SOPS_AGE_KEY_FILE` required to decrypt.

2. **ExternalSecret → 1Password Connect**: application secrets (database passwords, API keys, encryption keys). Each app's `externalsecret.yaml` defines which 1Password items to pull. The `ClusterSecretStore` named `onepassword-connect` handles auth. Secrets are refreshed every 4 minutes.

3. **postBuild variable substitution**: non-secret configuration values (`ROOT_DOMAIN`, `TIMEZONE`, `CLUSTER_DOMAIN`) injected from `cluster-secrets` and `shared-secrets` into all Flux Kustomizations at reconcile time.

**Hardcoding check**: no hardcoded credentials found in YAML manifests. All sensitive values use `${VAR}` substitution or `secretRef`. The `ignorePaths: ["**/*.sops.*"]` in Renovate prevents accidental secret updates.

**Key rotation**: age keys are in `.sops.yaml`; rotating them requires decrypting and re-encrypting all `*.sops.yaml` files. 1Password Connect token rotation requires updating the `onepassword-connect` ClusterSecretStore.

### 4) Reliability and Failure Behavior

- **Flux retry**: `retryInterval: 2m` on all Kustomizations; HelmRelease `remediation.retries: 7`
- **HelmRelease rollback**: `upgrade.remediation.strategy: rollback` with `rollback.force: true`
- **ExternalSecret refresh**: `refreshPolicy: Periodic, refreshInterval: 4m` — existing Secrets survive Connect outages until manually deleted
- **Circuit-breaker**: none at cluster level; Kubernetes readiness/liveness probes gate traffic
- **Timeout policy**: Kustomization `timeout: 10m`; HelmRelease `timeout: 10m`
- **Drift detection**: `driftDetection.mode: enabled` on HelmReleases — detects and re-reconciles out-of-band changes

### 5) Observability for Integrations

- **Prometheus scraping**: kube-prometheus-stack scrapes all annotated services; Longhorn, CNPG, Traefik, Cilium all export metrics
- **Grafana dashboards**: pre-built dashboards for Flux (`kubernetes/apps/observability/grafana/dashboards/flux.yaml`), Longhorn, CNPG
- **Alertmanager**: alert routing configured for cluster health events
- **Gatus**: uptime monitoring configured via `ApplicationDefinition` CRDs (Pulumi reads them to configure Gatus checks)
- **Loki + Alloy**: log aggregation from all pods
- **Alertmanager API** (troubleshooting): `https://alertmanager.driscoll.tech/api/v2/alerts` — no auth, Tailscale-only, covers firing alerts from both the Kubernetes cluster and the dockge (Docker Compose) stack. Query with `curl -s https://alertmanager.driscoll.tech/api/v2/alerts | jq .` when investigating incidents.
- **Missing visibility**: individual ExternalSecret refresh latency is not directly surfaced; 1Password Connect health is not explicitly monitored (would need custom probe)

### 6) Tailscale Node Map

External nodes (Proxmox hypervisors and dockge Docker Compose hosts) are bridged into the cluster via the Tailscale Operator's `ExternalName` services, generated by `kubernetes/apps/tailscale-system/services/Update.cs`. All nodes are reachable only over Tailscale — not the public internet.

| Server | Services on tailnet | Generated file |
|--------|---------------------|----------------|
| `celestia` | Dockge, Proxmox VE, PBS | `services/celestia.yaml` |
| `luna` | Dockge, Proxmox VE, PBS | `services/luna.yaml` |
| `skystar` | Dockge, Proxmox VE | `services/skystar.yaml` |
| `as` | Dockge | `services/as.yaml` |
| `twilight-sparkle` | Proxmox VE | `services/twilight-sparkle.yaml` |
| `alpha-site` | Proxmox VE | `services/alpha-site.yaml` |

Dockge instances follow the pattern `dockge-<server>.${TAILSCALE_DOMAIN}` (e.g. `dockge-celestia.<tailnet>`).

### Common Secret Operations

**Edit a cluster-wide variable:**
```bash
sops kubernetes/components/common/cluster-secrets.sops.yaml
# Add/change value → save (auto-re-encrypts)
```

**Rotate a database password:**
```bash
sops kubernetes/apps/database/postgres/app/passwords.sops.yaml
# Change password value → save
task update    # re-generates any derived resources
git add kubernetes/ && git commit -m "Rotate <app> password"
# Flux updates Secret → Reloader restarts pod
```

**Add a new 1Password vault entry for an app:**
1. Log into 1Password, go to vault `Eris`
2. Create item named `<app>` (or `<app>-<type>`)
3. Reference from ExternalSecret: `key: '<APP>'` under `dataFrom.extract`

**Debug ExternalSecret not syncing:**
```bash
kubectl get externalsecret -n <ns> -o wide
kubectl describe externalsecret <app>-env -n <ns>
# Check 1Password Connect pod
kubectl get pod -n kube-system -l app=onepassword-connect
# Check ClusterSecretStore
kubectl get clustersecretstore onepassword-connect -o wide
```

**Regenerate all database passwords (nuclear option):**
```bash
rm kubernetes/apps/database/postgres/app/passwords.sops.yaml
task update   # generates fresh UUIDs for all users
git add kubernetes/ && git commit -m "Regenerate all database passwords"
```

**Get a secret value for debugging:**
```bash
# Cluster variable
kubectl get secret cluster-secrets -n flux-system \
  -o jsonpath='{.data.ROOT_DOMAIN}' | base64 -d

# App database URL
kubectl get secret <app>-env -n equestria \
  -o jsonpath='{.data.DATABASE_URL}' | base64 -d

# Decrypt SOPS file locally
sops -d kubernetes/apps/database/postgres/app/passwords.sops.yaml
```

### 7) Evidence

- `kubernetes/apps/kube-system/external-secrets/` (External Secrets Operator)
- `kubernetes/apps/equestria/home/n8n/externalsecret.yaml`
- `kubernetes/apps/database/postgres/app/resources/values.yaml`
- `kubernetes/components/volsync/replicationsource.yaml`
- `kubernetes/apps/network/cloudflare-tunnel/`
- `kubernetes/apps/network/external-dns/`
- `.mise.toml` (CONNECT_HOST, CONNECT_TOKEN, TAILSCALE_CLIENT_*)
- `kubernetes/apps/observability/prometheus/helmrelease.yaml`
