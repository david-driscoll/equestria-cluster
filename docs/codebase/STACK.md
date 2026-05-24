# Technology Stack

> Last updated: 2026-05-24

## Core Sections (Required)

### 1) Runtime Summary

| Area | Value | Evidence |
|------|-------|----------|
| Primary language | YAML (Kubernetes manifests) | `kubernetes/` tree |
| Secondary languages | C# (.NET 10), Python 3.14, Bash | `.mise.toml` |
| Kubernetes version | v1.36.1 | `versions.env`, `talos/talconfig.yaml` |
| Talos Linux version | v1.13.2 | `versions.env`, `.mise.toml` |
| Package manager (tools) | mise (tool version manager) | `.mise.toml` |
| .NET runtime | dotnet 10.0.203 (C# scripts) | `.mise.toml` |
| Python runtime | 3.14.5 (scripts/automation) | `.mise.toml` |

### 2) Production Frameworks and Dependencies

This is a GitOps/infrastructure repo, not an application codebase. "Dependencies" are Helm charts and Kubernetes operators:

| Dependency | Version | Role in system | Evidence |
|------------|---------|----------------|----------|
| Flux Operator | 0.50.0 | GitOps controller — watches Git, reconciles cluster state | `bootstrap/helmfile.yaml` |
| Flux Instance (flux2) | 2.8.8 | Flux CD installation managed by Flux Operator | `bootstrap/helmfile.yaml`, `.mise.toml` |
| Cilium | 1.19.4 | CNI (container network interface), network policies, eBPF | `bootstrap/helmfile.yaml` |
| CoreDNS | 1.45.2 | Cluster DNS | `bootstrap/helmfile.yaml` |
| Cert-Manager | v1.20.2 | TLS certificate automation | `bootstrap/helmfile.yaml` |
| CloudNative-PG (CNPG) | — | PostgreSQL operator (cluster-in-a-box, WAL backups) | `kubernetes/apps/cloudnative-pg/` |
| Longhorn | — | Distributed block storage (primary PVC backend) | `kubernetes/apps/longhorn-system/` |
| Traefik | — | Ingress controller, gateway, middleware (auth forwarding) | `kubernetes/apps/network/traefik/` |
| Volsync | — | PVC replication + Backblaze B2 offsite backup | `kubernetes/apps/volsync-system/` |
| External Secrets Operator | — | Pulls secrets from 1Password Connect into Kubernetes Secrets | `kubernetes/apps/kube-system/` |
| Authentik | — | SSO/IdP (OAuth2/OIDC, forward auth) | `kubernetes/apps/equestria/idp/` |
| kube-prometheus-stack | 85.2.0 | Prometheus + Grafana + Alertmanager | `kubernetes/apps/observability/prometheus/helmrelease.yaml` |
| Tailscale Operator | — | Tailnet service discovery, IDP integration | `kubernetes/apps/tailscale-system/` |
| Cloudflare Tunnel (cloudflared) | 2026.5.0 | External traffic ingress via Cloudflare | `kubernetes/apps/network/cloudflare-tunnel/` |
| External-DNS | — | Automatic DNS record management (Cloudflare) | `kubernetes/apps/network/external-dns/` |
| bjw-s app-template | — | Generic Helm chart used by almost all workloads | `kubernetes/components/repos/app-template/` |
| Renovate | — | Automated dependency updates (Helm charts, container images) | `.github/renovate.json5` |
| CrowdSec | — | Security/IDS integration with Traefik | `kubernetes/apps/network/crowdsec/` |
| Spegel | 0.7.0 | P2P image registry mirror | `bootstrap/helmfile.yaml` |

### 3) Development Toolchain

| Tool | Purpose | Evidence |
|------|---------|----------|
| `mise` | Pinned version manager for all CLI tools | `.mise.toml` |
| `task` (Taskfile) | Task runner (bootstrap, talos ops, reconcile) | `Taskfile.yaml`, `.taskfiles/` |
| `flux-local` | Local validation of Flux kustomizations before push | `.mise.toml`, `.github/workflows/flux-local.yaml` |
| `sops` | Secret encryption/decryption (age backend) | `.sops.yaml`, `.mise.toml` |
| `age` | Encryption key backend for SOPS | `.sops.yaml`, `.mise.toml` |
| `talhelper` | Talos config generation (templated) | `.mise.toml`, `talos/talconfig.yaml` |
| `talosctl` | Talos node management CLI | `.mise.toml` |
| `kubectl` | Kubernetes API access | `.mise.toml` |
| `helm` | Helm chart management | `.mise.toml` |
| `helmfile` | Declarative Helm release management (bootstrap) | `.mise.toml`, `bootstrap/helmfile.yaml` |
| `kustomize` | Kubernetes manifest composition | `.mise.toml` |
| `kubeconform` | Schema validation for Kubernetes YAML | `.mise.toml` |
| `yq` / `jq` | YAML/JSON processing in scripts | `.mise.toml` |
| `1password-cli` (op) | Secrets retrieval from 1Password | `.mise.toml` |
| `gh` | GitHub CLI (PRs, releases) | `.mise.toml` |
| `apm` | Agent Package Manager (claude/copilot AI skill management) | `apm.yml`, `.mise.toml` |
| `skillfile` | Skill installation from GitHub | `Skillfile`, `.mise.toml` |
| Renovate (GitHub App) | Automated PR-based dependency updates | `.github/renovate.json5` |
| Husky | Git pre-commit hook runner | `.husky/` |
| `dotnet` | C# script runner for Update.cs automation scripts | `.mise.toml` |
| `makejinja` | Jinja2 templating for talos config | `.mise.toml` |

### 4) Key Commands

```bash
# Install all pinned tools
mise install

# Validate Flux manifests locally (REQUIRED before any kubernetes/ push)
flux-local test --enable-helm --all-namespaces --path ./kubernetes/flux/cluster -v

# Run Update.cs codegen scripts and validate
task update
# expands to: dotnet run .mise/tasks/do-update.cs && flux-local test ...

# Force Flux to resync from Git
task reconcile

# Talos operations
task talos:generate-config
task talos:apply-node IP=<ip> MODE=auto
task talos:upgrade-node IP=<ip>
task talos:upgrade-k8s
task talos:reset

# Bootstrap (initial cluster setup only — takes 45+ min)
task bootstrap:talos
task bootstrap:apps
```

### 5) Environment and Config

- Config sources: `.mise.toml` (env + tools), `Taskfile.yaml`, `talos/talconfig.yaml`
- Required env vars (set by mise): `KUBECONFIG`, `SOPS_AGE_KEY_FILE`, `TALOSCONFIG`, `BOOTSTRAP_DIR`, `KUBERNETES_DIR`, `ROOT_DIR`, `SCRIPTS_DIR`, `TALOS_DIR`, `CONNECT_HOST`, `CONNECT_VAULT`, `CONNECT_TOKEN`, `TAILSCALE_CLIENT_ID`, `TAILSCALE_CLIENT_SECRET`
- `age.key` and `eq.age.key`: private age keys for SOPS decryption — git-ignored, required locally
- `kubeconfig`: cluster access — git-ignored
- Deployment target: Talos Linux bare-metal/VM cluster, `equestria` cluster name
- Pod network: `10.206.0.0/16`, service network: `10.196.0.0/16`

### 6) Evidence

- `.mise.toml`
- `versions.env`
- `bootstrap/helmfile.yaml`
- `Taskfile.yaml`
- `.sops.yaml`
- `.github/renovate.json5`
