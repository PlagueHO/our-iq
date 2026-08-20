---
title: ADR-0023 - .NET Container Apps boundaries
status: Accepted
---

## ADR-0023: .NET Container Apps boundaries

## Status

Accepted

## Date and ownership

- Date: 2026-08-19
- Authors: @PlagueHO
- Reviewers: @PlagueHO

## Context

The logical public and private application boundaries were defined, but the
implementation stack and deployable units remained open. Combining those
boundaries would weaken independent ingress, identity, and authorization
controls.

## Decision

The initial implementation uses .NET and ASP.NET Core. The public Our IQ MCP
Server uses the official MCP C# SDK. Agent integration uses Microsoft Agent
Framework and Microsoft Foundry Agent Service.

The public Our IQ MCP Server and private Our IQ Tool Services are separate Azure
Container Apps deployables in one .NET solution. They use distinct ingress
policies and managed identities. Management operations share the private Tool
Services deployment for the pilot but remain a separate logical authorization
surface.

## Alternatives considered

| Alternative | Rejection or selection rationale |
| --- | --- |
| One modular Container App | Rejected because public and private operations require different trust and identity boundaries. |
| Python or TypeScript services | Not selected because .NET has official MCP, Azure SDK, identity, and hosting support aligned with the project requirements. |
| Separate public and private .NET Container Apps | Selected because it preserves the documented boundaries without premature microservice decomposition. |

## Consequences

### Positive

- POS-001: Public and private ingress and identities remain independently
  enforceable.
- POS-002: One solution can share contracts without merging trust boundaries.
- POS-003: The pilot avoids decomposing every logical Tool Service.

### Negative

- NEG-001: Two deployables require distributed tracing and contract testing.
- NEG-002: Some required SDK packages may be prerelease during initial
  implementation.

## Implementation notes

- IMP-001: Logical Tool Services remain modules until scaling, security, or
  reliability evidence justifies another deployable.
- IMP-002: MCP uses streamable HTTP; dedicated health endpoints are separate
  from MCP endpoints.
- IMP-003: Network topology and production private-endpoint coverage remain
  deployment design decisions.
- IMP-004: [ADR-0025](adr-0025-dotnet-technology-and-package-baseline) defines
  the .NET target, Agent Framework integration boundary, and package policy.

## References

- REF-001: [Azure Container Apps MCP hosting](https://learn.microsoft.com/azure/container-apps/mcp-overview).
- REF-002: [.NET MCP guidance](https://learn.microsoft.com/dotnet/ai/get-started-mcp).
- REF-003: [ADR-0008](adr-0008-service-managed-identities).

## Review record

- 2026-08-19: Accepted by @PlagueHO during issue #4 reconciliation.
