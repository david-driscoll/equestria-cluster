# Kubernetes Apps — Namespace Reference

> Last updated: 2026-05-24

All applications deployed in the cluster, organized by namespace. Covers both the app inventory (what's deployed) and namespace profiles (how each namespace is structured and wired).

## Namespace Directory

| Namespace | Purpose | Apps | Key Dependencies |
|-----------|---------|------|-----------------|
| **equestria** | User-facing applications (7 groups) | Home, Media, PVR, Downloads, Games, IDP, Shared | postgres, valkey, volsync, network |
| **database** | Data storage layer | PostgreSQL (CNPG), Valkey, Neo4j | longhorn-system, cloudnative-pg |
| **network** | Networking & ingress | Traefik, external-dns, k8s-gateway, CrowdSec | cert-manager |
| **kube-system** | Kubernetes infrastructure | cilium, coredns, 1password, external-secrets, reloader, reflector | — |
| **observability** | Monitoring & logging | Prometheus, Grafana, Loki, Alloy, Alertmanager, Blackbox | — |
| **flux-system** | GitOps controllers | Flux Operator, Flux Instance, Weave GitOps | — |
| **cert-manager** | TLS certificates | cert-manager, trust-manager, issuers | — |
| **volsync-system** | PVC backup & replication | Volsync | — |
| **longhorn-system** | Distributed block storage | Longhorn | — |
| **openebs-system** | Local block storage | OpenEBS CSI | — |
| **nfs-system** | NFS shared storage | NFS CSI driver | — |
| **tailscale-system** | Tailnet integration | Tailscale operator, golink, services | — |
| **system-upgrade** | Node upgrades | Talos/K8s upgrade controllers | — |
| **pulumi** | Infrastructure automation | Pulumi operator + stacks (Authentik, Cloudflare, Unifi, backups) | — |
| **cloudnative-pg** | PostgreSQL operator | CloudNative-PG | — |
| **github-actions** | CI/CD runners | ARC controller + runner scale sets | — |

---

## App Directory Pattern

Every app follows this canonical structure:

```
kubernetes/apps/<NAMESPACE>/<APP>/
├── ks.yaml              # Flux Kustomization: dependsOn, components, postBuild vars
├── kustomization.yaml   # Kustomize manifest: lists all resources for this app
├── helmrelease.yaml     # HelmRelease (almost always bjw-s app-template)
├── externalsecret.yaml  # Pulls secrets from 1Password Connect
├── definition.yaml      # (optional) ApplicationDefinition CRD → Authentik + Gatus
├── resources/           # (optional) Config files bundled as ConfigMaps
└── *.sops.yaml          # (optional) SOPS-encrypted static secrets
```

---

## Deployment Order

Flux respects `dependsOn` chains. Approximate startup order:

```
1. flux-system          GitOps controllers themselves
2. kube-system          Cilium (CNI), CoreDNS, external-secrets, 1password
3. cert-manager         TLS issuer infrastructure
4. cloudnative-pg       PostgreSQL operator (CRDs must exist before cluster)
5. network              Traefik, external-dns, k8s-gateway
6. database             PostgreSQL cluster, Valkey, Neo4j
7. observability        Prometheus, Loki, Grafana
8. volsync-system       Backup operator
9. tailscale-system     Tailnet services
10. pulumi              Authentik provisioning, network automation
11. equestria           All user apps (after all of the above)
```

---

## Namespace Profiles

### equestria — User Applications

**Path:** `kubernetes/apps/equestria/`  
**Components used:** postgres (16+ apps), volsync (53+ apps), tailscale (many), ingress variants  
**Storage:** Longhorn (primary PVCs) + NFS (shared media/home mounts)

#### equestria/home — Productivity & Personal Apps

| App | Purpose | Components |
|-----|---------|------------|
| immich | Photo library with ML | postgres, volsync, tailscale |
| n8n | Workflow automation | postgres, volsync, tailscale, failover |
| vikunja | Todo & kanban | postgres |
| obsidian-sync | Markdown note sync | volsync |
| tandoor | Recipe management | postgres |
| freshrss | RSS reader | postgres |
| dynacat | File browser | postgres |
| searxng | Metasearch engine | — |
| super-productivity | Time tracking | — |
| windmill | Low-code automation | postgres, valkey |
| meilisearch | Search engine | — |
| rustdesk | Remote desktop server | postgres, valkey |
| glance | Dashboard | — |

Inactive (commented out): karakeep, outline, collabora, technitium

#### equestria/media — Media Management

| App | Purpose |
|-----|---------|
| Plex | Media server |
| Radarr | Movie management |
| Sonarr | TV episode management |
| Readarr | Book management |
| Tdarr | Media transcoding (high churn) |
| Seerr | Request management |
| Other media services | — |

#### equestria/pvr — PVR & Recording

| App | Purpose |
|-----|---------|
| TVHeadend | TV recording backend |
| Dispatcharr | Recording scheduler (high churn) |
| Dispatcharr-addons | Scheduler extensions |
| xcproxy | Proxy with TMDB cache |

#### equestria/downloads — Download Clients

| App | Purpose |
|-----|---------|
| Transmission | Torrent client |
| Sabnzbd | Usenet client |
| Lidarr | Music management |

#### equestria/games — Gaming Services

Game servers and utilities. See `kubernetes/apps/equestria/games/` for current inventory.

Notable: **questarr** (highest-churn app in the repo — 55 commits/90d), **habitica**.

#### equestria/books — Book Services

Currently disabled in kustomization.

#### equestria/idp — Identity Provider

| App | Purpose |
|-----|---------|
| Authentik | SSO & OAuth2/OIDC provider |

App registrations are provisioned automatically by Pulumi reading `ApplicationDefinition` CRDs.

#### equestria/dns — DNS Services

Currently disabled. Reserved for future DNS automation.

#### equestria/shared — Namespace-Wide Resources

- Namespace-wide Traefik middleware
- Shared PrometheusRules (via `alerts` component)
- Common ConfigMaps

---

### database — Data Storage

**Path:** `kubernetes/apps/database/`

#### postgres — CloudNative-PG Cluster

```
database/postgres/
├── app/
│   ├── resources/values.yaml       # Cluster definition (PostgreSQL 17.5, standalone)
│   ├── users.yaml                  # Generated: all roles
│   ├── users/                      # Generated: per-user YAML files
│   │   ├── immich-postgres.yaml
│   │   ├── n8n-postgres.yaml
│   │   └── ...
│   └── passwords.sops.yaml         # Generated + encrypted: all passwords
└── postgres-push-secrets/          # Syncs passwords to 1Password
```

- **Version:** PostgreSQL 17.5, `mode: standalone`
- **Backups:** WAL + scheduled snapshots → Backblaze B2
- **Users:** Auto-provisioned by `kubernetes/components/postgres/Update.cs`
- **Count:** 16+ app users + postgres-superuser + postgres-user defaults

#### valkey — Redis-Compatible Cache

Used by: immich, n8n, rustdesk, windmill.  
No persistent storage (transient cache).

#### neo4j — Graph Database

Used for knowledge graph queries.  
Persistent storage on Longhorn. Single instance (not HA).

---

### network — Routing & Ingress

**Path:** `kubernetes/apps/network/`

| App | Purpose |
|-----|---------|
| traefik | API Gateway, TLS termination, middleware, HTTPRoute |
| traefik-crds | Traefik CRD definitions |
| external-dns | Cloudflare DNS record automation |
| k8s-gateway | In-cluster DNS for `${INTERNAL_DOMAIN}` |
| certificates | TLS cert resources |
| cloudflare-tunnel | Inbound external traffic via Cloudflare (no open ports) |
| crowdsec | IDS/IPS integration with Traefik |
| librespeed | Internet speed test |
| openspeedtest | Alternative speed test |
| whoami | Ingress debug service |
| secrets | Encrypted Cloudflare tokens, TLS certs |

**Routing patterns:**
- Internal: `<app>.${INTERNAL_DOMAIN}` → Traefik → pod (k8s-gateway provides DNS)
- External: `<app>.${EXTERNAL_DOMAIN}` → Cloudflare → cloudflared tunnel → Traefik → pod
- Tailnet: `<app>.${TAILSCALE_DOMAIN}` → Tailscale operator → pod

---

### kube-system — Cluster Infrastructure

**Path:** `kubernetes/apps/kube-system/`

| App | Role | Criticality |
|-----|------|-------------|
| cilium | CNI, network policy, eBPF | Critical |
| coredns | Cluster DNS | Critical |
| etcd | Kubernetes API backend | Critical |
| external-secrets | ExternalSecret operator | Critical |
| 1password (onepassword-connect) | Secret injection server | Critical |
| cert-manager | TLS certificate automation | High |
| reloader | Restart pods on secret/configmap change | High |
| reflector | Replicate secrets across namespaces | Medium |
| spegel | P2P image registry mirror | Medium |
| metrics-server | Pod/node resource metrics for HPA | Medium |
| headlamp | Kubernetes web dashboard | Low |
| snapshot-controller | Volume snapshot support | Medium |
| multus | Multi-NIC pod support | Low |

---

### observability — Monitoring & Logging

**Path:** `kubernetes/apps/observability/`

| App | Purpose |
|-----|---------|
| prometheus (kube-prometheus-stack 85.2.0) | Metrics collection + Alertmanager + Prometheus UI |
| alertmanager | Alert routing (also exposes `https://alertmanager.driscoll.tech/api/v2/alerts`) |
| grafana | Dashboards + visualization |
| grafana-operator | Dashboard/alert rules as code (GrafanaDashboard CRDs) |
| loki | Log aggregation |
| alloy | Observability distributor (collects logs, metrics, traces) |
| thanos | Long-term Prometheus metric storage |
| blackbox-exporter | HTTP/TCP/SSH/DNS probing (used for Tailnet node health checks) |
| silences | Declarative alert silences |
| nut-exporter | UPS/power monitoring |
| smartctl-exporter | Disk SMART health metrics |
| speedtest-exporter | Internet bandwidth metrics |
| unpoller | UniFi network equipment metrics |
| glance-k8s | Kubernetes events dashboard |
| crds | ApplicationDefinition CRD + other CRDs |
| priority-class | Pod QoS priority classes |

**Data flow:**
```
Metrics:  Prometheus scrapes → Thanos stores → Grafana visualizes
Logs:     Alloy collects → Loki stores → Grafana queries
Probes:   Blackbox → Prometheus → Alertmanager → receivers
Alerts:   Alertmanager API at https://alertmanager.driscoll.tech/api/v2/alerts
```

---

### flux-system — GitOps Controllers

**Path:** `kubernetes/apps/flux-system/`

| App | Purpose |
|-----|---------|
| flux-operator (0.50.0) | Manages Flux installation and upgrades |
| flux-instance | Source, Kustomize, Helm, Notification controllers |
| weave | Weave GitOps UI (visual resource browser) |

---

### cert-manager — TLS Certificates

**Path:** `kubernetes/apps/cert-manager/`

| App | Purpose |
|-----|---------|
| cert-manager (v1.20.2) | Issues and renews TLS certificates from Let's Encrypt |
| cert-issuers | `letsencrypt-staging` + `letsencrypt-production` ClusterIssuers (DNS01 via Cloudflare) |
| trust-bundles | CA bundle distribution to pods |
| trust-manager | Operator for CA cert propagation |

---

### volsync-system — PVC Backup

**Path:** `kubernetes/apps/volsync-system/`

Volsync controller manages `ReplicationSource` and `ReplicationDestination` CRs.  
Apps opt in via the `volsync` component in `ks.yaml`.  
Backs up to Backblaze B2 using restic.

---

### Storage Namespaces

| Namespace | Storage type | Primary use |
|-----------|-------------|-------------|
| `longhorn-system` | Distributed block (3-replica HA) | Databases, stateful app PVCs |
| `openebs-system` | Local hostpath volumes | Single-node workloads |
| `nfs-system` | NFS CSI provisioner | Shared media/home storage from TrueNAS |

---

### tailscale-system — Tailnet Integration

**Path:** `kubernetes/apps/tailscale-system/`

```
tailscale-system/
├── operator/        # Tailscale operator
├── authkey/         # Tailscale auth credentials
├── resources/       # Connector, DNS config, proxy, inbound/outbound
├── golink/          # Tailscale golink shortlink server
└── services/        # ExternalName services for external nodes (generated)
    ├── celestia.yaml    # Server: celestia  | Dockge, Proxmox, PBS
    ├── luna.yaml        # Server: luna      | Dockge, Proxmox, PBS
    ├── skystar.yaml     # Server: skystar   | Dockge, Proxmox
    ├── as.yaml          # Server: as        | Dockge
    ├── alpha-site.yaml  # Server: alpha-site| Proxmox
    └── twilight-sparkle.yaml  # Server: twilight-sparkle | Proxmox
```

`services/*.yaml` files are **generated by `Update.cs`** — do not edit manually. Each file contains:
- `ExternalName` Service for each service type (Dockge, Proxmox, PBS)
- `Probe` for HTTP/SSH health monitoring via blackbox-exporter
- `PrometheusRule` for alert rules

---

### system-upgrade — Node Upgrades

**Path:** `kubernetes/apps/system-upgrade/`

Manages Talos Linux and Kubernetes version upgrades via custom upgrade plan CRs. Upgrades are triggered by updating `versions.env` and running `task talos:upgrade-node` / `task talos:upgrade-k8s`.

---

### pulumi — Infrastructure Automation

**Path:** `kubernetes/apps/pulumi/`

| Stack | Purpose |
|-------|---------|
| applications | Reads `ApplicationDefinition` CRDs, provisions Authentik OAuth clients + proxy configs |
| authentik | Authentik configuration (groups, policies, flows) |
| backups | Backup scheduling configuration |
| gulf-of-mexico | Internal network config |
| home-operations | Home automation infrastructure |
| ocracoke | Infrastructure config |
| unifi-network | UniFi controller configuration |
| secrets | Pulumi state credentials |

The `applications` stack is the bridge between the `ApplicationDefinition` CRDs in `observability/crds/` and Authentik — Pulumi reconciles new/changed definitions into live Authentik apps automatically.

---

### cloudnative-pg — PostgreSQL Operator

**Path:** `kubernetes/apps/cloudnative-pg/`

The CNPG operator itself (CRDs + controller). The actual PostgreSQL cluster instance lives in `database/postgres/`. The operator must be running before the cluster resource can be created.

---

### github-actions — CI/CD Runners

**Path:** `kubernetes/apps/github-actions/`

| App | Purpose |
|-----|---------|
| controller | Actions Runner Controller (ARC) |
| runners | Runner scale sets (ephemeral pods) |
| base | Base runner image config (high churn — 43 commits/90d) |
| 1password | GitHub Actions secrets from 1Password |

---

## Common App Patterns

### Database App

```yaml
# ks.yaml
components:
  - ../../../../components/postgres
postBuild:
  substitute:
    APP: myapp
    NAMESPACE: equestria
    POSTGRES_NAME: myapp-postgres
```

### Backup-Enabled App

```yaml
components:
  - ../../../../components/volsync
postBuild:
  substitute:
    VOLSYNC_ACCESSMODES: ReadWriteMany
    VOLSYNC_PUID: "1000"
    VOLSYNC_PGID: "1000"
```

### Tailnet + Internal Ingress App

```yaml
components:
  - ../../../../components/tailscale
  - ../../../../components/ingress/internal
postBuild:
  substitute:
    TAILSCALE_HOST: myapp
```

### Accessing Apps

| Method | Pattern | Example |
|--------|---------|---------|
| Internal web | `https://<app>.${INTERNAL_DOMAIN}` | `https://n8n.internal.driscoll.tech` |
| Tailnet | `https://<host>.${TAILSCALE_DOMAIN}` | `https://n8n.<tailnet>` |
| In-cluster | `<svc>.<ns>.svc.cluster.local` | `postgres-rw.database.svc.cluster.local` |
| External | `https://<app>.${EXTERNAL_DOMAIN}` | `https://photos.driscoll.tech` |

## File Counts

- **Namespaces:** 16
- **App directories:** ~80+
- **HelmReleases:** ~60
- **ExternalSecrets:** ~40
- **ApplicationDefinition CRDs:** ~15
- **Update.cs scripts:** 3 (postgres, tailscale-services, common)
