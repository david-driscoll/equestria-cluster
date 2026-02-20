# Glance Dashboard Community Extensions & Widgets

A comprehensive catalog of community-created widgets and extensions available for the Glance dashboard. This document provides configuration details, requirements, and descriptions for integrating various homelab services with your Glance dashboard.

---

## Table of Contents

- [Authentication & Identity](#authentication--identity)
- [Media Server Widgets](#media-server-widgets)
- [Infrastructure & Extensions](#infrastructure--extensions)
- [Status Monitoring](#status-monitoring)
- [Arr Stack (Sonarr/Radarr/Lidarr)](#arr-stack)
- [Network & Security](#network--security)
- [Backup & Storage](#backup--storage)
- [Entertainment & Fun](#entertainment--fun)
- [Media Libraries](#media-libraries)
- [Development & Code](#development--code)

---

## Media Server Widgets

### Tautulli Stats

Display usage statistics, recently added content, and playback information from Tautulli/Plex.

**Type:** `custom-api`
**Cache:** 5m-60m
**Authentication:** `TAUTULLI_API_KEY`
**URL Variables:**

- `TAUTULLI_IP` - Local IP address of Tautulli instance
- `TAUTULLI_PORT` - Port of Tautulli instance
- `TAUTULLI_API_KEY` - API key from Settings → Web Interface → API
- `SERVER_ID` - Unique ID for Plex server

**Available Views:**

- Recently Added (with scroll carousel)
- Now Playing (active sessions)
- Recently Watched (with time tracking)
- Top Movies (with duration stats)
- Top Platforms (device statistics)
- Top Shows (with watch history)
- Top Users (with usage metrics)

**Configuration:** Supports both small and full size layouts, collapsible lists, and icon styling options. Includes custom CSS filter options for platform icons.

---

### Media Server History (Multi-Service)

Display playback history from Plex, Tautulli, Jellyfin, or Emby.

**Type:** `custom-api`
**Cache:** 5m
**Supported Services:** Plex, Tautulli, Jellyfin, Emby
**Features:**

- Show recently played items with thumbnail previews
- Filter by media type (Movies, Episodes, Music)
- Display user who played content
- Configurable compact or full layout
- Optional thumbnail aspect ratio control

**Environment Variables:**

- `PLEX_URL` / `PLEX_TOKEN`
- `TAUTULLI_URL` / `TAUTULLI_KEY`
- `JELLYFIN_URL` / `JELLYFIN_KEY`
- `EMBY_URL` / `EMBY_KEY`

**Options:**

```yaml
history-length: "10" # Items to fetch
media-types: "movie,episode" # Content types to fetch
small-column: false # Layout mode
compact: true # Compact vs spread-out display
show-thumbnail: false # Display cover art
show-user: true # Show username
time-absolute: false # Absolute vs relative time
```

---

### Media Server Playing (Multi-Service)

Display currently playing/streaming content across Plex, Tautulli, Jellyfin, and Emby.

**Type:** `custom-api`
**Cache:** 5m
**Supported Services:** Plex, Tautulli, Jellyfin, Emby
**Features:**

- Real-time active sessions display
- Play state indicators (playing/paused)
- Progress bar with remaining time
- Optional thumbnail previews
- User-per-session display
- Play/pause state indicators

**Options:**

```yaml
media-server: "plex"
base-url: ${PLEX_URL}
api-key: ${PLEX_TOKEN}
play-state: "indicator" # text or indicator
show-thumbnail: false # Display cover art
show-paused: false # Include paused sessions
show-progress-bar: false # Show playback progress
show-progress-info: true # Show time remaining
```

---

### Jellyfin Latest / Next Up

Display latest content additions or what's next to watch in Jellyfin.

**Type:** `custom-api`
**Cache:** 5m
**Authentication:** Jellyfin API Key
**Features:**

- Latest content in specific libraries
- Next Up resume queue
- Show/Movie/Music support
- Thumbnail display with progress indicators
- User-specific content

**Mode Options:**

- `latest` - Show newly added library content
- `nextup` - Show resume queue for continuing content

**Environment Variables:**

- `JELLYFIN_URL` - Jellyfin instance URL
- `JELLYFIN_KEY` - API key from Administration → Dashboard → API Keys

**Required Options:**

```yaml
base-url: ${JELLYFIN_URL}
api-key: ${JELLYFIN_KEY}
user-name: "yourUserName" # Jellyfin username
library-name: "Shows" # Library to display (for latest mode)
mode: "latest" # latest or nextup
```

---

### Jellyfin/Emby Stats

Display library statistics for Jellyfin or Emby.

**Type:** `custom-api`
**Cache:** 1d (configurable)
**Features:**

- Total movie count
- TV shows count
- Episode count
- Music/song count
- Clean grid layout display

**Environment Variables:**

- `JELLYFIN_URL` / `JELLYFIN_API_KEY`
- `EMBY_URL` / `EMBY_API_KEY`

---

## Infrastructure & Monitoring

### Proxmox VE Stats

Display Proxmox Virtual Environment cluster status and resource information.

**Type:** `custom-api`
**Cache:** 1m
**Features:**

- Nodes online/total count
- LXC containers status
- QEMU VMs status
- Storage availability status
- Custom PVEAPIToken authentication

**Environment Variables:**

- `PROXMOXVE_URL` - Proxmox URL (https://ip:8006)
- `PROXMOXVE_KEY` - API token (format: `<user>@pam!<tokenID>=<secret>`)

**Token Setup Instructions:**

1. Proxmox Portal → Datacenter → Permissions → Groups → Create "api-ro-users"
2. Add Group Permission (PVEAuditor role)
3. Permissions → Users → Add user (associate with group)
4. Permissions → API Tokens → Add token
5. Add API Token Permission with PVEAuditor role

---

### Proxmox Detailed Resources

Detailed resource view for Proxmox VMs, LXC containers, and storage.

**Type:** `custom-api`
**Cache:** 1m
**Features:**

- Individual resource status
- RAM usage with progress bars
- CPU load percentages
- Disk usage with progress bars
- One-click toggleable charts

**Options:**

```yaml
cpu_graph: true # Show CPU usage charts
disk_graph: true # Show disk usage charts
ram_graph: true # Show RAM usage charts
```

---

### Grafana

Display Grafana metrics and data source queries.

**Type:** `custom-api`
**Method:** POST
**Cache:** 5m
**Features:**

- Execute datasource queries
- Display Prometheus/Mimir/Loki data
- Direct dashboard links
- Customizable queries and colors

**Authentication:** Service Account with DataSource Reader permissions
**Environment Variables:**

- `GRAFANA_URL` - Grafana instance URL
- `GRAFANA_TOKEN` - Service Account token (Bearer)

**Configuration Example:**

```yaml
queries:
    - datasource:
          type: prometheus
          uid: grafanacloud-prom
      expr: group(up{job=~".*node_exporter.*"}) by (instance)
      legendFormat: "{{instance}}"
```

---

## Arr Stack

### Arr Releases (Sonarr/Radarr/Lidarr)

Unified widget for displaying upcoming, recent, and missing content across all \*arr services.

**Type:** `custom-api`
**Cache:** Configurable per type
**Supported Services:** Sonarr, Radarr, Lidarr
**Features:**

- Upcoming releases
- Recently grabbed content
- Missing items (available but not grabbed)
- Cover art with customizable size
- Optional proxy for cover images

**Environment Variables:**

- `*ARR_API_URL` - Service API URL (e.g., `http://192.168.1.36:8989`)
- `*ARR_KEY` - API key from Settings → General → Security
- `*ARR_URL` - (optional) External URL for links

**Configuration Options:**

```yaml
service: sonarr # sonarr, radarr, or lidarr
type: upcoming # upcoming, recent, or missing
size: medium # small, medium, large, huge
show-grabbed: true # Show grabbed/missing status
interval: 30 # Days ahead (for upcoming) or within (back)
sort-time: desc # asc or desc
cover-proxy: "" # Proxy URL for covers
```

---

### Prowlarr Indexers

Display the status and count of enabled/disabled indexers in Prowlarr.

**Type:** `custom-api`
**Cache:** 1m
**Features:**

- List of all configured indexers
- Enable/disable status with indicators
- Direct link to Prowlarr UI
- Visual status indicators (✓ enabled, ✗ disabled)

**Environment Variables:**

- `PROWLARR_HOST` - Prowlarr instance hostname/IP (e.g., `prowlarr.equestria.svc.cluster.local:9696`)
- `PROWLARR_API_KEY` - API key from Settings → General → Security

**Required Options:**

```yaml
type: custom-api
title: Prowlarr Indexers
cache: 1m
url: http://prowlarr.equestria.svc.cluster.local:9696/api/v1/indexer
headers:
    Accept: application/json
    X-Api-Key: ${prowlarr_apikey}
```

**Note:** Each indexer row shows the name (clickable link to Prowlarr) and either ✓ or ✗ status based on `enable` field.

---

### Unifi Widgets

Display UniFi network status, client counts, and gateway information.

**Type:** `custom-api`
**Cache:** 1m
**Features:**

- Gateway name and WAN status
- Uptime in days
- Wired client count
- Wireless client count
- Latency information
- Gateway CPU/RAM metrics
- Progress bars for resource usage

**Available Layouts:**

- Small (condensed stats)
- Details (full metrics with latency and IP)
- Progress Bars (visual resource Usage)

**Environment Variables:**

- `UNIFI_CONSOLE_IP` - IP Address of UniFi gateway
- `UNIFI_API_KEY` - Local API key from Site Settings → Control Plane → Integrations
- Must use `allow-insecure: true`

---

### Cloudflare Tunnels

Display your Cloudflare Tunnel ingress routes and their target services.

**Type:** `custom-api`
**Cache:** 4h
**Features:**

- List of active tunnel ingress hostnames
- Target service endpoints
- Direct links to tunnel hostnames
- Organized clean list layout

**Environment Variables:**

- `CF_ACCOUNTID` - Your Cloudflare Account ID (from Accounts → Account Details)
- `CFTUNNEL_USERNAME` - Tunnel UUID from `cloudflare-tunnel.json`
- `CF_CREDENTIAL` - Cloudflare API token with Tunnel Edit permissions

**Token Setup:**

1. Visit Cloudflare Dashboard → Account → API Tokens → Create Token
2. Minimum permissions: `Account→Cloudflare Tunnel→Edit`
3. Store token securely in 1Password or environment

**Required Options:**

```yaml
type: custom-api
title: Cloudflare Tunnels
cache: 4h
url: https://api.cloudflare.com/client/v4/accounts/${cf_accountId}/cfd_tunnel/${cftunnel_username}/configurations
headers:
    Authorization: Bearer ${cf_credential}
```

**Note:** Configuration is read from the tunnel's ingress rules. Uses Go templating to parse and format the ingress array.

---

## Backup & Storage

### Backrest Job Status

Display restic-based backup job status from Backrest instance.

**Type:** `custom-api`
**Method:** POST
**Cache:** 1m
**Features:**

- Backup job date
- Snapshot ID (truncated)
- Backup size in configurable units
- Success/Error status with icons
- Error message tooltips

**Environment Variables:**

- `BACKREST_URL` - Backrest server URL (e.g., `http://backrest.local:9898`)
- `BACKREST_PLAN_LOCAL` - Plan ID for local backups
- Authentication disabled on Backrest instance required

**Options:**

```yaml
LIMIT: 5 # Maximum snapshots to show
PRECISION: 1 # Decimal precision for sizes
UNIT: GB # Size unit (GB, MB, TB)
BACKREST_PLAN_TITLE: "Local USB" # Display title
```

---

### Proxmox Backup Server Stats

Display backup job status from PBS (Proxmox Backup Server).

**Type:** `custom-api`
**Cache:** 5m
**Features:**

- Backup status and dates
- Job completion status
- Node and datastore usage
- Task monitoring

**Authentication:** `PBS_TOKEN` (PBSAPIToken format)
**Environment Variables:**

- `PBS_HOST` - PBS server hostname/IP
- `PBS_TOKEN` - API token from PBS UI

---

## Authentication & Identity

### Authentik Applications

Display your organization's applications from Authentik with grouped layout and launch links.

**Type:** `custom-api`
**Cache:** 1h
**Features:**

- Grouped applications by category/group
- Direct launch links (`meta_launch_url`)
- Application icons and descriptions
- Responsive card layout with hover effects

**Environment Variables:**

- `AUTHENTIK_API_TOKEN` - API token from Authentik Admin Panel → Tokens & APP passwords
- `ROOT_DOMAIN` - Your root domain for HTTPS base URL

**Required Options:**

```yaml
type: custom-api
title: Applications
url: https://authentik.${ROOT_DOMAIN}/api/v3/core/applications/
cache: 1h
headers:
    Authorization: Bearer ${AUTHENTIK_API_TOKEN}
    Accept: application/json
```

**Note:** Uses Go templating to group results by `group` field and filters only apps with `meta_launch_url` set.

---

## Infrastructure & Extensions

### glance-k8s (Kubernetes Cluster Info)

Display Kubernetes cluster information including node status and application pod visibility.

**Type:** `extension`
**Cache:** 30s-1m
**Features:**

- Node status and resource metrics (CPU, memory)
- Pod/application status across namespaces
- Real-time cluster health
- Cross-cluster support via Tailscale/internal DNS

**Environment Variables:**

- `GLANCE_K8S_HOST` - Service URL or hostname for glance-k8s instances
- For cross-cluster: use Tailscale hostnames like `glance-k8s.sgc.internal`

**Available Endpoints:**

- `/extension/nodes` - Shows node status, resource usage, uptime
- `/extension/apps` - Shows pod status, containers, resource limits

**Configuration:**

```yaml
- type: extension
  url: http://glance-k8s.observability.svc.cluster.local:8080/extension/nodes
  allow-potentially-dangerous-html: true
  cache: 30s
  title: Cluster Nodes

- type: extension
  url: http://glance-k8s.observability.svc.cluster.local:8080/extension/apps
  allow-potentially-dangerous-html: true
  cache: 1m
  title: Cluster Apps
```

**Cross-Cluster Access:**

```yaml
- type: extension
  url: https://glance-k8s.sgc.internal/extension/apps
  allow-insecure: true
  cache: 1m
  title: Remote Cluster Apps
```

**Note:** Requires `allow-potentially-dangerous-html: true` for HTML rendering.

---

## Status Monitoring

### Gatus (Simple)

Unified status display with summary and failed services list.

**Type:** `custom-api`
**Cache:** 1m
**Features:**

- Sites up/down counts
- Overall uptime percentage
- Failed services (with collapsible)
- Success/failure status icons

**Environment Variables:**

- `GATUS_URL` - Gatus instance URL (no trailing slash)

---

### Gatus Monitor (Advanced)

Full-featured Gatus monitor widget with uptime, response times, and condition details.

**Type:** `custom-api`
**Cache:** 5m
**Features:**

- Individual endpoint status
- Uptime percentages
- Response time metrics
- Condition result details (tooltips)
- Custom icons and link URLs
- Compact and full layouts
- Group filtering
- Failing-only mode

**Layout Modes:**

- `full` - Complete endpoint details with stats
- `compact` - Simplified list view

**Options:**

```yaml
base-url: ${GATUS_URL}
style: full # full or compact
duration: 24h # Stat duration (24h, 7d, etc.)
groupFilter: "" # Filter by group (empty = all)
compactMetric: uptime # uptime or response-time
showFailingOnly: false # Show only failing services
showOnlyConfigured: false # Show only configured endpoints
```

**Icon Format Support:**

- Simple Icons: `si:immich`
- Dashboard Icons: `di:it-tools-light`
- Material Design: `mdi:home`
- Selfhosted: `sh:beszel`
- Direct URL: `https://example.com/icon.png`

---

## Media Libraries

### RomM Stats

Display retro game library statistics from RomM (ROM Manager).

**Type:** `custom-api`
**Cache:** 1d
**Features:**

- Platform count
- ROM/game count
- Total filesize (auto-scales TB/GB)

**Environment Variables:**

- `ROMM_URL` - RomM instance URL

**No Authentication Required**

---

### Immich Stats

Display photo/video library statistics from Immich.

**Type:** `custom-api`
**Cache:** 1d
**Features:**

- Photo count
- Video count
- Storage usage in GB

**Authentication:** `x-api-key` header
**Environment Variables:**

- `IMMICH_URL` - Immich server URL
- `IMMICH_API_KEY` - API key from Account Settings → API Keys (requires `server.statistics` permission)

---

## Entertainment & Fun

### Epic Games Free Games

Display currently free games available on the Epic Games Store.

**Type:** `custom-api`
**Cache:** 1h
**Features:**

- Thumbnail preview images
- Game titles
- Free period expiration dates
- Direct links to Epic Games Store
- Horizontal card carousel layout

**No Environment Variables Required**

Public API endpoint (no authentication needed)

**Configuration:**

```yaml
type: custom-api
title: Epic Games Free
cache: 1h
url: https://store-site-backend-static.ak.epicgames.com/freeGamesPromotions?locale=en&country=US&allowCountries=US
```

**Optional Parameters:**

```yaml
locale: en # Language locale
country: US # Country code
allowCountries: US # Allowed countries filter
```

**Note:** Filters to show only games with active promotional offers and $0 price. Uses Go templating to parse product catalog and display promotional windows.

---

### NHL Scores

Display current NHL game scores and matchups.

**Type:** `custom-api`
**Cache:** 5s (real-time updates)
**Features:**

- Today's NHL games
- Team matchups with abbreviations
- Live score updates
- Game state indicators
- Direct links to NHL.com game center

**No Environment Variables Required**

Public NHL API endpoint

**Configuration:**

```yaml
type: custom-api
title: NHL Today
cache: 5s
url: https://api-web.nhle.com/v1/score/now
```

**Note:** Updates every 5 seconds for live game tracking. Shows home vs away teams with current scores.

---

### XKCD Comic

Display the latest XKCD comic with title and alt text.

**Type:** `custom-api`
**Cache:** 2m
**Features:**

- Latest published comic image
- Comic title
- Alt text (hover tooltip)
- Centered responsive layout

**No Environment Variables Required**

Public XKCD API endpoint

**Configuration:**

```yaml
type: custom-api
title: XKCD
cache: 2m
url: https://xkcd.com/info.0.json
```

**Note:** Returns the latest published comic. For specific comics, modify the URL: `https://xkcd.com/<comic_number>/info.0.json`

---

## Development & Code

### GitHub Personal Repos

Display list of your personal GitHub repositories.

**Type:** `custom-api`
**Cache:** 30m
**Features:**

- Repository list with descriptions
- Last update date
- Visibility status (public/private)
- Star count
- Programming language
- Direct links to repos

**Authentication:** GitHub Personal Access Token (Bearer)
**Environment Variables:**

- `Github_Personal_Access_Token` - PAT from GitHub Settings → Developer Settings → Personal Access Tokens

**Token Requirements:**

- Minimum scope: `repo`
- Create at GitHub Settings → Developer Settings → Personal Access Tokens → Tokens (classic)

---

### GitHub Notifications

Display recent GitHub notifications.

**Type:** `custom-api`
**Cache:** 30m
**Features:**

- Issue mentions
- Pull request reviews
- Subscription updates
- Repository activity

**Authentication:** GitHub Personal Access Token
**Environment Variables:**

- `GITHUB_TOKEN` - Personal Access Token

---

## Additional Services

### MediaTracker Upcoming TV Shows

Display upcoming TV shows, movies, or video games from MediaTracker.

**Type:** `custom-api`
**Cache:** 1h
**Features:**

- Horizontal carousel layout
- Poster images
- Release dates
- Episode information (for TV)
- Configurable media types
- Watchlist filtering

**Options:**

```yaml
base-url: ${MEDIATRACKER_URL}
api-key: ${MEDIATRACKER_API_KEY}
media-type: tv # tv, movie, or video_game
only-on-watchlist: "false" # true or false
items: 10 # Number of cards to show
```

**API Token:** Available in MediaTracker UI under Account Name → Application Tokens

---

### SABnzbd Stats

Display SABnzbd usenet download manager statistics.

**Type:** `custom-api`
**Cache:** 30s
**Features:**

- Download speed
- Time remaining
- Queue item count
- Queue size
- Current status (Paused/Downloading)

**Environment Variables:**

- `SABNZBD_URL` - SABnzbd instance URL
- `SABNZBD_API_KEY` - API key from Settings → General

---

## Configuration Best Practices

### Environment Variables

Always use environment variables instead of hardcoding values:

```yaml
url: ${MY_SERVICE_URL}
api-key: ${MY_SERVICE_KEY}
```

Store sensitive data in `.env` file (Docker) or 1Password with ExternalSecrets (Kubernetes).

### Caching

- **5s-30s:** Real-time stats (playback, downloads, transfers)
- **1m:** Frequently changing data (active sessions, network)
- **5m:** Standard monitoring (server stats, status checks)
- **30m:** Moderate change (repos, notifications)
- **1h:** Slow-changing (upcoming releases)
- **1d:** Static content (library counts, storage)

### Authentication Methods

- **Header:** `X-API-Key`, `Authorization: Bearer`, custom headers
- **Query Parameters:** `?api_key=VALUE`
- **Body:** POST requests with credentials in JSON body

### HTTPS & Self-Signed Certs

Add `allow-insecure: true` when using self-signed certificates:

```yaml
- type: custom-api
  title: My Service
  allow-insecure: true
  url: https://service.local:port/api
```

### Proxy & Cover Images

Use nginx/haproxy to proxy API calls and image URLs to avoid exposing API keys in HTML:

```nginx
location /myservice-api/ {
    rewrite ^/myservice-api/(.*)$ /api/$1 break;
    proxy_pass http://myservice-backend:8080;
    proxy_set_header Authorization "Bearer YOUR-TOKEN";
}
```

---

## Additional Resources

- **Official Community Widgets:** https://github.com/glanceapp/community-widgets
- **Glance Documentation:** https://github.com/glanceapp/glance
- **Homelab Services:**
    - Tautulli (Plex monitoring): https://github.com/Tautulli/Tautulli
    - Jellyfin (Media server): https://github.com/jellyfin/jellyfin
    - Proxmox VE: https://www.proxmox.com/
    - Gatus (Status monitoring): https://github.com/TwiN/gatus
    - Unifi (Network): https://unifi.ui.com/
    - RomM (Game library): https://github.com/zurdi15/romm
    - Immich (Photo library): https://github.com/immich-app/immich
    - Backrest (Restic backups): https://github.com/garethgeorge/restic-rest-server
    - MediaTracker (Entertainment): https://github.com/bonukai/MediaTracker

---

**Last Updated:** February 18, 2026
**Glance Version Tested:** v0.8.0+
