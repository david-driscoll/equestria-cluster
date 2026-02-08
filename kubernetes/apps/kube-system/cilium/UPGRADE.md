# Cilium Upgrade Pre-flight Checklist

## Before Upgrade

### 1. Verify Current State
```bash
# Check Cilium status
cilium status --wait

# Verify all pods are healthy
kubectl get pods -n kube-system -l k8s-app=cilium

# Count healthy pods across cluster
kubectl get pods -A | grep -E '(Running|Completed)' | wc -l

# Check all nodes are Ready
kubectl get nodes -o wide
```

### 2. Backup Current Configuration
```bash
# Note current version
kubectl get helmrelease -n kube-system cilium -o jsonpath='{.spec.chart.spec.version}'

# Current working version: v1.19.0-pre.3
```

### 3. Prepare Emergency Rollback
```bash
# Keep these commands ready in a terminal:
# Suspend HelmRelease if upgrade fails:
flux suspend hr -n kube-system cilium

# Force rollback to working version:
kubectl set image daemonset/cilium -n kube-system \
  cilium-agent=quay.io/cilium/cilium:v1.19.0-pre.3

# Resume HelmRelease:
flux resume hr -n kube-system cilium
```

## During Upgrade

### Monitor Rollout
```bash
# Terminal 1: Watch DaemonSet rollout
kubectl rollout status ds/cilium -n kube-system -w

# Terminal 2: Stream pod logs
kubectl logs -n kube-system -l k8s-app=cilium -f --prefix --max-log-requests=10

# Terminal 3: Check status every 30s
watch -n 30 'cilium status --wait'
```

### Network Failure Symptoms
If you see any of these, immediately suspend the HelmRelease:
- Total loss of pod-to-pod communication
- Nodes showing NotReady
- Cilium pods in CrashLoopBackOff
- Timeout errors from kubectl commands

## After Upgrade

### Post-Upgrade Validation
```bash
# Full connectivity test (takes ~5 minutes)
cilium connectivity test --test-concurrency=1

# Quick validation pod
kubectl run test-pod --rm -i --tty --image=nicolaka/netshoot -- /bin/bash
# Inside pod:
ping 8.8.8.8
nslookup kubernetes.default
curl <any-service>

# Verify LoadBalancer services
kubectl get svc -A | grep LoadBalancer

# Check Talos API access
talosctl health --talosconfig talos/clusterconfig/talosconfig
```

## Configuration Changes Applied

✅ Added `upgradeCompatibility: "1.18"` to maintain backward compatibility during upgrade
✅ Increased timeout from 10m to 15m for better observability
✅ No CiliumNetworkPolicy resources in cluster (no breaking policy changes to worry about)

## Key Safety Features

- Current Talos workaround (`hostLegacyRouting: true`) remains in place
- Non-exclusive CNI mode preserved for Multus compatibility
- L2 announcements with DSR loadbalancing maintained
- Flux automatic rollback configured (7 retries)

## Notes

- Target version: 1.19.0 (stable, released Feb 4, 2026)
- Expected upgrade time: 10-15 minutes
- Network disruption: Minimal (rolling update)
- If failure occurs: Network restoration within 1-2 minutes via rollback

## Breaking Changes in 1.19.0

⚠️ **Network Policy Scope**: Selectors now default to local cluster only
- Impact: Minimal (no CiliumNetworkPolicy resources found in cluster)
- No action required

## References

- [Cilium 1.19.0 Release Notes](https://github.com/cilium/cilium/releases/tag/v1.19.0)
- [Cilium Upgrade Guide](https://docs.cilium.io/en/stable/operations/upgrade/)
- [Talos Issue #10002](https://github.com/siderolabs/talos/issues/10002) (hostLegacyRouting requirement)
