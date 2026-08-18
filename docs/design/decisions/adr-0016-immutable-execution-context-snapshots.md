---
title: ADR-0016 - Immutable execution-context snapshots
status: Accepted
---

## ADR-0016: Immutable execution-context snapshots

## Status

Accepted

## Date and ownership

- Date: 2026-08-18
- Authors: @PlagueHO
- Reviewers: @PlagueHO

## Context

Shared Domain Agents are parameterized by knowledge space. Planning against
mutable ontology, policy, agent, or canonical state could create a plan whose
meaning changes before it reaches a Tool Service. The system must attribute
agent behaviour and reject stale mutation safely.

## Decision

Every agent invocation receives an immutable execution-context snapshot. The
snapshot pins the knowledge space and lifecycle state, Domain Agent definition
version, active ontology version and digest, mutation-policy version, canonical
head version, identities, trace information, and any unattended execution
grant. Tool Services validate the snapshot for state-sensitive operations and
reject stale mutations.

## Alternatives considered

| Alternative | Rejection or selection rationale |
| --- | --- |
| Load current state dynamically throughout an invocation | Rejected because planning meaning can change during execution and cannot be reproduced reliably. |
| Pin ontology only | Rejected because policy, canonical state, and agent version also affect governance and attribution. |
| Pin immutable execution context for every invocation | Selected because it supports reproducibility, audit, and safe stale-write detection. |

## Consequences

### Positive

- POS-001: Every plan and change set identifies the context that governed it.
- POS-002: Tools can reject stale mutations rather than silently applying them
  to newer state.
- POS-003: Agent evaluation can replay a representative pinned context.

### Negative

- NEG-001: Callers must re-plan after a relevant state change.
- NEG-002: Context snapshot storage, expiry, and token representation need
  implementation design.

## Implementation notes

- IMP-001: Context snapshots are private architecture records, not public MCP
  request shapes.
- IMP-002: Retrieval may retain the snapshot for attribution even when
  projection freshness differs from canonical head.

## References

- REF-001: [ADR-0006](adr-0006-shared-versioned-domain-agents).
- REF-002: [ADR-0007](adr-0007-agent-identity-and-execution-context).
- REF-003: FR-0036, FR-0040, NFR-0024, and NFR-0061.

## Review record

- 2026-08-18: Accepted by @PlagueHO for design slice #7.
