---
title: C4 component view
status: Proposed
---

## C4 component view

## Purpose

Show the proposed responsibility boundaries within the public MCP Server and
private Tool Services. These are logical components, not deployed units or
public contracts.

## Public orchestration

```mermaid
flowchart LR
  client[Client Agent]
  auth[Authentication and authorization]
  context[Execution-context coordinator]
  invoke[Domain Agent invoker]
  result[Intent outcome formatter]
  agent[Shared Domain Agent]

  client -->|MCP intent and identity context| auth
  auth -->|authorized space-scoped intent| context
  context -->|immutable execution-context snapshot| invoke
  invoke -->|private invocation and pinned context| agent
  agent -->|plan, evidence, or outcome| result
  result -->|MCP response| client
```

| Component | Responsibility | Collaborators |
| --- | --- | --- |
| Authentication and authorization | Validate inbound identity and authorize public intent at the space boundary. | Microsoft Entra ID; execution-context coordinator |
| Execution-context coordinator | Create immutable snapshots that pin state for an invocation. | Control metadata; Domain Agent invoker |
| Domain Agent invoker | Invoke a versioned shared Domain Agent with private context. | Microsoft Foundry Agent Service |
| Intent outcome formatter | Return change plans, cited evidence, operation status, or errors without exposing document CRUD. | Client Agent |

## Contribution and change-set handling

```mermaid
flowchart LR
  agent[Domain Agent]
  planning[Plan and ontology validation]
  policy[Policy routing]
  staging[Immutable revision staging]
  publication[Change-set publication]
  ledger[(Control metadata ledger)]
  markdown[(Canonical Markdown revisions)]

  agent -->|proposed item revisions and rationale| planning
  planning -->|validation findings and plan| policy
  policy -->|approved plan and execution context| staging
  staging -->|non-canonical immutable revisions| markdown
  staging -->|revision manifest candidates| publication
  publication -->|transactional manifest and active pointer| ledger
  publication -->|committed revisions referenced by manifest| markdown
```

| Component | Responsibility | Collaborators |
| --- | --- | --- |
| Plan and ontology validation | Evaluate Required, Recommended, and Informational rules against the pinned ontology. | Domain Agent; policy routing |
| Policy routing | Choose automatic commit, confirmation, or review and retain approval evidence. | Control metadata; management path |
| Immutable revision staging | Persist non-canonical candidate revisions. | Canonical Markdown revisions |
| Change-set publication | Publish a complete manifest and active pointer through the visibility fence. | Cosmos DB control metadata |

## Retrieval and ontology lifecycle

```mermaid
flowchart LR
  agent[Domain Agent]
  retrieval[Retrieval coordinator]
  projection((Derived projection))
  canonical[(Canonical revisions)]
  evidence[Citation assembler]
  ontology[Ontology proposal and migration coordinator]
  jobs[Operation status and grants]

  agent -->|retrieval plan| retrieval
  retrieval -->|candidate lookup| projection
  retrieval -->|canonical revision read| canonical
  canonical -->|cited source content| evidence
  evidence -->|structured grounded evidence| agent
  agent -->|ontology intent| ontology
  ontology -->|migration plan and context| jobs
  jobs -->|authorized long-running work status| ontology
```

| Component | Responsibility | Collaborators |
| --- | --- | --- |
| Retrieval coordinator | Obtain authorized candidate knowledge and verify canonical citation sources. | Derived projections; canonical revisions |
| Citation assembler | Build structured grounded evidence defined by the API contract baseline. | Retrieval coordinator; Domain Agent |
| Ontology proposal and migration coordinator | Create reviewable ontology proposals and governed migration plans. | Domain Agent; operation status |
| Operation status and grants | Track long-running work and validate bounded unattended authority. | Control metadata; Tool Services |

## Related views

- [Agentic execution model](../agentic-execution-model) defines the runtime
  flows, identity-sensitive hops, and state lifecycle.
- [Logical knowledge model](../logical-knowledge-model) defines the canonical
  item and ontology-rule semantics.
- The [API contract baseline](../api-contract-baseline) binds initial private
  tools to the three Domain Agent definitions. Logical Tool Service components
  share one private deployable for the pilot.
