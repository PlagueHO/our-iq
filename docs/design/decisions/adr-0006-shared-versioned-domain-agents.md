---
title: ADR-0006 - Shared versioned domain agents
status: Accepted
---

## ADR-0006: Shared versioned domain agents

## Status

Accepted

## Date and ownership

- Date: 2026-08-18
- Authors: @PlagueHO
- Reviewers: @PlagueHO

## Context

The initial version hosts multiple knowledge spaces. Replicating an agent
deployment for each space would make version control, rollout, and operations
needlessly fragmented.

## Decision

Our IQ Domain Agents are shared, versioned definitions parameterized by the
knowledge-space identifier. A knowledge space does not receive its own agent
deployment.

## Alternatives considered

| Alternative | Rejection or selection rationale |
| --- | --- |
| Deploy an agent per knowledge space | Rejected because it duplicates definitions and complicates rollouts. |
| Share parameterized, versioned definitions | Selected because it centralizes lifecycle and preserves per-space context. |

## Consequences

### Positive

- POS-001: Agent definitions have a single controlled versioning and rollout path.
- POS-002: Space context is explicit on every invocation.

### Negative

- NEG-001: Invocation-time loading and version pinning of space context need
  definition.

## Implementation notes

- IMP-001: The invocation snapshot mechanism for active ontology state is
  defined by [ADR-0016](adr-0016-immutable-execution-context-snapshots).

## References

- REF-001: C-06 in [assumptions and open questions](../product/assumptions-and-open-questions).
- REF-002: NFR-0061 and CON-43.

## Review record

- 2026-08-18: Accepted by @PlagueHO for structural architecture slice #6.
