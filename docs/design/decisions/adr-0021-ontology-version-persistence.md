---
title: ADR-0021 - Ontology version persistence
status: Accepted
---

## ADR-0021: Ontology version persistence

## Status

Accepted

## Date and ownership

- Date: 2026-08-19
- Authors: @PlagueHO
- Reviewers: @PlagueHO

## Context

The design requires immutable ontology versions, deterministic validation,
snapshot pinning, compatibility assessment, and atomic activation, but did not
define the persisted ontology aggregate or canonical serialization.

## Decision

Ontology versions are immutable records in the knowledge space's Cosmos DB
control-metadata partition. A mutable active-ontology pointer in the same
partition identifies the active version and digest. Activation transactionally
updates that pointer and records approval and activation evidence.

The canonical ontology payload is JSON. Document front-matter and extension
constraints use JSON Schema 2020-12. Explicit payload sections define document
types, hierarchy, relationship types, Required, Recommended, and Informational
rules, filterable fields, and immutable template references.

The payload digest is SHA-256 over its RFC 8785 JSON Canonicalization Scheme
representation. Optional Markdown templates are immutable referenced assets,
not embedded executable instructions and not strict validation contracts.

## Alternatives considered

| Alternative | Rejection or selection rationale |
| --- | --- |
| Immutable ontology assets outside Cosmos DB | Rejected for the pilot because ontology activation and governing control state would span transaction boundaries. |
| Mutable ontology records | Rejected because snapshots, replay, and audit require immutable meaning. |
| Immutable Cosmos DB versions with a transactional pointer | Selected because ontology and activation control share the per-space transaction boundary. |

## Consequences

### Positive

- POS-001: A snapshot can pin one immutable version and digest.
- POS-002: Approval and activation cannot expose a partial ontology transition.
- POS-003: JSON Schema supplies deterministic structural validation.

### Negative

- NEG-001: Ontology payload size must remain within Cosmos DB item limits.
- NEG-002: Large guidance assets require separate immutable storage.

## Implementation notes

- IMP-001: The
  [ontology storage contract](../architecture/ontology-storage-contract)
  defines the proposed record shapes and invariants.
- IMP-002: New ontology semantics require a new immutable version, never an
  in-place update.

## References

- REF-001: [ADR-0003](adr-0003-agent-mediated-ontology-management).
- REF-002: [ADR-0014](adr-0014-cosmos-db-control-metadata).
- REF-003: [ADR-0016](adr-0016-immutable-execution-context-snapshots).

## Review record

- 2026-08-19: Accepted by @PlagueHO during issue #4 reconciliation.
