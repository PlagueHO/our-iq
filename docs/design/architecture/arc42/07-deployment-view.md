---
title: arc42 7 - Deployment view
status: Proposed
---

## 7. Deployment view

## Purpose

Describe environments, nodes, communication paths, and operational boundaries.

## Candidate deployment direction

This view is `Proposed`. No deployment exists. It maps accepted constraints and
candidate services to operational boundaries so they can be reviewed without
claiming implementation.

Microsoft Azure is required by CON-08. Microsoft Foundry Agent Service is the
required runtime for Domain Agents. Microsoft Entra provides required identity
constraints. All other named platform services in this section are
`Candidate`, unless stated otherwise.

| Boundary or node | Role | Status |
| --- | --- | --- |
| Client environment | Runs a Client Agent that calls the public MCP interface. | External |
| Azure Container Apps environment | Candidate compute boundary for the Our IQ MCP Server, private Tool Services, management APIs, and command-line-hosted maintenance entry points. | Candidate |
| Microsoft Foundry Agent Service | Runs shared, versioned Our IQ Domain Agents. | Required |
| Microsoft Entra | Authenticates users and agent identities; supports attended on-behalf-of context. | Required |
| Azure virtual network | Candidate private application and data network boundary. | Candidate |
| Private endpoints | Candidate private connectivity from application compute to supported Azure Data Services. | Candidate |
| Azure Blob Storage | Candidate canonical Markdown store. | Candidate |
| Cosmos DB | Preferred initial Candidate backing store for control metadata behind a storage abstraction. | Candidate |
| Azure Table Storage | Alternative Candidate backing store for control metadata. | Candidate |
| Azure AI Search | Candidate retrieval projection. | Candidate |
| Observability service | Collects logs, metrics, traces, and audit evidence. | Open |

The Candidate Azure Container Apps environment supplies compute for API and MCP
server workloads. It does not decide the number of applications, scaling rules,
network topology, or environment tiers.

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

Environment count, region, subscription, resource groups, tenant configuration,
network segmentation, and private-endpoint coverage remain open. This view
deliberately does not propose them.

## Open questions

- Which Azure services finalize control metadata and long-running-work
  orchestration (Q-24)?
- Which data residency, classification, retention, and network-isolation
  constraints apply (Q-21)?
- Which observability services and audit retention controls satisfy the
  requirements?
- Which components require separate scaling, deployment, or availability
  boundaries?
