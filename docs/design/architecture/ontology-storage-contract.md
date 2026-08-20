---
title: Ontology storage contract
status: Proposed
---

## Ontology storage contract

## Purpose

Define the proposed normative persistence contract for immutable ontology
versions selected by
[ADR-0021](../decisions/adr-0021-ontology-version-persistence). This is an
implementation contract, not a public ontology CRUD interface.

## Aggregate and transaction boundary

All ontology control records use the knowledge-space identifier as their Cosmos
DB logical partition key. The aggregate contains:

| Record | Mutability | Purpose |
| --- | --- | --- |
| Ontology version | Immutable | Canonical machine-validatable ontology payload and digest. |
| Active ontology pointer | Mutable by transactional activation only | Identifies the version and digest governing new executions. |
| Proposal and compatibility assessment | Immutable | Records review inputs, compatibility findings, and migration need. |
| Approval and activation evidence | Immutable | Records actor, authority, decision, and activation transaction. |

An ontology version is never edited. A correction creates a new version.
Activation writes approval evidence and replaces the active pointer in one
transactional batch. The previous version remains addressable for replay and
audit.

## Ontology version envelope

The stored envelope contains:

| Field | Requirement |
| --- | --- |
| `id` | Unique record identifier derived from the ontology version identifier. |
| `recordType` | Constant `ontologyVersion`. |
| `knowledgeSpaceId` | Partition key and owning space. |
| `ontologyId` | Stable ontology identity across versions. |
| `ontologyVersionId` | Immutable opaque version identity. |
| `schemaVersion` | Version of this storage contract. |
| `payload` | Canonical ontology JSON described below. |
| `payloadDigest` | Lowercase SHA-256 digest of RFC 8785 canonicalized `payload`. |
| `createdAt` | RFC 3339 UTC timestamp. |
| `createdBy` | Initiating user and acting Ontology Agent references. |
| `sourceReferences` | Immutable references to grounding material and proposal evidence. |

The digest covers `payload` only. Storage metadata, timestamps, and identity
references do not alter ontology meaning.

## Canonical ontology payload

The payload is a JSON object with these required sections:

| Section | Contract |
| --- | --- |
| `ontologyId` | Must equal the envelope ontology identity. |
| `ontologyVersionId` | Must equal the envelope version identity. |
| `title` and `description` | Human-readable ontology purpose. |
| `documentTypes` | Non-empty set of stable document-type definitions. |
| `hierarchy` | Allowed parent and root rules for document types. |
| `relationshipTypes` | Stable relationship vocabulary, direction, target constraints, and cardinality where constrained. |
| `rules` | Required, Recommended, and Informational rules with stable codes and rationale. |
| `filterableFields` | Ontology metadata paths projected for deterministic retrieval filtering. |
| `templateReferences` | Zero or more immutable Markdown template asset references. |

Each document type contains a stable identifier, purpose, and a JSON Schema
2020-12 schema for its front matter and approved extension namespaces.
Undeclared top-level front-matter fields are invalid. Body Markdown is not
validated by JSON Schema.

```json
{
  "ontologyId": "ontology-product",
  "ontologyVersionId": "ontology-product-v1",
  "title": "Product knowledge",
  "description": "Structures product decisions and requirements.",
  "documentTypes": [
    {
      "documentTypeId": "decision-record",
      "description": "A governed product or architecture decision.",
      "frontMatterSchema": {
        "$schema": "https://json-schema.org/draft/2020-12/schema",
        "type": "object",
        "required": ["status"],
        "properties": {
          "status": {
            "type": "string",
            "enum": ["proposed", "accepted", "superseded", "deprecated"]
          }
        },
        "additionalProperties": false
      }
    }
  ],
  "hierarchy": {
    "roots": ["decision-record"],
    "allowedParents": []
  },
  "relationshipTypes": [
    {
      "relationshipTypeId": "supersedes",
      "sourceDocumentTypes": ["decision-record"],
      "targetDocumentTypes": ["decision-record"],
      "maximumTargets": 1
    }
  ],
  "rules": [
    {
      "code": "decision-status-required",
      "level": "required",
      "rationale": "Every decision must expose its lifecycle state."
    }
  ],
  "filterableFields": [
    {
      "path": "metadata.status",
      "valueType": "string"
    }
  ],
  "templateReferences": []
}
```

## Template references

A template reference contains `templateId`, `revisionId`, `mediaType`,
`contentDigest`, and an immutable asset reference. Templates use
`text/markdown`; they are data governed by ADR-0020. They guide agent output but
do not add validation rules. A missing or digest-mismatched template makes the
version invalid for activation.

## Validation and activation

Before staging, Tool Services validate:

1. Envelope and payload identities match.
1. The payload conforms to the ontology storage schema.
1. Document type, relationship, rule, and filter paths use unique stable
   identifiers.
1. Every referenced document type, metadata path, and template resolves.
1. Every Required rule has deterministic validation semantics.
1. The computed payload digest matches the envelope.

Before activation, Tool Services also require an approved compatibility
assessment and verify that the expected active pointer has not changed.
Activation writes immutable approval evidence and the new pointer
transactionally. New execution-context snapshots can use the version only after
that transaction commits.

## Initial increment boundary

The thin slice supports one ontology with one or more document types, front
matter JSON Schemas, hierarchy rules, relationship definitions, rule levels,
filterable fields, and optional Markdown templates. Incompatible migration and
bulk ontology conversion remain deferred monitored operations.
