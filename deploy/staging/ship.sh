#!/usr/bin/env bash
# Build MALÉ MOVE images on this workstation, load them on zouriel-server and (re)start the staging stack.
# Usage: deploy/staging/ship.sh   (from the backend repo; expects ../male-move-frontend next to it)
# Touches only /opt/apps/male-move, the male-move compose project, and Caddyfile.d/male-move.caddy.
set -euo pipefail
HOST=${HOST:-zouriel-server}
here=$(cd "$(dirname "$0")" && pwd)
backend=$(cd "$here/../.." && pwd)
frontend=${FRONTEND_DIR:-$(cd "$backend/../male-move-frontend" && pwd)}
auth_file=${AUTH_FILE:-$HOME/.config/male-move/staging-basic-auth}

(cd "$backend" && rm -rf out && dotnet publish src/MaleMove.Api/MaleMove.Api.csproj -c Release -o out \
  && docker build -q -f Dockerfile.prebuilt -t male-move/api:latest .)
(cd "$frontend" && npx ng build --configuration staging && docker build -q -f Dockerfile.prebuilt -t male-move/web:latest .)

docker save male-move/api:latest male-move/web:latest | gzip | ssh "$HOST" 'gunzip | docker load'

ssh "$HOST" 'mkdir -p /opt/apps/male-move && chmod 700 /opt/apps/male-move'
scp -q "$here/compose.staging.yml" "$HOST:/opt/apps/male-move/compose.staging.yml"
# Secrets are generated on the server once and never leave it.
ssh "$HOST" 'cd /opt/apps/male-move && if [ ! -f .env ]; then umask 077; printf "POSTGRES_PASSWORD=%s\nDEV_SIGNING_KEY=%s\n" "$(openssl rand -hex 32)" "$(openssl rand -base64 48 | tr -d "\n")" > .env; fi'
ssh "$HOST" 'cd /opt/apps/male-move && docker compose -p male-move -f compose.staging.yml --env-file .env up -d --remove-orphans'

# Basic-auth password lives only in a private local file; the server keeps a bcrypt hash.
if [ ! -f "$auth_file" ]; then
  mkdir -p "$(dirname "$auth_file")"; (umask 077; openssl rand -base64 18 | tr -d '\n/+=' > "$auth_file")
fi
hash=$(ssh "$HOST" 'docker exec -i caddy caddy hash-password' < "$auth_file")
[[ $hash == \$2* ]] || { echo "hash-password failed" >&2; exit 1; }
sed "s|__BASIC_AUTH_HASH__|$hash|" "$here/male-move.caddy.template" | ssh "$HOST" 'cat > /opt/apps/male-move/male-move.caddy'
ssh "$HOST" 'set -e; route=/opt/stacks/proxy/Caddyfile.d/male-move.caddy
  [ -f "$route" ] && cp "$route" "/opt/apps/male-move/male-move.caddy.prev"
  cp /opt/apps/male-move/male-move.caddy "$route"
  if docker exec caddy caddy validate --config /etc/caddy/Caddyfile --adapter caddyfile >/dev/null 2>&1; then
    docker exec caddy caddy reload --config /etc/caddy/Caddyfile --adapter caddyfile
  else
    echo "Caddy config invalid; restoring previous route" >&2
    if [ -f /opt/apps/male-move/male-move.caddy.prev ]; then cp /opt/apps/male-move/male-move.caddy.prev "$route"; else rm -f "$route"; fi
    exit 1
  fi'
ssh "$HOST" 'docker compose -p male-move -f /opt/apps/male-move/compose.staging.yml ps'
echo "SHIP OK"
