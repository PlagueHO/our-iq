---
title: Assumptions, open questions, and deferred items
status: Proposed
owner: TBD
reviewers: TBD
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
| C-21 | Cosmos DB holds control metadata. Azure Blob Storage for canonical knowledge and Azure AI Search for retrieval remain Candidate directions, not deployed implementation claims. |
| C-22 | A per-space Cosmos DB transactional publication record is the visibility fence that makes staged immutable revisions canonical as one change set. |
| C-23 | Every invocation uses an immutable execution-context snapshot that pins its governing state; stale mutations are rejected. |
| C-24 | Unattended execution requires an immutable, bounded execution grant linked to an attended approval or space policy. |
| C-25 | Ontology rules use Required, Recommended, and Informational levels so structural validation can coexist with flexible knowledge guidance. |

## Assumptions

| ID | Assumption | If it proves wrong |
| --- | --- | --- |
| A-01 | A team can express its domain structure well enough, through grounding material and conversation, to produce a useful ontology. | The ontology-first model does not hold, and contribution must tolerate largely unstructured knowledge. |
| A-02 | Ontologies change infrequently relative to knowledge. | Migration becomes a routine hot path rather than a maintenance event, changing availability requirements substantially. |
| A-03 | Markdown with front matter is expressive enough for the knowledge a team needs to store. | A richer canonical representation is required, affecting the storage and projection design. |
| A-04 | A knowledge space is small enough that an ontology migration can complete within an acceptable maintenance window. | Migration must become incremental and online, which is a materially harder design. |
| A-05 | Contributors accept that an agent decides where their contribution is placed. | The deterministic path becomes the primary contribution route rather than an exception. |
| A-06 | Calling agents prefer structured evidence over a narrative answer. | The default response shape must be reconsidered. |
| A-08 | A knowledge space is usable once contributions accumulate through normal use. | Bulk import must be pulled into the initial version. |
| A-10 | One instance serves one organization, so cross-tenant isolation is not required. | Multi-tenancy must be designed in rather than added later. |

## Open questions

### Blocking the execution and domain model slice

| ID | Question |
| --- | --- |
| Q-03 | What are the legal knowledge-space lifecycle states and transitions, and what is readable or writable in each? |
| Q-04 | Within the confirmed role set (Owner, Ontology Manager, Contributor, Reader), what exact capabilities does each role grant, and how is group assignment and delegation handled? |
| Q-07 | How is knowledge content prevented from influencing the instructions or permitted tool set of any agent that processes it? |

### Blocking the API contract slice

| ID | Question |
| --- | --- |
| Q-10 | Does idempotency apply to intent submission, the resulting plan, or the commit, and what happens when identical input is resubmitted after the ontology or knowledge has changed? |
| Q-11 | What is the complete error taxonomy for intent-level operations? |
| Q-12 | What is the shape of an evidence item and its citation, and what confidence or completeness signals accompany it? |
| Q-13 | Given the confirmed target of MCP spec `2026-07-28`, what is the compatibility and deprecation policy as later spec versions are released? |
| Q-14 | Who may configure a space's mutation policy, may it vary by operation risk, who may approve, and does approval expire? |
| Q-15 | What is the bulk import path, is it agent-mediated or operator-only, and does it bypass mutation policy? |

### Blocking quality targets and platform decisions

| ID | Question |
| --- | --- |
| Q-21 | What data classification, residency, and retention constraints apply? |
| Q-22 | What is the acceptable cost envelope per instance and per knowledge space? |
| Q-23 | Which model deployments back each agent, and who governs prompt and model changes? |
| Q-24 | Which service coordinates messaging or orchestration for long-running ontology and change-set jobs? |
| Q-25 | What is the maximum size of a single canonical knowledge item? |

### Product direction

| ID | Question |
| --- | --- |
| Q-30 | Which single workflow should the first validated increment prove? |
| Q-31 | What is the response when a contribution is ambiguous rather than invalid? |
| Q-32 | Should typed relationships between knowledge items be validated against the ontology, or may they be arbitrary? |
| Q-33 | What evidence would justify introducing retrieval across multiple knowledge spaces? |

## Deferred items

| ID | Item | Reason |
| --- | --- | --- |
| D-01 | Administrative web portal with visual knowledge-graph exploration | Operator value, but not required to prove the agent-first model |
| D-02 | MCP Apps visual surfaces | Depends on host support and on the core contracts being stable |
| D-03 | Bulk import from external systems | Likely the first follow-up; see A-08 |
| D-04 | Retrieval across multiple knowledge spaces | Requires cross-space authorization and ranking semantics |
| D-05 | Multi-tenancy | The initial version is single-tenant; see A-10 |
| D-06 | External identity federation beyond the instance tenant | No confirmed requirement |
| D-07 | Public or anonymous read access | No confirmed requirement |
| D-08 | A dedicated graph database as a projection | Only justified if relationship traversal proves inadequate over the primary projection |

## Related documents

- [Vision and scope](vision-and-scope)
- [Functional requirements](functional-requirements)
- [Non-functional requirements](non-functional-requirements)
- [Decision register](../decisions/)
