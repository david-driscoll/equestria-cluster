// REVIEWABLE COPY — NOT AN INPUT TO ANYTHING.
//
// The config Flux actually applies lives in secret.sops.yaml, inside a
// values.yaml whose server.ha.config holds this same HCL with real values
// substituted for the two <<...>> placeholders below. It is encrypted because
// OpenBao must not depend on a SecretStore in order to boot — see the comment
// block in helmrelease.yaml.
//
// This file exists so the config is reviewable in a diff. Keep the two in sync;
// nothing enforces that automatically.

ui = true

listener "tcp" {
  address     = "0.0.0.0:8200"
  // TLS terminates at the internal gateway, and this listener is only reachable
  // in-cluster. Matches how the other kube-system services here are exposed.
  tls_disable = true
}

storage "postgresql" {
  // openbao role in the shared CNPG cluster (database namespace), created by
  // `task update` via components/postgres. Host is the read-write service.
  connection_url = "<<POSTGRES_CONNECTION_URL>>"
  table          = "openbao_kv_store"

  // HA through Postgres advisory locking rather than Raft. OpenBao creates
  // both tables on first start unless skip_create_table is set.
  ha_enabled = "true"
  ha_table   = "openbao_ha_locks"
}

seal "transit" {
  // bao-transit on alpha-site, over Tailscale. This is what makes the pods
  // self-unseal on restart — and what makes alpha-site a hard dependency for
  // starting (not for serving; already-unsealed pods keep working).
  // See vault/bootstrap/RUNBOOK.md, Scenario A.
  address    = "http://<<ALPHA_SITE_TAILSCALE_IP>>:8200"
  token      = "<<TRANSIT_TOKEN>>"
  key_name   = "openbao-equestria-unseal"
  mount_path = "transit/"

  // The transit token should be an orphan periodic token so it survives its
  // parent expiring and can be renewed indefinitely — per the OpenBao transit
  // seal docs.
  disable_renewal = "false"
}

// Lets OpenBao label its own pods with active/standby so the chart's
// -active and -standby Services route correctly.
service_registration "kubernetes" {}
