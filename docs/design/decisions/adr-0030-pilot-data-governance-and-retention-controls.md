---
title: ADR-0030 - Pilot data governance and retention controls
status: Accepted
---

## ADR-0030: Pilot data governance and retention controls

## Status

Accepted

## Date and ownership

- Date: 2026-08-23
- Authors: @PlagueHO
- Reviewers: @PlagueHO

## Context

The pilot is intentionally limited to one deployment-configured Azure geography
and non-sensitive synthetic or internal test data. That boundary is sufficient
for the thin slice but does not define the controls needed before production
data is admitted. Q-21 leaves classification, residency, retention, audit
retention, and production network isolation unresolved.

ADR-0022 and ADR-0029 establish the initial Azure data roles and pilot private
network shape. This decision adds governance requirements without claiming that
the required production controls or environments have been deployed.

## Decision

Our IQ adopts the following production data-governance baseline:

1. Every knowledge space has a declared classification before activation.
   The classification applies to canonical knowledge, referenced assets,
   control records that reveal the space contents, and every derived
   projection. The pilot admits only non-sensitive synthetic or internal test
   data. Production admission of confidential, regulated, or restricted data
   requires a later decision with the applicable policy and service evidence;
   this ADR does not authorize those classes.
2. Each deployment declares an approved geography set. One-geography
   deployments keep knowledge, derived projections, backups, and recovery
   copies within that geography. Multi-geography production operation requires
   an explicit approved geography set and evidence that every copy remains
   within it. Cross-geography replication or export is disabled unless that
   evidence exists.
3. Canonical and derived knowledge is retained while a space is active and
   for no longer than 30 days after an approved space or item deletion. Expired
   content must be removed from active stores, projections, and new backups;
   recovery copies follow the same classification and geography rules and must
   expire within 35 days unless a documented legal hold applies.
4. Audit evidence is separate from diagnostic telemetry in policy and access
   control. Audit records for security-relevant, authorization, governance,
   knowledge-changing, deletion, restore, and policy decisions are immutable,
   append-only, access-controlled, and retained for at least 365 days.
   Diagnostic logs and traces contain no knowledge content, secrets, or
   unnecessary identity data and use an environment-specific retention period no
   longer than 30 days by default.
5. Production data services are private by default. Blob Storage, Cosmos DB,
   and Azure AI Search use private connectivity, public network access is
   disabled, and the private Tool Services path is the only application data
   path. Tool Services and management surfaces remain internally reachable;
   public ingress is not a fallback for data access.
6. Storage and retrieval controls preserve the authoritative roles in ADR-0022:
   Blob Storage remains the canonical content store, Cosmos DB remains the
   control and ontology store, and Azure AI Search remains rebuildable. A
   deletion or classification-policy change is not complete until affected
   derived data and eligible recovery copies are handled.
7. Promotion from pilot to production is a governance gate, not an automatic
   environment change. Before production admission, the deployment must have
   evidence for classification enforcement, geography and backup placement,
   retention and deletion behaviour, audit access and retention, private
   connectivity, and restore/purge validation. This ADR does not assert that
   any of that evidence or deployment exists.

These rules establish the measurable baselines for NFR-0005, NFR-0007, and
NFR-0008. The requirement register remains `Proposed` until implementation
evidence is available.

## Required control matrix

| Area | Required control | Evidence before production admission |
| --- | --- | --- |
| Blob Storage | Private connectivity, public access disabled, encryption at rest and in transit, classification-compatible access policy, deletion and recovery-copy expiry | Configuration review plus deletion and restore test |
| Cosmos DB | Private connectivity, public access disabled, least-privilege service identity, geography-constrained backups, retention for transient control data | Configuration review plus backup placement and purge test |
| Azure AI Search | Private connectivity, public access disabled, derived-data classification no higher than its source, purge/rebuild path | Configuration review plus index purge and rebuild test |
| Telemetry | Separate audit policy, no knowledge payloads or secrets, access control, documented retention and alert scopes | Sampled-event review plus retention and access test |
| Backups | Same classification and approved geography as the source, bounded expiry, protected restore access, tested purge/restore process | Backup inventory, restore test, and expiry evidence |
| Private connectivity | Private endpoints for supported data services, private DNS, disabled public data access, internal Tool Services and management ingress | Network reachability test proving no public fallback |

## Alternatives considered

| Alternative | Rejection or selection rationale |
| --- | --- |
| Keep the non-sensitive pilot boundary and defer all production controls | Rejected because V1 completion needs measurable admission criteria and a safe migration boundary. |
| Permit all business classifications by default | Rejected because classification-specific handling and regulatory obligations are not yet established. |
| Use diagnostic telemetry as the audit system of record | Rejected because diagnostic retention, mutability, and access patterns do not by themselves satisfy audit governance. |
| Permit public data-service endpoints when private connectivity is inconvenient | Rejected because it weakens the data boundary and creates an unsafe fallback path. |
| Select a specific audit or backup product in this ADR | Rejected because the policy is required now, while service selection and implementation evidence belong to the delivery design. |

## Consequences

### Positive

- POS-001: Production admission has explicit, testable governance gates rather
  than relying on the pilot's non-sensitive-data restriction.
- POS-002: Classification, geography, retention, and audit rules apply
  consistently across canonical data, projections, backups, and operational
  evidence.
- POS-003: Private connectivity and no-public-fallback rules preserve the
  application and data trust boundaries established by ADR-0029.
- POS-004: Service selection can satisfy the policy without prematurely
  selecting an audit or backup implementation.

### Negative

- NEG-001: Production rollout requires evidence for purge, restore, retention,
  geography, and network controls before sensitive data can be admitted.
- NEG-002: Bounded retention and deletion require coordination across canonical,
  derived, telemetry, and recovery-copy lifecycles.
- NEG-003: A future multi-geography design needs a separate approved geography
  set and replication evidence; this ADR does not provide that design.

## Implementation notes

- IMP-001: Store the declared classification and approved geography as
  governance metadata for every knowledge space and validate them at activation,
  mutation, projection, backup, and restore boundaries.
- IMP-002: Implement retention jobs and deletion verification for canonical
  content, projections, transient control records, telemetry, audit evidence,
  and recovery copies; record exceptions such as legal holds explicitly.
- IMP-003: Keep audit records policy-distinct from diagnostic logs and traces,
  and ensure telemetry filters remove knowledge content, secrets, and
  unnecessary identity data before emission.
- IMP-004: Validate private endpoint, private DNS, public-access, ingress, and
  managed-identity settings with deployment preview and network reachability
  tests before production admission.
- IMP-005: Treat the pilot-to-production checklist as an implementation gate;
  this ADR defines the gate but does not claim that its controls are deployed.

## References

- REF-001: [ADR-0022](adr-0022-initial-azure-data-plane).
- REF-002: [ADR-0026](adr-0026-implementation-platform-and-delivery-conventions).
- REF-003: [ADR-0029](adr-0029-pilot-network-and-environment-topology).
- REF-004: [Non-functional requirements](../product/non-functional-requirements).
- REF-005: [Assumptions and open questions](../product/assumptions-and-open-questions).
- REF-006: [V1 implementation backlog](../implementation-backlog).

## Review record

- 2026-08-23: Accepted by @PlagueHO for V1-D01.
