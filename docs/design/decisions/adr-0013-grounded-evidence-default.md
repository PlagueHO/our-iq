---
title: ADR-0013 - Grounded evidence by default
status: Accepted
---

## ADR-0013: Grounded evidence by default

## Status

Accepted

## Date and ownership

- Date: 2026-08-18
- Authors: @PlagueHO
- Reviewers: @PlagueHO

## Context

Client Agents need material they can inspect and reason over. A synthesized
answer without evidence would hide provenance and introduce an additional
generation step by default.

## Decision

Knowledge queries return structured grounded evidence with citations by default.
Synthesis into a narrative answer is an explicitly requested mode.

## Alternatives considered

| Alternative | Rejection or selection rationale |
| --- | --- |
| Return a narrative answer by default | Rejected because it obscures evidence and adds a generation pass. |
| Return evidence without citations | Rejected because groundedness must be inspectable. |
| Return structured cited evidence by default | Selected because it supports trust and Client Agent reasoning. |

## Consequences

### Positive

- POS-001: Every returned claim can identify its canonical source.
- POS-002: Default retrieval avoids an unnecessary synthesis pass.

### Negative

- NEG-001: Evidence and citation schemas add canonical-read and freshness
  verification work to every retrieval result.

## Implementation notes

- IMP-001: The
  [API contract baseline](../architecture/api-contract-baseline)
  defines evidence, citation, freshness, and completeness semantics without a
  model-generated numeric confidence score.

## References

- REF-001: C-12 in [assumptions and open questions](../product/assumptions-and-open-questions).
- REF-002: FR-0050 to FR-0056 and NFR-0022.

## Review record

- 2026-08-18: Accepted by @PlagueHO for structural architecture slice #6.
