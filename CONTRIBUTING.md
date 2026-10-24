# Contributing

- Use .NET 10, nullable reference types, implicit usings, and the layered project boundaries.
- Keep controllers thin. Put business rules in Application services and persistence in Infrastructure.
- Use request/response DTOs; do not expose EF entities directly.
- Use async EF Core calls with cancellation tokens. Bound all pagination and request bodies.
- Keep audit-worthy changes attributable and preserve source provenance.
- Add no secrets, uploaded files, production extracts, or fabricated population/traffic data.
- Keep routing assumptions visible in responses and documentation.
- Follow OWASP ASVS 5.0 and the repository security policy for changes involving auth, uploads, data retention, or external sources.

