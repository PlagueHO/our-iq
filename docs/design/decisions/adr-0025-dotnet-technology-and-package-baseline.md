---
title: ADR-0025 - .NET technology and package baseline
status: Accepted
---

## ADR-0025: .NET technology and package baseline

## Status

Accepted

## Date and ownership

- Date: 2026-08-19
- Authors: @PlagueHO
- Reviewers: @PlagueHO

## Context

ADR-0023 selected .NET and ASP.NET Core, and ADR-0024 selected prompt-based
Foundry agents where they satisfy the required behaviour. The implementation
still needs a supported target framework, a clear relationship between Foundry
and Microsoft Agent Framework, and a reproducible dependency policy.

## Decision

The first implementation targets .NET 10 and ASP.NET Core.

Microsoft Foundry Prompt Agents remain the managed runtime for the shared
Ontology, Contribution, and Retrieval Domain Agents. Microsoft Agent Framework
is included in the .NET application as the supported integration and
orchestration library. It may invoke the managed agents and compose explicit
multi-step workflows, but it does not replace Foundry as the Domain Agent
runtime or move agent definitions into application code.

NuGet dependencies are centrally managed and pinned to exact stable versions.
Prerelease packages are not used by default. A prerelease dependency is allowed
only when a required capability has no stable package, and then requires an
explicit ADR or amendment documenting the package, reason, compatibility
evidence, and exit plan.

The exact package versions are recorded in the implementation repository's
central package-management file at implementation kickoff, after resolving the
current stable versions and validating them together against .NET 10. The
baseline includes only packages required for the selected boundaries, including
the official MCP C# SDK ASP.NET Core integration, Microsoft Agent Framework,
Azure Identity, Azure Cosmos DB, Azure Blob Storage, and Azure AI Search SDKs.

Azure service clients use Microsoft Entra ID and managed identities in hosted
environments. Connection strings, shared keys, and API keys are not the
application's default authentication path.

## Alternatives considered

| Alternative | Rejection or selection rationale |
| --- | --- |
| .NET 10 with the selected libraries | Selected for the supported LTS baseline and alignment with the official .NET, Azure, MCP, and Agent Framework ecosystems. |
| .NET 9 | Rejected for the first baseline because it is a shorter-lived STS target than .NET 10. |
| Application-owned Agent Framework agents | Rejected for the first baseline because Foundry Agent Service is the accepted managed Domain Agent runtime. |
| Foundry without Microsoft Agent Framework | Rejected because the application still needs a supported .NET integration and explicit workflow-composition abstraction. |
| Floating dependency ranges | Rejected because changing transitive behaviour without review would weaken reproducibility and evaluation evidence. |

## Consequences

### Positive

- POS-001: The runtime and dependency baseline is reproducible before code is
  introduced.
- POS-002: Foundry owns shared agent definitions while application code retains
  a typed integration boundary.
- POS-003: Stable-package preference limits avoidable prerelease compatibility
  risk.
- POS-004: Managed identity use aligns local development and hosted
  authentication without embedding secrets.

### Negative

- NEG-001: Exact versions require deliberate upgrade reviews.
- NEG-002: Agent Framework and Foundry integration may expose overlapping
  abstractions that must remain separated in code.
- NEG-003: A required capability available only in prerelease form may require a
  follow-up decision before implementation can proceed.

## Implementation notes

- IMP-001: Use central NuGet package management rather than repeating versions
  across project files.
- IMP-002: Keep Domain Agent definitions, prompts, model deployment references,
  and tool manifests in the governed Foundry configuration boundary.
- IMP-003: Keep public MCP contracts and private Tool Service contracts in
  application-owned code and test them independently of agent prompts.
- IMP-004: Verify package compatibility, supported target frameworks, and
  release status before creating the first implementation commit.

## References

- REF-001: [Get started with .NET AI and the Model Context Protocol](https://learn.microsoft.com/dotnet/ai/mcp/).
- REF-002: [Microsoft Agent Framework documentation](https://learn.microsoft.com/agent-framework/).
- REF-003: [Azure SDK for .NET package index](https://learn.microsoft.com/dotnet/azure/sdk/packages).
- REF-004: [Azure Identity client library](https://www.nuget.org/packages/Azure.Identity).
- REF-005: [MCP C# SDK ASP.NET Core package](https://www.nuget.org/packages/ModelContextProtocol.AspNetCore).
- REF-006: [ADR-0023](adr-0023-dotnet-container-apps-boundaries).
- REF-007: [ADR-0024](adr-0024-domain-agent-capability-governance).

## Review record

- 2026-08-19: Accepted by @PlagueHO during issue #4 technology-selection
  discussion.
