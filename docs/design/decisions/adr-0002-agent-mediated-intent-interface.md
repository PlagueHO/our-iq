---
title: ADR-0002 - Agent-mediated intent interface
status: Accepted
---

## ADR-0002: Agent-mediated intent interface

## Status

Accepted

## Date and ownership

- Date: 2026-08-18
- Authors: @PlagueHO
- Reviewers: @PlagueHO

## Context

Contributors should not need to understand document layout, while the platform
must preserve its governance of canonical knowledge.

## Decision

The public Model Context Protocol interface exposes intent-level operations
resolved by an agent. It does not expose create, read, update, or delete
operations over knowledge documents.

## Alternatives considered

| Alternative | Rejection or selection rationale |
| --- | --- |
| Public document CRUD | Rejected because it exposes storage concerns and bypasses agent planning. |
| Intent-level, agent-mediated operations | Selected because it supports low-friction contribution and governed change. |

## Consequences

### Positive

- POS-001: Public contracts align with contributor goals rather than storage.
- POS-002: The agent can validate intent against the active ontology.

### Negative

- NEG-001: Tool contracts need clear plan, approval, and error semantics.

## Implementation notes

- IMP-001: The baseline public inventory, idempotency, and errors are defined
  in [API contract baseline](../architecture/api-contract-baseline). Exact
  schema-hosting mechanics remain an implementation decision.

## References

- REF-001: C-02 in [assumptions and open questions](../product/assumptions-and-open-questions).
- REF-002: FR-0030 to FR-0039 and CON-03 to CON-04.

## Review record

- 2026-08-18: Accepted by @PlagueHO for structural architecture slice #6.
