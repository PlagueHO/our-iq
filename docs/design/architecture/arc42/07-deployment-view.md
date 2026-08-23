---
title: arc42 7 - Deployment view
status: Proposed
---

## 7. Deployment view

## Purpose

Describe environments, nodes, communication paths, and operational boundaries.

## Initial deployment direction

This view is `Proposed`. No deployment exists. It maps accepted constraints and
selected services to operational boundaries so they can be reviewed without
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
| Pilot virtual network | One VNet-integrated application boundary with application and private-endpoint subnet roles. | Selected for pilot |
| Private endpoints | Private connectivity from pilot application compute to Blob Storage, Cosmos DB, and Azure AI Search where supported. | Selected for pilot |
| Azure Blob Storage | Immutable canonical Markdown and referenced asset store. | Selected |
| Cosmos DB | Per-space transactional control metadata and change-set coordination. | Selected |
| Azure AI Search | Hybrid lexical, vector, metadata, and relationship retrieval projection. | Selected |
| Application Insights and Azure Monitor | Collects logs, metrics, traces, and audit evidence through OpenTelemetry. | Selected |

The Container Apps environment supplies compute for the two selected
application deployables. The pilot topology is one non-production environment:
the public MCP Server uses external HTTPS ingress, while Tool Services and
management use internal ingress only. Production environments, scaling rules,
and availability boundaries remain future design work.

## Pilot network topology

The pilot uses one virtual network with two logical subnet roles: an application
subnet for the VNet-integrated Container Apps environment and a
private-endpoint subnet for supported Azure data services. Private DNS links
resolve Blob Storage, Cosmos DB, and Azure AI Search through their private
endpoints. Subscription, resource group, geography, address spaces, and CIDRs
are deployment parameters.

The public MCP Server is the only externally reachable application surface.
Tool Services and their separate logical management surface have internal
ingress and are not exposed publicly. Domain Agents reach Tool Services through
the supported private service integration; the pilot does not use public Tool
Services ingress as a fallback.

Bicep under `infra/` is the infrastructure source of truth and Azure Developer
CLI is the deployment workflow. Microsoft Aspire supports local orchestration
and service discovery only; it does not replace the Bicep deployment contract.

## Connectivity and identity

1. The Client Agent crosses the public MCP boundary to the Our IQ MCP Server.
1. The MCP Server invokes Domain Agents through a private agent-runtime
   boundary.
1. Domain Agents invoke private Tool Services, which use service-specific
   managed identities to access data dependencies.
1. Pilot private endpoints place supported Azure Data Service access behind the
   pilot virtual-network boundary.

User and agent identities remain authorization and audit context; they are not
the dependency access identities. This follows
[ADR-0007](../../decisions/adr-0007-agent-identity-and-execution-context) and
[ADR-0008](../../decisions/adr-0008-service-managed-identities).

## Environments

The pilot uses one deployment-configured Azure geography and accepts only
non-sensitive synthetic or internal test data. Local Aspire orchestration is the
inner-loop tier. The Azure pilot is one parameterized non-production environment.
Production is a separate future tier; its classification, residency, retention,
network isolation, and availability controls remain open under Q-21.

## Open questions

- Which Azure service coordinates long-running-work orchestration after the
  synchronous thin slice (Q-24)?
- Which production residency, classification, retention, and stronger
  network-isolation controls apply beyond the pilot boundary (Q-21)?
- Which production observability retention, alert thresholds, and workbook
  scopes satisfy the requirements?
- Which components require separate scaling, deployment, or availability
  boundaries?
