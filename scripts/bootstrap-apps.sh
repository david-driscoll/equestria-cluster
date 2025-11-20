#!/usr/bin/env bash
set -Eeuo pipefail

source "$(dirname "${0}")/lib/common.sh"

export LOG_LEVEL="debug"
export ROOT_DIR="$(git rev-parse --show-toplevel)"

# Talos requires the nodes to be 'Ready=False' before applying resources
function wait_for_nodes() {
    log debug "Waiting for nodes to be available"

    # Skip waiting if all nodes are 'Ready=True'
    if kubectl wait nodes --for=condition=Ready=True --all --timeout=10s &>/dev/null; then
        log info "Nodes are available and ready, skipping wait for nodes"
        return
    fi

    # Wait for all nodes to be 'Ready=False'
    until kubectl wait nodes --for=condition=Ready=False --all --timeout=10s &>/dev/null; do
        log info "Nodes are not available, waiting for nodes to be available. Retrying in 10 seconds..."
        sleep 10
    done
}

# Namespaces to be applied before the SOPS secrets are installed
function apply_namespaces() {
    log debug "Applying namespaces"

    local -r apps_dir="${ROOT_DIR}/kubernetes/apps"

    if [[ ! -d "${apps_dir}" ]]; then
        log error "Directory does not exist" "directory=${apps_dir}"
    fi

    for app in "${apps_dir}"/*/; do
        namespace=$(basename "${app}")

        # Check if the namespace resources are up-to-date
        if kubectl get namespace "${namespace}" &>/dev/null; then
            log info "Namespace resource is up-to-date" "resource=${namespace}"
            continue
        fi

        # Apply the namespace resources
        if kubectl create namespace "${namespace}" --dry-run=client --output=yaml \
            | kubectl apply --server-side --filename - &>/dev/null;
        then
            log info "Namespace resource applied" "resource=${namespace}"
        else
            log error "Failed to apply namespace resource" "resource=${namespace}"
        fi
    done
}

# SOPS secrets to be applied before the helmfile charts are installed
function apply_sops_secrets() {
    log debug "Applying secrets"

    local -r secrets=(
        # "${ROOT_DIR}/bootstrap/github-deploy-key.sops.yaml"
        "${ROOT_DIR}/bootstrap/sops-age.sops.yaml"
        "${ROOT_DIR}/kubernetes/components/common/cluster-secrets.sops.yaml"
        "${ROOT_DIR}/kubernetes/components/common/shared-secrets.sops.yaml"
    )

    for secret in "${secrets[@]}"; do
        if [ ! -f "${secret}" ]; then
            log warn "File does not exist" "file=${secret}"
            continue
        fi

        # Check if the secret resources are up-to-date
        if sops exec-file "${secret}" "kubectl --namespace flux-system diff --filename {}" &>/dev/null; then
            log info "Secret resource is up-to-date" "resource=$(basename "${secret}" ".sops.yaml")"
            continue
        fi

        # Apply secret resources
        if sops exec-file "${secret}" "kubectl --namespace flux-system apply --server-side --filename {}" &>/dev/null; then
            log info "Secret resource applied successfully" "resource=$(basename "${secret}" ".sops.yaml")"
        else
            log error "Failed to apply secret resource" "resource=$(basename "${secret}" ".sops.yaml")"
        fi
    done
}

# CRDs to be applied before the helmfile charts are installed
function apply_crds() {
    log debug "Applying CRDs"

    local -r crd_helmfile="${ROOT_DIR}/bootstrap/helmfile.d/00-crds.yaml"

    if [[ ! -f "${crd_helmfile}" ]]; then
        log error "CRD helmfile does not exist" "file=${crd_helmfile}"
    fi

    # Extract CRDs using helmfile template with yq post-renderer
    if ! helmfile --file "${crd_helmfile}" template | kubectl apply --server-side --filename - &>/dev/null; then
        log error "Failed to apply CRDs from helmfile"
    fi

    log info "CRDs applied successfully from helmfile"

    # Apply additional custom CRDs
    local -r custom_crds=(
        # renovate: datasource=github-releases depName=prometheus-operator/prometheus-operator
        https://github.com/prometheus-operator/prometheus-operator/releases/download/v0.86.2/stripped-down-crds.yaml
        # renovate: datasource=github-releases depName=kubernetes-sigs/external-dns
        # https://raw.githubusercontent.com/kubernetes-sigs/external-dns/refs/tags/v0.20.0/docs/sources/crd/crd-manifest.yaml
        "${ROOT_DIR}/kubernetes/apps/observability/crds/application/application-crd.yaml"
        "${ROOT_DIR}/kubernetes/apps/observability/crds/application/cluster-crd.yaml"
        "${ROOT_DIR}/kubernetes/apps/observability/crds/kuma/crds.yaml"
    )

    for crd in "${custom_crds[@]}"; do
        if [[ ! -f "${crd}" ]]; then
            log warn "Custom CRD file does not exist" "crd=${crd}"
            continue
        fi
        if kubectl diff --filename "${crd}" &>/dev/null; then
            log info "Custom CRD is up-to-date" "crd=${crd}"
            continue
        fi
        if kubectl apply --server-side --filename "${crd}" &>/dev/null; then
            log info "Custom CRD applied" "crd=${crd}"
        else
            log error "Failed to apply custom CRD" "crd=${crd}"
        fi
    done
}

# Apply Helm releases using helmfile
function apply_helm_releases() {
    log debug "Applying Helm releases with helmfile"

    local -r app_helmfile="${ROOT_DIR}/bootstrap/helmfile.d/01-apps.yaml"

    if [[ ! -f "${app_helmfile}" ]]; then
        log error "App helmfile does not exist" "file=${app_helmfile}"
    fi

    if ! helmfile --file "${app_helmfile}" apply --hide-notes --skip-diff-on-install --suppress-diff --suppress-secrets; then
        log error "Failed to apply Helm releases"
    fi

    log info "Helm releases applied successfully"
}

function main() {
    check_cli helmfile kubectl kustomize sops talhelper yq

    # Apply resources and Helm releases
    # wait_for_nodes
    apply_namespaces
    apply_sops_secrets
    apply_crds
    # apply_helm_releases

    log info "Congrats! The cluster is bootstrapped and Flux is syncing the Git repository"
}

main "$@"
