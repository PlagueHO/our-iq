---
title: arc42 8 - Cross-cutting concepts
status: Proposed
---

## 8. Cross-cutting concepts

## Purpose

Capture concepts that affect multiple building blocks.

This section is `Proposed`. It links accepted decisions to cross-cutting
concepts without defining protocols or implementation mechanisms that remain
open.

## Identity, authorization, and audit

The initiating user and the acting agent are distinct identities. Attended work
preserves user context through an on-behalf-of flow; unattended work is limited
to previously authorized or policy-authorized maintenance. Each Tool Service
uses its own managed identity for data access.

Authorization applies at the knowledge-space boundary and evaluates the
intersection of user permissions and agent capabilities. The fixed role set and
its exact capabilities remain open. See
[ADR-0007](../../decisions/adr-0007-agent-identity-and-execution-context) and
[ADR-0008](../../decisions/adr-0008-service-managed-identities).

An immutable execution-context snapshot pins the agent definition, ontology,
policy, canonical head, and identities for every invocation. Tool Services
reject stale state-sensitive work. Unattended execution also requires a
bounded, auditable execution grant linked to an attended approval or space
policy. See [ADR-0016](../../decisions/adr-0016-immutable-execution-context-snapshots)
and [ADR-0017](../../decisions/adr-0017-bounded-unattended-execution-grants).

## Canonical knowledge, provenance, and projections

Canonical knowledge is Markdown with structured front matter. Every canonical
write passes through Our IQ and an approved mutation commits as one versioned
change set. Canonical state carries provenance including the initiating user,
acting agent, source material, ontology version, approval evidence, and
resulting version.

Immutable staged revisions become canonical only when a per-space transactional
publication writes the committed manifest and active pointer. The pointer is
the visibility fence, so readers never observe a partial change set. See
[ADR-0015](../../decisions/adr-0015-transactional-change-set-visibility-fence).

Search and graph stores are rebuildable projections. They may support
retrieval, but never decide what canonical knowledge contains. See
[ADR-0001](../../decisions/adr-0001-canonical-knowledge-ownership),
[ADR-0009](../../decisions/adr-0009-canonical-markdown-and-rebuildable-projections),
and [ADR-0010](../../decisions/adr-0010-atomic-versioned-change-sets).

## Knowledge-space governance

Every operation identifies a knowledge space. The space supplies the active
ontology and mutation policy. Mutation policy selects automatic commitment,
contributor confirmation, or review. Privileged deterministic correction is a
separate, governed management path.

Ontology rules distinguish Required validation from Recommended guidance and
Informational agent guidance. This retains structural integrity without
misrepresenting evolving shared knowledge as relational data. See the
[logical knowledge model](../logical-knowledge-model).

See [ADR-0003](../../decisions/adr-0003-agent-mediated-ontology-management),
[ADR-0004](../../decisions/adr-0004-per-space-mutation-policy), and
[ADR-0012](../../decisions/adr-0012-governed-deterministic-correction).

## Grounded retrieval and untrusted content

The default retrieval result is structured evidence with citations to canonical
knowledge. Narrative synthesis is opt-in. Knowledge content is untrusted input
when processed by agents and must not alter agent instructions or permitted
tools. See [ADR-0013](../../decisions/adr-0013-grounded-evidence-default) and
NFR-0010.

## Observability and resilience

Requests, agent invocations, Tool Service work, change sets, projection work,
and long-running operations require traceability and diagnosability.
Projection failure must not roll back or corrupt committed canonical state.
Specific telemetry services, alerting, audit retention, and recovery targets
remain open.

## Open questions

- How are untrusted-content controls enforced throughout agent processing
  (Q-07)?
- What exact capabilities and delegation rules apply to each knowledge-space
  role (Q-04)?
- How do grant issuance, revocation, and limit accounting appear in the eventual
  management contract?
- What error taxonomy, evidence schema, and public compatibility policy apply
  (Q-11 to Q-13)?
