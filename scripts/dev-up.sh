#!/usr/bin/env bash
# Local development: start PostGIS + ClamAV, apply migrations and run the API on 127.0.0.1:5080.
set -euo pipefail
cd "$(dirname "$0")/.."
[ -f .env ] || { echo "Create .env from .env.example first." >&2; exit 1; }
set -a; . ./.env; set +a
: "${DEV_SIGNING_KEY:?Set DEV_SIGNING_KEY (32+ random characters) in .env}"

docker compose -f compose.yaml -f compose.dev.yaml up -d db clamav
until docker compose exec -T db pg_isready -U "$POSTGRES_USER" -d "$POSTGRES_DB" >/dev/null 2>&1; do sleep 1; done

export ConnectionStrings__Postgres="Host=127.0.0.1;Port=${POSTGRES_HOST_PORT:-5432};Database=$POSTGRES_DB;Username=$POSTGRES_USER;Password=$POSTGRES_PASSWORD;Include Error Detail=false"
export Authentication__DevelopmentSigningKey="$DEV_SIGNING_KEY" Authentication__Audience="${JWT_AUDIENCE:-male-move-api}"
export MalwareScanner__Host=127.0.0.1 MalwareScanner__Port="${CLAMAV_HOST_PORT:-3310}" PermitStorage__Path="$PWD/uploads"
mkdir -p uploads

dotnet tool restore >/dev/null
dotnet ef database update --project src/MaleMove.Infrastructure --startup-project src/MaleMove.Infrastructure
ASPNETCORE_ENVIRONMENT=Development ASPNETCORE_URLS=http://127.0.0.1:5080 exec dotnet run --project src/MaleMove.Api
