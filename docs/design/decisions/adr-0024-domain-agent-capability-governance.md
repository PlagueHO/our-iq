---
title: ADR-0024 - Domain Agent capability governance
status: Accepted
---

## ADR-0024: Domain Agent capability governance

## Status

Accepted

## Date and ownership

- Date: 2026-08-19
- Authors: @PlagueHO
- Reviewers: @PlagueHO

## Context

Private tool families were inventoried but not bound to Domain Agent
definitions. Model and hosting choices also need reproducible governance without
hard-coding a deployment in application logic.

## Decision

The initial implementation has three shared, versioned Domain Agent
definitions: Ontology, Contribution, and Retrieval. Each definition carries a
fixed least-privilege private tool manifest. Tool Services validate that the
calling agent identity, definition version, and requested operation match that
manifest.

Prompt-based Microsoft Foundry Agent Service agents are used wherever they
satisfy the required behaviour. A Hosted Agent requires an accepted follow-up
decision demonstrating why prompt-based agents are insufficient.

The model deployment is configuration pinned by each immutable agent definition.
Promotion of a new model, prompt, instructions, or tool manifest requires the
documented evaluation gates and owner approval.

## Alternatives considered

| Alternative | Rejection or selection rationale |
| --- | --- |
| One general-purpose agent with all tools | Rejected because it violates least privilege and increases injection impact. |
| One agent per knowledge space | Rejected by ADR-0006 because definitions are shared and parameterized. |
| Three capability-specific shared agents | Selected because ontology, mutation, and retrieval have distinct authority and evaluation needs. |

## Consequences

### Positive

- POS-001: Tool authority is explicit, versioned, and testable.
- POS-002: Model and prompt changes remain attributable and reversible.
- POS-003: Prompt-based agents minimize custom hosting until evidence requires
  it.

### Negative

- NEG-001: Cross-capability workflows require explicit orchestration.
- NEG-002: Agent-definition promotion needs a governed lifecycle.

## Implementation notes

- IMP-001: The API contract baseline records the initial tool manifests.
- IMP-002: Model selection remains deployment configuration, not a public
  request field.
- IMP-003: [ADR-0025](adr-0025-dotnet-technology-and-package-baseline) defines
  Microsoft Agent Framework as the application integration and workflow
  composition library without replacing Foundry Agent Service as the managed
  Domain Agent runtime.

## References

- REF-001: [ADR-0005](adr-0005-foundry-agent-runtime).
- REF-002: [ADR-0006](adr-0006-shared-versioned-domain-agents).
- REF-003: [ADR-0020](adr-0020-untrusted-content-isolation).

## Review record

- 2026-08-19: Accepted by @PlagueHO during issue #4 reconciliation.
