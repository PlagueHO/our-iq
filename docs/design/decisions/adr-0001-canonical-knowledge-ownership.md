---
title: ADR-0001 - Canonical knowledge ownership
status: Accepted
---

## ADR-0001: Canonical knowledge ownership

## Status

Accepted

## Date and ownership

- Date: 2026-08-18
- Authors: @PlagueHO
- Reviewers: @PlagueHO

## Context

Knowledge must remain attributable, governed, and reconstructable. Allowing
canonical writes to bypass Our IQ would make those guarantees unenforceable.

## Decision

Our IQ owns canonical knowledge. All writes to canonical knowledge pass through
Our IQ and are subject to its authorization, policy, validation, provenance,
and versioning controls.

## Alternatives considered

| Alternative | Rejection or selection rationale |
| --- | --- |
| Allow direct writes to canonical stores | Rejected because governance and provenance could be bypassed. |
| Keep canonical knowledge in external source systems | Rejected because the initial version requires Our IQ to govern its own canonical knowledge. |
| Route all writes through Our IQ | Selected because it preserves the required control point. |

## Consequences

### Positive

- POS-001: Canonical changes have a single governed path.
- POS-002: Provenance, policy, and atomicity controls apply consistently.

### Negative

- NEG-001: Every canonical-write integration must use an Our IQ operation.

## Implementation notes

- IMP-001: The change-set publication protocol is defined by
  [ADR-0015](adr-0015-transactional-change-set-visibility-fence). Job
  orchestration remains open; see Q-24.

## References

- REF-001: C-01 in [assumptions and open questions](../product/assumptions-and-open-questions).
- REF-002: FR-0035, FR-0037, and NFR-0020 to NFR-0021.

## Review record

- 2026-08-18: Accepted by @PlagueHO for structural architecture slice #6.
