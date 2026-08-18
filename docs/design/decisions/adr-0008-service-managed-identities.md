---
title: ADR-0008 - Service managed identities
status: Accepted
---

## ADR-0008: Service managed identities

## Status

Accepted

## Date and ownership

- Date: 2026-08-18
- Authors: @PlagueHO
- Reviewers: @PlagueHO

## Context

Tool Services require access to platform dependencies while preserving the user
and agent identities as authorization and audit context rather than as shared
infrastructure credentials.

## Decision

Our IQ Tool Services access Azure Data Services using their own managed
identities. User and agent identities remain authorization and audit context;
they do not become the dependency access identity.

## Alternatives considered

| Alternative | Rejection or selection rationale |
| --- | --- |
| Share a dependency credential | Rejected because it weakens least privilege and audit isolation. |
| Access dependencies as the user or agent | Rejected because dependency access should be scoped to service responsibility. |
| Use a managed identity per service boundary | Selected because it supports least privilege. |

## Consequences

### Positive

- POS-001: Dependency permissions can be granted per service responsibility.
- POS-002: Application-level attribution remains available alongside access logs.

### Negative

- NEG-001: Permission assignments and network controls must be designed per
  dependency.

## Implementation notes

- IMP-001: Specific Azure service and network choices remain candidates; see
  Q-24.

## References

- REF-001: C-08 in [assumptions and open questions](../product/assumptions-and-open-questions).
- REF-002: NFR-0002 to NFR-0004.

## Review record

- 2026-08-18: Accepted by @PlagueHO for structural architecture slice #6.
