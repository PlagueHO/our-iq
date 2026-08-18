---
title: arc42 9 - Architecture decisions
status: Proposed
---

## 9. Architecture decisions

## Purpose

Summarize the decisions that shape the architecture and link to their ADRs.

## Decision register

The following `Accepted` decisions define the structural architecture. The
[ADR register](../../decisions/) is authoritative for each decision's context,
alternatives, and consequences.

| Decision | ADR | Architectural effect |
| --- | --- | --- |
| Canonical ownership | [ADR-0001](../../decisions/adr-0001-canonical-knowledge-ownership) | All canonical writes pass through Our IQ. |
| Intent-level interface | [ADR-0002](../../decisions/adr-0002-agent-mediated-intent-interface) | Public MCP tools are agent-mediated, not document CRUD. |
| Ontology management | [ADR-0003](../../decisions/adr-0003-agent-mediated-ontology-management) | Public ontology CRUD is excluded. |
| Mutation policy | [ADR-0004](../../decisions/adr-0004-per-space-mutation-policy) | Governance is configured per knowledge space. |
| Agent runtime | [ADR-0005](../../decisions/adr-0005-foundry-agent-runtime) | Foundry Agent Service is required. |
| Domain Agent deployment | [ADR-0006](../../decisions/adr-0006-shared-versioned-domain-agents) | Definitions are shared, versioned, and parameterized by space. |
| Identity | [ADR-0007](../../decisions/adr-0007-agent-identity-and-execution-context) | User and agent contexts remain distinct. |
| Service access | [ADR-0008](../../decisions/adr-0008-service-managed-identities) | Tool Services use their own managed identities. |
| Knowledge topology | [ADR-0009](../../decisions/adr-0009-canonical-markdown-and-rebuildable-projections) | Canonical Markdown is distinct from rebuildable projections. |
| Change consistency | [ADR-0010](../../decisions/adr-0010-atomic-versioned-change-sets) | Canonical mutations are atomic, versioned change sets. |
| Vocabulary | [ADR-0011](../../decisions/adr-0011-architecture-vocabulary) | Architecture roles use stable names. |
| Deterministic correction | [ADR-0012](../../decisions/adr-0012-governed-deterministic-correction) | Privileged correction remains governed and separate from ordinary contribution. |
| Retrieval default | [ADR-0013](../../decisions/adr-0013-grounded-evidence-default) | Cited structured evidence is the default query response. |
| Control metadata | [ADR-0014](../../decisions/adr-0014-cosmos-db-control-metadata) | Cosmos DB supplies the per-space control-record transaction boundary. |
| Change-set visibility | [ADR-0015](../../decisions/adr-0015-transactional-change-set-visibility-fence) | Staged revisions become canonical through one committed manifest and pointer. |
| Invocation context | [ADR-0016](../../decisions/adr-0016-immutable-execution-context-snapshots) | Agent work pins state and rejects stale mutations. |
| Unattended authority | [ADR-0017](../../decisions/adr-0017-bounded-unattended-execution-grants) | Private tools validate immutable, bounded authorization grants. |

## Open questions

- Which service coordinates long-running work (Q-24)?
- What model, prompt, and evaluation governance applies to the required agent
  runtime (Q-23)?
