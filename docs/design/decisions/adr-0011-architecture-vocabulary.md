---
title: ADR-0011 - Architecture vocabulary
status: Accepted
---

## ADR-0011: Architecture vocabulary

## Status

Accepted

## Date and ownership

- Date: 2026-08-18
- Authors: @PlagueHO
- Reviewers: @PlagueHO

## Context

The architecture spans public protocol handling, agent reasoning, private tool
execution, and data access. Unstable terminology would obscure boundaries and
make future decisions ambiguous.

## Decision

Architecture documentation uses these terms: Client Agent, Our IQ MCP Server,
Our IQ Domain Agents, Our IQ Tool Services, and Our IQ Data Services.

## Alternatives considered

| Alternative | Rejection or selection rationale |
| --- | --- |
| Use generic terms such as service and backend interchangeably | Rejected because they do not identify responsibility or trust boundaries. |
| Use the defined vocabulary | Selected because it gives architecture views stable terms. |

## Consequences

### Positive

- POS-001: Architecture views have consistent labels for responsibilities.
- POS-002: Public and private interfaces can be distinguished consistently.

### Negative

- NEG-001: New documentation must use the terms precisely rather than treat them
  as generic implementation names.

## Implementation notes

- IMP-001: This decision does not determine component or deployment granularity.

## References

- REF-001: C-14 in [assumptions and open questions](../product/assumptions-and-open-questions).
- REF-002: CON-31 and [glossary](../architecture/arc42/12-glossary).

## Review record

- 2026-08-18: Accepted by @PlagueHO for structural architecture slice #6.
