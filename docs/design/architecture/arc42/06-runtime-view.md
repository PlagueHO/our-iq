---
title: arc42 6 - Runtime view
status: Proposed
---

## 6. Runtime view

## Purpose

Describe important runtime scenarios and interactions.

## Runtime scenarios

The detailed flows are [proposed agentic-execution behaviour](../agentic-execution-model).
They apply accepted structural decisions but do not define public MCP contracts.

| Scenario | Runtime boundary | Detailed view |
| --- | --- | --- |
| Attended contribution | Intent, snapshot, agent plan, policy route, visibility-fence commit | [Attended contribution](../agentic-execution-model#attended-contribution-and-change-set-publication) |
| Attended retrieval | Intent, snapshot, authorized candidate retrieval, canonical citation | [Retrieval and optional synthesis](../agentic-execution-model#retrieval-and-optional-synthesis) |
| Privileged correction | Management request, authorization, revision publication | [Privileged deterministic correction](../agentic-execution-model#privileged-deterministic-correction) |
| Unattended maintenance | Agent identity, bounded grant, private tool validation | [Identity, authorization, and unattended execution](../agentic-execution-model#identity-authorization-and-unattended-execution) |
| Ontology migration | Agent-mediated proposal, approval, monitored migration job | [C4 ontology lifecycle](../c4/component#retrieval-and-ontology-lifecycle) |

The change-set state diagram separates canonical commitment from projection
work. The detailed logical model defines the validations applied before staging.

## Open questions

- Which scenario is the first validation target?
- Which operations become asynchronous jobs and what service coordinates them?
- What exact public errors surface stale snapshots, validation findings, and
  grant denials?
