---
title: arc42 1 - Introduction and goals
status: Proposed
---

## 1. Introduction and goals

## Purpose

Describe the essential goals, stakeholders, and business context of Our IQ.

This section is `Proposed`. It summarizes direction confirmed with the project
owner and recorded in [Vision and scope](../../product/vision-and-scope). It
describes intent, not implemented behaviour.

## Problem statement

Teams accumulate knowledge faster than they can organize it. That knowledge ends
up in documents, wikis, chat threads, and individual memory. Agents working on
the team's behalf cannot use it reliably, because nothing describes how it is
structured, how items relate, or which subset is relevant to a given question.

Storing files in a folder hierarchy does not solve this. A folder path carries
almost no meaning an agent can reason about, and it cannot express that a piece
of knowledge relates to several things at once.

Our IQ makes the structure explicit and machine-readable, so that an agent can
place new knowledge correctly and retrieve relevant knowledge precisely.

## Quality goals

The top quality goals for the initial version, in priority order. Measurable
targets are held in the
[non-functional requirements](../../product/non-functional-requirements).

| Priority | Quality goal | Motivation |
| --- | --- | --- |
| 1 | Trustworthy knowledge | An agent acting on Our IQ's output must be able to show where each claim came from. Without provenance and groundedness the knowledge base is worse than no knowledge base. |
| 2 | Safe agent behaviour | Knowledge content is user-supplied and is fed back into agents. It must never be able to change what an agent is instructed to do or which tools it may call. |
| 3 | Precise retrieval | Knowledge must be findable by structure and metadata, not only by similarity, so that scoped questions return complete and relevant results. |
| 4 | Governed change | Every change is attributable, versioned, policy-checked, and atomic. Partial or unattributed change is not acceptable. |
| 5 | Low-friction contribution | A contributor supplies information without needing to know where it belongs, or the knowledge base will not be kept current. |
| 6 | Operability | Long-running structural work must be observable, resumable, and reversible. |

Quality goals 1 and 2 are release-blocking.

## Stakeholders

| Stakeholder | Interest | Concern | Owner |
| --- | --- | --- | --- |
| Client Agent developer | Integrate Our IQ as a knowledge source for an agent | Unambiguous contracts, predictable errors, stable versioning | TBD |
| Knowledge contributor | Capture what they know without administrative overhead | Contribution is understood correctly and is attributable to them | TBD |
| Knowledge consumer | Get accurate, cited answers through their agent | Evidence is relevant, current, and traceable | TBD |
| Ontology steward | Shape and evolve the structure of a knowledge space | Ontology changes are safe, reviewable, and reversible | TBD |
| Space administrator | Govern membership, lifecycle, and policy for a space | Access control and mutation policy are enforceable | TBD |
| Instance administrator | Operate the instance and create spaces | Isolation between spaces, cost visibility, capacity | TBD |
| Operator | Keep the platform healthy | Diagnosability, resumable jobs, actionable alerts | TBD |
| Security and compliance reviewer | Assure appropriate handling of information | Identity, audit, classification, retention, residency | TBD |
| Project owner | Deliver a useful platform | Scope stays coherent and decisions are recorded | @PlagueHO |

## Open questions

- Who reviews and approves design decisions besides the project owner?
- Which stakeholder represents the first target team, and what is their domain?
- What outcome would demonstrate that the agent-first model works?
