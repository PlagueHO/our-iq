---
title: ADR-0015 - Transactional change-set visibility fence
status: Accepted
---

## ADR-0015: Transactional change-set visibility fence

## Status

Accepted

## Date and ownership

- Date: 2026-08-18
- Authors: @PlagueHO
- Reviewers: @PlagueHO

## Context

An approved change set can affect multiple Markdown knowledge items and control
metadata. Azure Blob Storage provides immutable document revision storage but
does not make independently written blobs and metadata visible as one
transaction. Partial canonical visibility is prohibited.

## Decision

Our IQ stages immutable candidate knowledge-item revisions before commitment.
A per-space Cosmos DB transactional batch publishes the change-set manifest,
provenance, and next active revision pointer together. The active pointer is
the canonical visibility fence: canonical readers resolve it and read only the
revisions listed by its committed manifest.

Staged revisions are not canonical and are not visible to normal readers.
Projection updates occur after publication and are independently recoverable.

## Alternatives considered

| Alternative | Rejection or selection rationale |
| --- | --- |
| Independently update each canonical item | Rejected because readers could observe a partial multi-item change. |
| Rely on best-effort compensation | Rejected because it cannot guarantee that no partial change is observable. |
| Store all knowledge in one mutable transaction record | Rejected because it conflicts with reviewable Markdown knowledge and independent immutable revisions. |
| Stage immutable revisions and atomically publish one manifest and pointer | Selected because it makes complete versions visible without requiring a cross-store transaction. |

## Consequences

### Positive

- POS-001: Readers observe one complete committed change set or the preceding
  committed version.
- POS-002: A manifest supplies a stable provenance and reconstruction unit.
- POS-003: Projection failure cannot corrupt or roll back canonical publication.

### Negative

- NEG-001: Staged revision cleanup and failed-publication recovery need
  operational design.
- NEG-002: Readers and tools must consistently resolve canonical state through
  the active pointer.

## Implementation notes

- IMP-001: A commit verifies the execution-context snapshot before publication.
- IMP-002: The transactional batch is constrained to one knowledge-space
  partition; cross-space mutation is out of scope.
- IMP-003: Blob and control-record retention policies remain open.

## References

- REF-001: [ADR-0001](adr-0001-canonical-knowledge-ownership).
- REF-002: [ADR-0009](adr-0009-canonical-markdown-and-rebuildable-projections).
- REF-003: [ADR-0010](adr-0010-atomic-versioned-change-sets).
- REF-004: NFR-0020, NFR-0021, NFR-0023, and NFR-0045.

## Review record

- 2026-08-18: Accepted by @PlagueHO for design slice #7.
