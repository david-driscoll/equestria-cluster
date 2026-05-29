# Architecture

> Last updated: 2026-05-24

## Core Sections (Required)

### 1) Architectural Style

- **Primary style**: GitOps (declarative infrastructure-as-code, reconciliation loop)
- **Why this classification**: Flux CD continuously watches this Git repository and reconciles the live Kubernetes cluster state to match the declared manifests. No imperative `kubectl apply` in production — all changes go through Git.
- **Primary constraints**:
  1. **Git is the source of truth** — cluster state must always be derivable from this repo's `main` branch
  2. **Secrets never travel unencrypted** — all sensitive data must be SOPS-encrypted before commit; runtime secrets are injected via External Secrets Operator from 1Password Connect
  3. **No manual cluster modifications** — changes not reflected in Git will be pruned by Flux (`prune: true` everywhere)

### 2) System Flow

#### GitOps Reconciliation Flow

```text
Developer pushes to Git
  → GitHub Actions runs flux-local test/diff (PR validation)
  → Merged to main
  → Flux GitRepository detects change (1h poll or webhook)
  → cluster-meta Kustomization reconciles (common secrets, SOPS key, cluster vars)
  → cluster-apps Kustomization reconciles (applies SOPS decryption + postBuild substitutions to children)
  → Per-app Kustomization reconciles (ks.yaml)
    → Components injected (postgres, volsync, tailscale, ingress patches)
    → postBuild variable substitution (APP, NAMESPACE, app-specific vars)
    → HelmRelease reconciles (Helm chart deployed/upgraded)
    → ExternalSecret reconciles (pulls secrets from 1Password Connect)
    → App pod starts with injected secrets and configuration
```

#### Cluster Architecture Diagram

```
Kubernetes Cluster (Talos Linux)
│
├─ flux-system/
│  ├─ Flux Controllers (source, kustomize, helm, notification)
│  └─ Weave GitOps UI
│
├─ kube-system/
│  ├─ cilium (CNI / network policy)
│  ├─ 1password-connect (secret injection server)
│  ├─ external-secrets (ExternalSecret operator)
│  ├─ reloader (restart pods on secret/configmap change)
│  └─ ...other cluster infrastructure...
│
├─ database/
│  ├─ postgres (CloudNative-PG cluster, PostgreSQL 17.5)
│  ├─ valkey (Redis-compatible cache)
│  └─ neo4j (Graph database)
│
├─ network/
│  ├─ traefik (API Gateway / ingress)
│  ├─ cloudflare-tunnel (external traffic — no open ports)
│  ├─ external-dns (Cloudflare record automation)
│  └─ k8s-gateway (internal DNS)
│
├─ observability/
│  ├─ prometheus + alertmanager (metrics + alerts)
│  ├─ grafana (dashboards)
│  ├─ loki + alloy (log aggregation)
│  └─ blackbox-exporter (HTTP/SSH probing)
│
├─ equestria/ (User Applications)
│  ├─ home/ (n8n, immich, vikunja, windmill, ...)
│  ├─ media/ (Plex, Radarr, Sonarr, Tdarr, ...)
│  ├─ pvr/ (TVHeadend, Dispatcharr, ...)
│  ├─ downloads/ (Transmission, Sabnzbd, ...)
│  ├─ games/ (game servers)
│  └─ idp/ (Authentik SSO)
│
├─ pulumi/
│  ├─ Pulumi Operator
│  └─ Stacks: Authentik provisioning, Cloudflare, Unifi, backups
│
└─ ...storage, tailscale, cert-manager, volsync, upgrade controllers...
```

#### Secret Injection Flow

```text
1Password Connect (op://Eris/...)
  ← External Secrets Operator (ClusterSecretStore: onepassword-connect)
  → ExternalSecret object in app namespace
  → Kubernetes Secret (e.g., n8n-env)
  → Pod env vars / volume mounts
```

#### Bootstrap Flow (one-time)

```text
talhelper → generates Talos node configs → talosctl apply-config
  → Talos bootstraps etcd → kubectl available
  → helmfile apply: Cilium → CoreDNS → Spegel → cert-manager → Flux Operator → Flux Instance
  → Flux reads this Git repo → reconciles everything else
```

### 3) Layer/Module Responsibilities

| Layer or module | Owns | Must not own | Evidence |
|-----------------|------|--------------|----------|
| `kubernetes/flux/cluster/ks.yaml` | Root Flux Kustomizations; injects SOPS + postBuild into all children | Application-specific config | `kubernetes/flux/cluster/ks.yaml` |
| `kubernetes/components/common/` | Cluster-wide shared secrets (`cluster-secrets`, `shared-secrets`), SOPS age secret, namespace, middleware, version ConfigMap | App workloads | `kubernetes/components/common/kustomization.yaml` |
| `kubernetes/components/<name>/` | Reusable patch sets (postgres provisioning, volsync CRs, ingress annotations, tailscale services) | App-specific business logic | `kubernetes/components/` |
| `kubernetes/apps/<ns>/<app>/ks.yaml` | App-level Flux Kustomization: dependsOn ordering, component composition, postBuild var substitution | Container config | `kubernetes/apps/equestria/home/n8n/ks.yaml` |
| `kubernetes/apps/<ns>/<app>/helmrelease.yaml` | Container image, resource limits, probes, persistence, service/route definition | Secret values (reference via `${VAR}` substitution or secretRef) | `kubernetes/apps/equestria/home/n8n/helmrelease.yaml` |
| `kubernetes/apps/<ns>/<app>/externalsecret.yaml` | Secret templating and 1Password pull config | Workload logic | `kubernetes/apps/equestria/home/n8n/externalsecret.yaml` |
| `Update.cs` scripts | Code generation for derived/computed resources (CNPG users, volsync sources, tailscale services); handles SOPS re-encryption | Human-edited config | `kubernetes/components/postgres/Update.cs` |
| `talos/` | Node hardware config, extensions, network, disk layout | Kubernetes applications | `talos/talconfig.yaml` |
| `bootstrap/helmfile.yaml` | Pre-Flux platform layer installation | App-level workloads | `bootstrap/helmfile.yaml` |

### 4) Reused Patterns

| Pattern | Where found | Why it exists |
|---------|-------------|---------------|
| **YAML anchor (`&anchor` / `*anchor`)** | Nearly every `helmrelease.yaml` and `ks.yaml` | DRY repetition of app name, namespace, port numbers |
| **postBuild variable substitution** | All `ks.yaml` files | Allows shared templates to be parameterized per app without Helm |
| **Kustomize Components** | `kubernetes/components/*/` | Reusable patch sets that are composed into apps via `ks.yaml components:` |
| **OCIRepository + HelmRelease** | All app helmreleases | Pulls Helm charts from OCI registries rather than HTTP repos |
| **bjw-s app-template chart** | ~90% of all app HelmReleases | Single generic Helm chart for any containerized app — avoids chart maintenance |
| **ExternalSecret → 1Password** | All apps with secrets | Centralized secret management; secrets never in Git |
| **ApplicationDefinition CRD** | Apps with Authentik/Gatus integration | Custom resource consumed by Pulumi to auto-generate SSO configs and health checks |
| **Renovate annotations** | `versions.env`, all image tags | `# renovate: datasource=...` comments allow Renovate to discover and update versions |
| **Stakater Reloader** | All apps using secrets | `reloader.stakater.com/auto: "true"` triggers pod restarts on secret/configmap changes |
| **Reflector** | `cluster-secrets.sops.yaml` | `reflector.v1.k8s.emberstack.com` annotations allow Secret reflection across namespaces |
| **DriftDetection** | Most HelmReleases | `driftDetection.mode: enabled` detects out-of-band changes and reconciles |

## Update Scripts (C# Codegen)

Three `Update.cs` scripts generate derived YAML files that are committed to Git. They are **not run by Flux** — they run locally via `task update` and their output is committed.

```
Developer modifies ks.yaml (adds postgres component)
  ↓ task update
  Update.cs scans ks.yaml files → generates roles/passwords/services
  ↓ git commit (includes generated files)
  ↓ git push
  Flux reads generated manifests → applies to cluster
```

### postgres/Update.cs

**Location:** `kubernetes/components/postgres/Update.cs`

Scans all `ks.yaml` files for `components/postgres` references. For each detected app:
- Generates a CloudNative-PG role in `database/postgres/app/resources/values.yaml`
- Creates a per-user YAML in `database/postgres/app/users/<app>-postgres.yaml`
- Generates (or preserves) a UUID password in `database/postgres/app/passwords.sops.yaml` (SOPS-encrypted automatically)

### tailscale-system/services/Update.cs

**Location:** `kubernetes/apps/tailscale-system/services/Update.cs`

Fetches device list from Tailscale API (falls back to static list). For each external node:
- Generates an `ExternalName` Service with `tailscale.com/tailnet-fqdn` annotation
- Generates `Probe` resources for HTTP + SSH health checks via blackbox-exporter
- Generates `PrometheusRule` alert rules
- Updates `services/kustomization.yaml` with resource list

### common/Update.cs

**Location:** `kubernetes/components/common/Update.cs`

Regenerates Traefik middleware configs, substitution mappings, and Jinja2 template outputs.

### Script Structure

All scripts follow this pattern:

```csharp
#!/usr/bin/dotnet run
#:package YamlDotNet@16.3.0
#:package Spectre.Console@0.50.0
// ...other NuGet packages...

// 1. SCAN — read existing files / call external APIs
// 2. PROCESS — compute what should be generated
// 3. OUTPUT — write files; .sops.yaml files are auto-encrypted
```

SOPS encryption is handled transparently — scripts decrypt on read and encrypt on write, so no manual `sops` calls are needed.

### 5) Known Architectural Risks

- **Single Flux source**: all apps depend on the one `flux-system` GitRepository; a Flux control plane outage halts all reconciliation
- **Shared SOPS age key**: three age recipients share access to all secrets; key rotation requires re-encrypting every `*.sops.yaml` file
- **Large etcd snapshots**: `talos/` contains three etcd DB snapshots (up to 439GB combined) left from upgrade operations — these are not application code but inflate the repo significantly
- **Generated files depend on `task update`**: files produced by `Update.cs` scripts are committed; if a developer edits them manually and forgets to run `task update`, they will be overwritten and the manual change lost
- **1Password Connect as single secret backend**: all ExternalSecrets use one ClusterSecretStore (`onepassword-connect`); Connect outage = all secret refreshes fail (existing Secrets remain valid until rotated)

### 6) Evidence

- `kubernetes/flux/cluster/ks.yaml`
- `kubernetes/components/common/kustomization.yaml`
- `kubernetes/apps/equestria/home/n8n/ks.yaml`
- `kubernetes/apps/equestria/home/n8n/externalsecret.yaml`
- `kubernetes/components/postgres/Update.cs`
- `bootstrap/helmfile.yaml`
- `talos/talconfig.yaml`
