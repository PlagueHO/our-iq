---
title: arc42 5 - Building block view
status: Proposed
---

## 5. Building block view

## Purpose

Describe the static decomposition of the system at appropriate levels of
detail.

## Level 1

This view is `Proposed`. It documents responsibilities that follow from the
accepted ADRs; it does not describe deployed components or API contracts.

| Building block | Responsibility | Interfaces | Status |
| --- | --- | --- | --- |
| Client Agent | Represents a user and invokes public intent-level operations. | Public MCP tools | External |
| Our IQ MCP Server | Exposes the public MCP interface; authenticates, authorizes, validates, and routes intent. | Public MCP tools; private Domain Agent invocation | Selected .NET ASP.NET Core Azure Container App |
| Our IQ Domain Agents | Interpret contribution, retrieval, and ontology intent using space-specific context. | Private tool invocations | Required runtime: Microsoft Foundry Agent Service |
| Our IQ Tool Services | Execute deterministic operations for domain agents, including context validation, policy, canonical change-set, retrieval, projection, grant, and operation services. | Private domain tools and management APIs | Selected .NET ASP.NET Core Azure Container App |
| Our IQ Data Services | Hold canonical knowledge, control metadata, projections, and audit/observability data. | Private data interfaces | Blob Storage, Cosmos DB, and Azure AI Search selected for the initial implementation |
| Management clients | Perform privileged operator and steward work outside ordinary contributor tools. | Private management APIs and command-line workflows | Logical surface in the private Tool Services deployment for the pilot |

The public MCP boundary ends at the Our IQ MCP Server. Domain Agent and Tool
Service interfaces are private architecture boundaries and are not public MCP
tools. This distinction preserves
[ADR-0002](../../decisions/adr-0002-agent-mediated-intent-interface) and
[ADR-0012](../../decisions/adr-0012-governed-deterministic-correction).

## Responsibility boundaries

| Boundary | Decision basis | Constraint |
| --- | --- | --- |
| Public intent boundary | [ADR-0002](../../decisions/adr-0002-agent-mediated-intent-interface) | No public document or ontology CRUD. |
| Agent-runtime boundary | [ADR-0005](../../decisions/adr-0005-foundry-agent-runtime) and [ADR-0006](../../decisions/adr-0006-shared-versioned-domain-agents) | Foundry runtime is required; definitions are shared and parameterized by knowledge-space ID. |
| Tool-service boundary | [ADR-0008](../../decisions/adr-0008-service-managed-identities) | Each service accesses dependencies with its own managed identity. |
| Canonical-data boundary | [ADR-0001](../../decisions/adr-0001-canonical-knowledge-ownership), [ADR-0009](../../decisions/adr-0009-canonical-markdown-and-rebuildable-projections), and [ADR-0010](../../decisions/adr-0010-atomic-versioned-change-sets) | Canonical writes are governed and atomic; projections are not authoritative. |
| Privileged-management boundary | [ADR-0012](../../decisions/adr-0012-governed-deterministic-correction) | Deterministic correction is separate from ordinary contributor operations. |
| Execution-context boundary | [ADR-0016](../../decisions/adr-0016-immutable-execution-context-snapshots) | State-sensitive tools validate the pinned invocation context. |

## Data classification

| Data kind | Authority | Required treatment |
| --- | --- | --- |
| Canonical Markdown and front matter | Authoritative | Immutable Azure Blob revisions, governed writes, atomic change sets, versioning, provenance |
| Control metadata | Authoritative | Cosmos DB records governance, grants, and per-space change-set coordination |
| Azure AI Search projection | Derived and rebuildable | May lag canonical commits; never determines canonical truth |
| Audit and observability records | Operational evidence | Audit separation, data minimization, and retention follow ADR-0030; service selection remains open |

## Deferred questions

- Which logical Tool Service, if any, needs a separate deployment after the
  pilot?
- Which service coordinates long-running work after the synchronous thin slice
  (Q-24)?
