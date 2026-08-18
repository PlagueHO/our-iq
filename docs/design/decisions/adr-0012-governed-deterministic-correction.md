---
title: ADR-0012 - Governed deterministic correction
status: Accepted
---

## ADR-0012: Governed deterministic correction

## Status

Accepted

## Date and ownership

- Date: 2026-08-18
- Authors: @PlagueHO
- Reviewers: @PlagueHO

## Context

The everyday public interface is agent-mediated intent, but factual corrections
and compliance removals may require a deterministic action against one known
document.

## Decision

An authorized steward or operator may deterministically correct or remove a
specific document through a privileged path. This path remains subject to
knowledge-space policy, authorization, versioning, and audit.

## Alternatives considered

| Alternative | Rejection or selection rationale |
| --- | --- |
| Require all corrections to be agent-mediated | Rejected because urgent or compliance actions may need deterministic targeting. |
| Expose public document CRUD | Rejected because the exception must not become an ordinary contributor path. |
| Provide a privileged governed path | Selected because it balances deterministic correction with control. |

## Consequences

### Positive

- POS-001: Identified content can be corrected or removed without agent interpretation.
- POS-002: The exception remains attributable and governed.

### Negative

- NEG-001: Exact authorization and audit requirements for the path need contract
  definition.

## Implementation notes

- IMP-001: This is a management capability, separate from ordinary public
  contributor operations.

## References

- REF-001: C-11 in [assumptions and open questions](../product/assumptions-and-open-questions).
- REF-002: FR-0038 to FR-0039 and FR-0068.

## Review record

- 2026-08-18: Accepted by @PlagueHO for structural architecture slice #6.
