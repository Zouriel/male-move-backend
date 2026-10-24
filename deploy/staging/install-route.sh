#!/usr/bin/env bash
# Run on zouriel-server as root: installs Caddyfile.d/male-move.caddy for staging-4.
# The whole Caddy config is validated before reload; on failure the previous route is restored.
set -euo pipefail
here=$(cd "$(dirname "$0")" && pwd)
route=/opt/stacks/proxy/Caddyfile.d/male-move.caddy
backup=/opt/apps/male-move/male-move.caddy.prev

[ -f "$route" ] && cp "$route" "$backup"
cp "$here/male-move.caddy.template" "$route"
if docker exec caddy caddy validate --config /etc/caddy/Caddyfile --adapter caddyfile >/dev/null 2>&1; then
  docker exec caddy caddy reload --config /etc/caddy/Caddyfile --adapter caddyfile
  echo "ROUTE OK"
else
  echo "Caddy config invalid; restoring previous route" >&2
  if [ -f "$backup" ]; then cp "$backup" "$route"; else rm -f "$route"; fi
  exit 1
fi
