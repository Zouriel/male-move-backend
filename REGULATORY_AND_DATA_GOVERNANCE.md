# Regulatory and data-governance baseline

MALÉ MOVE is designed to support responsible handling of planning data; this document is not a legal opinion and does not certify compliance with any law or regulation.

## Controls built into the implementation

- Collect only account/role information needed to administer the service. Public route requests are not persisted by the current API.
- Keep permit originals private, record their checksum and uploader, and avoid exposing source documents through static web hosting.
- Record source, source year, license, approval state, coverage, limitations, and source locator for population values.
- Separate observed values, published projections, and modeled scenarios in API fields.
- Require an authorized human approval and confirmed road mapping before a permit can affect routing.
- Attribute permit write operations to authenticated actors. Dataset import and operational administration are not implemented yet.
- Append actor-attributed permit lifecycle audit events atomically with permit changes; privileged database administrators can still modify these events, so the table is not cryptographically tamper-evident.
- Do not downscale published population totals to unsupported geography.
- Carry OSM attribution and licensing obligations into map UI, derived database distribution, and documentation.

## Deployment gate

Before operational use, the project owner must confirm the currently applicable Maldives laws, regulations, government policies, data-sharing agreements, record-retention duties, and cross-border hosting rules with the responsible authority or qualified local counsel. The source project documents note that external data approvals are required and that the stated datathon submission deadline was 19 October 2026; the organizer must confirm any late approval or exception.

Use the existing `Zouriel/zouriel-workflows` .NET lifecycle engine and `@zouriel/ui/workflow` runtime/builder for permit state transitions, approval tasks, and tamper-evident workflow history before real permit records are used. Do not treat the interim permit audit table as immutable evidence.

The release must not describe traffic estimates as live observations. A public release must identify the source, license, and limitations of every displayed dataset and map layer.

