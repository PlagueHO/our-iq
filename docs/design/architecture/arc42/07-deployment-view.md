---
title: arc42 7 - Deployment view
status: Proposed
---

## 7. Deployment view

## Purpose

Describe environments, nodes, communication paths, and operational boundaries.

## Initial deployment direction

This view is `Proposed`. No deployment exists. It maps accepted constraints and
candidate services to operational boundaries so they can be reviewed without
claiming implementation.

Microsoft Azure is required by CON-08. Microsoft Foundry Agent Service is the
required runtime for Domain Agents. Microsoft Entra provides required identity
constraints. ADR-0022, ADR-0023, ADR-0025, and ADR-0026 select the initial compute, data,
delivery, and observability direction. Network and production operational
controls remain proposed.

| Boundary or node | Role | Status |
| --- | --- | --- |
| Client environment | Runs a Client Agent that calls the public MCP interface. | External |
| Azure Container Apps environment | Hosts separate public MCP Server and private Tool Services deployables. | Selected |
| Microsoft Foundry Agent Service | Runs shared, versioned Our IQ Domain Agents. | Required |
| Microsoft Entra | Authenticates users and agent identities; supports attended on-behalf-of context. | Required |
| Azure virtual network | Candidate private application and data network boundary. | Candidate |
| Private endpoints | Candidate private connectivity from application compute to supported Azure Data Services. | Candidate |
| Azure Blob Storage | Immutable canonical Markdown and referenced asset store. | Selected |
| Cosmos DB | Per-space transactional control metadata and change-set coordination. | Selected |
| Azure AI Search | Hybrid lexical, vector, metadata, and relationship retrieval projection. | Selected |
| Application Insights and Azure Monitor | Collects logs, metrics, traces, and audit evidence through OpenTelemetry. | Selected |

The Container Apps environment supplies compute for the two selected
application deployables. It does not decide scaling rules, network topology, or
production environment tiers.

Bicep under `infra/` is the infrastructure source of truth and Azure Developer
CLI is the deployment workflow. Microsoft Aspire supports local orchestration
and service discovery only; it does not replace the Bicep deployment contract.

## Connectivity and identity

1. The Client Agent crosses the public MCP boundary to the Our IQ MCP Server.
1. The MCP Server invokes Domain Agents through a private agent-runtime
   boundary.
1. Domain Agents invoke private Tool Services, which use service-specific
   managed identities to access data dependencies.
1. Candidate private endpoints place supported Azure Data Service access behind
   the candidate virtual-network boundary.

User and agent identities remain authorization and audit context; they are not
the dependency access identities. This follows
[ADR-0007](../../decisions/adr-0007-agent-identity-and-execution-context) and
[ADR-0008](../../decisions/adr-0008-service-managed-identities).

## Environments

The pilot uses one deployment-configured Azure geography and accepts only
non-sensitive synthetic or internal test data. Subscription, resource groups,
tenant configuration, network segmentation, and private-endpoint coverage
remain environment design inputs.

## Open questions

- Which Azure service coordinates long-running-work orchestration after the
  synchronous thin slice (Q-24)?
- Which production residency, classification, retention, and network-isolation
  constraints apply beyond the pilot boundary (Q-21)?
- Which production observability retention, alert thresholds, and workbook
  scopes satisfy the requirements?
- Which components require separate scaling, deployment, or availability
  boundaries?
