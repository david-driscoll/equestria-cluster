# Testing Patterns

> Last updated: 2026-05-24

## Core Sections (Required)

### 1) Test Stack and Commands

- **Primary validation framework**: `flux-local` (Python CLI, v8.2.0)
- **Schema validation**: `kubeconform` (v0.7.0) + YAML language-server schemas in every file
- **No unit test framework**: this is an infrastructure repo — there is no application code to unit test
- **Commands**:

```bash
# Full validation (required before any kubernetes/ push)
flux-local test --enable-helm --all-namespaces --path ./kubernetes/flux/cluster -v

# Diff against main branch (shows what would change)
flux-local diff kustomization --path ./kubernetes/flux/cluster --path-orig <main-path> --all-namespaces

# Run all validations + regenerate derived resources
task update
# Expands to: dotnet run .mise/tasks/do-update.cs && flux-local test ...
```

### 2) Test Layout

- **No dedicated test directory**: validation is CI-driven, not file-organized
- **CI test config**: `.github/workflows/flux-local.yaml`
- **Local validation**: `flux-local` run via `task update` or directly
- **Pre-commit hooks**: `.husky/` runs `dotnet husky` before every commit
- **Agent skill tests**: `.claude/skills/*/tests.yaml` — skill-level test definitions (for AI agent skills, not cluster manifests)

### 3) Test Scope Matrix

| Scope | Covered? | Typical target | Notes |
|-------|----------|----------------|-------|
| Schema validation | Yes | All YAML files | Via `# yaml-language-server: $schema=` annotations + IDE integration |
| Flux kustomization rendering | Yes | `kubernetes/` tree | `flux-local test` renders all Kustomizations including Helm charts |
| Helm diff on PR | Yes | All HelmReleases + Kustomizations | `flux-local diff` posts diff as PR comment |
| Secret rendering | No | `*.sops.yaml` | SOPS-encrypted files are not decrypted in CI (no age key in CI) |
| Integration/E2E against live cluster | No | — | No automated E2E testing; manual verification via `kubectl` |
| Generated file correctness | Partial | `Update.cs` output | `task update` re-runs generation and then runs `flux-local test` |
| Unit tests | No | — | No application source code; no unit tests |

### 4) Mocking and Isolation Strategy

- **No mocking needed**: infrastructure YAML has no runtime logic to mock
- **Isolation for CI**: `flux-local` renders Helm charts in-process without a live cluster — effectively a simulation of what Flux would apply
- **SOPS in CI**: secrets are not decrypted; `flux-local` skips encrypted fields during rendering
- **Common CI failure mode**: Helm chart version mismatch (chart API changed) or missing `OCIRepository` reference causes `flux-local` to fail

### 5) Coverage and Quality Signals

- **Coverage tool**: N/A (infrastructure)
- **Quality signals**:
  - CI pass/fail on `flux-local test` (required for merge)
  - Renovate PRs keep all image tags and chart versions current
  - `driftDetection` in HelmReleases catches out-of-band cluster changes
  - Alertmanager + Gatus provide live cluster health signals
- **Known gaps**:
  - No automated testing that apps are actually healthy post-deploy (just schema/rendering)
  - No automated recovery testing (Volsync restore, CNPG failover)
  - SOPS-encrypted files are never schema-validated in CI

### 6) Evidence

- `.github/workflows/flux-local.yaml`
- `.mise.toml` (flux-local version pin)
- `.husky/`
- `kubernetes/apps/equestria/home/n8n/helmrelease.yaml` (schema annotation example)
- `.claude/skills/*/tests.yaml` (AI skill tests, not cluster tests)
