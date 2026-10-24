# Security policy

## Security baseline

- Follow OWASP ASVS 5.0 as the verification checklist and OWASP Top 10 (2025) as an awareness baseline. A checklist is not a certification.
- Report vulnerabilities privately to the repository owner. Do not put live credentials, permit documents, personal data, or exploit details in public issues.
- Production deployments must use HTTPS, a configured OIDC issuer/audience, exact CORS origins, strong managed database credentials, private document storage, backups, monitoring, and a trusted reverse proxy.

## Implemented controls

- JWT bearer validation against a configured identity issuer; production startup fails if the issuer/audience or allowed browser origin is missing.
- Role-gated controllers; authorization is enforced by the API.
- Fallback authorization requires authentication across endpoints unless an endpoint is explicitly anonymous; roles are read from the validated `role` claim.
- Global per-client request rate limit, response security headers, no-store responses, bounded upload size, extension/MIME/signature checks, generated storage keys, SHA-256 digest, and private storage outside the web root.
- Parameterized EF Core queries, explicit pagination limits, spatial and lookup indexes, and sanitized error responses.
- Docker image runs as a non-root user, with a read-only root filesystem, no added Linux capabilities, and no-new-privileges. The development Compose file binds API/database ports to loopback only; production must use an approved network policy and TLS reverse proxy.
- No credentials or production data are included in the repository.
- Permit write actions append an actor-attributed audit event in the same database save transaction. There are no audit update/delete endpoints, but a privileged database operator could still modify the table.

## Required before operational use

- Keep ClamAV signatures current and monitor scanner health; the API fails closed when scanning is unavailable. Production should use a managed isolated malware scanning service if the Compose service is not an appropriate trust boundary.
- Connect an isolated OCR worker; never execute or render uploaded content in the API process.
- Configure an OIDC provider and permission claims for the named roles.
- Deploy audit retention, restricted database write access, monitored export, and tamper-evident audit through the existing workflow engine or an equivalent immutable audit service.
- Connect the shared workflow runtime and `@zouriel/ui/workflow` components for permit lifecycle configuration, per-record status/history, tasks and audit. The workflow engine currently exists as source in `Zouriel/zouriel-workflows`; it is not published to NuGet.
- Generate and apply EF Core migrations, verify database backups/restore, and configure production secrets outside source control.
- Set and review document, OCR, audit, and route-request retention periods.
- Complete a threat model and an OWASP ASVS 5.0 verification review against the deployed infrastructure.
- Obtain local legal review of applicable privacy, public-records, data-sharing, accessibility, procurement, and dataset-license obligations before production use.

This repository does not claim legal compliance or independent security certification.

