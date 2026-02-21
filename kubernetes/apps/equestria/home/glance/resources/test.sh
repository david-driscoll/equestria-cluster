#!/bin/bash
# docker run --rm -v .:/app/config/ glanceapp/glance config:print
op run --no-masking -- docker run -e ROOT_DOMAIN -e AUTHENTIK_API_TOKEN -e cf_accountId -e cf_username -e cf_credential -e equestria_tunnel_username -e equestria_tunnel_credential -e sgc_tunnel_username -e sgc_tunnel_credential -e prowlarr_apikey -e unifi_api_key -e github_token -e grafana_token -e sonarr_api_key -e tautulli_api_key -e sabnzbd_api_key -e jellyfin_api_key -e jellyfin_user_id -e immich_api_key -p 8080:8080 --rm -v .:/app/config/ glanceapp/glance

