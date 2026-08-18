---
title: ADR-0007 - Agent identity and execution context
status: Accepted
---

## ADR-0007: Agent identity and execution context

## Status

Accepted

## Date and ownership

- Date: 2026-08-18
- Authors: @PlagueHO
- Reviewers: @PlagueHO

## Context

An agent may act for a user or run approved maintenance without a user present.
Authorization and audit must distinguish the initiating human from the agent
performing work.

## Decision

Backend agents use a Microsoft Entra agent identity. Attended execution
preserves the initiating user through an on-behalf-of flow. Unattended
execution is restricted to work already authorized by an attended request or
explicit space policy and carries the agent identity alone.

## Alternatives considered

| Alternative | Rejection or selection rationale |
| --- | --- |
| Use only a human identity | Rejected because agent actions need distinct attribution and capability limits. |
| Use only a service identity | Rejected because attended work must preserve its initiating user. |
| Distinct agent identity with attended and restricted unattended contexts | Selected because it supports authorization intersection and audit. |

## Consequences

### Positive

- POS-001: User and agent attribution remain distinguishable.
- POS-002: Unattended authority has an explicit governance boundary.

### Negative

- NEG-001: Delegation proof, duration, and renewal semantics require definition.

## Implementation notes

- IMP-001: Unattended authorization evidence is defined by
  [ADR-0017](adr-0017-bounded-unattended-execution-grants).

## References

- REF-001: C-07 in [assumptions and open questions](../product/assumptions-and-open-questions).
- REF-002: FR-0010 to FR-0015 and CON-02.

## Review record

- 2026-08-18: Accepted by @PlagueHO for structural architecture slice #6.
