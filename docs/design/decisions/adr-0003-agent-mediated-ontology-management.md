---
title: ADR-0003 - Agent-mediated ontology management
status: Accepted
---

## ADR-0003: Agent-mediated ontology management

## Status

Accepted

## Date and ownership

- Date: 2026-08-18
- Authors: @PlagueHO
- Reviewers: @PlagueHO

## Context

An ontology defines how knowledge in a space is organized and changes. Its
design must remain accessible to teams without exposing low-level editing as a
public protocol concern.

## Decision

Ontology management is agent-mediated. The public interface does not expose
low-level ontology create, update, or delete operations.

## Alternatives considered

| Alternative | Rejection or selection rationale |
| --- | --- |
| Public low-level ontology CRUD | Rejected because it exposes internal representation and weakens guided governance. |
| Agent-mediated ontology design and refinement | Selected because it can use supplied grounding material and produce reviewable proposals. |

## Consequences

### Positive

- POS-001: Ontology changes can be reasoned about in their domain context.
- POS-002: Proposals and migrations can be reviewed before commitment.

### Negative

- NEG-001: Formal ontology semantics and migration rules require later design.

## Implementation notes

- IMP-001: Invocation pinning is defined by
  [ADR-0016](adr-0016-immutable-execution-context-snapshots), and the
  flexible rule model is defined in the logical knowledge model. Lifecycle
  transitions remain open; see Q-03.

## References

- REF-001: C-03 in [assumptions and open questions](../product/assumptions-and-open-questions).
- REF-002: FR-0020 to FR-0028.

## Review record

- 2026-08-18: Accepted by @PlagueHO for structural architecture slice #6.
