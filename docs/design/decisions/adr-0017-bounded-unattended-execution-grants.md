---
title: ADR-0017 - Bounded unattended execution grants
status: Accepted
---

## ADR-0017: Bounded unattended execution grants

## Status

Accepted

## Date and ownership

- Date: 2026-08-18
- Authors: @PlagueHO
- Reviewers: @PlagueHO

## Context

Unattended maintenance has no initiating user delegation at execution time, but
it must remain limited to work authorized by an attended request or explicit
knowledge-space policy. Agent identity alone does not provide sufficient proof,
scope, expiry, or cost control.

## Decision

Unattended work requires an immutable execution grant stored in control
metadata. A grant identifies its authorizing attended approval or
knowledge-space policy, space, permitted operation scope, agent capability,
validity interval, and applicable execution limits. Tool Services validate the
grant on every private call and record its use, denial, revocation, or expiry.

## Alternatives considered

| Alternative | Rejection or selection rationale |
| --- | --- |
| Trust the agent identity alone | Rejected because it cannot prove the scope or duration of unattended authority. |
| Re-evaluate a mutable policy only when work starts | Rejected because it does not retain the authorizing evidence for audit or bound an approved request. |
| Use immutable, bounded execution grants | Selected because it provides explicit, auditable, revocable authority. |

## Consequences

### Positive

- POS-001: Unattended authority is attributable to a concrete governing
  decision.
- POS-002: Scope, duration, and limits can be enforced independently of agent
  identity.

### Negative

- NEG-001: Grant issuance, revocation, and limit accounting need implementation
  design.
- NEG-002: A denied or expired grant can require operator intervention or a new
  approval.

## Implementation notes

- IMP-001: A grant is authorization evidence, not an infrastructure credential.
- IMP-002: The exact public management contract for grants remains for the
  API-contract slice.

## References

- REF-001: [ADR-0007](adr-0007-agent-identity-and-execution-context).
- REF-002: FR-0014, FR-0015, NFR-0002, and NFR-0082.

## Review record

- 2026-08-18: Accepted by @PlagueHO for design slice #7.
