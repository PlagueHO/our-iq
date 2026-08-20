---
title: ADR-0027 - Simplicity and code quality principles
status: Accepted
---

## ADR-0027: Simplicity and code quality principles

## Status

Accepted

## Date and ownership

- Date: 2026-08-20
- Authors: @PlagueHO
- Reviewers: @PlagueHO

## Context

The selected technology stack does not by itself establish how implementation
should be shaped. The project needs durable defaults for simplicity,
testability, readability, consistency, and clean code so that the first
implementation does not accumulate speculative complexity.

## Decision

The implementation starts with the smallest useful use case and the simplest
design that satisfies the current requirements. YAGNI and KISS are explicit
defaults: speculative abstractions, extensibility, and complex workflows are
not introduced until evidence or a stated requirement justifies them.

Testability and readability are first-class design constraints. Naming,
terminology, formatting, project structure, and patterns must remain consistent
across the solution. Code should be clean and self-documenting, with short,
focused methods and classes and no avoidable code smells or duplication.

Comments are used sparingly. A comment is appropriate when it communicates
complex intent that cannot be made clear through better names, structure, or
code. Complexity is refactored in response to evidence rather than predicted
in advance.

SOLID, DRY, separation of concerns, Domain-Driven Design, and Onion
Architecture are applied pragmatically. They guide boundaries and
dependencies but must not become ceremony that makes the minimal solution
harder to understand or test.

## Alternatives considered

| Alternative | Rejection or selection rationale |
| --- | --- |
| Minimal, readable, testable implementation with pragmatic principles | Selected because it supports the thin slice without speculative complexity. |
| Design the most general architecture first | Rejected because it increases complexity before requirements and evidence justify it. |
| Prefer comments over clear code | Rejected because comments can drift; intent should primarily be expressed through names and structure. |
| Apply SOLID, DDD, and Onion Architecture rigidly | Rejected because unnecessary ceremony conflicts with simplicity and readability. |

## Consequences

### Positive

- POS-001: The first implementation remains understandable and focused.
- POS-002: Small, testable units make defects and behavior changes easier to
  isolate.
- POS-003: Consistent naming and structure reduce cognitive load across the
  solution.
- POS-004: Complexity is justified by requirements or evidence instead of
  speculation.

### Negative

- NEG-001: Some abstractions will be added later when real requirements expose
  the need.
- NEG-002: Applying the principles well requires active review discipline.

## Implementation notes

- IMP-001: Repository guidance in `AGENTS.md` and
  `.github/copilot-instructions.md` is normative for agent-assisted changes.
- IMP-002: Reviews should challenge unnecessary complexity, inconsistent
  naming, long methods, duplicated logic, and untestable design.
- IMP-003: The first thin slice should be the simplest representative use case,
  not the most complex future workflow.

## References

- REF-001: [AGENTS.md](https://github.com/PlagueHO/our-iq/blob/main/AGENTS.md).
- REF-002: [Copilot instructions](https://github.com/PlagueHO/our-iq/blob/main/.github/copilot-instructions.md).
- REF-003: [ADR-0025](adr-0025-dotnet-technology-and-package-baseline).
- REF-004: [ADR-0026](adr-0026-implementation-platform-and-delivery-conventions).

## Review record

- 2026-08-20: Accepted by @PlagueHO during issue #4 engineering-principles
  discussion.
