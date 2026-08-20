---
title: Initial implementation readiness
status: Proposed
---

## Initial implementation readiness

## Purpose

Define the first implementable increment and separate resolved implementation
inputs from work intentionally deferred beyond it. This document does not claim
that the increment has been implemented.

## First validated increment

The first increment proves the structure-first product bet end to end:

1. An Ontology Manager submits and approves a minimal ontology.
1. A Contributor submits one UTF-8 text or Markdown contribution.
1. The Contribution Agent creates a governed plan and commits one canonical
   change set.
1. A Reader queries the space and receives canonical cited evidence.

The increment uses one active knowledge space and the fixed Owner, Ontology
Manager, Contributor, and Reader roles. It does not need ontology migration,
binary extraction, bulk bootstrap, projection rebuild orchestration, deletion,
or unattended execution.

## Selected implementation baseline

| Concern | Initial decision |
| --- | --- |
| Public implementation | .NET and ASP.NET Core using the official MCP C# SDK. |
| Agent integration | Microsoft Agent Framework with prompt-based Foundry Agent Service agents by default. |
| Deployable boundaries | Separate public MCP Server and private Tool Services Azure Container Apps. |
| Infrastructure delivery | Bicep under `infra/`, provisioned through Azure Developer CLI and `azure.yaml`. |
| Inner loop | Microsoft Aspire AppHost for local orchestration and service discovery. |
| Frontend | React with ShadCN/UI where a frontend is required for the increment. |
| Testing | MSTest with current Microsoft Testing Platform patterns and centrally managed packages. |
| Observability | OpenTelemetry with Application Insights and Azure Monitor. |
| Domain Agents | Shared, versioned Ontology, Contribution, and Retrieval definitions with fixed tool manifests. |
| Ontology | Immutable canonical JSON versions in Cosmos DB with JSON Schema 2020-12 document contracts. |
| Canonical knowledge | Immutable Markdown revisions in Azure Blob Storage. |
| Control metadata | Cosmos DB, partitioned by knowledge space. |
| Retrieval | Azure AI Search hybrid projection; canonical Blob reads supply evidence. |
| Input boundary | UTF-8 text and Markdown using non-sensitive synthetic or internal test data only. |
| Geography | One deployment-configured Azure geography. |

Model deployment is configuration pinned in an immutable agent definition.
Changing the model, prompt, instructions, or tool manifest requires evaluation
and owner approval. Hosted Agents are introduced only through a follow-up
decision demonstrating that prompt-based agents cannot satisfy a requirement.

## Normative behaviour added by reconciliation

- Untrusted content is data, not instructions. It cannot change system
  instructions, tool manifests, identity, or policy.
- An ambiguous contribution returns `clarification_required` with grounded
  reasons and focused questions. It creates no plan or mutation.
- Evidence contains item and revision identities, title, canonical excerpt,
  citation, matched fields or relationships, and projection freshness.
- Query completeness is `complete`, `partial`, or `insufficient` with reasons.
  The contract does not invent a numeric confidence score.

## Release gates for the increment

| Gate | Required evidence |
| --- | --- |
| Authorization | Zero successful operations outside the user and agent capability intersection. |
| Content isolation | Injection cases cannot alter instructions, tools, identity, policy, or unsupported claims. |
| Atomicity | Readers observe either the previous or next complete change set, never a partial set. |
| Provenance | Every committed revision identifies source, plan, approval route, identities, ontology version, and change set. |
| Grounding | Every evidence item resolves to the cited active canonical revision. |
| Stale-state safety | Changed ontology, policy, lifecycle, or canonical head returns `replan_required`. |
| Contract compatibility | Public and private schemas pass compatibility tests against their declared versions. |
| Performance evidence | Instrument p50 and p95 latency for ontology approval, contribution planning and commit, projection visibility, and query evidence. |

Correctness and security gates are release-blocking. Initial latency values are
measurements, not arbitrary pass/fail targets. Targets are set after the first
representative baseline.

## Explicitly deferred beyond the increment

| Work | Reason it does not block implementation |
| --- | --- |
| Binary attachments and extraction | The thin slice contract is text and Markdown only. |
| Bulk bootstrap and external connectors | Not needed to prove one governed contribution. |
| Ontology migration orchestration | The increment creates and activates its first ontology version. |
| Projection rebuild orchestration | Initial indexing can be deterministic and manually initiated in test environments. |
| Space deletion orchestration | No production or regulated data is permitted in the pilot. |
| Unattended execution | All thin-slice actions are attended. |
| Long-running-work service selection | No selected thin-slice operation requires asynchronous orchestration. |
| Production retention, residency, RPO, RTO, and availability targets | Pilot data is non-sensitive and the service is not production-approved. |
| Exact latency budgets and cost envelope | They require measured pilot evidence. |

These items remain required for a production-capable initial release unless a
later scope decision explicitly defers them.
