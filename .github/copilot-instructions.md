# Equestria Cluster - Talos Kubernetes GitOps Repository

This is a production Talos Kubernetes cluster using GitOps with Flux, managed via mise for development tooling and Task for build automation.

Always reference these instructions first and fallback to search or bash commands only when you encounter unexpected information that does not match the info here.

## Working Effectively

### Essential Tool Installation
- **CRITICAL**: Install mise first, then all required tools:
  ```bash
  # Install mise (may fail due to network restrictions - see troubleshooting)
  curl https://mise.run | sh
  # OR try alternative: curl -L https://install.mise.jdx.dev/install.sh | sh
  
  # Trust the config and install all tools - takes 10-15 minutes. NEVER CANCEL.
  mise trust
  pip install pipx  # Required for makejinja
  mise install  # TIMEOUT: 20+ minutes
  ```
- **If mise installation fails due to network restrictions**: Document this limitation and proceed with available tools for validation only.

### Bootstrap and Build Process
- **NEVER attempt full bootstrap without physical Talos cluster hardware**
- Configuration generation (without deployment):
  ```bash
  task init        # Generate config files from templates
  task configure   # Template out Kubernetes and Talos configs - takes 2-5 minutes
  ```
- **Cluster Bootstrap (requires physical hardware)**:
  ```bash
  task bootstrap:talos  # Install Talos - takes 15-30 minutes. NEVER CANCEL.
  task bootstrap:apps   # Bootstrap applications - takes 10-20 minutes. NEVER CANCEL.
  ```

### Validation and Testing
- **Primary validation method** (works without cluster):
  ```bash
  # Flux manifest validation - takes 79 seconds. NEVER CANCEL. Set timeout to 120+ seconds.
  flux-local test --enable-helm --all-namespaces --path ./kubernetes/flux/cluster -v
  # OR use Docker if flux-local not installed:
  docker run --rm -v "$(pwd):/workspace" -w /workspace ghcr.io/allenporter/flux-local:v7.8.0 test --enable-helm --all-namespaces --path /workspace/kubernetes/flux/cluster -v
  ```
- **Additional validation tools**:
  ```bash
  # Basic Kubernetes manifest validation (install kubeconform first)
  kubeconform -summary -verbose kubernetes/apps/*/kustomization.yaml
  # Check dependencies in bootstrap script
  bash scripts/bootstrap-apps.sh  # Will fail gracefully, showing missing tools
  ```

### Cluster Operations (requires running cluster)
- **Post-deployment verification**:
  ```bash
  cilium status                    # Check Cilium CNI status
  flux check                       # Verify Flux system health
  flux get sources git flux-system
  flux get ks -A                   # Check Kustomizations
  flux get hr -A                   # Check HelmReleases
  kubectl get pods --all-namespaces --watch  # Monitor rollout
  ```
- **Force Flux sync**:
  ```bash
  task reconcile  # Force pull from Git repository
  ```

## Validation Scenarios
- **ALWAYS run flux-local testing** after making changes to kubernetes/ directory
- **ALWAYS test** that bootstrap script dependency validation works: `bash scripts/bootstrap-apps.sh`
- **After cluster changes**: Run complete verification workflow including cilium status and flux checks
- **Manual testing**: If cluster is available, perform end-to-end GitOps workflow testing

## Common Troubleshooting

### Installation Issues
- **"Missing required deps"**: Normal when tools not installed - shows helmfile, sops, talhelper, etc.
- **Network restrictions blocking mise**: Use available tools (kubectl, yq, docker) for validation only
- **Python compilation errors**: Run `mise settings python.compile=0` then retry
- **GitHub token issues**: Unset `GITHUB_TOKEN` environment variable and retry

### Expected Timing and Failures
- **mise install**: 10-20 minutes depending on network. NEVER CANCEL.
- **flux-local test**: 79 seconds. NEVER CANCEL. Set timeout to 120+ seconds minimum.  
- **task bootstrap:talos**: 15-30 minutes. NEVER CANCEL.
- **task bootstrap:apps**: 10-20 minutes. NEVER CANCEL.
- **Cluster rollout**: 10+ minutes normal. Do not interrupt.

## Infrastructure Requirements

### Physical Requirements
- **This is a running production cluster config**, not a template
- **Requires specific Talos hardware**: Multiple nodes with exact network configurations
- **Storage**: Longhorn with specific disk configurations per node
- **Network**: Static IPs, specific hardware addresses defined in talconfig.yaml

### External Dependencies
- **OnePassword Connect**: Used for secret management (required for SOPS)
- **Cloudflare account**: Required for DNS and tunnel management  
- **Talos Image Factory**: For custom system extensions

### Secret Management
- **SOPS with Age**: All secrets encrypted with age.key file
- **OnePassword integration**: Credentials managed via op:// references
- **NEVER commit unencrypted secrets**: All .sops.yaml files must be encrypted

## Repository Structure

### Key Directories
- `/.taskfiles/`: Build automation (bootstrap, k8s, talos)
- `/kubernetes/flux/cluster/`: Main Flux configuration and applications
- `/kubernetes/apps/`: Application deployments organized by namespace
- `/kubernetes/components/`: Shared components and templates
- `/talos/`: Talos Linux configuration and patches
- `/scripts/`: Shell scripts, primarily bootstrap-apps.sh
- `/bootstrap/`: Helmfile-based initial cluster setup

### Configuration Files
- `.mise.toml`: Development tool versions and environment setup
- `Taskfile.yaml`: Main task runner with includes
- `talos/talconfig.yaml`: Complete Talos cluster configuration
- `talos/talenv.yaml`: Version pinning for Talos and Kubernetes

## Automation and Updates
- **Renovate**: Automated dependency updates via .github/renovate.json5
- **GitHub Actions**: flux-local testing on all kubernetes/ changes  
- **Dotnet tooling**: Custom update scripts in kubernetes/*/Update.cs files
- **Husky pre-commit**: Runs dotnet husky tasks automatically

## Critical Reminders
- **NEVER CANCEL long-running builds or deploys** - may take 45+ minutes
- **ALWAYS validate with flux-local** before pushing kubernetes/ changes
- **VERIFY .sops.yaml encryption** before committing secrets
- **SET EXPLICIT TIMEOUTS** (60+ minutes) for bootstrap commands
- **This is a production config** - exercise extreme caution with changes