---
title: ADR-0004 - Per-space mutation policy
status: Accepted
---

## ADR-0004: Per-space mutation policy

## Status

Accepted

## Date and ownership

- Date: 2026-08-18
- Authors: @PlagueHO
- Reviewers: @PlagueHO

## Context

Knowledge spaces have independent governance needs. A single instance-wide
approval rule would either over-constrain routine work or under-protect
sensitive spaces.

## Decision

Each knowledge space configures a mutation policy: automatic commit,
contributor confirmation, or review workflow. A proposed change is evaluated
against the target space's policy before canonical commitment.

## Alternatives considered

| Alternative | Rejection or selection rationale |
| --- | --- |
| One instance-wide policy | Rejected because knowledge spaces have different governance needs. |
| Always require review | Rejected because it adds avoidable friction for lower-risk spaces. |
| Per-space policy | Selected because it makes governance explicit at the space boundary. |

## Consequences

### Positive

- POS-001: Spaces can balance contribution speed and governance independently.
- POS-002: Approval evidence can become part of change-set provenance.

### Negative

- NEG-001: Policy administration and approval-expiry semantics require definition.

## Implementation notes

- IMP-001: Policy authority, operation-risk variation, and approval expiry remain
  open; see Q-14.

## References

- REF-001: C-04 in [assumptions and open questions](../product/assumptions-and-open-questions).
- REF-002: FR-0033 to FR-0034.

## Review record

- 2026-08-18: Accepted by @PlagueHO for structural architecture slice #6.
