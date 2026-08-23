---
title: ADR-0029 - Pilot network and environment topology
status: Accepted
---

## ADR-0029: Pilot network and environment topology

## Status

Accepted

## Date and ownership

- Date: 2026-08-23
- Authors: @PlagueHO
- Reviewers: @PlagueHO

## Context

ADR-0023 selects separate public MCP Server and private Tool Services Azure
Container Apps deployables, but leaves their network topology and ingress
policies open. V1-D05 must resolve enough of that topology for Bicep to model a
pilot without silently selecting production data governance or residency
controls.

The pilot is limited to one deployment-configured Azure geography, one team,
under 20 users, under 5,000 knowledge items per space, and non-sensitive
synthetic or internal test data. The design must preserve the public intent
boundary, the private deterministic tool boundary, and service-managed
identities while keeping the first deployment simple.

## Decision

The pilot uses one non-production Azure deployment environment containing one
virtual network and one VNet-integrated Azure Container Apps environment. The
network has two logical subnet roles:

- an application subnet for the Container Apps environment; and
- a private-endpoint subnet for supported Azure data services.

The actual subscription, resource group, geography, address spaces, and subnet
CIDRs are deployment parameters and are not selected by this ADR.

The Container Apps environment contains the two selected deployables:

- The public Our IQ MCP Server has external HTTPS ingress for authenticated
  intent-level MCP operations and separate health endpoints.
- The private Our IQ Tool Services deployable has internal ingress only. Its
  management surface is also internal and remains a separate logical
  authorization surface.

The pilot data path uses private endpoints for Azure Blob Storage, Cosmos DB,
and Azure AI Search where the services support the required configuration.
Public network access is disabled for those data services in the pilot
configuration. Private DNS resolution and endpoint subnet placement are part of
the Bicep topology. The services remain authoritative or derived according to
ADR-0022; network placement does not change their data roles.

The supported request paths are:

1. A Client Agent reaches only the public MCP Server through the public
   authenticated ingress.
2. The MCP Server invokes Microsoft Foundry Agent Service using the required
   platform identity and authorization boundary.
3. Domain Agents use the supported private service integration to reach the
   internal Tool Services ingress. The pilot does not make Tool Services public
   as a fallback.
4. Tool Services reach Blob Storage, Cosmos DB, and Azure AI Search through the
   private endpoints using their own service-managed identities.
5. Application and infrastructure telemetry reaches Application Insights and
   Azure Monitor through secured outbound paths and contains no secrets or
   knowledge content.

Microsoft Entra user and agent identities remain authorization and audit
context. They are not reused as the dependency access identities. Each
deployable uses its own managed identity, following ADR-0007 and ADR-0008.

The environment tiers are intentionally small:

| Tier | Topology | Data boundary | Status |
| --- | --- | --- | --- |
| Local inner loop | Aspire-managed local services and dependencies | Local synthetic data only | Selected |
| Azure pilot | One parameterized VNet-integrated Container Apps environment | Non-sensitive synthetic or internal test data only | Selected |
| Production | Separate environment design with stronger governance and availability controls | Classification, residency, retention, and isolation not yet selected | Deferred |

## Alternatives considered

| Alternative | Rejection or selection rationale |
| --- | --- |
| One public Container App for MCP and Tool Services | Rejected because it would merge public and private ingress, identity, and authorization boundaries. |
| Public ingress for Tool Services with application-level filtering | Rejected because a filtering mistake would expose deterministic private tools and management operations. |
| Public data-service endpoints for the pilot | Rejected because private endpoints provide a smaller pilot trust boundary for canonical, control, and retrieval data. |
| Separate Container Apps environments for every logical Tool Service | Rejected because ADR-0023 defers deployable decomposition until scaling, security, or reliability evidence justifies it. |
| Production-grade multi-environment topology | Deferred because the pilot topology is intentionally small; ADR-0030 now defines governance admission controls, while production topology and availability details remain future work. |

## Consequences

### Positive

- POS-001: Public MCP ingress and private Tool Services ingress are independently
  enforceable.
- POS-002: Pilot data services are reachable through a private network path and
  service-managed identities.
- POS-003: One environment keeps the pilot topology small while retaining
  logical management and service boundaries.
- POS-004: Environment-specific values remain deployment parameters rather than
  becoming undocumented architecture decisions.

### Negative

- NEG-001: The pilot has one Azure application environment and therefore does
  not provide production isolation or availability evidence.
- NEG-002: The private Domain Agent to Tool Services path depends on a supported
  Foundry and Container Apps private connectivity configuration.
- NEG-003: Private endpoints add DNS, subnet, and deployment-validation
  dependencies.

## Implementation notes

- IMP-001: Bicep modules must model the application subnet, private-endpoint
  subnet, private DNS links, internal Tool Services ingress, and external MCP
  ingress without committing environment-specific CIDRs.
- IMP-002: Deployment validation must prove that Tool Services and management
  endpoints are not publicly reachable and that the private data paths resolve
  through the intended endpoints.
- IMP-003: Azure service support for the selected private endpoints and the
  Foundry-to-Tool Services private integration must be validated with
  deployment preview before implementation is treated as ready.
- IMP-004: Production private-link coverage, firewall policy, egress control,
  telemetry isolation, retention, residency, RPO, RTO, and availability targets
  remain outside this pilot decision. Data governance and retention are defined
  by ADR-0030; the remaining production topology and availability details require
  later decisions and implementation evidence.

## References

- REF-001: [ADR-0007](adr-0007-agent-identity-and-execution-context).
- REF-002: [ADR-0008](adr-0008-service-managed-identities).
- REF-003: [ADR-0022](adr-0022-initial-azure-data-plane).
- REF-004: [ADR-0023](adr-0023-dotnet-container-apps-boundaries).
- REF-005: [Assumptions and open questions](../product/assumptions-and-open-questions).
- REF-006: [C4 initial Azure deployment](../architecture/c4/azure-deployment).
- REF-007: [V1 implementation backlog](../implementation-backlog).

## Review record

- 2026-08-23: Accepted by @PlagueHO for V1-D05.
