# MALÉ MOVE Backend

Controller-based .NET 10 REST API for MALÉ MOVE. The solution follows the layered structure used by `Zouriel/invites-blog-backend`: `Api`, `Application`, `Domain`, and `Infrastructure`. Controllers translate HTTP requests into Application service calls; EF Core and PostGIS live in Infrastructure.

## Current implementation

- JWT bearer authentication and role-based policies.
- Permit list/detail/edit/submit/review endpoints and private upload storage with size, media-type, and signature checks.
- Population-series and source-register read endpoints.
- A graph-based route calculation service restricted to the supported Greater Malé bounds (latitude 4.0–4.5, longitude 73.2–73.8), with approval/time-window-aware closures and clearly labelled estimated times.
- PostgreSQL/PostGIS EF Core model and Docker Compose stack.
- Permit audit events written atomically with uploads, edits, submissions, approvals, and rejections.

Malware scanning is fail-closed through the private ClamAV service in Compose. The audit-event table is append-only by application convention but is not a cryptographic hash chain; before operational use, connect the existing Zouriel Workflows engine for permit lifecycles and tamper-evident workflow audit. OCR extraction, official dataset imports, OSM graph ingestion, identity-provider configuration, and production migrations remain deployment/integration work. Uploaded permit text is explicitly marked as needing review; no OCR result is fabricated.

## Local setup

1. Install the .NET 10 SDK and Docker Compose.
2. Copy `.env.example` to `.env` and replace every placeholder with local development values. Never commit `.env`. If port 5432 is already used on your machine, set `POSTGRES_HOST_PORT` (for example `55433`). Add a local-only `DEV_SIGNING_KEY` of at least 32 random characters (e.g. `openssl rand -base64 48`).
3. Start PostGIS and ClamAV with the development override, which also publishes ClamAV on `127.0.0.1:3310` for an API running on the host: `docker compose -f compose.yaml -f compose.dev.yaml up -d db clamav`. ClamAV downloads its signatures on first start and can take a few minutes before uploads are accepted (uploads fail closed until then).
4. Export the settings in the same shell (bash/zsh: `set -a; . ./.env; set +a`):
   ```sh
   export ConnectionStrings__Postgres="Host=127.0.0.1;Port=${POSTGRES_HOST_PORT:-5432};Database=$POSTGRES_DB;Username=$POSTGRES_USER;Password=$POSTGRES_PASSWORD;Include Error Detail=false"
   export Authentication__DevelopmentSigningKey="$DEV_SIGNING_KEY" Authentication__Audience="$JWT_AUDIENCE"
   export MalwareScanner__Host=127.0.0.1 MalwareScanner__Port=3310 PermitStorage__Path="$PWD/uploads"
   ```
5. Apply the committed migrations: `dotnet tool restore && dotnet ef database update --project src/MaleMove.Infrastructure --startup-project src/MaleMove.Infrastructure`. (The Infrastructure project holds the design-time factory; use it as the startup project for `migrations add` too.)
6. Start the API: `ASPNETCORE_ENVIRONMENT=Development ASPNETCORE_URLS=http://127.0.0.1:5080 dotnet run --project src/MaleMove.Api`.

`scripts/dev-up.sh` performs steps 3 to 6 from an existing `.env`.

### Local tokens

With `ASPNETCORE_ENVIRONMENT=Development` and `Authentication__DevelopmentSigningKey` set, the API validates locally signed tokens instead of an OIDC issuer, and `POST /api/v1/dev/token?role=Administrator` issues an 8-hour token for a fixed local user (roles: `Administrator`, `DataSteward`, `PermitOfficer`, `PermitApprover`, `Planner`). The endpoint returns 404 when the key is not set, and the API refuses to start if the key is set outside Development. The Angular dev server requests a token automatically. For a real identity provider, leave the key unset and set `Authentication__Authority` and `Authentication__Audience`; the issuer must provide a UUID `sub` and `role` claims matching the roles above.

OpenAPI is exposed only in Development. The API expects the database schema to have been migrated before startup. `/api/v1/health` is an anonymous liveness endpoint; `/api/v1/health/ready` checks database readiness without exposing connection details.

## API

All routes are under `/api/v1`. Staff endpoints require a valid JWT and one of the configured roles. Main routes are `GET /api/v1/permits`, `POST /api/v1/permits/upload`, `PUT /api/v1/permits/{id}`, `POST /api/v1/permits/{id}/submit`, `POST /api/v1/permits/{id}/review`, `GET /api/v1/population/series`, `GET /api/v1/dataSources`, and `POST /api/v1/routes`.

## Security and governance

Read [SECURITY.md](SECURITY.md) and [REGULATORY_AND_DATA_GOVERNANCE.md](REGULATORY_AND_DATA_GOVERNANCE.md) before deployment. The repo is not a legal-compliance or security-certification claim. Do not place private permit documents, credentials, or unapproved datasets in source control.


## Staging

https://staging-4.zouriel.com runs on zouriel-server. `/opt/apps/male-move` holds clones of both repos and a root-only `.env` (`POSTGRES_PASSWORD`, `DEV_SIGNING_KEY`, `NODE_AUTH_TOKEN` with `read:packages`). `deploy male-move` (installed from `deploy/staging/deploy-script-male-move`) fast-forwards both clones to `main` and rebuilds the `male-move` compose project from `deploy/staging/compose.staging.yml`: its own Postgres, ClamAV, volumes and network; only the API and web containers join the shared `proxy` network. Migrations run on startup there (`Database__MigrateOnStartup=true`).

Staging uses the local token issuer, so the whole site is behind Caddy basic auth (`deploy/staging/install-route.sh`, run once on the server with the password on stdin; only the bcrypt hash is stored). The password is kept outside the repos.
