---
title: arc42 2 - Architecture constraints
status: Proposed
---

## 2. Architecture constraints

## Purpose

Record constraints the architecture must respect, distinguishing mandatory
constraints from preferences and from options still under evaluation.

This section is `Proposed`. Constraints listed as mandatory were confirmed with
the project owner. Their rationale is recorded in Architecture Decision Records
in the structural architecture slice; this section states the constraint, not
the decision.

## Mandatory constraints

### Technical

| ID | Constraint | Source |
| --- | --- | --- |
| CON-01 | Backend agents run on Microsoft Foundry Agent Service. | Project owner |
| CON-02 | Agents act under a Microsoft Entra agent identity, distinct from any human user identity. | Project owner |
| CON-03 | The public interface is a Model Context Protocol server. | Project owner |
| CON-04 | The public interface exposes intent-level operations resolved by an agent. It does not expose create, read, update, and delete operations over knowledge documents. | Project owner |
| CON-05 | Canonical knowledge is Markdown with structured front matter. | Project owner |
| CON-06 | Search and graph stores are rebuildable projections, never a source of truth. | Project owner |
| CON-07 | Agent-planned changes commit as one atomic, versioned change set. Partial canonical commits are not permitted. | Project owner |
| CON-08 | The solution is hosted on Microsoft Azure. | Project owner |
| CON-09 | The initial version is single-tenant and serves multiple users and multiple knowledge spaces. | Project owner |

### Organizational

| ID | Constraint | Source |
| --- | --- | --- |
| CON-20 | Design decisions are recorded as Architecture Decision Records before implementation depends on them. | `AGENTS.md` |
| CON-21 | Documentation is versioned and reviewable in the repository, and carries an explicit status. | `AGENTS.md` |
| CON-22 | Proposals and placeholders are never presented as implemented behaviour. | `AGENTS.md` |
| CON-23 | Architecture documentation follows arc42, C4, and Architecture Decision Record conventions. Reader-facing documentation follows Diataxis. | `AGENTS.md` |
| CON-24 | No credentials, tokens, private keys, or environment-specific secrets are committed. | `AGENTS.md` |

## Conventions

| ID | Convention | Source |
| --- | --- | --- |
| CON-30 | Diagrams are authored in Mermaid so they are reviewable and versioned alongside the prose. | Project convention |
| CON-31 | Architecture uses the vocabulary Client Agent, Our IQ MCP Server, Our IQ Domain Agents, Our IQ Tool Services, Our IQ Data Services. | Project owner |
| CON-32 | Functional requirements use `FR-nnnn` identifiers and non-functional requirements use `NFR-nnnn`. | Project convention |

## Constraints derived from the mandatory set

These are consequences rather than independent decisions, recorded because they
constrain later design.

| ID | Constraint | Derived from |
| --- | --- | --- |
| CON-40 | No component may treat a projection as authoritative when answering a question about what the knowledge base contains. | CON-06 |
| CON-41 | Any store chosen for canonical knowledge must support the atomicity guarantee in CON-07, or a commit protocol must supply it. | CON-07 |
| CON-42 | Knowledge content is untrusted input wherever an agent processes it. | CON-04, CON-05 |
| CON-43 | Every operation targeting a knowledge space must identify that space explicitly. | CON-09 |

## Open constraints

These may become mandatory once the corresponding question is answered.

| Candidate constraint | Blocking question | Status |
| --- | --- | --- |
| Data residency within a declared geography | Q-21 | Open |
| Data classification limits per knowledge space | Q-21 | Open |
| Audit retention period | Q-21 | Open |
| Network isolation requirements between components | Q-24 | Open |
| Cost envelope per instance | Q-22 | Open |

`CON-09` is amended: the initial version targets pilot scale (one team, under
20 users, under 5,000 knowledge items per space; see `C-17`). Revisit before a
wider rollout.

The initial version targets Model Context Protocol specification `2026-07-28`
(`C-19`); the compatibility and deprecation policy as later versions release
remains open (`Q-13`).

Question identifiers refer to the
[assumptions and open questions register](../../product/assumptions-and-open-questions).

## Open questions

- Which regulatory or organizational compliance regimes apply to this instance?
- Are there constraints on which Azure regions or subscriptions may be used?
- Is there an organizational standard for platform identity and network isolation
  that Our IQ must adopt rather than choose?
