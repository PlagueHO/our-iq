---
title: ADR-0014 - Cosmos DB control metadata
status: Accepted
---

## ADR-0014: Cosmos DB control metadata

## Status

Accepted

## Date and ownership

- Date: 2026-08-18
- Authors: @PlagueHO
- Reviewers: @PlagueHO

## Context

Control metadata coordinates knowledge-space governance, execution state,
change-set publication, and audit links. The atomic change-set requirement
needs a control-metadata service that can publish related records together for
one knowledge space. Azure Table Storage was retained as an alternative while
this requirement was unresolved.

## Decision

Our IQ uses Azure Cosmos DB for control metadata. Records requiring atomic
coordination for a knowledge space use that space as their logical partition
key, so a transactional batch can atomically publish the related control
records.

This decision selects a control-metadata direction. It does not select a
messaging, job-orchestration, networking, retention, or deployment design.

## Alternatives considered

| Alternative | Rejection or selection rationale |
| --- | --- |
| Azure Table Storage control metadata | Rejected because it does not provide the required multi-record transactional publication boundary. |
| Azure Cosmos DB control metadata partitioned by knowledge space | Selected because transactional batches can coordinate one space's control records. |
| Put control records only in canonical Markdown | Rejected because execution coordination, policy state, and atomic publication need a private authoritative control boundary. |

## Consequences

### Positive

- POS-001: A per-space transaction boundary is available for change-set
  coordination.
- POS-002: Control metadata remains distinct from canonical knowledge items.

### Negative

- NEG-001: Operations requiring cross-space transactions are excluded from this
  initial design.
- NEG-002: Partition-key design and operational cost need careful review before
  implementation.

## Implementation notes

- IMP-001: Control metadata remains private to Tool Services and management
  capabilities.
- IMP-002: Long-running-work orchestration remains open; this ADR does not
  select a job or messaging service.

## References

- REF-001: C-21 and Q-24 in [assumptions and open questions](../product/assumptions-and-open-questions).
- REF-002: [ADR-0015](adr-0015-transactional-change-set-visibility-fence).

## Review record

- 2026-08-18: Accepted by @PlagueHO for design slice #7.
