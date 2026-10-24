# MALÉ MOVE Backend

Controller-based .NET 10 REST API for MALÉ MOVE. The solution follows the layered structure used by `Zouriel/invites-blog-backend`: `Api`, `Application`, `Domain`, and `Infrastructure`. Controllers translate HTTP requests into Application service calls; EF Core and PostGIS live in Infrastructure.

## Current implementation

- JWT bearer authentication and role-based policies.
- Permit list/detail/edit/submit/review endpoints and private upload storage with size, media-type, and signature checks.
- Population-series and source-register read endpoints.
- A graph-based route calculation service with approval/time-window-aware closures and clearly labelled estimated times.
- PostgreSQL/PostGIS EF Core model and Docker Compose stack.
- Permit audit events written atomically with uploads, edits, submissions, approvals, and rejections.

Malware scanning is fail-closed through the private ClamAV service in Compose. The audit-event table is append-only by application convention but is not a cryptographic hash chain; before operational use, connect the existing Zouriel Workflows engine for permit lifecycles and tamper-evident workflow audit. OCR extraction, official dataset imports, OSM graph ingestion, identity-provider configuration, and production migrations remain deployment/integration work. Uploaded permit text is explicitly marked as needing review; no OCR result is fabricated.

## Local setup

1. Install the .NET 10 SDK and Docker Compose.
2. Copy `.env.example` to `.env` and replace every placeholder with local development values. Never commit `.env`.
3. Set the OIDC authority/audience and exact Angular origin. The issuer must provide a UUID `sub` and `role` claims matching `Administrator`, `DataSteward`, `PermitOfficer`, `PermitApprover`, or `Planner`. A development identity provider or token issuer is required for protected endpoints.
4. Start PostGIS with `docker compose up -d db`. Compose binds PostgreSQL only to `127.0.0.1:5432` and the API only to `127.0.0.1:5080` on the host.
5. In the same shell, load the local database values with `. ./.env` and export a host-reachable connection string: `export ConnectionStrings__Postgres="Host=127.0.0.1;Port=5432;Database=$POSTGRES_DB;Username=$POSTGRES_USER;Password=$POSTGRES_PASSWORD;Include Error Detail=false"`.
6. From this directory, run `dotnet tool restore`, then `dotnet ef migrations add InitialCreate --project src/MaleMove.Infrastructure --startup-project src/MaleMove.Api`, then `dotnet ef database update --project src/MaleMove.Infrastructure --startup-project src/MaleMove.Api`.
7. Configure a local OIDC issuer and set `Authentication__Authority`, `Authentication__Audience`, and the matching role claims. Start the API with `ASPNETCORE_URLS=http://127.0.0.1:5080 dotnet run --project src/MaleMove.Api`.

OpenAPI is exposed only in Development. The API expects the database schema to have been migrated before startup. `/api/v1/health` is an anonymous liveness endpoint; `/api/v1/health/ready` checks database readiness without exposing connection details.

## API

All routes are under `/api/v1`. Staff endpoints require a valid JWT and one of the configured roles. Main routes are `GET /api/v1/permits`, `POST /api/v1/permits/upload`, `PUT /api/v1/permits/{id}`, `POST /api/v1/permits/{id}/submit`, `POST /api/v1/permits/{id}/review`, `GET /api/v1/population/series`, `GET /api/v1/dataSources`, and `POST /api/v1/routes`.

## Security and governance

Read [SECURITY.md](SECURITY.md) and [REGULATORY_AND_DATA_GOVERNANCE.md](REGULATORY_AND_DATA_GOVERNANCE.md) before deployment. The repo is not a legal-compliance or security-certification claim. Do not place private permit documents, credentials, or unapproved datasets in source control.

