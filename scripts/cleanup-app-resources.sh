#!/usr/bin/env bash
set -euo pipefail

# cleanup-app-resources.sh
# Usage: cleanup-app-resources.sh <APP_NAME> [--yes] [--delete-pvcs] [--dry-run]
# Example: ./scripts/cleanup-app-resources.sh romm --yes --delete-pvcs

APP_LABEL_VALUE=${1:-}
shift || true

CONFIRM=false
DRY_RUN=false

while [[ "$#" -gt 0 ]]; do
  case "$1" in
    --yes) CONFIRM=true; shift ;;
    --dry-run) DRY_RUN=true; shift ;;
    *) echo "Unknown arg: $1"; exit 2 ;;
  esac
done

if [[ -z "$APP_LABEL_VALUE" ]]; then
  echo "Usage: $0 <APP_NAME> [--yes] [--delete-pvcs] [--dry-run]"
  exit 2
fi

echo "Cleanup plan for label: app.kubernetes.io/name=${APP_LABEL_VALUE}"
if [[ "$DRY_RUN" == "true" ]]; then
  echo "DRY RUN mode - no deletions will be performed"
fi

if [[ "$CONFIRM" != "true" && "$DRY_RUN" != "true" ]]; then
  read -r -p "Proceed to delete resources with label app.kubernetes.io/name=${APP_LABEL_VALUE}? [y/N] " ans
  if [[ "$ans" != "y" && "$ans" != "Y" ]]; then
    echo "Aborted by user"
    exit 0
  fi
fi

echo "Scanning API resources and deleting matches..."

DRY_FLAG=""
if [[ "$DRY_RUN" == "true" ]]; then
  DRY_FLAG="--dry-run=client"
fi

# Iterate all listable API resource kinds and delete any objects with the label
for api in $(kubectl api-resources --verbs=list -o name | sort -u); do
  # get matching objects (namespaced output includes namespace prefix)
  matches=$(kubectl get "$api" -A -l "app.kubernetes.io/name=${APP_LABEL_VALUE}" -o name 2>/dev/null || true)
  if [[ -n "$matches" ]]; then
    echo "Found for $api:"
    echo "$matches"
    if [[ "$DRY_RUN" == "true" ]]; then
      continue
    fi
    # delete each object individually to preserve any namespace scoping in the name
    echo "$matches" | xargs -r -n1 kubectl delete --now --ignore-not-found
  fi
done

# Optional: delete PVCs if requested
echo "Deleting PVCs with label app.kubernetes.io/name=${APP_LABEL_VALUE}"
pvcs=$(kubectl get pvc -A -l "app.kubernetes.io/name=${APP_LABEL_VALUE}" -o name 2>/dev/null || true)
if [[ -n "$pvcs" ]]; then
echo "$pvcs"
if [[ "$DRY_RUN" != "true" ]]; then
    echo "$pvcs" | xargs -r -n1 kubectl delete --ignore-not-found
fi
else
echo "No PVCs found"
fi

echo "Cleanup complete for app.kubernetes.io/name=${APP_LABEL_VALUE}"

# Second pass: handle objects that might be stuck due to finalizers and try to remove them
echo "Checking for any remaining objects with finalizers to force-clean..."
for api in $(kubectl api-resources --verbs=list -o name | sort -u); do
  remaining=$(kubectl get "$api" -A -l "app.kubernetes.io/name=${APP_LABEL_VALUE}" -o name 2>/dev/null || true)
  if [[ -n "$remaining" ]]; then
    echo "Remaining for $api:"
    echo "$remaining"
    if [[ "$DRY_RUN" == "true" ]]; then
      continue
    fi
    # Attempt to remove finalizers then delete
    while IFS= read -r obj; do
      echo "Patching finalizers for $obj"
      kubectl patch "$obj" --type=merge -p '{"metadata":{"finalizers":null}}' || true
      echo "Deleting $obj"
      kubectl delete "$obj" --now --ignore-not-found || true
    done <<< "$remaining"
  fi
done

echo "Final cleanup pass complete."
