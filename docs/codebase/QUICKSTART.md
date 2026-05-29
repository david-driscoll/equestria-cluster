# Quick Reference Guide

> Last updated: 2026-05-24

## Common Commands

```bash
# Validate Kubernetes manifests before pushing (REQUIRED)
flux-local test --enable-helm --all-namespaces --path ./kubernetes/flux/cluster -v

# Regenerate derived resources (postgres users, tailscale services, etc.)
task update

# Force Flux to resync from git
task reconcile

# Edit encrypted secrets (auto-decrypt and re-encrypt)
sops kubernetes/components/common/cluster-secrets.sops.yaml

# Check app status
kubectl get -n equestria deployment <app> -o wide
kubectl logs -n equestria deployment/<app> --tail=50

# Get all env vars from a Secret
kubectl get secret <app>-env -n equestria -o jsonpath='{.data}' \
  | jq -r 'to_entries | .[] | "\(.key): \(.value | @base64d)"'

# Check firing alerts (Tailscale required)
curl -s https://alertmanager.driscoll.tech/api/v2/alerts | jq .
```

## File Locations

| What | Where |
|------|-------|
| All apps | `kubernetes/apps/<NAMESPACE>/<APP>/` |
| Shared components | `kubernetes/components/` |
| Flux root entry | `kubernetes/flux/cluster/ks.yaml` |
| Cluster-wide secrets | `kubernetes/components/common/cluster-secrets.sops.yaml` |
| Shared secrets | `kubernetes/components/common/shared-secrets.sops.yaml` |
| DB user passwords | `kubernetes/apps/database/postgres/app/passwords.sops.yaml` |
| DB user roles | `kubernetes/apps/database/postgres/app/users.yaml` |
| Tailscale services | `kubernetes/apps/tailscale-system/services/` |
| Version pins | `versions.env` |
| Dev tool config | `.mise.toml` |

## Adding a New App

```bash
# 1. Create app directory and files
mkdir -p kubernetes/apps/<namespace>/<app>
# Create: ks.yaml, kustomization.yaml, helmrelease.yaml, externalsecret.yaml

# 2. Regenerate derived resources
task update

# 3. Validate
flux-local test --enable-helm --all-namespaces --path ./kubernetes/flux/cluster -v

# 4. Commit everything (including generated files)
git add kubernetes/
git commit -m "feat(<app>): add <app> to <namespace>"
git push
```

See [`COMPONENTS.md`](./COMPONENTS.md) for which components to add to `ks.yaml`.

## App Structure Checklist

```
kubernetes/apps/<NAMESPACE>/<APP>/
├── ks.yaml              ✓ Flux Kustomization (dependsOn, components, postBuild vars)
├── kustomization.yaml   ✓ Kustomize manifest (lists resources)
├── helmrelease.yaml     ✓ HelmRelease (app-template chart)
├── externalsecret.yaml  ✓ ExternalSecret (pulls from 1Password)
└── resources/           ✓ Config files bundled as ConfigMaps (if needed)
```

**Optional files:**
- `definition.yaml` — ApplicationDefinition CRD (if app needs Authentik SSO + Gatus monitoring)
- `*.sops.yaml` — Static SOPS-encrypted secrets
- `Update.cs` — Code generation script (rare; only postgres, tailscale, common have this)

## Component Quick Reference

```yaml
# Add postgres database support
components:
  - ../../../../components/postgres
postBuild:
  substitute:
    POSTGRES_NAME: <app>-postgres   # optional override

# Add Volsync backup
components:
  - ../../../../components/volsync
postBuild:
  substitute:
    VOLSYNC_ACCESSMODES: ReadWriteMany
    VOLSYNC_PUID: "1000"
    VOLSYNC_PGID: "1000"

# Add Tailscale exposure
components:
  - ../../../../components/tailscale
postBuild:
  substitute:
    TAILSCALE_HOST: <hostname>

# Ingress variants (choose one or combine)
components:
  - ../../../../components/ingress/internal        # internal network only
  - ../../../../components/ingress/authenticated   # SSO-protected (Authentik)
  - ../../../../components/ingress/external        # public internet (Cloudflare)
```

## Substitution Variables

Available in all apps via `cluster-secrets` and `shared-secrets`:

| Variable | Purpose |
|----------|---------|
| `${APP}` | App name (set in ks.yaml postBuild) |
| `${NAMESPACE}` | Target namespace |
| `${ROOT_DOMAIN}` | Primary domain (e.g. `driscoll.tech`) |
| `${INTERNAL_DOMAIN}` | Internal ingress domain |
| `${EXTERNAL_DOMAIN}` | Public internet domain |
| `${TAILSCALE_DOMAIN}` | Tailnet FQDN suffix |
| `${TIMEZONE}` | Cluster timezone |
| `${POSTGRES_NAME}` | DB role name (defaults to `${APP}`) |
| `${VOLSYNC_ACCESSMODES}` | PVC access mode for backup |
| `${TAILSCALE_HOST}` | Tailnet hostname |

## Namespace Selector

| App type | Namespace |
|----------|-----------|
| User-facing app (media, home, gaming) | `equestria` |
| Database or cache | `database` |
| Networking (DNS, reverse proxy) | `network` |
| Monitoring, alerts, logs | `observability` |
| Infrastructure operators | `kube-system` |
| GitOps control plane | `flux-system` |
| Storage | `longhorn-system`, `openebs-system`, `nfs-system` |
| Certificates | `cert-manager` |
| Infrastructure automation | `pulumi` |

## Troubleshooting

### Check alerts first

```bash
# See all currently firing alerts (Tailscale required)
curl -s https://alertmanager.driscoll.tech/api/v2/alerts | jq '[.[] | {alert: .labels.alertname, namespace: .labels.namespace, pod: .labels.pod, summary: .annotations.summary, state: .status.state}]'
```

### App won't start (CrashLoopBackOff)

```bash
kubectl logs -n <ns> deployment/<app> --previous
kubectl describe pod -n <ns> -l app.kubernetes.io/name=<app>

# Common causes:
# 1. Missing Secret — check ExternalSecret
kubectl get externalsecret -n <ns>
kubectl describe externalsecret <app>-env -n <ns>

# 2. Database not ready
kubectl get cluster -n database

# 3. Missing PVC
kubectl get pvc -n <ns>
```

### ExternalSecret stuck / secret not injecting

```bash
kubectl get externalsecret -n <ns> -o wide
kubectl describe externalsecret <app>-env -n <ns>

# Check 1Password Connect is running
kubectl get pod -n kube-system -l app=onepassword-connect

# Check ClusterSecretStore
kubectl get clustersecretstore onepassword-connect -o wide
```

### Flux not syncing

```bash
flux reconcile source git flux-system
flux reconcile kustomization cluster-apps
flux get kustomization --all-namespaces
flux get events --all-namespaces --follow
```

### Database user not created

```bash
task update
# verify postgres component is referenced in ks.yaml
grep -r "components/postgres" kubernetes/apps/
# check generated file
ls kubernetes/apps/database/postgres/app/users/
```

## Secrets Workflow

```bash
# Edit cluster-wide variable
sops kubernetes/components/common/cluster-secrets.sops.yaml

# Rotate database password
sops kubernetes/apps/database/postgres/app/passwords.sops.yaml
# change value → save → task update → commit

# Get current value of cluster variable
kubectl get secret cluster-secrets -n flux-system \
  -o jsonpath='{.data.ROOT_DOMAIN}' | base64 -d

# Debug app secret
kubectl get secret <app>-env -n <ns> \
  -o jsonpath='{.data.DATABASE_URL}' | base64 -d
```

## Git Workflow

```bash
# Before every commit touching kubernetes/
task update                   # regenerate derived files
flux-local test --enable-helm --all-namespaces --path ./kubernetes/flux/cluster -v
git diff kubernetes/          # review all changes
git add kubernetes/
git commit -m "..."
git push                      # Flux syncs within 1h (or use task reconcile)
```

## Health Checks

```bash
# Nodes
kubectl get node -o wide                          # STATUS: Ready

# Core namespaces
kubectl get pod -n kube-system
kubectl get pod -n flux-system
kubectl get pod -n network

# Flux reconciling
kubectl get kustomization -n flux-system -o wide  # READY: True

# Database
kubectl get cluster -n database                   # STATUS: Cluster in healthy state

# Firing alerts (quick overview)
curl -s https://alertmanager.driscoll.tech/api/v2/alerts | jq 'length'
```

## Safety Checklist

Before pushing to `main`:

- [ ] Ran `task update` — generated files look correct
- [ ] Ran `flux-local test` — all passed
- [ ] No unencrypted secrets (`grep -r "password:" kubernetes/ --include="*.yaml"`)
- [ ] All `*.sops.yaml` files are encrypted (no plaintext `stringData` visible)
- [ ] `age.key` is git-ignored
- [ ] ExternalSecret references correct 1Password vault entry (`op://Eris/...`)
- [ ] PVC accessModes match cluster capabilities
- [ ] `postBuild.substituteFrom` sources preserved in child Kustomizations
