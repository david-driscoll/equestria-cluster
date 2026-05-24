# Codebase Structure

> Last updated: 2026-05-24

## Core Sections (Required)

### 1) Top-Level Map

| Path | Purpose | Evidence |
|------|---------|----------|
| `kubernetes/` | All Kubernetes manifests — source of truth for cluster state | `CLAUDE.md` |
| `kubernetes/flux/` | Flux CD entrypoints and GitRepository source definitions | `kubernetes/flux/cluster/ks.yaml` |
| `kubernetes/apps/` | All deployed applications, grouped by namespace | directory tree |
| `kubernetes/components/` | Reusable Kustomize components shared across apps | `kubernetes/components/` |
| `talos/` | Talos Linux node configuration (talconfig, patches, secrets) | `talos/talconfig.yaml` |
| `bootstrap/` | One-time cluster bootstrap via Helmfile (Cilium, CoreDNS, Flux, cert-manager) | `bootstrap/helmfile.yaml` |
| `.mise.toml` | All pinned tool versions + environment variables | `.mise.toml` |
| `Taskfile.yaml` | Task runner entrypoint; includes `.taskfiles/` subtasks | `Taskfile.yaml` |
| `.taskfiles/` | Subtask definitions for talos, flux, k8s, bootstrap operations | `.taskfiles/` |
| `.github/` | GitHub Actions workflows, Renovate config, agent configs | `.github/` |
| `scripts/` | Bash utility scripts (bootstrap, cleanup, restore-databases) | `scripts/` |
| `docs/` | Documentation (CODEMAPS, codebase docs) | `docs/` |
| `.sops.yaml` | SOPS encryption rules (age keys, path regex patterns) | `.sops.yaml` |
| `versions.env` | Kubernetes + Talos version pins (consumed by kustomize + renovate) | `versions.env` |
| `apm.yml` | Agent Package Manager config (AI skills for claude/copilot) | `apm.yml` |
| `Skillfile` | GitHub-sourced skills for AI agent assistance | `Skillfile` |
| `AGENTS.md` | Agent instructions for AI assistants | `AGENTS.md` |
| `CLAUDE.md` | Claude Code project instructions | `CLAUDE.md` |
| `.husky/` | Git pre-commit hooks | `.husky/` |
| `age.key` / `eq.age.key` | SOPS private age keys — git-ignored, never committed | `.gitignore` |
| `kubeconfig` | Cluster API access config — git-ignored | `.gitignore` |

### 2) Entry Points

- **Flux GitOps entry**: `kubernetes/flux/cluster/ks.yaml` — root Kustomization, loads `cluster-meta` then `cluster-apps`
- **Bootstrap entry**: `bootstrap/helmfile.yaml` — used once to seed the cluster (Cilium → CoreDNS → cert-manager → Flux)
- **Task runner entry**: `Taskfile.yaml` — human operator interface; delegates to `.taskfiles/`
- **Update script entry**: `.mise/tasks/do-update.cs` — orchestrates all `Update.cs` codegen scripts
- **CI entry**: `.github/workflows/flux-local.yaml` — validates `kubernetes/` changes on every PR

### 3) Module Boundaries

| Boundary | What belongs here | What must not be here |
|----------|-------------------|------------------------|
| `kubernetes/apps/<namespace>/<app>/` | All resources for one app: `ks.yaml`, `helmrelease.yaml`, `kustomization.yaml`, `externalsecret.yaml`, `*.sops.yaml` | Resources belonging to other apps |
| `kubernetes/components/` | Reusable Kustomize Components (patch sets, template files) | App-specific logic |
| `kubernetes/flux/` | Flux source objects and root Kustomizations | App HelmReleases or secrets |
| `talos/` | Talos node configuration and patches | Kubernetes application manifests |
| `bootstrap/` | Pre-Flux initial setup only | Ongoing app management |
| `.sops.yaml` encrypted files | Secret data (`data`/`stringData` fields only) | Unencrypted secrets |
| `Update.cs` scripts | Code generation for derived resources (db users, volsync CRs) | Manual one-off changes that belong in YAML directly |

### 4) Naming and Organization Rules

- **App directories**: `kebab-case` (e.g., `n8n`, `home-assistant`, `external-dns`)
- **Namespace directories**: match Kubernetes namespace names, `kebab-case`
- **YAML files**: named by resource type — `ks.yaml` (Kustomization), `helmrelease.yaml` (HelmRelease), `kustomization.yaml` (Kustomize), `externalsecret.yaml` (ExternalSecret)
- **Encrypted secrets**: `*.sops.yaml` suffix — SOPS encrypts only `data`/`stringData` fields
- **Generated files**: do not edit directly — managed by `Update.cs` scripts (`task update`)
- **Component organization**: `kubernetes/components/<name>/` — each component is a Kustomize Component (kind: Component)
- **Label pattern**: all apps use `app.kubernetes.io/name` and `driscoll.dev/name` labels via `ks.yaml` `commonMetadata`
- **Namespace grouping**: `kubernetes/apps/<namespace>/kustomization.yaml` lists all apps in that namespace

### 5) Kubernetes App Directory Pattern

Every app follows this canonical structure:

```
kubernetes/apps/<namespace>/<app>/
├── ks.yaml              # Flux Kustomization: dependsOn, components, postBuild vars
├── kustomization.yaml   # Kustomize manifest list
├── helmrelease.yaml     # HelmRelease (usually bjw-s app-template)
├── externalsecret.yaml  # ExternalSecret pulling from 1Password Connect
├── definition.yaml      # (optional) ApplicationDefinition CRD for Authentik/Gatus integration
└── *.sops.yaml          # (optional) SOPS-encrypted static secrets
```

### 6) Evidence

- `kubernetes/flux/cluster/ks.yaml`
- `kubernetes/apps/equestria/home/n8n/ks.yaml`
- `kubernetes/apps/equestria/home/n8n/helmrelease.yaml`
- `kubernetes/components/common/kustomization.yaml`
- `Taskfile.yaml`
- `.mise.toml`

## Extended Sections

### Namespace Map

For full app-level detail, see [`KUBERNETES_APPS.md`](./KUBERNETES_APPS.md).

| Namespace | Purpose | Key apps |
|-----------|---------|----------|
| `equestria` | User-facing apps (home, media, pvr, downloads, games, idp) | n8n, immich, Plex, Radarr, Authentik |
| `observability` | Monitoring, logging, alerting | Prometheus, Grafana, Loki, Alertmanager, Alloy |
| `kube-system` | Cluster infrastructure | Cilium, CoreDNS, 1password-connect, external-secrets, reloader |
| `network` | Ingress, DNS, tunnels | Traefik, cloudflare-tunnel, external-dns, k8s-gateway, CrowdSec |
| `pulumi` | Infrastructure automation | Pulumi operator + stacks (Authentik, Cloudflare, Unifi) |
| `tailscale-system` | Tailnet integration | Tailscale operator, golink, generated ExternalName services |
| `database` | Data stores | PostgreSQL (CNPG), Valkey, Neo4j |
| `cert-manager` | TLS certificates | cert-manager, trust-manager, Let's Encrypt issuers |
| `github-actions` | CI/CD runners | ARC controller, runner scale sets |
| `flux-system` | GitOps controllers | Flux Operator, Flux Instance, Weave GitOps |
| `system-upgrade` | Node upgrades | Talos + Kubernetes upgrade controllers |
| `longhorn-system` | Distributed block storage | Longhorn |
| `cloudnative-pg` | PostgreSQL operator | CNPG controller |
| `nfs-system` | NFS shared storage | NFS CSI driver |
| `openebs-system` | Local block storage | OpenEBS CSI |
| `volsync-system` | PVC backup | Volsync |
