---
title: ADR-0010 - Atomic versioned change sets
status: Accepted
---

## ADR-0010: Atomic versioned change sets

## Status

Accepted

## Date and ownership

- Date: 2026-08-18
- Authors: @PlagueHO
- Reviewers: @PlagueHO

## Context

Agent-planned work can affect multiple canonical documents and control records.
Partial canonical updates would break provenance, ontology conformance, and
trust in the knowledge space.

## Decision

An approved agent-planned mutation commits as one atomic, versioned change set.
Partial canonical commits are not permitted or observable.

## Alternatives considered

| Alternative | Rejection or selection rationale |
| --- | --- |
| Commit each document independently | Rejected because a multi-document plan could become partially visible. |
| Use best-effort compensation only | Rejected because it does not satisfy the no-partial-commit requirement. |
| Commit one atomic, versioned change set | Selected because it preserves canonical consistency. |

## Consequences

### Positive

- POS-001: A change is attributable as one unit with a stable version.
- POS-002: Validation can apply to the complete planned mutation.

### Negative

- NEG-001: The cross-document commit protocol requires immutable staging and a
  separate transactional visibility fence.

## Implementation notes

- IMP-001: The atomic publication mechanism is defined by
  [ADR-0015](adr-0015-transactional-change-set-visibility-fence).

## References

- REF-001: C-10 in [assumptions and open questions](../product/assumptions-and-open-questions).
- REF-002: FR-0035 to FR-0040, CON-07, and NFR-0020.

## Review record

- 2026-08-18: Accepted by @PlagueHO for structural architecture slice #6.
