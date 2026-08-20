---
title: ADR-0020 - Untrusted content isolation
status: Accepted
---

## ADR-0020: Untrusted content isolation

## Status

Accepted

## Date and ownership

- Date: 2026-08-19
- Authors: @PlagueHO
- Reviewers: @PlagueHO

## Context

Knowledge and source material are untrusted inputs to Domain Agents. Prompt
instructions alone cannot guarantee that embedded instructions will not affect
agent behaviour or tool use. NFR-0010 requires an enforceable boundary before
agent-mediated contribution or retrieval can be implemented.

## Decision

All knowledge, ontology grounding, and source content is treated as data, never
as instructions. Domain Agent system instructions are immutable for an agent
definition version. Each agent definition has a fixed, least-privilege private
tool manifest that content cannot extend or replace.

Tool requests and agent outcomes are schema validated. Content and derived
claims retain provenance and trust labels through planning, validation, and
retrieval. Policy checks reject an outcome when content attempts to influence
instructions, tool selection, authorization context, or governing policy.
Rejected outcomes fail closed and produce auditable diagnostics.

Evaluation gates include adversarial content that attempts instruction
injection, tool escalation, provenance removal, and unsupported claims.

## Alternatives considered

| Alternative | Rejection or selection rationale |
| --- | --- |
| Prompt instructions and regression evaluation only | Rejected because a model instruction is not an authorization boundary. |
| Permit content-directed behaviour after source approval | Rejected because source approval does not make embedded instructions trustworthy. |
| Immutable instructions, fixed tools, schema validation, provenance, and fail-closed policy | Selected because independent controls enforce the instruction/data boundary. |

## Consequences

### Positive

- POS-001: Knowledge content cannot grant capabilities or alter governing
  instructions.
- POS-002: Injection attempts produce explicit, testable failures.
- POS-003: Provenance remains available for validation and audit.

### Negative

- NEG-001: Some legitimate content that resembles instructions may require
  clarification or deterministic handling.
- NEG-002: Agent and tool changes require security regression cases.

## Implementation notes

- IMP-001: Content delimiters are defence in depth, not the trust boundary.
- IMP-002: Tool Services authorize every call independently of agent output.
- IMP-003: The initial pilot accepts only non-sensitive text and Markdown.

## References

- REF-001: NFR-0010.
- REF-002: Q-07 in the
  [assumptions and open questions](../product/assumptions-and-open-questions).
- REF-003: [ADR-0016](adr-0016-immutable-execution-context-snapshots).

## Review record

- 2026-08-19: Accepted by @PlagueHO during issue #4 reconciliation.
