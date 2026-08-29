---
title: API contract baseline
status: Proposed
---

## API contract baseline

## Purpose

Define the proposed contract baseline for Our IQ before implementation. It
separates the public intent-level MCP interface from the private deterministic
MCP tools used by Our IQ Domain Agents. It does not describe deployed APIs,
storage APIs, or a selected implementation framework.

The target public MCP specification is `2026-07-28`. Compatibility, versioning,
and deprecation direction are recorded in
[ADR-0018](../decisions/adr-0018-mcp-contract-boundaries-and-compatibility).
The first implementable subset is defined in the
[initial implementation readiness baseline](implementation-readiness).

## Contract boundaries

| Boundary | Consumer | Contract style | Prohibited |
| --- | --- | --- | --- |
| Public Our IQ MCP Server | Client Agents acting for users | Intent-level tools, resources, and prompts | Direct knowledge-item or ontology CRUD |
| Private Domain Agent MCP tools | Authorized Our IQ Domain Agents | Deterministic schema-bound JSON operations | Use by Client Agents or callers without a private execution context |
| Management API and CLI | Operators and administrators | Deterministic management operations | Bypassing policy, audit, or the canonical visibility fence |

The public interface expresses what a caller wants to achieve. A private tool
expresses the specific, authorized operation an agent needs to perform. Private
tools may offer narrow CRUD-like operations over controlled records and staged
revisions, but only after authorization and snapshot validation. This does not
weaken the public intent-only boundary defined by
[ADR-0002](../decisions/adr-0002-agent-mediated-intent-interface).

## Contract conventions

Every operation contract must specify:

| Concern | Requirement |
| --- | --- |
| Identity and scope | The knowledge-space identifier, initiating user where attended, acting agent, and required capability. |
| Input and output | JSON Schema-compatible request and response shapes, required fields, optional fields, and field constraints. |
| State | Legal lifecycle states, mutation-policy route, and whether an execution-context snapshot is required. |
| Validation | Required ontology findings, recommended findings, and informational guidance. |
| Idempotency | Whether the caller supplies an idempotency key, its replay scope, and the result returned for a replay. |
| Failure | A stable machine-readable error code, HTTP-like category where relevant, human-readable explanation, correlation ID, and remediation. |
| Long-running work | Operation identifier, state, progress shape, cancellation semantics, and terminal outcomes. |
| Versioning | Contract version, supported compatibility range, and deprecation behaviour. |
| Collection access | Cursor pagination, stable ordering, filters, and page-size limits; state `not applicable` for non-collection operations. |

Responses use camelCase JSON property names. Stable identifiers are opaque
strings. Timestamps use RFC 3339 UTC strings. Contract schema publication and
exact-version resolution follow the
[contract schema publication reference](contract-schema-publication) and
[ADR-0028](../decisions/adr-0028-contract-schema-publication). The final
identifier syntax remains an implementation decision. Each operation below is
a proposed normative contract entry; a surface marked Deferred is not part of
the initial implementation contract.

Until `1.0` has been published and a formal GA release has been declared, these
contracts may change incompatibly without a compatibility or deprecation
guarantee. The post-GA versioning and support policy is defined by
[ADR-0018](../decisions/adr-0018-mcp-contract-boundaries-and-compatibility).

## Knowledge-space lifecycle

The proposed one-word lifecycle status codes are:

| Status | Meaning | Read | Write |
| --- | --- | --- | --- |
| `draft` | Space definition is being prepared. | Administrators only | Configuration only |
| `pending` | Ontology and governance await approval. | Authorized reviewers | Approval-related operations only |
| `active` | Normal governed operation. | Authorized users | Allowed by mutation policy |
| `readonly` | Knowledge remains available but mutation is intentionally disabled. | Authorized users | No |
| `maintenance` | A governed operation, such as migration, temporarily restricts normal operation. | As operation policy permits | As operation policy permits |
| `retired` | Normal use has ended while retention applies. | Authorized administrators and auditors | No |
| `deleting` | Irreversible deletion is in progress. | Progress and audit only | No |
| `deleted` | The space is no longer available. | No | No |

Creation produces a `draft` space. Supplying ontology and governance artifacts
moves it to `pending`; an Ontology Manager's approval activates it.

| From | To | Required capability |
| --- | --- | --- |
| `draft` | `pending` | Owner or Ontology Manager |
| `pending` | `active` | Ontology Manager |
| `active` | `readonly`, `maintenance`, `retired` | Owner |
| `readonly` | `active`, `maintenance`, `retired` | Owner |
| `maintenance` | `active`, `readonly`, `retired` | Owner |
| `retired` | `deleting` | Owner |
| `deleting` | `deleted` | Owner; service performs irreversible work |

No other transition is legal. A transition returns `space_state_conflict` when
the source state does not match. `deleted` is terminal.

| Role | Capabilities |
| --- | --- |
| Owner | Delegate and revoke roles; configure mutation policy; transition lifecycle; start deletion; approve review-route plans; inspect all space records. |
| Ontology Manager | Submit and approve ontology setup; stage ontology versions; inspect ontology, plans, operations, and canonical evidence. |
| Contributor | Submit contributions and bootstrap intents; confirm plans where the mutation policy permits contributor confirmation; read authorized evidence. |
| Reader | Read authorized evidence and inspect public space and operation representations. |

## Ontology assets

An ontology describes a knowledge space's structure. It includes its immutable
identity and version, document types, primary hierarchy, relationship
vocabulary, metadata and extension rules, and Required, Recommended, and
Informational guidance.

An ontology may additionally include optional templates. A template is example
Markdown, optionally with illustrative front matter, that an add/update agent
can use as a content-shaping guide. A template is not a strict schema and does
not itself block a change set. Only an ontology rule marked Required has that
effect. Templates and ontology artifacts are versioned assets scoped to their
knowledge space.

## Public MCP inventory

| Surface | Operation | Required capability and legal state | Result and collection behaviour |
| --- | --- | --- | --- |
| Tool | `create_space` | Owner capability at instance scope | Returns draft space and operation; no collection. |
| Tool | `submit_space_setup` | Owner or Ontology Manager; `draft` | Returns pending setup plan; no collection. |
| Tool | `approve_ontology` | Ontology Manager; `pending` | Returns active ontology and space reference; no collection. |
| Tool | `contribute_knowledge` | Contributor; `active` | Returns no-change or plan outcome; no collection. |
| Tool | `approve_change_plan` | Policy-authorized role; `active` | Returns rejection, confirmation, or committed change set; no collection. |
| Tool | `query_knowledge` | Reader; `active`, `readonly`, or `maintenance` as policy permits | Returns cited evidence; supports `documentType`, `metadata`, and `relationship` filters, descending relevance order, cursor, and requested page size. |
| Tool | `bootstrap_knowledge` | Contributor; `draft`, `pending`, or `maintenance` | Returns monitored bootstrap operation; no collection. |
| Resource | Space, ontology, plan, operation, evidence, and public contract schema references | Corresponding read capability and state | `ouriq://spaces{?cursor,pageSize,lifecycleState}` lists spaces visible through a caller's role grants in stable knowledge-space-ID order; page size is 1-100 and lifecycle state is optional. `ouriq://spaces/{knowledgeSpaceId}` exposes only public space state. Singular resources have no pagination; other list resources use stable identifier order, cursor, filters, and requested page size. Contract schemas resolve by exact public version; private schemas are never public resources. |
| Prompt | Contribution, ontology-design, and retrieval guidance | No data access | Static guidance; no pagination. |
| Task | Provisioning, migration, projection rebuild, and bulk bootstrap | Operation-specific capability and state | Returns operation resource with progress and terminal outcome; no collection. |
| MCP App | Visual review, graph, or status views | N/A | Deferred because host support is not required for an operation. |

Public callers never identify a storage path or invoke knowledge-item CRUD.
They may supply optional target or update hints, but the agent determines
whether the resulting plan creates, revises, or leaves canonical knowledge
unchanged.

## Private deterministic MCP tool inventory

Private tools return validated JSON matching their published schemas. They
require a private execution-context snapshot unless the operation is explicitly
read-only and independently authorized.

| Tool family | Representative operations | Purpose |
| --- | --- | --- |
| Space control | `get_space`, `list_spaces`, `transition_space` | Read or govern lifecycle records. |
| Ontology assets | `get_ontology`, `list_all_templates`, `get_template`, `stage_ontology_version` | Read and prepare versioned ontology structure and advisory templates. |
| Source assets | `stage_source_asset`, `get_source_asset`, `get_extraction_result` | Preserve immutable attachments and expose supported extracted representations. |
| Canonical planning | `get_canonical_snapshot`, `validate_change_plan`, `stage_knowledge_revisions` | Read pinned evidence, enforce ontology rules, and stage immutable candidate revisions. |
| Publication | `commit_change_set`, `get_change_set` | Validate freshness and publish one manifest and active pointer. |
| Retrieval | `search_evidence`, `read_canonical_evidence` | Return authorized candidates and cited canonical material. |
| Operations | `create_operation`, `get_operation`, `cancel_operation` | Coordinate monitored work and progress. |
| Governance | `authorize_capability`, `record_approval`, `validate_execution_grant` | Enforce authorization, approval, and unattended authority. |

Private deterministic tools must not silently apply an agent's inferred change
to newer state. A state-sensitive call validates the pinned lifecycle status,
ontology version and digest, mutation policy, canonical head, identity context,
and unattended grant when applicable.

### Domain Agent tool manifests

Each immutable agent definition pins its model deployment, instructions,
contract version, and fixed private tool manifest. Tool Services reject calls
outside that manifest even when a model requests them.

| Agent | Initial private tool manifest |
| --- | --- |
| Ontology Agent | `get_space`, `get_ontology`, `list_all_templates`, `get_template`, `stage_ontology_version`, `validate_ontology_compatibility`, `record_approval`, `activate_ontology_version` |
| Contribution Agent | `get_space`, `get_ontology`, `get_canonical_snapshot`, `search_evidence`, `read_canonical_evidence`, `validate_change_plan`, `stage_knowledge_revisions`, `commit_change_set` |
| Retrieval Agent | `get_space`, `get_ontology`, `search_evidence`, `read_canonical_evidence` |

The Retrieval Agent manifest is read-only. The Contribution Agent cannot stage
or activate an ontology. The Ontology Agent cannot commit knowledge revisions.
Changing a manifest creates a new agent-definition version and requires
evaluation and owner approval.

## Bulk bootstrap contract

`bootstrap_knowledge` accepts source assets and optional text for a new or
maintenance-gated space. It is agent-mediated: the Domain Agent creates
reviewable plans and the space mutation policy determines automatic commit,
contributor confirmation, or review. It never bypasses policy.

The request requires `knowledgeSpaceId`, a non-empty `sourceAssets` list, and
an `idempotencyKey`; optional `batchSizeHint` is advisory. The response returns
`operationId`, the pinned snapshot, and `accepted` state. The operation reports
`accepted`, `planning`, `awaiting_approval`, `committing`, `completed`,
`failed`, or `cancelled`, with completed/failed source counts and a correlation
ID. Chunk checkpoints permit resumption without reprocessing a committed
change-set. Cancellation is legal only before a chunk enters commit; otherwise
it returns `operation_not_cancellable`. Every created source asset, plan,
approval, checkpoint, and change set is linked by provenance.

## Attachment and source-asset contract

A contribution may contain text and zero or more attachments. Attachments are
immutable source assets, not canonical knowledge items. The system preserves
the original asset and links it to extraction results, plans, change sets, and
citations through provenance.

Supported extraction produces a usable representation for agent interpretation.
When extraction or interpretation is incomplete, unsupported, or uncertain, the
plan identifies the affected source, its gaps, and only grounded high-confidence
findings. It must not claim unsupported knowledge. Whether a particular asset
type is supported is returned explicitly; the final supported media matrix and
size limits remain open.

The first implementation increment accepts UTF-8 text and Markdown only.
Binary attachments, extraction, and source-specific retention are explicitly
deferred beyond that increment. The broader contract above remains the design
target and must not be presented as implemented by the text-only slice.

## Evidence and citation contract

`query_knowledge` returns a `completeness` value of `complete`, `partial`, or
`insufficient`, plus zero or more stable reason codes. It does not return a
model-generated numeric confidence score.

Each evidence item contains:

| Field | Requirement |
| --- | --- |
| `knowledgeItemId` | Stable canonical item identity. |
| `revisionId` | Active immutable revision used as evidence. |
| `title` | Human-readable title from canonical front matter. |
| `excerpt` | Exact canonical Markdown excerpt; never projection-generated text. |
| `citation` | Canonical asset reference, revision digest, and excerpt locator. |
| `matchedBy` | Metadata paths, relationship assertions, lexical terms, or semantic candidate reason used to select the item. |
| `projectionFreshness` | Indexed canonical-head version, current canonical-head version, and freshness state. |
| `provenance` | Change-set and source references the authorized caller may inspect. |

Azure AI Search returns candidates and match metadata. Tool Services resolve
every candidate to the active canonical Blob revision and construct the excerpt
and citation from that revision. A missing or mismatched canonical revision is
not returned as evidence.

## Idempotency and concurrency

Idempotency applies to the caller's submitted operation, not to agent reasoning
in general:

1. A mutating public or management operation accepts a caller-generated
   idempotency key scoped to operation, caller, and knowledge space.
2. A replay with the same normalized request returns the original accepted,
   completed, or failed outcome and correlation identifiers.
3. Reusing a key with a different normalized request returns
   `idempotency_key_conflict`.
4. Re-submitting equivalent content with a new key creates a new execution
   context. If ontology, policy, lifecycle, or canonical head changed, the agent
   plans again rather than replaying a stale plan.
5. Approval and publication bind to the plan identifier and its snapshot. A
   stale plan returns `replan_required`; it is never retargeted silently.

## Error taxonomy

| Code | Meaning | Caller action |
| --- | --- | --- |
| `authentication_required` | No valid authenticated caller context. | Authenticate and retry. |
| `authorization_denied` | User, agent, or grant lacks capability. | Request appropriate authorization. |
| `space_state_conflict` | Current lifecycle state does not permit the operation. | Inspect state or wait for transition. |
| `ontology_not_active` | A required active ontology is unavailable. | Complete or approve setup. |
| `validation_failed` | Required schema or ontology validation failed. | Correct input or plan. |
| `policy_rejected` | Mutation policy rejects the requested route. | Use the required approval route. |
| `insufficient_grounding` | Evidence cannot support a requested conclusion. | Supply evidence or narrow the request. |
| `clarification_required` | Contribution intent has multiple materially different grounded interpretations. | Answer the focused questions and resubmit; no plan or mutation exists. |
| `partial_plan` | Some source material could not be interpreted safely. | Review gaps and decide whether to proceed. |
| `approval_expired` | Required confirmation or approval is no longer valid. | Re-plan or obtain a new approval. |
| `replan_required` | Pinned ontology version, lifecycle, policy, or canonical head is stale or incompatible. | Submit or obtain a new plan. |
| `idempotency_key_conflict` | Key was reused with different input. | Use a new key. |
| `asset_unsupported` | The asset type or extraction capability is unsupported. | Provide a supported source or preserve for later processing. |
| `operation_not_cancellable` | Cancellation would be unsafe at the current operation stage. | Wait for terminal outcome. |
| `contract_version_unsupported` | The requested contract version is not published or supported. | Select a declared supported version. |
| `contract_schema_not_found` | The requested schema is not present in the selected surface and version. | Check the schema name or contract version. |
| `contract_schema_integrity_failure` | The published schema does not match its declared digest. | Fail closed and report the publication integrity issue. |

`partial_plan` is the only partial-planning result: it names each incomplete
source and never authorizes unsupported conclusions. `approval_expired` is the
confirmation-timeout result. A stale ontology version always returns
`replan_required`; it is not silently migrated.

`clarification_required` is not a validation failure or partial plan. It returns
grounded ambiguity reasons and focused questions, consumes no approval, and
creates no staged revision.

## Worked public exemplar: contribute knowledge

`contribute_knowledge` is the normative proposed contract for simple
statements, prose, and source assets. It does not require a contributor to
choose a target knowledge item. It requires Contributor capability and an
`active` space; otherwise it returns `authorization_denied` or
`space_state_conflict`. Its contract version follows ADR-0018; pagination and
filtering are not applicable.

| Request field | Required | Constraints |
| --- | --- | --- |
| `knowledgeSpaceId` | Yes | Opaque identifier for an `active` space. |
| `content` | Yes | At least `text` or one attachment; `text` is a non-empty string when present. |
| `attachments` | No | Each entry has source asset identity, media type, and display name. |
| `hints` | No | Advisory document-type and related-item references only; invalid hints do not target a write. |
| `idempotencyKey` | Yes | Caller-generated opaque key, scoped to caller, operation, and space. |

| Response outcome | Required fields | Meaning |
| --- | --- | --- |
| `no_change` | `knowledgeSpaceId`, `evidence`, `correlationId` | Cited canonical evidence shows no change is required. |
| `clarification_required` | `knowledgeSpaceId`, `ambiguities`, `questions`, `correlationId` | More than one materially different grounded interpretation exists; no plan is created. |
| `plan_ready` | `planId`, `snapshot`, `policyRoute`, `changes`, `findings`, `correlationId` | Reviewable, snapshot-pinned plan is available. |
| `partial_plan` | Plan-ready fields and source gaps | Only high-confidence grounded changes are planned. |
| `replan_required` | `correlationId`, current-state reason | Pinned state became stale before approval or commit. |

Required ontology findings return `validation_failed`; policy-route denial
returns `policy_rejected`; insufficient evidence returns
`insufficient_grounding`. Replaying the same normalized request with the same
key returns the original result. A different request with that key returns
`idempotency_key_conflict`; equivalent content under a new key creates a fresh
snapshot and plans against current state.

```json
{
  "knowledgeSpaceId": "ks-product",
  "content": {
    "text": "The team agreed that evidence must be returned before synthesis."
  },
  "attachments": [
    {
      "sourceAssetId": "asset-meeting-notes-20260819",
      "mediaType": "text/markdown",
      "displayName": "meeting-notes.md"
    }
  ],
  "hints": {
    "possibleDocumentTypes": ["decision-record"],
    "possibleRelatedKnowledgeItemIds": ["ki-retrieval"]
  },
  "idempotencyKey": "4c1c9070-93f0-4db9-9cc8-9015d1dff2bd"
}
```

The response below is an example conforming to the proposed normative response
shape, not an implemented wire response.

```json
{
  "outcome": "plan_ready",
  "planId": "plan-01JABC",
  "knowledgeSpaceId": "ks-product",
  "snapshot": {
    "ontologyVersion": "ontology-product-v3",
    "canonicalHeadVersion": "head-0015"
  },
  "policyRoute": "confirmation",
  "changes": [
    {
      "action": "revise",
      "knowledgeItemId": "ki-product-decisions",
      "expectedRevisionId": "rev-20260818-001",
      "summary": "Adds the agreed evidence-before-synthesis rule."
    }
  ],
  "findings": [
    {
      "level": "informational",
      "code": "template_guidance_applied",
      "message": "The decision-record template informed the proposed structure."
    }
  ],
  "sourceAssets": [
    {
      "sourceAssetId": "asset-meeting-notes-20260819",
      "interpretationStatus": "complete"
    }
  ],
  "correlationId": "corr-01JABC"
}
```

The plan can be approved only by the policy route's authorized actor. Commit
validates its pinned snapshot, publishes atomically through the visibility
fence, and returns either a committed change-set reference or
`replan_required`.

## Interaction flow baseline

The selected thin slice has sufficient lifecycle, capability, evidence,
ambiguity, ontology-storage, contribution, and stale-state contracts to begin
implementation. Later release flows retain these additional contract needs:

| Flow | Required contract decisions |
| --- | --- |
| Bulk bootstrap | Batching, checkpoint persistence, cancellation, extraction support, and selected long-running orchestrator. |
| Change an active ontology | Migration scheduling, compensation, and maintenance-state recovery. |
| Rebuild projections | Asynchronous orchestration, generation switching, and rollback. |
| Delete a space | Retention enforcement, irreversible cleanup order, and operational proof. |

## Deferred contract questions

- Which binary source-asset media types, extraction outputs, retention rules,
  and size limits are supported after the text-only increment?
- Which orchestrator implements monitored long-running operations?
