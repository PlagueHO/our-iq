---
title: ADR-0009 - Canonical Markdown and rebuildable projections
status: Accepted
---

## ADR-0009: Canonical Markdown and rebuildable projections

## Status

Accepted

## Date and ownership

- Date: 2026-08-18
- Authors: @PlagueHO
- Reviewers: @PlagueHO

## Context

Knowledge needs a reviewable canonical representation while supporting hybrid
retrieval and relationship traversal. Derived stores must never become the
authority for what the knowledge base contains.

## Decision

Canonical knowledge is Markdown with structured front matter, organized by a
primary hierarchy and typed relationships. Search and graph stores are
rebuildable projections, never sources of truth.

The accepted topology direction uses Azure Blob Storage for canonical knowledge,
Azure AI Search for retrieval projection, and Cosmos DB for control metadata.
Blob Storage and Azure AI Search remain Candidate services; Cosmos DB is
selected for the per-space control-record transaction boundary.

## Alternatives considered

| Alternative | Rejection or selection rationale |
| --- | --- |
| Treat a search or graph store as canonical | Rejected because derived indexes must be safely rebuildable. |
| Use unstructured files without metadata | Rejected because hierarchy and typed relationships must be explicit. |
| Markdown with front matter and rebuildable projections | Selected because it keeps the canonical representation reviewable and portable. |

## Consequences

### Positive

- POS-001: Canonical state can be inspected and used to rebuild projections.
- POS-002: Retrieval can use specialized projections without granting them authority.

### Negative

- NEG-001: Projection freshness may lag canonical commitment.
- NEG-002: The final control-metadata store and graph-projection need remain open.

## Implementation notes

- IMP-001: Azure Blob Storage and Azure AI Search are `Candidate` services;
  Cosmos DB is selected only for control metadata. No deployed implementation
  is implied.
- IMP-002: Projection lag is acceptable for the initial version; see C-16.

## References

- REF-001: C-09, C-16, C-21, and D-08 in [assumptions and open questions](../product/assumptions-and-open-questions).
- REF-002: FR-0052 to FR-0054, FR-0064, and NFR-0023.

## Review record

- 2026-08-18: Accepted by @PlagueHO for structural architecture slice #6.
