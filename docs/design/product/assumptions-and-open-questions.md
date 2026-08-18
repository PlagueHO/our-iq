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

## Assumptions

| ID | Assumption | If it proves wrong |
| --- | --- | --- |
| A-01 | A team can express its domain structure well enough, through grounding material and conversation, to produce a useful ontology. | The ontology-first model does not hold, and contribution must tolerate largely unstructured knowledge. |
| A-02 | Ontologies change infrequently relative to knowledge. | Migration becomes a routine hot path rather than a maintenance event, changing availability requirements substantially. |
| A-03 | Markdown with front matter is expressive enough for the knowledge a team needs to store. | A richer canonical representation is required, affecting the storage and projection design. |
| A-04 | A knowledge space is small enough that an ontology migration can complete within an acceptable maintenance window. | Migration must become incremental and online, which is a materially harder design. |
| A-05 | Contributors accept that an agent decides where their contribution is placed. | The deterministic path becomes the primary contribution route rather than an exception. |
| A-06 | Calling agents prefer structured evidence over a narrative answer. | The default response shape must be reconsidered. |
| A-07 | Access control at knowledge-space granularity is sufficient for the initial version. | Item-level or hierarchy-level authorization is required, affecting retrieval, projection, and filtering throughout. |
| A-08 | A knowledge space is usable once contributions accumulate through normal use. | Bulk import must be pulled into the initial version. |
| A-09 | Retrieval reading a projection that lags a commit by a short interval is acceptable. | Read-after-write consistency must be guaranteed, constraining the projection design. |
| A-10 | One instance serves one organization, so cross-tenant isolation is not required. | Multi-tenancy must be designed in rather than added later. |

A-07 and A-09 are the assumptions most likely to be wrong and most expensive to
reverse. Both should be validated before the execution model is finalized.

## Open questions

### Blocking the execution and domain model slice

| ID | Question |
| --- | --- |
| Q-01 | By what mechanism does a change set commit atomically across multiple documents and control metadata? |
| Q-02 | How does a shared agent obtain the correct, current ontology on each invocation, and is the ontology version pinned for the duration of a change set? |
| Q-03 | What are the legal knowledge-space lifecycle states and transitions, and what is readable or writable in each? |
| Q-04 | What is the exact role taxonomy and capability granularity, and can permissions attach below a knowledge space? |
| Q-05 | How does an unattended maintenance job prove the attended request or policy that authorized it, and for how long does that authority remain valid? |
| Q-06 | What are the ontology's formal semantics: identity, referential integrity, cardinality, inheritance, and extensibility? |
| Q-07 | How is knowledge content prevented from influencing the instructions or permitted tool set of any agent that processes it? |

### Blocking the API contract slice

| ID | Question |
| --- | --- |
| Q-10 | Does idempotency apply to intent submission, the resulting plan, or the commit, and what happens when identical input is resubmitted after the ontology or knowledge has changed? |
| Q-11 | What is the complete error taxonomy for intent-level operations? |
| Q-12 | What is the shape of an evidence item and its citation, and what confidence or completeness signals accompany it? |
| Q-13 | Which Model Context Protocol specification versions are supported, and what is the compatibility and deprecation policy? |
| Q-14 | Who may configure a space's mutation policy, may it vary by operation risk, who may approve, and does approval expire? |
| Q-15 | What is the bulk import path, is it agent-mediated or operator-only, and does it bypass mutation policy? |

### Blocking quality targets and platform decisions

| ID | Question |
| --- | --- |
| Q-20 | What scale should the initial version be designed for, in users, spaces, items per space, and item size? |
| Q-21 | Which availability, recovery point, and recovery time objectives apply? |
| Q-22 | What data classification, residency, and retention constraints apply? |
| Q-23 | What is the acceptable cost envelope per instance and per knowledge space? |
| Q-24 | Which model deployments back each agent, and who governs prompt and model changes? |
| Q-25 | Which platform services are selected for canonical storage, control metadata, retrieval projection, messaging, and orchestration? |

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
