---
title: ADR-0026 - Implementation platform and delivery conventions
status: Accepted
---

## ADR-0026: Implementation platform and delivery conventions

## Status

Accepted

## Date and ownership

- Date: 2026-08-20
- Authors: @PlagueHO
- Reviewers: @PlagueHO

## Context

The technology baseline in ADR-0025 identifies the runtime and dependency
policy, but implementation also needs consistent infrastructure, inner-loop,
frontend, testing, and observability conventions. The
[Libris Maleficarum repository](https://github.com/PlagueHO/libris-maleficarum)
provides an established .NET 10 and Azure implementation pattern that should be
reused where its boundaries and resources match Our IQ.

## Decision

Infrastructure is authored in Bicep under `infra/` and provisioned through the
Azure Developer CLI using `azure.yaml`. The implementation follows the
referenced repository's environment configuration, resource naming, parameter,
module, and output conventions where they fit Our IQ's resource topology.
Existing modules are reused only after their parameters, security posture,
dependencies, and ownership boundaries are reviewed; unrelated application
resources are not copied blindly.

Microsoft Aspire is the inner-loop orchestration and local service-discovery
tool. The AppHost models the public MCP Server, private Tool Services, data
dependencies, and the React frontend as appropriate for local development. It
does not replace Bicep or azd as the deployment contract.

The frontend uses React with ShadCN/UI and follows the referenced repository's
TypeScript, Vite, accessibility, and frontend testing conventions where
applicable.

Backend unit and component tests use MSTest with the current MSTest SDK and
Microsoft Testing Platform patterns. Test projects use central package
management and follow Arrange-Act-Assert, focused test categories, and the
repository's established assertion and substitution conventions.

All infrastructure and application services emit structured telemetry through
OpenTelemetry to Application Insights and Azure Monitor. Monitoring resources,
diagnostic settings, dashboards or workbooks, and alert conventions are
reused from the reference repository when their signals and scopes match Our
IQ. Monitoring is part of the initial deployment design, not an optional
post-deployment addition.

## Alternatives considered

| Alternative | Rejection or selection rationale |
| --- | --- |
| Bicep with azd, Aspire inner loop, React/ShadCN frontend, MSTest, and Application Insights/Azure Monitor | Selected to align with the established reference repository and the adopted Azure deployment boundary. |
| Terraform or imperative deployment scripts | Rejected for the initial implementation because Bicep and azd are the requested and reference-aligned deployment path. |
| Aspire as the production deployment contract | Rejected because Aspire models the inner loop; Bicep and azd remain the infrastructure and environment deployment contract. |
| Unstructured infrastructure copied from the reference repository | Rejected because reuse must preserve Our IQ's security, data, and trust boundaries. |

## Consequences

### Positive

- POS-001: Developers get a consistent local orchestration and service
  discovery experience.
- POS-002: Infrastructure changes remain reviewable, repeatable, and aligned
  with the existing Azure delivery workflow.
- POS-003: Testing and monitoring conventions are established before
  implementation expands.
- POS-004: Proven infrastructure modules and naming patterns can be reused
  without treating another application's topology as authoritative.

### Negative

- NEG-001: Aspire, Bicep, azd, frontend tooling, and test tooling each require
  version compatibility checks.
- NEG-002: Reference infrastructure still requires a resource-by-resource
  security and boundary review.
- NEG-003: Monitoring dashboards and alerts need Our IQ-specific queries and
  thresholds rather than direct reuse.

## Implementation notes

- IMP-001: Add `azure.yaml`, `infra/`, and Aspire AppHost projects only when
  implementation begins; this ADR does not claim they exist yet.
- IMP-002: Use the reference repository's `infra/abbreviations.json`,
  environment naming, module layout, and monitoring patterns as starting
  points, subject to review.
- IMP-003: Use central package management for .NET and explicit lock or review
  policy for frontend dependencies.
- IMP-004: Keep telemetry free of secrets and sensitive knowledge content.
- IMP-005: Validate Bicep with what-if or azd preview before deployment and
  validate the local container listening ports against Container Apps ingress.

## References

- REF-001: [Libris Maleficarum](https://github.com/PlagueHO/libris-maleficarum).
- REF-002: [Libris Maleficarum `azure.yaml`](https://github.com/PlagueHO/libris-maleficarum/blob/main/azure.yaml).
- REF-003: [Libris Maleficarum central package management](https://github.com/PlagueHO/libris-maleficarum/blob/main/libris-maleficarum-service/Directory.Packages.props).
- REF-004: [Libris Maleficarum infrastructure](https://github.com/PlagueHO/libris-maleficarum/tree/main/infra).
- REF-005: [Libris Maleficarum testing guidance](https://github.com/PlagueHO/libris-maleficarum/blob/main/docs/design/testing.md).
- REF-006: [Azure Developer CLI documentation](https://learn.microsoft.com/azure/developer/azure-developer-cli/).
- REF-007: [Microsoft Aspire documentation](https://learn.microsoft.com/dotnet/aspire/).
- REF-008: [Azure Monitor OpenTelemetry enablement](https://learn.microsoft.com/azure/azure-monitor/app/opentelemetry-enable).
- REF-009: [ADR-0025](adr-0025-dotnet-technology-and-package-baseline).

## Review record

- 2026-08-20: Accepted by @PlagueHO during issue #4 technology-selection
  discussion.
