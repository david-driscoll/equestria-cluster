# Coding Conventions

> Last updated: 2026-05-24

## Core Sections (Required)

### 1) Naming Rules

| Item | Rule | Example | Evidence |
|------|------|---------|----------|
| App directories | `kebab-case` matching the app name | `home-assistant`, `n8n`, `external-dns` | `kubernetes/apps/` tree |
| Namespace directories | Match Kubernetes namespace name (`kebab-case`) | `equestria`, `kube-system`, `volsync-system` | `kubernetes/apps/` tree |
| YAML resource files | Named by resource type (fixed names) | `ks.yaml`, `helmrelease.yaml`, `kustomization.yaml`, `externalsecret.yaml` | all apps |
| Encrypted secret files | `*.sops.yaml` | `secret.sops.yaml`, `cluster-secrets.sops.yaml` | `.sops.yaml` |
| Kubernetes resource names | Match app name, set via YAML anchor `&app` | `name: &app n8n` | `ks.yaml` files |
| Kubernetes labels | `app.kubernetes.io/name` + `driscoll.dev/name` | `driscoll.dev/name: n8n` | `ks.yaml` commonMetadata |
| postBuild substitution vars | `UPPER_SNAKE_CASE` | `APP`, `NAMESPACE`, `VOLSYNC_PUID`, `ROOT_DOMAIN` | `ks.yaml` postBuild |
| Component directories | `kebab-case`, descriptive | `postgres`, `volsync`, `ingress/internal` | `kubernetes/components/` |
| C# scripts | PascalCase filename, `.cs` extension | `Update.cs`, `Tasks.cs` | `kubernetes/components/postgres/Update.cs` |

### 2) Formatting and Linting

- **Formatter**: EditorConfig (`.editorconfig`) — 2-space indent for YAML, 4-space for shell/CUE, LF line endings, UTF-8, trailing newline
- **Linter**: `flux-local test` — validates Flux kustomizations + Helm rendering before push
- **Schema validation**: YAML language-server schema annotations (`# yaml-language-server: $schema=...`) on virtually every file
- **YAML key ordering**: no enforced ordering, but common convention is `apiVersion → kind → metadata → spec`
- **Run commands**:
  ```bash
  flux-local test --enable-helm --all-namespaces --path ./kubernetes/flux/cluster -v
  ```

### 3) Import and Module Conventions

Kubernetes/Flux YAML:
- **Component references**: relative paths from `ks.yaml` to `kubernetes/components/` using `../../../../components/<name>` pattern
- **Resource references**: within an app directory, all files are relative siblings
- **Schema references**: always include `# yaml-language-server: $schema=<URL>` at top of YAML files to enable IDE validation

C# `Update.cs` scripts:
- **NuGet packages**: declared via `#:package <name>@<version>` shebang-style comments at top of file
- **Script entrypoint**: `#!/usr/bin/dotnet run` shebang, enabling direct execution
- **No project files**: C# scripts are standalone `.cs` files executed by `dotnet run`

### 4) Error and Logging Conventions

Flux reconciliation:
- All Kustomizations set `retryInterval: 2m` — Flux retries failed reconciliations every 2 minutes
- HelmReleases set `remediation.retries: 7` with `strategy: rollback` on upgrade failures
- `rollback.force: true` and `rollback.cleanupOnFail: true` ensure failed installs are cleaned up

Alertmanager:
- Alert routing configured via `kubernetes/apps/observability/alertmanager/` — routes to configured receivers
- All HelmReleases include `driftDetection.mode: enabled` for drift alerts

No application-level logging conventions apply (this is infrastructure, not application code).

### 5) Testing Conventions

- **Pre-push validation**: `flux-local test` — mandatory before pushing any `kubernetes/` changes (enforced by CI and documented in `CLAUDE.md`)
- **CI gate**: `.github/workflows/flux-local.yaml` runs test + diff on every PR touching `kubernetes/`
- **Diff on PR**: `flux-local diff` posts HelmRelease and Kustomization diffs as PR comments
- **Pre-commit hook**: Husky runs `dotnet husky` tasks before every commit
- **No unit tests**: infrastructure manifests are validated by schema + flux-local rendering only
- **No coverage threshold**: N/A for infrastructure

### 6) Key YAML Patterns

**YAML anchors for DRY**:
```yaml
metadata:
  name: &app n8n
  namespace: &namespace equestria
spec:
  targetNamespace: *namespace
```

**postBuild substitution**:
```yaml
# In ks.yaml - sets variables resolved at reconcile time
postBuild:
  substitute:
    APP: *app
    NAMESPACE: *namespace
    VOLSYNC_PUID: "1000"
```

**Variable references in manifests**:
```yaml
# Resolved at Flux reconcile time from cluster-secrets or postBuild
env:
  N8N_HOST: "n8n.${ROOT_DOMAIN}"
  GENERIC_TIMEZONE: "${TIMEZONE}"
```

**Renovate discovery annotations**:
```yaml
# renovate: datasource=docker depName=ghcr.io/siderolabs/kubelet
KUBERNETES_VERSION=v1.36.1
```

### 7) Evidence

- `.editorconfig`
- `kubernetes/apps/equestria/home/n8n/ks.yaml`
- `kubernetes/apps/equestria/home/n8n/helmrelease.yaml`
- `kubernetes/components/common/kustomization.yaml`
- `.github/workflows/flux-local.yaml`
- `.github/renovate.json5`
