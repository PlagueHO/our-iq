---
title: ADR-0019 - Space lifecycle and capabilities
status: Accepted
---

## ADR-0019: Space lifecycle and capabilities

## Status

Accepted

## Date and ownership

- Date: 2026-08-19
- Authors: @PlagueHO
- Reviewers: @PlagueHO

## Context

Public and private contracts need deterministic state gates and authorization.
The fixed role names alone do not establish which actor may transition a space,
administer policy, or approve a plan.

## Decision

Knowledge spaces use `draft`, `pending`, `active`, `readonly`, `maintenance`,
`retired`, `deleting`, and `deleted`. The transition and capability matrices in
the API contract baseline are normative. Owners delegate and revoke space roles;
services enforce the intersection of user permission and agent capability.

## Alternatives considered

| Alternative | Rejection or selection rationale |
| --- | --- |
| A reduced active/readonly/retired lifecycle | Rejected because setup, governed maintenance, and deletion need distinct gates. |
| Broad administrative authority for Ontology Managers | Rejected because role and policy administration require Owner authority. |
| Explicit state and least-privilege capability matrices | Selected because each operation can be authorized and audited consistently. |

## Consequences

### Positive

- POS-001: Contract consumers can determine legal operations from stable state.
- POS-002: Role delegation and approvals have an explicit authority boundary.

### Negative

- NEG-001: Each new operation must declare required capability and legal states.

## Implementation notes

- IMP-001: This decision does not choose an identity-provider API or role-store
  implementation.
- IMP-002: Private tools still require execution-context validation where
  state-sensitive.

## References

- REF-001: [API contract baseline](../architecture/api-contract-baseline).
- REF-002: ADR-0004 and ADR-0016.

## Review record

- 2026-08-19: Accepted by @PlagueHO for API-contract baseline slice #8.
