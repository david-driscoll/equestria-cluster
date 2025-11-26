Grafana Dashboard Ownership and Deployment
=======================================

This cluster follows a Grafana-first dashboard ownership model.

Key rules:
- Grafana is the canonical owner of dashboards. Prometheus operator dashboard provisioning is disabled: `grafana.forceDeployDashboards: false` in the Prometheus HelmRelease.
- Grafana sidecar is configured to discover dashboards only in the `observability` namespace and to look for `grafana_dashboard: "true"` labels on `GrafanaDashboard` CRDs.
- Dashboards are organized via the `grafana_folder` annotation (e.g. `Network`, `Kubernetes`, `Data`, `Media`, `System`).
- App-level or exporter dashboards should be added as `GrafanaDashboard` CRDs under `kubernetes/apps/observability/exporters/*` and include the label and annotation.

How to add a dashboard:
1. Add a `GrafanaDashboard` CRD under `kubernetes/apps/observability/exporters/<app>/dashboards.yaml`.
2. Include `metadata.labels: grafana_dashboard: "true"` and `metadata.annotations: grafana_folder: "<Folder>"`.
3. Ensure the datasource in the dashboard uses the configured Grafana Prometheus datasource (`Prometheus`).
4. Validate using `flux-local test --enable-helm`.

Why this approach?
- Centralizes dashboards and folder organization.
- Prevents duplicate dashboards from multiple sources (operator and grafana). 
- Offers clear ownership for app maintainers, who add dashboards via CRDs in the observability namespace.

Rollback:
- Re-enable Prometheus operator dashboard provisioning by setting `grafana.forceDeployDashboards: true` in `kubernetes/apps/observability/prometheus/app/helmrelease.yaml`.
