#!/bin/sh
set -x
# Set the namespace and pod name for garage
NAMESPACE="database"
POD=$(kubectl get pods -n database -l app.kubernetes.io/controller=garage -o json | jq -r '.items[0].metadata.name')
GARAGE_CMD="kubectl exec -n $NAMESPACE $POD -- ./garage"

$GARAGE_CMD key import -n cluster-user --yes "$GARAGE_USER_CLUSTER_USER" "$GARAGE_PASSWORD_CLUSTER_USER" || true
$GARAGE_CMD key allow --create-bucket cluster-user
$GARAGE_CMD key import -n mysql --yes "$GARAGE_USER_MYSQL" "$GARAGE_PASSWORD_MYSQL" || true
$GARAGE_CMD bucket create mysql || true
$GARAGE_CMD bucket allow --read --write --owner mysql --key mysql
$GARAGE_CMD bucket allow --read --write mysql --key cluster-user
$GARAGE_CMD key import -n postgres --yes "$GARAGE_USER_POSTGRES" "$GARAGE_PASSWORD_POSTGRES" || true
$GARAGE_CMD bucket create postgres || true
$GARAGE_CMD bucket allow --read --write --owner postgres --key postgres
$GARAGE_CMD bucket allow --read --write postgres --key cluster-user