---
title: ADR-0005 - Microsoft Foundry Agent Service runtime
status: Accepted
---

## ADR-0005: Microsoft Foundry Agent Service runtime

## Status

Accepted

## Date and ownership

- Date: 2026-08-18
- Authors: @PlagueHO
- Reviewers: @PlagueHO

## Context

The architecture requires backend agents to interpret intent, plan retrieval and
mutation work, and support governed ontology operations.

## Decision

Microsoft Foundry Agent Service is the required runtime for Our IQ backend
agents.

## Alternatives considered

| Alternative | Rejection or selection rationale |
| --- | --- |
| Self-host an agent runtime | Rejected for the initial architecture because it does not meet the confirmed runtime constraint. |
| Use Microsoft Foundry Agent Service | Selected as the required backend agent runtime. |

## Consequences

### Positive

- POS-001: Backend-agent deployment has a confirmed runtime boundary.
- POS-002: Agent concerns can be separated from public and data-service concerns.

### Negative

- NEG-001: Agent model deployments, prompt governance, and evaluation remain
  unresolved.

## Implementation notes

- IMP-001: Model selection and agent evaluation strategy remain open; see Q-23
  and NFR-0025.

## References

- REF-001: C-05 in [assumptions and open questions](../product/assumptions-and-open-questions).
- REF-002: CON-01 and NFR-0061.

## Review record

- 2026-08-18: Accepted by @PlagueHO for structural architecture slice #6.
