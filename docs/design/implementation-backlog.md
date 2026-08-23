---
title: V1 implementation backlog
status: Proposed
---

## V1 implementation backlog

## Purpose

Propose a dependency-ordered backlog of small, testable GitHub issues for
implementing V1. This backlog translates the accepted architecture decisions
and proposed implementation contracts into delivery work. It does not claim
that any issue, milestone, service, or behaviour has been implemented.

The backlog separates the first validated increment from the remaining V1
scope. The first increment proves ontology approval, one attended text
contribution, atomic publication, projection, and cited retrieval end to end.
The remaining V1 work adds the lifecycle, operations, migration, management,
and hardening capabilities required by the initial-version requirements.

## Backlog conventions

- Use the existing `enhancement` label for implementation issues and `design`
  for decision issues.
- Create three milestones: `V1 - Foundation`, `V1 - Validated thin slice`, and
  `V1 - Completion`.
- Replace the local identifiers below with GitHub issue numbers when the issues
  are created, while preserving the identifiers in issue bodies for
  traceability.
- Do not pull deferred portal, MCP Apps, external connector, cross-space
  retrieval, multi-tenancy, external federation, public access, or dedicated
  graph-database work into V1.
- Do not implement binary extraction, production data handling, or long-running
  orchestration until their named decision issue is accepted.

## Common definition of done

Every implementation issue is complete only when:

1. Behaviour is covered by focused unit, component, contract, or integration
   tests at the lowest useful boundary.
1. Public and private contracts remain schema-valid and compatibility tests
   pass where a contract changes.
1. Authorization, lifecycle, snapshot, and provenance rules are enforced by
   deterministic services rather than by agent prompts.
1. Logs, metrics, and traces contain correlation identifiers but no secrets or
   knowledge content.
1. Relevant design and operator documentation is updated without presenting
   proposed or deferred behaviour as implemented.
1. The repository build, test, lint, and deployment-validation checks that
   exist at that point pass.

## Proposed delivery order

```mermaid
flowchart LR
  decisions[Blocking decisions] --> foundation[Foundation]
  foundation --> ontology[Ontology setup]
  ontology --> contribution[Contribution and publication]
  contribution --> retrieval[Projection and retrieval]
  retrieval --> slice[Validated thin slice]
  slice --> operations[V1 operations and lifecycle]
  operations --> hardening[V1 release hardening]
```

Foundation work can proceed in parallel where dependencies permit. Contract,
security, and data-model tests should precede Azure integration so most
behaviour remains fast and deterministic in the inner loop.

## Blocking decision issues

These are implementation inputs, not opportunities to choose silently while
coding. `V1-D01` to `V1-D03` do not block the validated thin slice but do block
the named V1 completion work.

### V1-D01: Define pilot data governance and retention controls

**Milestone:** V1 - Completion  
**Label:** `design`

**Goal:** Resolve the production data classification, residency, retention,
audit-retention, and network-isolation rules that replace the thin slice's
non-sensitive single-geography restriction.

**Acceptance criteria:**

- An accepted ADR or requirements update resolves Q-21 and sets measurable
  targets for NFR-0005, NFR-0007, and NFR-0008.
- The decision identifies required controls for Blob Storage, Cosmos DB, Azure
  AI Search, telemetry, backups, and private connectivity.
- Migration from the pilot boundary is described without claiming that the
  controls are deployed.

**Depends on:** None.  
**Traceability:** Q-21; NFR-0005, NFR-0007, NFR-0008; ADR-0026.

### V1-D02: Set item-size and cost budgets

**Milestone:** V1 - Completion  
**Label:** `design`

**Goal:** Resolve the maximum canonical item size and acceptable per-instance
and per-space cost envelope before production hardening.

**Acceptance criteria:**

- Q-22 and Q-25 have accepted, measurable answers.
- The item-size decision accounts for request limits, agent context, Blob
  access, projection fields, and citation extraction.
- The cost envelope identifies attributable meters and review thresholds
  without introducing unsupported optimization work.

**Depends on:** Thin-slice performance and cost measurements from `V1-TS20`.  
**Traceability:** Q-22, Q-25; NFR-0036, NFR-0080, NFR-0081.

### V1-D03: Select long-running work orchestration

**Milestone:** V1 - Completion  
**Label:** `design`

**Goal:** Select the smallest Azure orchestration approach that satisfies
resumable migration, rebuild, bootstrap, and deletion work.

**Acceptance criteria:**

- An accepted ADR resolves Q-24 and compares at least the viable Azure options.
- Checkpointing, retries, cancellation, compensation, identity, observability,
  and cost boundaries are explicit.
- The decision does not introduce asynchronous orchestration into the
  synchronous thin slice.

**Depends on:** `V1-TS20`.  
**Traceability:** Q-24; FR-0062 to FR-0064; NFR-0044, NFR-0052.

### V1-D04: Define contract schema publication

**Milestone:** V1 - Foundation  
**Label:** `design`

**Goal:** Decide how public and private JSON Schemas are versioned, published,
and consumed by compatibility tests.

**Acceptance criteria:**

- The selected repository and runtime publication mechanism is documented.
- Consumers can resolve a schema by contract version.
- The design preserves the public/private trust boundary and the MCP
  compatibility policy.

**Depends on:** None.  
**Traceability:** ADR-0018; API contract baseline, deferred contract questions.

### V1-D05: Define pilot network and environment topology

**Milestone:** V1 - Foundation  
**Label:** `design`

**Goal:** Resolve the pilot environment, ingress, virtual-network, and private
endpoint choices needed for deployable Bicep.

**Acceptance criteria:**

- Public MCP ingress and private Tool Services ingress are explicitly separated.
- Supported service-to-service and data-service paths are documented with their
  identities and trust boundaries.
- Environment tiers and intentionally deferred production controls are clear.

**Depends on:** `V1-D01` may refine, but does not block, the pilot topology.  
**Traceability:** ADR-0023; arc42 deployment view, open questions.

### V1-D06: Define post-thin-slice source asset support

**Milestone:** V1 - Completion  
**Label:** `design`

**Goal:** Decide whether V1 extends beyond UTF-8 text and Markdown and, if so,
define the first supported binary media types and extraction contract.

**Acceptance criteria:**

- Q-17 has an accepted answer covering media types, representations, limits,
  failure reporting, and retention.
- Unsupported media and partial extraction outcomes remain explicit and
  fail-closed.
- If no binary type is justified for V1, the scope decision explicitly defers
  `V1-C13`.

**Depends on:** Validated text-only feedback from `V1-TS20`.  
**Traceability:** Q-17; ADR-0020; API attachment and source-asset contract.

## Milestone: V1 - Foundation

### V1-F01: Resolve and pin the .NET package baseline

**Goal:** Establish the reproducible .NET 10 dependency baseline before
application code is introduced.

**Acceptance criteria:**

- Required SDKs are checked for .NET 10 compatibility and pinned to exact
  versions through central package management.
- Stable packages are used unless an accepted ADR records the required
  prerelease package, evidence, risks, and exit plan.
- Dependency restoration is repeatable from a clean checkout.

**Depends on:** None.  
**Traceability:** ADR-0025, IMP-001 to IMP-004.

### V1-F02: Scaffold the .NET solution and test projects

**Goal:** Create the smallest solution structure that preserves the public MCP,
private Tool Services, shared-contract, domain, and test boundaries.

**Acceptance criteria:**

- The solution targets .NET 10 and contains separate executable projects for
  the public MCP Server and private Tool Services.
- Shared code is limited to contracts and domain behaviour that both boundaries
  genuinely require.
- MSTest projects demonstrate unit and component test discovery.
- A clean restore, build, and test succeeds.

**Depends on:** `V1-F01`.  
**Traceability:** ADR-0023, ADR-0025, ADR-0026, ADR-0027.

### V1-F03: Publish the initial versioned contract schemas

**Goal:** Turn the documented public and private contract subset into
machine-validatable, versioned schemas.

**Acceptance criteria:**

- Schemas cover identity and scope, state requirements, idempotency, errors,
  pagination applicability, and version metadata.
- The first subset includes space setup, ontology approval, contribution,
  change-plan approval, query, and their required private tools.
- Golden payload tests validate examples and reject incompatible shapes.

**Depends on:** `V1-D04`, `V1-F02`.  
**Traceability:** ADR-0018; API contract conventions and inventories.

### V1-F04: Create the public MCP Server host

**Goal:** Establish the public streamable HTTP MCP boundary without implementing
domain behaviour prematurely.

**Acceptance criteria:**

- The ASP.NET Core host uses the official MCP C# SDK and exposes a separate
  health endpoint.
- Only the selected thin-slice intent tools are discoverable.
- Direct knowledge-item and ontology CRUD is absent.
- Component tests verify discovery, health, and unsupported-operation handling.

**Depends on:** `V1-F02`, `V1-F03`.  
**Traceability:** ADR-0002, ADR-0018, ADR-0023.

### V1-F05: Create the private Tool Services host

**Goal:** Establish the private deterministic tool and management boundary as a
separate deployable.

**Acceptance criteria:**

- The ASP.NET Core host has private-tool and management authorization surfaces
  that are logically separate.
- A caller without private execution context cannot invoke deterministic tools.
- Health and readiness endpoints are separate from private tool endpoints.
- Component tests verify ingress assumptions and denied public access.

**Depends on:** `V1-F02`, `V1-F03`.  
**Traceability:** ADR-0018, ADR-0023; arc42 building-block view.

### V1-F06: Add Aspire inner-loop orchestration

**Goal:** Run both application boundaries and their development dependencies
through one local AppHost.

**Acceptance criteria:**

- The AppHost models the MCP Server, Tool Services, Cosmos DB, Blob Storage,
  Search dependency, and service discovery needed by the current increment.
- Local configuration uses developer identity or local emulation without
  committed secrets.
- A documented command starts the system and reports healthy resources.

**Depends on:** `V1-F04`, `V1-F05`.  
**Traceability:** ADR-0026.

### V1-F07: Add end-to-end telemetry and correlation

**Goal:** Make one request traceable across MCP, Domain Agent invocation, Tool
Services, data access, and audit references.

**Acceptance criteria:**

- OpenTelemetry emits structured traces, metrics, and logs from both hosts.
- Execution, trace, correlation, space, and operation identifiers propagate
  across boundaries.
- Telemetry excludes tokens, secrets, prompts, source text, and canonical
  knowledge bodies.
- Component tests verify propagation and redaction.

**Depends on:** `V1-F04`, `V1-F05`.  
**Traceability:** ADR-0026; FR-0066; NFR-0050, NFR-0051.

### V1-F08: Implement the knowledge-space control record

**Goal:** Persist a stable space identifier, lifecycle state, mutation policy,
and canonical control references in one Cosmos DB partition.

**Acceptance criteria:**

- Creating a space produces a unique stable identifier and `draft` state.
- Records are partitioned by knowledge-space identifier.
- Optimistic concurrency prevents lost control-state updates.
- Repository and component tests cover creation, reads, conflicts, and invalid
  state.

**Depends on:** `V1-F02`.  
**Traceability:** FR-0001 to FR-0003; ADR-0014, ADR-0019.

### V1-F09: Implement lifecycle transition rules

**Goal:** Enforce the normative knowledge-space lifecycle independently of
agents.

**Acceptance criteria:**

- Every permitted transition and required capability is represented.
- Unlisted transitions return `space_state_conflict`.
- `deleted` is terminal and normal operations are state-gated.
- Table-driven tests cover every allowed and rejected transition.

**Depends on:** `V1-F08`.  
**Traceability:** FR-0003, FR-0005, FR-0006; ADR-0019.

### V1-F10: Implement role grants and capability intersection

**Goal:** Enforce the Owner, Ontology Manager, Contributor, and Reader capability
matrix at the space boundary.

**Acceptance criteria:**

- Owners can grant and revoke fixed space roles.
- Every operation checks both user permission and acting-agent capability.
- Neither principal can extend the other's authority.
- Matrix tests prove zero successful operations outside the intersection.

**Depends on:** `V1-F08`, `V1-F03`.  
**Traceability:** FR-0012, FR-0013; NFR-0002; ADR-0019, ADR-0024.

### V1-F11: Implement attended Entra identity propagation

**Goal:** Preserve the authenticated initiating user and distinct acting agent
identity while services use managed identity for Azure dependencies.

**Acceptance criteria:**

- Public requests require a valid user identity for attended work.
- Private calls carry independently verifiable agent identity and user context.
- Data clients authenticate through developer credentials locally and managed
  identity in Azure, without shared keys as the default.
- Authentication and identity-substitution tests fail closed.

**Depends on:** `V1-F04`, `V1-F05`, `V1-F10`.  
**Traceability:** FR-0010, FR-0011, FR-0015; NFR-0001, NFR-0003; ADR-0007,
ADR-0008, ADR-0025.

### V1-F12: Implement immutable execution-context snapshots

**Goal:** Pin the state governing every Domain Agent invocation and reject stale
state-sensitive work.

**Acceptance criteria:**

- A snapshot records the required execution, trace, space, lifecycle, agent,
  ontology, policy, canonical-head, and identity fields.
- Snapshots are immutable and addressable for audit and replay.
- Tool validation returns `replan_required` when pinned mutable state changes.
- Tests cover stale lifecycle, ontology, policy, and canonical-head cases.

**Depends on:** `V1-F08`, `V1-F10`, `V1-F11`.  
**Traceability:** FR-0040; ADR-0016.

### V1-F13: Implement idempotency and the shared error model

**Goal:** Give all mutating operations deterministic replay and machine-readable
failure behaviour.

**Acceptance criteria:**

- Idempotency keys are scoped to operation, caller, and knowledge space.
- An identical replay returns its original outcome; changed input returns
  `idempotency_key_conflict`.
- The documented thin-slice error codes include category, explanation,
  correlation identifier, and remediation.
- Contract tests cover every replay and error branch used by the thin slice.

**Depends on:** `V1-F03`, `V1-F08`, `V1-F12`.  
**Traceability:** API contract baseline, idempotency and error taxonomy.

### V1-F14: Provision the pilot data plane with Bicep

**Goal:** Define reviewable pilot infrastructure for Blob Storage, Cosmos DB,
Azure AI Search, and their required identity assignments.

**Acceptance criteria:**

- Bicep modules follow repository naming, parameter, output, and geography
  conventions.
- Data services disable key-based application access where supported and grant
  least-privilege managed-identity roles.
- Cosmos DB partitioning and Search index inputs match the accepted contracts.
- Bicep build and a documented preview or what-if validation succeed.

**Depends on:** `V1-D05`, `V1-F08`.  
**Traceability:** ADR-0014, ADR-0022, ADR-0026.

### V1-F15: Provision compute and monitoring with azd

**Goal:** Define the two Container Apps, Foundry integration inputs, monitoring,
and deployment workflow without merging trust boundaries.

**Acceptance criteria:**

- `azure.yaml` deploys separate public MCP and private Tool Services apps with
  distinct ingress and managed identities.
- OpenTelemetry connects to Application Insights and Azure Monitor.
- Deployment parameters require one geography and reject committed secrets.
- Bicep build and a documented preview or what-if validation succeed.

**Depends on:** `V1-D05`, `V1-F07`, `V1-F14`.  
**Traceability:** ADR-0023, ADR-0026; arc42 deployment view.

## Milestone: V1 - Validated thin slice

### V1-TS01: Implement the ontology payload model and digest

**Goal:** Represent the canonical ontology payload and calculate its stable
identity.

**Acceptance criteria:**

- The model covers document types, hierarchy, relationships, rule levels,
  filterable fields, and template references.
- Payloads are canonicalized with RFC 8785 and hashed with SHA-256.
- Duplicate identifiers, mismatched identities, and invalid references are
  rejected.
- Tests use stable digest fixtures.

**Depends on:** `V1-F02`, `V1-F03`.  
**Traceability:** ADR-0021; ontology storage contract.

### V1-TS02: Validate ontology and front-matter schemas

**Goal:** Apply JSON Schema 2020-12 and deterministic ontology invariants before
an ontology can be staged.

**Acceptance criteria:**

- Ontology envelopes and payloads are schema validated.
- Every Required rule has deterministic validation semantics.
- Referenced document types, paths, relationships, and templates must resolve.
- Tests cover Required, Recommended, and Informational rule behaviour.

**Depends on:** `V1-TS01`.  
**Traceability:** FR-0022, FR-0025; ADR-0021; ontology storage contract.

### V1-TS03: Persist and activate immutable ontology versions

**Goal:** Store immutable ontology records and atomically activate an approved
version.

**Acceptance criteria:**

- Versions, proposals, compatibility findings, approvals, and activation
  evidence are immutable records in the space partition.
- Activation transactionally writes approval evidence and replaces the active
  pointer.
- Activation rejects a changed expected pointer or digest.
- Component tests prove that partial activation is never observable.

**Depends on:** `V1-F08`, `V1-TS02`.  
**Traceability:** FR-0023, FR-0024; ADR-0021.

### V1-TS04: Implement the thin-slice ontology private tools

**Goal:** Expose deterministic ontology operations required by the Ontology
Agent.

**Acceptance criteria:**

- `get_space`, `get_ontology`, template reads, version staging, compatibility,
  approval recording, and activation conform to their schemas.
- Calls require Ontology Agent identity, allowed manifest entry, capability,
  legal state, and valid snapshot.
- The first-version compatibility path succeeds without migration orchestration.
- Contract and authorization tests cover every operation.

**Depends on:** `V1-F05`, `V1-F10`, `V1-F12`, `V1-TS03`.  
**Traceability:** ADR-0024; API private tool inventory.

### V1-TS05: Define and evaluate the Ontology Agent

**Goal:** Create the immutable shared Ontology Agent definition that proposes a
minimal ontology through its fixed private tools.

**Acceptance criteria:**

- The definition pins instructions, configured model deployment, contract
  version, and the accepted least-privilege manifest.
- Representative cases cover valid proposal, invalid grounding, unauthorized
  tool request, and instruction-injection content.
- Promotion requires recorded evaluation results and owner approval.
- No model name or prompt can be supplied by a public request.

**Depends on:** `V1-TS04`, `V1-TS18`.  
**Traceability:** FR-0020 to FR-0023; NFR-0025, NFR-0061; ADR-0020, ADR-0024.

### V1-TS06: Implement the public space-setup workflow

**Goal:** Complete `create_space`, `submit_space_setup`, and
`approve_ontology` for one minimal ontology.

**Acceptance criteria:**

- The workflow moves a space from `draft` to `pending` to `active` only through
  legal, authorized transitions.
- The caller receives reviewable proposal, validation findings, immutable
  ontology identity, and correlation identifiers.
- Idempotent replay and stale approval behave according to contract.
- An MCP component test proves the complete setup flow.

**Depends on:** `V1-F04`, `V1-F13`, `V1-TS05`.  
**Traceability:** FR-0001 to FR-0006, FR-0020 to FR-0024; API public inventory.

### V1-TS07: Implement canonical Markdown parsing

**Goal:** Parse, normalize, and serialize canonical Markdown revisions with
structured front matter.

**Acceptance criteria:**

- Stable item and revision identities, type, title, parent, relationships,
  metadata, extensions, and provenance are represented.
- Undeclared top-level fields and malformed references are rejected.
- Markdown bodies remain unchanged except for documented normalization.
- Round-trip and invalid-fixture tests cover the logical knowledge model.

**Depends on:** `V1-F02`, `V1-TS02`.  
**Traceability:** ADR-0009; logical knowledge model.

### V1-TS08: Validate canonical revisions against the active ontology

**Goal:** Deterministically evaluate planned revisions before any canonical
publication.

**Acceptance criteria:**

- Required findings block staging.
- Recommended findings require recorded rationale when policy permits approval.
- Informational findings remain advisory.
- Required hierarchy, relationship, metadata, and extension references resolve
  against the pinned ontology.

**Depends on:** `V1-F12`, `V1-TS03`, `V1-TS07`.  
**Traceability:** FR-0036; NFR-0024; logical knowledge model.

### V1-TS09: Stage immutable source and knowledge revisions

**Goal:** Preserve text or Markdown input and candidate canonical revisions in
Blob Storage before publication.

**Acceptance criteria:**

- The thin slice accepts UTF-8 text and Markdown only.
- Source assets and knowledge revisions are immutable and content-digested.
- Staged revisions are not returned by canonical readers.
- Tests cover duplicate writes, digest mismatch, unsupported media, and failed
  staging.

**Depends on:** `V1-F14`, `V1-TS07`, `V1-TS08`.  
**Traceability:** ADR-0020, ADR-0022; implementation-readiness boundary.

### V1-TS10: Publish atomic change sets through the visibility fence

**Goal:** Make staged revisions canonical as one all-or-nothing per-space
publication.

**Acceptance criteria:**

- Commit verifies the pinned snapshot and every staged revision.
- One Cosmos DB transactional batch writes the manifest, provenance, and next
  active pointer.
- Canonical readers resolve only revisions in the committed manifest.
- Fault-injection tests prove readers see either the prior or next complete
  version, never a partial change set.

**Depends on:** `V1-F12`, `V1-TS09`.  
**Traceability:** FR-0035, FR-0037; NFR-0020, NFR-0021; ADR-0015.

### V1-TS11: Implement mutation-policy routing and approvals

**Goal:** Route each plan through automatic commit, contributor confirmation, or
review without allowing agent interpretation to bypass policy.

**Acceptance criteria:**

- The snapshot pins the mutation-policy version and allowed approval route.
- Approval records actor, authority, decision, rationale, and expiry.
- Unauthorized, expired, rejected, and stale approvals cannot commit.
- Table-driven tests cover every policy route.

**Depends on:** `V1-F10`, `V1-F12`, `V1-F13`, `V1-TS10`.  
**Traceability:** FR-0033, FR-0034; ADR-0004.

### V1-TS12: Implement the Contribution Agent private tools

**Goal:** Expose the deterministic read, validation, staging, and commit
operations in the Contribution Agent's fixed manifest.

**Acceptance criteria:**

- Canonical snapshot, evidence search/read, plan validation, staging, and commit
  operations conform to schemas.
- Tool Services reject calls outside the Contribution Agent manifest.
- Every state-sensitive call validates authorization and snapshot freshness.
- Contract tests cover success and every documented failure used by the flow.

**Depends on:** `V1-F05`, `V1-F10`, `V1-F12`, `V1-TS08`, `V1-TS11`.  
**Traceability:** ADR-0024; API Domain Agent tool manifests.

### V1-TS13: Define and evaluate the Contribution Agent

**Goal:** Create the immutable shared Contribution Agent definition for one
attended text contribution.

**Acceptance criteria:**

- The definition pins its model deployment, instructions, version, and accepted
  least-privilege manifest.
- Evaluation cases cover no change, one valid plan, Required-rule failure,
  ambiguity, stale state, unsupported claims, and forbidden tool escalation.
- Outputs are schema validated and retain source provenance.
- Promotion requires recorded evaluation results and owner approval.

**Depends on:** `V1-TS12`, `V1-TS18`.  
**Traceability:** FR-0030 to FR-0037, FR-0040; NFR-0025; ADR-0020, ADR-0024.

### V1-TS14: Implement the attended contribution workflow

**Goal:** Complete `contribute_knowledge` and `approve_change_plan` from public
intent to canonical commit.

**Acceptance criteria:**

- Outcomes include `no_change`, `clarification_required`, `plan_ready`,
  `partial_plan`, and `replan_required` as applicable.
- `clarification_required` creates no plan, approval, staged revision, or
  mutation.
- Approved plans publish through the visibility fence and return complete
  provenance.
- MCP component tests cover each policy route and outcome.

**Depends on:** `V1-F04`, `V1-F13`, `V1-TS13`.  
**Traceability:** FR-0030 to FR-0040; API contribution exemplar.

### V1-TS15: Define the Azure AI Search projection

**Goal:** Project committed canonical revisions for lexical, vector, metadata,
hierarchy, and relationship candidate retrieval.

**Acceptance criteria:**

- The index schema represents stable item and revision identities, ontology
  type, filterable metadata, hierarchy, relationships, and canonical-head
  version.
- Projection starts only after canonical publication.
- A failed projection does not roll back or alter canonical state.
- Integration tests cover incremental indexing, lag, retry, and stale entries.

**Depends on:** `V1-F14`, `V1-TS10`.  
**Traceability:** FR-0052, FR-0053; NFR-0045; ADR-0022.

### V1-TS16: Resolve canonical evidence and citations

**Goal:** Convert authorized projection candidates into evidence verified
against active canonical Blob revisions.

**Acceptance criteria:**

- Every evidence item includes item and revision identity, title, exact excerpt,
  citation, match reasons, freshness, and authorized provenance.
- Missing, inactive, or digest-mismatched revisions are never returned.
- Completeness is `complete`, `partial`, or `insufficient` with stable reasons
  and no numeric confidence.
- Tests cover projection lag, canonical fallback, and insufficient evidence.

**Depends on:** `V1-TS10`, `V1-TS15`.  
**Traceability:** FR-0051, FR-0056; NFR-0022, NFR-0041; API evidence contract.

### V1-TS17: Implement the Retrieval Agent and query workflow

**Goal:** Complete attended `query_knowledge` and return cited evidence by
default.

**Acceptance criteria:**

- The Retrieval Agent has only the accepted read-only private tool manifest.
- The public operation supports document type, metadata, and relationship
  filters, stable paging, and descending relevance.
- Results are space-authorized and resolve through `V1-TS16`.
- MCP component tests cover structured retrieval, metadata-only entity lookup,
  insufficient evidence, stale projection, and denied access.

**Depends on:** `V1-F04`, `V1-TS16`, `V1-TS18`.  
**Traceability:** FR-0050 to FR-0053, FR-0056, FR-0057; ADR-0013, ADR-0024.

### V1-TS18: Enforce the untrusted-content boundary

**Goal:** Add deterministic controls and adversarial evaluations that prevent
knowledge or source content from becoming instructions or authority.

**Acceptance criteria:**

- Agent instructions and tool manifests are immutable per definition version.
- Agent and tool inputs and outputs are schema validated with provenance and
  trust labels.
- Policy rejects instruction replacement, tool escalation, identity or policy
  manipulation, provenance removal, and unsupported claims.
- Adversarial tests produce explicit, auditable, fail-closed outcomes.

**Depends on:** `V1-F03`, `V1-F10`, `V1-F12`.  
**Traceability:** NFR-0010; ADR-0020.

### V1-TS19: Record immutable audit evidence

**Goal:** Make every security-relevant and knowledge-changing thin-slice
operation attributable and inspectable.

**Acceptance criteria:**

- Audit records identify initiating user, acting agent, service identity,
  capability decision, snapshot, source, approval, ontology, change set, and
  outcome as applicable.
- Audit writes are immutable and linked by correlation identifiers.
- Denied and failed operations are recorded without sensitive content.
- Tests assert required audit events for setup, contribution, commit, query, and
  security failures.

**Depends on:** `V1-F07`, `V1-F11`, `V1-F12`, `V1-TS10`.  
**Traceability:** FR-0065; NFR-0004; implementation-readiness provenance gate.

### V1-TS20: Prove the validated thin slice and release gates

**Goal:** Demonstrate the structure-first product bet end to end with
release-blocking correctness and security evidence.

**Acceptance criteria:**

- An Ontology Manager approves a minimal ontology, a Contributor commits one
  text contribution, and a Reader retrieves cited canonical evidence.
- Automated evidence proves authorization, content isolation, atomicity,
  provenance, grounding, stale-state safety, and contract compatibility gates.
- Telemetry reports p50 and p95 ontology approval, contribution plan/commit,
  projection visibility, and query latency without inventing pass/fail budgets.
- The scenario runs locally and against the pilot Azure environment.

**Depends on:** `V1-F15`, `V1-TS06`, `V1-TS14`, `V1-TS17`, `V1-TS19`.  
**Traceability:** Implementation-readiness release gates; C-29, C-41.

## Milestone: V1 - Completion

### V1-C01: Discover and inspect authorized knowledge spaces

**Goal:** Let an authorized caller list and inspect accessible spaces and their
lifecycle state.

**Acceptance criteria:**

- Results include only spaces visible to the caller.
- Stable ordering, cursor pagination, filters, and page-size limits follow the
  contract conventions.
- Space inspection exposes public state without private control metadata.

**Depends on:** `V1-F09`, `V1-F10`.  
**Traceability:** FR-0004; API resource conventions.

### V1-C02: Complete lifecycle administration

**Goal:** Implement Owner-governed transitions to and from `readonly`,
`maintenance`, and `retired`.

**Acceptance criteria:**

- The accepted transition matrix and capability checks are enforced.
- Read and write operations respond correctly in every lifecycle state.
- Every transition is idempotent, audited, and concurrency-safe.

**Depends on:** `V1-F09`, `V1-TS19`.  
**Traceability:** FR-0005 to FR-0007; ADR-0019.

### V1-C03: Add bounded unattended execution grants

**Goal:** Permit unattended maintenance only through immutable, scoped,
expiring, and auditable grants.

**Acceptance criteria:**

- Grants pin space, operation scope, agent definition or capability, issuing
  authority, validity, and execution limits.
- Private tools reject expired, revoked, exhausted, or out-of-scope grants.
- Grant creation, use, revocation, and expiry are auditable.

**Depends on:** `V1-F10`, `V1-F12`, `V1-TS19`.  
**Traceability:** FR-0014; NFR-0082; ADR-0017.

### V1-C04: Implement monitored long-running operations

**Goal:** Provide the selected reusable operation state, checkpoint, retry,
cancellation, and outcome model.

**Acceptance criteria:**

- Operations expose stable status, progress, errors, and terminal outcomes.
- Interrupted work resumes or compensates without inconsistent state.
- Cancellation is allowed only at safe boundaries and otherwise returns
  `operation_not_cancellable`.
- Component tests cover retries, duplicate delivery, cancellation, and restart.

**Depends on:** `V1-D03`, `V1-C03`.  
**Traceability:** FR-0062, FR-0063; NFR-0044, NFR-0052.

### V1-C05: Assess active-ontology compatibility

**Goal:** Compare a proposed ontology version with current canonical knowledge
before activation.

**Acceptance criteria:**

- Compatibility reports affected items and deterministic incompatibility
  reasons.
- Compatible versions can follow the normal approval and activation path.
- Incompatible versions cannot activate without a migration plan.

**Depends on:** `V1-TS03`, `V1-TS08`.  
**Traceability:** FR-0025, FR-0026.

### V1-C06: Execute resumable ontology migration

**Goal:** Apply an approved incompatible ontology through a monitored,
maintenance-gated migration.

**Acceptance criteria:**

- The operation creates revision plans at a pinned snapshot and publishes only
  valid change sets.
- Interruption resumes from checkpoints without re-publishing committed work.
- Success activates the new ontology; failure leaves an explainable,
  recoverable state.
- Integration tests cover success, interruption, stale state, and rollback or
  compensation.

**Depends on:** `V1-C02`, `V1-C04`, `V1-C05`.  
**Traceability:** FR-0026, FR-0027; NFR-0037, NFR-0044.

### V1-C07: Implement agent-mediated text bootstrap

**Goal:** Load multiple text or Markdown source assets under normal policy as a
monitored operation.

**Acceptance criteria:**

- Bootstrap accepts an idempotency key and non-empty text/Markdown source list.
- Chunk checkpoints avoid reprocessing committed change sets.
- Every source, plan, approval, checkpoint, and change set is linked by
  provenance.
- Cancellation and partial-source failures follow the operation contract.

**Depends on:** `V1-C04`, `V1-TS14`.  
**Traceability:** API bulk bootstrap contract; D-03 excludes external
connectors.

### V1-C08: Rebuild the retrieval projection

**Goal:** Recreate Azure AI Search state entirely from committed manifests and
canonical Blob revisions.

**Acceptance criteria:**

- Rebuild never treats an existing projection as canonical input.
- A new projection generation is validated before becoming active.
- Failure leaves the prior usable generation intact or reports canonical-only
  degraded operation.
- Integration tests prove no retrievable committed information is lost.

**Depends on:** `V1-C04`, `V1-TS15`, `V1-TS16`.  
**Traceability:** FR-0064; NFR-0023, NFR-0041, NFR-0045.

### V1-C09: Implement deterministic correction and removal

**Goal:** Give authorized stewards or operators a non-agent path to revise or
remove an identified item without bypassing governance.

**Acceptance criteria:**

- Requests require a known item identity, correction reason, capability, and
  valid snapshot.
- Required ontology checks and mutation policy apply where relevant.
- Changes publish through the same visibility fence and audit path.
- Ordinary contributors cannot discover or invoke this management operation.

**Depends on:** `V1-TS10`, `V1-TS19`.  
**Traceability:** FR-0038; ADR-0012.

### V1-C10: Complete retrieval and opt-in synthesis

**Goal:** Add typed relationship traversal and explicitly requested narrative
synthesis while preserving evidence as the authoritative response.

**Acceptance criteria:**

- Relationship traversal observes ontology type, direction, and target
  constraints.
- Synthesis occurs only when requested and always returns the supporting
  evidence and citations.
- Unsupported claims and insufficient evidence fail closed.
- Tests compare relationship, filter, lexical, semantic, and synthesis
  outcomes.

**Depends on:** `V1-TS17`.  
**Traceability:** FR-0054, FR-0055; ADR-0013.

### V1-C11: Expose space health, size, and composition

**Goal:** Report operational state and whether projections are current without
exposing sensitive internals.

**Acceptance criteria:**

- Health includes lifecycle, canonical availability, projection freshness, and
  active operation state.
- Size and composition use stable, documented measures.
- Degraded projection status distinguishes canonical readability from search
  availability.
- Authorization and pagination follow management contract conventions.

**Depends on:** `V1-C01`, `V1-C04`, `V1-C08`.  
**Traceability:** FR-0060, FR-0061.

### V1-C12: Complete management APIs and maintenance CLI

**Goal:** Provide deterministic operator surfaces for lifecycle, roles,
operations, correction, rebuild, health, and deletion.

**Acceptance criteria:**

- Management endpoints remain logically separate from private Domain Agent
  tools and public MCP operations.
- The CLI is a thin client over supported management contracts and contains no
  bypass logic.
- Authentication, authorization, idempotency, errors, and audit are consistent
  across API and CLI.
- End-to-end tests cover the supported maintenance workflows.

**Depends on:** `V1-C02`, `V1-C04`, `V1-C08`, `V1-C09`, `V1-C11`.  
**Traceability:** FR-0067, FR-0068; API contract boundaries.

### V1-C13: Add approved binary source-asset extraction

**Goal:** Implement only the source media and extraction representations
accepted by `V1-D06`.

**Acceptance criteria:**

- Original assets and extraction outputs are immutable, digested, and linked
  through provenance.
- Unsupported, incomplete, and uncertain extraction is explicit and cannot
  authorize unsupported conclusions.
- Size, retention, safety, and content-type controls match the accepted
  decision.
- Each supported media type has representative success and failure tests.

**Depends on:** `V1-D06`, `V1-C07`.  
**Traceability:** API attachment and source-asset contract.

### V1-C14: Implement governed space deletion

**Goal:** Delete a retired space through an irreversible monitored operation
that proves cleanup.

**Acceptance criteria:**

- Only an Owner can transition `retired` to `deleting`.
- Cleanup order covers projection, source and canonical assets, control records,
  and retained audit evidence according to policy.
- Progress, retries, terminal proof, and failure recovery are exposed.
- Tests show normal reads and writes remain disabled throughout deletion.

**Depends on:** `V1-D01`, `V1-C04`, `V1-C12`.  
**Traceability:** FR-0008; ADR-0019.

### V1-C15: Enforce production data and item limits

**Goal:** Apply the accepted classification, geography, retention, network, and
item-size controls at every relevant boundary.

**Acceptance criteria:**

- Space configuration declares allowed classification and geography.
- Input, storage, retrieval, telemetry, and deletion enforce the accepted
  policies and maximum item size.
- Infrastructure policy tests and application tests fail closed on violations.
- Operator documentation describes evidence and remediation.

**Depends on:** `V1-D01`, `V1-D02`, `V1-F15`.  
**Traceability:** NFR-0005 to NFR-0009, NFR-0036.

### V1-C16: Add integrity and availability alerts

**Goal:** Turn release-critical failures into actionable Azure Monitor signals.

**Acceptance criteria:**

- Alerts cover failed publication, canonical digest mismatch, projection
  unavailability or excessive lag, authorization anomalies, and failed
  long-running work.
- Alert payloads identify the failing stage and correlation identifiers without
  knowledge content.
- Thresholds, ownership, and response guidance are documented and tested.

**Depends on:** `V1-C04`, `V1-C08`, `V1-C11`, `V1-C15`.  
**Traceability:** NFR-0051, NFR-0053; ADR-0026.

### V1-C17: Attribute and guard operational cost

**Goal:** Measure Azure and agent consumption by knowledge space and enforce the
accepted pilot cost controls.

**Acceptance criteria:**

- Relevant telemetry and resource tags support per-instance and per-space
  attribution.
- Unattended work enforces per-execution and per-period limits.
- Dashboards or reports compare measured cost with the accepted envelope.
- Exceeding a hard limit fails safely and produces an actionable event.

**Depends on:** `V1-D02`, `V1-C03`, `V1-C16`.  
**Traceability:** NFR-0080 to NFR-0082.

### V1-C18: Validate the complete V1 release

**Goal:** Provide objective evidence that the implemented V1 satisfies its
in-scope functional and release-blocking non-functional requirements.

**Acceptance criteria:**

- A traceability report maps every in-scope FR and NFR to an automated test,
  operational check, accepted exception, or explicitly deferred decision.
- Security, atomicity, provenance, groundedness, degraded-read, projection
  isolation, agent-regression, and contract-compatibility suites pass.
- Migration, bootstrap, rebuild, correction, lifecycle, CLI, and deletion
  journeys pass in the pilot environment.
- Measured latency, scale, cost, and availability evidence is recorded against
  the accepted V1 targets without converting unknowns into silent assumptions.

**Depends on:** `V1-C06` to `V1-C17`, excluding `V1-C13` when `V1-D06`
explicitly defers binary support.  
**Traceability:** Functional requirements; non-functional requirements;
implementation-readiness release gates.

## Explicitly excluded backlog

The following items should be proposed only after a later scope decision:

- Administrative web portal and visual graph exploration.
- MCP Apps visual surfaces.
- External-system-specific import connectors.
- Retrieval spanning more than one knowledge space.
- Multi-tenant isolation.
- External identity federation beyond the instance tenant.
- Public or anonymous access.
- A dedicated graph database.
- Hosted Domain Agents without an accepted justification.

## Source baseline

- [Vision and scope](product/vision-and-scope)
- [Functional requirements](product/functional-requirements)
- [Non-functional requirements](product/non-functional-requirements)
- [Assumptions and open questions](product/assumptions-and-open-questions)
- [Initial implementation readiness](architecture/implementation-readiness)
- [API contract baseline](architecture/api-contract-baseline)
- [Agentic execution model](architecture/agentic-execution-model)
- [Logical knowledge model](architecture/logical-knowledge-model)
- [Ontology storage contract](architecture/ontology-storage-contract)
- [Architecture decisions](decisions/)
