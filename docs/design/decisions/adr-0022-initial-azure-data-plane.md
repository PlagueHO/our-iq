---
title: ADR-0022 - Initial Azure data plane
status: Accepted
---

## ADR-0022: Initial Azure data plane

## Status

Accepted

## Date and ownership

- Date: 2026-08-19
- Authors: @PlagueHO
- Reviewers: @PlagueHO

## Context

ADR-0009 distinguishes canonical knowledge from rebuildable projections, but
Azure Blob Storage and Azure AI Search remained Candidate services. The first
implementation increment needs concrete canonical and retrieval boundaries.

## Decision

Azure Blob Storage holds immutable canonical Markdown revisions and immutable
referenced assets. Cosmos DB holds change-set manifests, active revision
pointers, and control metadata. A per-space transactional publication updates
the manifest and active pointer only after every referenced blob revision is
durably staged.

Azure AI Search is the initial rebuildable retrieval projection. It supports
hybrid lexical and vector candidate retrieval together with ontology-declared
filterable metadata and relationships. Retrieval resolves candidate identities
to active canonical Blob revisions before returning evidence and citations.

Azure AI Search is never authoritative. Projection loss, lag, or corruption is
repaired from committed manifests and canonical Blob revisions.

## Alternatives considered

| Alternative | Rejection or selection rationale |
| --- | --- |
| Store canonical Markdown in Cosmos DB | Rejected because immutable document assets and control records have different access and lifecycle concerns. |
| Query Cosmos DB without a retrieval projection | Rejected because it does not satisfy hybrid retrieval requirements. |
| Blob canonical revisions plus Azure AI Search projection | Selected because it preserves a portable canonical form and supports the required retrieval modes. |

## Consequences

### Positive

- POS-001: Canonical Markdown remains portable and independently recoverable.
- POS-002: Hybrid retrieval and deterministic metadata filters share one
  projection.
- POS-003: Returned evidence can be verified against canonical revisions.

### Negative

- NEG-001: Publication and projection update are separate consistency domains.
- NEG-002: Index schema changes require rebuild and compatibility handling.

## Implementation notes

- IMP-001: The initial pilot permits eventual projection consistency and reports
  projection freshness.
- IMP-002: A dedicated graph database remains deferred until evidence justifies
  it.

## References

- REF-001: [ADR-0009](adr-0009-canonical-markdown-and-rebuildable-projections).
- REF-002: [ADR-0015](adr-0015-transactional-change-set-visibility-fence).
- REF-003: FR-0051 to FR-0054.

## Review record

- 2026-08-19: Accepted by @PlagueHO during issue #4 reconciliation.
