---
title: Assumptions, open questions, and deferred items
status: Proposed
owner: "@PlagueHO"
reviewers: "@PlagueHO"
---

## Assumptions, open questions, and deferred items

## Purpose

Record what the design currently takes for granted, what remains undecided, and
what has been intentionally pushed beyond the initial version.

This register exists so that reviewers can separate confirmed direction from
working hypotheses, and so that no open question is silently resolved by
omission. It is maintained across every design slice.

## How to read this register

| Marker | Meaning |
| --- | --- |
| **Confirmed** | Agreed with the project owner. Consequential entries become Architecture Decision Records. |
| **Assumed** | Taken as true to make progress. Not yet validated. If wrong, the linked design must change. |
| **Open** | Not decided. Named artefacts are blocked until it is. |
| **Deferred** | Intentionally excluded from the initial version. Not rejected. |

## Confirmed direction

These are recorded here for traceability. Each becomes an Architecture Decision
Record in the structural architecture slice.

| ID | Statement |
| --- | --- |
| C-01 | Our IQ owns canonical knowledge. All canonical writes pass through Our IQ. |
| C-02 | Public interface operations express intent and are resolved by an agent. They are not create, read, update, and delete operations over documents. |
| C-03 | Ontology management is agent-mediated. No low-level ontology editing is exposed publicly. |
| C-04 | Mutation policy is configured per knowledge space: automatic commit, contributor confirmation, or review. |
| C-05 | Microsoft Foundry Agent Service is a required runtime for backend agents. |
| C-06 | Backend agents are shared, versioned definitions parameterized by knowledge-space identifier, not one deployment per space. |
| C-07 | Agents act under a Microsoft Entra agent identity. Attended execution preserves the initiating user; unattended execution carries the agent identity alone. |
| C-08 | Services access platform dependencies using their own managed identities. User and agent identities remain authorization and audit context. |
| C-09 | Canonical knowledge is Markdown with structured front matter, organized by a primary hierarchy and arbitrary typed relationships. Search and graph stores are rebuildable projections. |
| C-10 | Agent-planned changes commit as one atomic, versioned change set. |
| C-11 | A privileged steward or operator path may deterministically change or remove an identified document, subject to policy and audit. |
| C-12 | Retrieval returns structured grounded evidence with citations by default. Synthesis into a narrative answer is opt-in. |
| C-13 | The initial version is single-tenant and hosts multiple knowledge spaces. |
| C-14 | The architecture vocabulary is Client Agent, Our IQ MCP Server, Our IQ Domain Agents, Our IQ Tool Services, Our IQ Data Services. |
| C-15 | Authorization is managed at knowledge-space level, not per document, for the initial version. |
| C-16 | Retrieval may read a projection that lags a canonical commit; eventual read-after-write consistency is acceptable. |
| C-17 | The initial version targets pilot scale: one team, under 20 users, under 5,000 knowledge items per space. Revisit before a wider rollout. |
| C-18 | Access control uses a small fixed role set per knowledge space: Owner, Ontology Manager, Contributor, Reader. |
| C-19 | The initial version targets Model Context Protocol specification `2026-07-28`, accepting the compatibility risk of a very new spec. |
| C-20 | The initial version targets best-effort availability with no formal recovery point or recovery time objective. Revisit before a wider rollout. |
| C-21 | Cosmos DB holds control metadata. Canonical and retrieval data services were initially Candidate directions and are selected for the first implementation by C-32 and C-33. |
| C-22 | A per-space Cosmos DB transactional publication record is the visibility fence that makes staged immutable revisions canonical as one change set. |
| C-23 | Every invocation uses an immutable execution-context snapshot that pins its governing state; stale mutations are rejected. |
| C-24 | Unattended execution requires an immutable, bounded execution grant linked to an attended approval or space policy. |
| C-25 | Ontology rules use Required, Recommended, and Informational levels so structural validation can coexist with flexible knowledge guidance. |
| C-26 | A knowledge-space ontology may include optional example Markdown templates that guide agents but are not strict validation contracts. |
| C-27 | Domain Agents use private deterministic MCP tools with schema-bound JSON contracts; public MCP consumers use only intent-level operations. |
| C-28 | Attachments are immutable source assets linked through provenance and citations; supported extraction produces representations that agents may interpret into canonical Markdown knowledge. |
| C-29 | The first validated increment creates and approves a minimal ontology, contributes one text item, and retrieves cited evidence end to end. |
| C-30 | All knowledge and source content is untrusted data. Immutable instructions, fixed tool manifests, schema validation, provenance, output-policy checks, and fail-closed handling enforce the boundary. |
| C-31 | Immutable ontology versions use canonical JSON with JSON Schema 2020-12 document contracts and are stored with a transactional active pointer in the space's Cosmos DB partition. |
| C-32 | Immutable Markdown revisions use Azure Blob Storage; manifests and active pointers remain in Cosmos DB. |
| C-33 | Azure AI Search supplies the initial hybrid retrieval projection; canonical Blob revisions supply returned evidence. |
| C-34 | The implementation uses .NET and ASP.NET Core with separate public MCP Server and private Tool Services Azure Container Apps. |
| C-35 | Ontology, Contribution, and Retrieval are separate shared, versioned Domain Agent definitions with fixed least-privilege tool manifests. |
| C-36 | Ambiguous contribution returns `clarification_required` without a plan or mutation. Retrieval reports deterministic evidence and completeness without numeric confidence. |
| C-37 | The first increment accepts UTF-8 text and Markdown only; binary attachment extraction is deferred. |
| C-38 | The pilot permits only non-sensitive synthetic or internal test data in one configured Azure geography. |
| C-39 | Prompt-based Foundry Agent Service agents are the default. Agent definitions pin model deployment configuration; promotion requires evaluation and owner approval. |
| C-40 | Long-running migration, bootstrap, rebuild, and deletion orchestration is deferred beyond the synchronous thin slice. |
| C-41 | The thin slice records p50 and p95 latency baselines; correctness and security gates are release-blocking before numeric performance budgets are set. |
| C-42 | The implementation targets .NET 10 and ASP.NET Core, includes Microsoft Agent Framework for typed agent integration and explicit workflow composition, and centrally pins exact stable NuGet versions. |
| C-43 | Infrastructure is authored in Bicep under `infra/` and provisioned through Azure Developer CLI using `azure.yaml`. |
| C-44 | Microsoft Aspire is used for inner-loop orchestration and local service discovery, but Bicep and azd remain the deployment contract. |
| C-45 | Frontends use React and ShadCN/UI. |
| C-46 | Backend unit and component tests use MSTest and current Microsoft Testing Platform patterns. |
| C-47 | Application and infrastructure observability uses OpenTelemetry with Application Insights and Azure Monitor. |
| C-48 | Bicep modules, naming, monitoring, and delivery patterns from Libris Maleficarum are reused selectively after reviewing fit, security, and ownership boundaries. |
| C-49 | Implementation prioritizes simplicity, YAGNI, KISS, testability, readability, naming consistency, clean code, short focused methods, and evidence-driven refactoring. |
| C-50 | SOLID, DRY, separation of concerns, Domain-Driven Design, and Onion Architecture guide implementation pragmatically without unnecessary ceremony. |
| C-51 | Public and private JSON Schemas are repository-owned, surface-separated, versioned assets packaged as immutable runtime bundles and resolved by exact contract version. |
| C-52 | Until `1.0` is published and a formal GA release is declared, any contract may change incompatibly without a backward-compatibility or deprecation guarantee. |
| C-53 | The pilot uses one non-production VNet-integrated Azure Container Apps environment, external ingress only for the public MCP Server, internal ingress for Tool Services and management, and private endpoints for supported canonical and projection data services. |

## Assumptions

| ID | Assumption | If it proves wrong |
| --- | --- | --- |
| A-01 | A team can express its domain structure well enough, through grounding material and conversation, to produce a useful ontology. | The ontology-first model does not hold, and contribution must tolerate largely unstructured knowledge. |
| A-02 | Ontologies change infrequently relative to knowledge. | Migration becomes a routine hot path rather than a maintenance event, changing availability requirements substantially. |
| A-03 | Markdown with front matter is expressive enough for the knowledge a team needs to store. | A richer canonical representation is required, affecting the storage and projection design. |
| A-04 | A knowledge space is small enough that an ontology migration can complete within an acceptable maintenance window. | Migration must become incremental and online, which is a materially harder design. |
| A-05 | Contributors accept that an agent decides where their contribution is placed. | The deterministic path becomes the primary contribution route rather than an exception. |
| A-06 | Calling agents prefer structured evidence over a narrative answer. | The default response shape must be reconsidered. |
| A-08 | Agent-mediated bulk bootstrap can load existing team source assets under normal mutation policy. | External-system connectors require a separate ingestion design. |
| A-10 | One instance serves one organization, so cross-tenant isolation is not required. | Multi-tenancy must be designed in rather than added later. |

## Open questions

### Not blocking the first implementation increment

| ID | Question |
| --- | --- |
| Q-17 | After the text-only increment, what media types, extraction representations, size limits, and retention rules apply to source assets? |
| Q-21 | What production data classification, residency, and retention constraints apply beyond the non-sensitive pilot boundary? |
| Q-22 | What is the acceptable cost envelope per instance and per knowledge space? |
| Q-24 | Which service coordinates messaging or orchestration for long-running ontology and change-set jobs? |
| Q-25 | What is the maximum size of a single canonical knowledge item? |

### Product direction

| ID | Question |
| --- | --- |
| Q-33 | What evidence would justify introducing retrieval across multiple knowledge spaces? |

Q-07, Q-12, Q-16, Q-23, Q-30, Q-31, and Q-32 were resolved during issue #4
reconciliation. Their outcomes are recorded as C-29 to C-50 and in ADR-0020 to
ADR-0027.

## Deferred items

| ID | Item | Reason |
| --- | --- | --- |
| D-01 | Administrative web portal with visual knowledge-graph exploration | Operator value, but not required to prove the agent-first model |
| D-02 | MCP Apps visual surfaces | Depends on host support and on the core contracts being stable |
| D-03 | External-system import connectors | Agent-mediated source-asset bootstrap is in scope; connectors need source-specific ingestion design. |
| D-04 | Retrieval across multiple knowledge spaces | Requires cross-space authorization and ranking semantics |
| D-05 | Multi-tenancy | The initial version is single-tenant; see A-10 |
| D-06 | External identity federation beyond the instance tenant | No confirmed requirement |
| D-07 | Public or anonymous read access | No confirmed requirement |
| D-08 | A dedicated graph database as a projection | Only justified if relationship traversal proves inadequate over the primary projection |
| D-09 | Binary attachment extraction in the first increment | Text and Markdown are sufficient to validate the end-to-end product bet |
| D-10 | Long-running operation orchestration in the first increment | No selected thin-slice operation requires asynchronous orchestration |
| D-11 | Hosted Foundry Agents by default | Prompt-based agents are preferred until a requirement demonstrates the need for custom hosted code |

## Related documents

- [Vision and scope](vision-and-scope)
- [Functional requirements](functional-requirements)
- [Non-functional requirements](non-functional-requirements)
- [Decision register](../decisions/)
