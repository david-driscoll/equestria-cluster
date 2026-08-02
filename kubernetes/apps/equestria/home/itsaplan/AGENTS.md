# itsaplan

Self-hosted project management / issue tracking, migrated from the upstream
`docker-compose.yml` at [croffasia/itsaplan](https://github.com/croffasia/itsaplan)
v0.5.0 (AGPL-3.0). Four processes — `api`, `worker`, `bot`, `web` — plus a MinIO
object store for issue attachments.

## THE BLOCKER: upstream publishes no images

This is the one thing that separates itsaplan from every other app in this
directory, and it is not a detail.

- `.github/workflows/release.yml` upstream is release-please plus a `release`
  branch mirror. There is **no docker build and no registry push**. `ghcr.io`
  appears zero times in the repository.
- All four services are `build:` from the checkout in `docker-compose.yml`.
- `web` is worse than the other three: `apps/web/Dockerfile` takes
  `NEXT_PUBLIC_API_URL` as a **build arg** and Next.js inlines it into the
  browser bundle. The image is therefore bound to the api origin it was built
  for. Even a hypothetical upstream image could not be used unmodified.

**Nothing in this directory can reconcile until the four
`ghcr.io/david-driscoll/itsaplan-*` images exist.** That is why
`../kustomization.yaml` does not yet list `./itsaplan/ks.yaml`.

## How the images get built

A small standalone repo, `david-driscoll/itsaplan-images`, modelled on the
existing `david-driscoll/github-runner-az-dotnet` (the estate already builds and
publishes `github-arc-runner-az-dotnet` this way, so this is not a new pattern).
It holds no application source — only a pinned upstream version and a workflow.

```
itsaplan-images/
  versions.env                     ITSAPLAN_VERSION=v0.5.0
  .github/workflows/build.yaml
```

`versions.env`:

```sh
# renovate: datasource=github-releases depName=croffasia/itsaplan
ITSAPLAN_VERSION=v0.5.0
```

`.github/workflows/build.yaml` (GitHub-hosted runners — the estate's own ARC
runners are `containerMode: kubernetes` and have no docker daemon):

```yaml
name: build
on:
  push: { branches: [main], paths: [versions.env, .github/workflows/build.yaml] }
  workflow_dispatch:
permissions: { contents: read, packages: write }
jobs:
  build:
    runs-on: ubuntu-latest
    strategy:
      matrix:
        app: [api, worker, bot, web]
    steps:
      - uses: actions/checkout@v5
      - id: ver
        run: cat versions.env >> "$GITHUB_OUTPUT"
      - uses: actions/checkout@v5
        with:
          repository: croffasia/itsaplan
          ref: ${{ steps.ver.outputs.ITSAPLAN_VERSION }}
          path: src
      - uses: docker/setup-qemu-action@v3
      - uses: docker/setup-buildx-action@v3
      - uses: docker/login-action@v3
        with:
          registry: ghcr.io
          username: ${{ github.actor }}
          password: ${{ secrets.GITHUB_TOKEN }}
      - uses: docker/build-push-action@v6
        with:
          context: src
          file: src/apps/${{ matrix.app }}/Dockerfile
          push: true
          # web only: the api origin is inlined into the browser bundle here.
          build-args: |
            NEXT_PUBLIC_API_URL=https://itsaplan-api.<ROOT_DOMAIN>
          tags: |
            ghcr.io/david-driscoll/itsaplan-${{ matrix.app }}:${{ steps.ver.outputs.ITSAPLAN_VERSION }}
```

### Who rebuilds, and on what trigger

1. Upstream cuts a release. Renovate (already configured org-wide via
   `local>david-driscoll/.github:renovate-config`) sees the `github-releases`
   annotation in `versions.env` and opens a PR in `itsaplan-images` bumping
   `ITSAPLAN_VERSION`.
2. Merging that PR **is** the rebuild trigger — the workflow pushes four new
   tags to `ghcr.io`.
3. Renovate in *this* repo then sees the new tag on the four
   `ghcr.io/david-driscoll/itsaplan-*` packages through the ordinary `docker`
   datasource, and opens the HelmRelease bump PR here with a pinned digest, like
   every other image in the estate.

Two PRs per upstream release, both reviewable, no manual step, no cron nobody
watches.

### Why not the estate's zot

`docker/celestia/zot` and `kube-system/registry` are both configured purely as
**pull-through mirrors**: `extensions.sync` with `onDemand: true` against
docker.io/ghcr.io/quay.io/etc., no `auth` block and no `accessControl`, and a
retention policy (`deleteUntagged: true`, `gc: true`) tuned for a cache rather
than an artifact store. They are also behind the internal Gateway's
`internal-network` ipAllowList, so a GitHub-hosted runner cannot reach them at
all. Publishing to ghcr.io keeps working *with* them — the mirror proxies
ghcr.io, so in-cluster pulls still get cached.

### Why not build in-cluster (kaniko / buildkit Job)

Worse on every axis that matters here. The ARC runners are
`containerMode: kubernetes` with no docker daemon, so it needs a
privileged-or-rootless build path the estate does not have today; a
`bun install` + `next build` is a heavy job competing with real workloads on
Talos nodes; registry push credentials would have to live in-cluster; and
Renovate cannot see any of it, so the rebuild trigger degrades to a CronJob with
no PR trail. The only thing it buys is independence from GitHub Actions, which
this estate already depends on.

### Renovate visibility, stated plainly

The estate convention — digest-pinned upstream images that Renovate updates — is
**preserved from this repo's point of view**: these are ordinary published
images and Renovate tracks them normally. What changes is that the estate is the
publisher, and an extra Renovate-driven PR upstream of that (in
`itsaplan-images`) is what produces a new tag. If `itsaplan-images` is ever
deleted or its workflow breaks, this app silently stops receiving updates and
nothing here will say so.

### Changing the api origin means rebuilding

`API_URL` in `helmrelease.yaml` and `NEXT_PUBLIC_API_URL` in the web build arg
are the same value and must be changed together. Editing only the HelmRelease
leaves the browser bundle pointing at the old origin, and the symptom is a web
UI that loads and then fails every XHR with a CORS error.

## First deploy

1. Create the 1Password item **`Itsaplan`** with five fields, each
   `openssl rand -base64 32`:
   `better auth secret`, `app encryption key`, `worker internal token`,
   `s3 access key id`, `s3 secret access key`.
   `app encryption key` is effectively **write-once**: it encrypts stored
   AI-provider credentials with AES-256-GCM and there is no re-wrap path, so
   changing it makes every stored credential undecryptable.
2. Run the postgres generator so the `itsaplan` role, database and push secret
   exist (`task update`, or `dotnet run .mise/tasks/do-update.cs`). The
   `components/postgres` entry in `ks.yaml` is what it keys off.
3. Uncomment `../../../../components/volsync-restore` in `ks.yaml` for the
   first reconcile only — see the comment there for why, and remove it once
   `kubectl -n equestria get replicationdestination itsaplan-dst` reports a
   `lastSyncTime`.
4. Uncomment `./itsaplan/ks.yaml` in `../kustomization.yaml`.
5. After the first account is created, set the instance registration mode to
   `closed` (or `invite`) in god mode. It defaults to **open**.

## Storage note

`snapshotMaxCount` is `5` cluster-wide in `longhorn-system/longhorn/values.yaml`
and is **sticky per-volume at creation time**, so the `itsaplan` volume inherits
5 and keeping it is not a later decision. The steady-state ReplicationSource
runs once daily with `copyMethod: Snapshot`, creating and releasing one snapshot
per run, so 5 is ample — but if this volume is ever put on a tighter schedule,
or if manual snapshots are taken alongside the backup, that ceiling is what will
deadlock it. Two volumes hit exactly that in 2026-07.
