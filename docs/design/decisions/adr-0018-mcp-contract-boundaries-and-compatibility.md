---
title: ADR-0018 - MCP contract boundaries and compatibility
status: Accepted
---

## ADR-0018: MCP contract boundaries and compatibility

## Status

Accepted

## Date and ownership

- Date: 2026-08-19
- Authors: @PlagueHO
- Reviewers: @PlagueHO

## Context

Our IQ needs a public MCP interface for Client Agents and separate deterministic
tools for Domain Agents. The accepted intent interface prohibits public
knowledge-item CRUD, but Domain Agents must still read ontology assets, preserve
source assets, validate plans, and stage or publish canonical changes through
deterministic contracts.

## Decision

Our IQ supports MCP specification `2026-07-28` for this baseline. Public MCP
operations remain intent-level and use stateless request behaviour. Private MCP
tools are schema-bound deterministic JSON operations available only to
authorized Our IQ Domain Agents through a private execution context.

An ontology may include optional example Markdown templates. Templates are
private deterministic ontology assets that guide agents; they are not strict
validation contracts. Required ontology rules remain the only template-adjacent
mechanism that blocks change-set commitment.

Long-running work is represented as monitored operations. MCP Apps remain
deferred and must not be required to complete a public operation. Additive
contract changes are backward compatible. A breaking change requires a new
major contract version and support for the immediately preceding minor version
through its published deprecation window. The window lasts at least one minor
version. Exact transport and schema-hosting mechanics remain implementation
decisions.

## Alternatives considered

| Alternative | Rejection or selection rationale |
| --- | --- |
| Expose public document CRUD | Rejected because it bypasses agent planning and governance. |
| Use one MCP tool set for public and private callers | Rejected because it risks exposing deterministic storage operations to Client Agents. |
| Use separate public intent and private deterministic MCP contracts | Selected because it preserves the public boundary and gives agents reliable tool contracts. |

## Consequences

### Positive

- POS-001: Client Agents do not need storage or document-layout knowledge.
- POS-002: Domain Agents can reliably use schema-bound, deterministic tools.
- POS-003: Optional templates guide consistent agent output without creating
  brittle schema constraints.

### Negative

- NEG-001: Two distinct contract surfaces need independent authorization,
  documentation, and compatibility testing.
- NEG-002: Private-tool discovery and agent capability binding require
  versioned manifests and compatibility testing.

## Implementation notes

- IMP-001: The public and private inventories, error model, and worked
  contribution contract are proposed in the
  [API contract baseline](../architecture/api-contract-baseline).
- IMP-002: A state-sensitive private tool validates the immutable execution
  context defined by [ADR-0016](adr-0016-immutable-execution-context-snapshots).
- IMP-003: This ADR does not select a management API transport, storage SDK, or
  JSON Schema hosting mechanism.
- IMP-004: [ADR-0024](adr-0024-domain-agent-capability-governance) binds the
  initial private tools to versioned Ontology, Contribution, and Retrieval
  Agent definitions.

## References

- REF-001: [ADR-0002](adr-0002-agent-mediated-intent-interface).
- REF-002: [ADR-0013](adr-0013-grounded-evidence-default).
- REF-003: C-19 and resolved Q-12 and Q-16 context, plus open Q-17, in
  [assumptions and open questions](../product/assumptions-and-open-questions).

## Review record

- 2026-08-19: Accepted by @PlagueHO for API-contract baseline slice #8.
