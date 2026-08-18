---
title: arc42 4 - Solution strategy
status: Proposed
---

## 4. Solution strategy

## Purpose

Summarize the decisions that shape the solution and connect them to the quality
goals they serve.

This section is `Proposed`. Each strategy below reflects direction confirmed
with the project owner. The reasoning and alternatives for each are recorded as
Architecture Decision Records in the structural architecture slice; this section
states the approach and why it exists.

## Strategy summary

| Strategy | Serves quality goal | Consequence |
| --- | --- | --- |
| Structure before content | Precise retrieval | A knowledge space defines its ontology before it accepts knowledge |
| Intent in, agent decides | Low-friction contribution | The public interface never edits documents directly |
| Canonical documents, derived projections | Trustworthy knowledge | Any index can be discarded and rebuilt without loss |
| Atomic versioned change sets | Governed change | Multi-document changes commit together or not at all |
| Two identities on every call | Safe agent behaviour, governed change | Both the user and the acting agent are authorized and audited |
| Evidence over answers | Trustworthy knowledge | Retrieval returns cited material by default, not a summary |
| Escape hatch for determinism | Trustworthy knowledge | A steward can correct a specific document without agent interpretation |
| Long-running work is explicit | Operability | Structural work is a monitored job, not a blocking call |

## Structure before content

An ontology describes a knowledge space's document types, primary hierarchy,
typed relationships, required metadata, and which fields must be filterable. It
is defined by the team that owns the knowledge, from grounding material they
supply, through an agent-mediated conversation.

This is the central bet of the product. It is what allows an agent to answer a
scoped question completely, rather than hoping that similarity search surfaces
everything relevant. It also means a knowledge space is not usable until its
ontology exists, and that changing an ontology is a governed migration rather
than an edit.

## Intent in, agent decides

The public interface accepts what the contributor knows, not where it should go.
A Domain Agent reads the active ontology and the current knowledge, then plans
the resulting changes, which may affect zero or many documents.

This removes the need for contributors to learn the structure, which is what
keeps the knowledge base current. It also means the platform must treat the
agent's plan as a proposal subject to policy, validation, and audit rather than
as a trusted instruction.

## Canonical documents, derived projections

Canonical knowledge is Markdown with structured front matter. Relationships
between knowledge items are recorded in front matter, so the canonical form is
self-describing and portable.

Search indexes and any graph store are projections built from that canonical
state. They can be rebuilt at any time. No component answers a question about
what the knowledge base contains by consulting a projection alone.

## Atomic versioned change sets

A single contribution can require coordinated changes across several documents,
because knowledge items reference each other. Committing those changes
individually would leave the knowledge base internally inconsistent and would
make provenance ambiguous.

Every plan therefore commits as one atomic, versioned change set recording the
initiating user, the acting agent, the ontology version, the source material,
the approval evidence, and the resulting version.

## Two identities on every call

Every operation carries the identity of the human who initiated it and the
identity of the agent performing it. Authorization is the intersection of the
two: neither can extend the other's authority.

Services access platform dependencies with their own managed identities. User
and agent identities remain authorization and audit context and are not used as
platform data-plane credentials. This keeps platform permissions narrow and
prevents an agent from reaching storage directly.

## Evidence over answers

Our IQ's usual caller is another agent. Returning a synthesized answer would mean
that agent summarizes a summary, which compounds error, discards structure it
could reason over, and pays for generation twice.

Retrieval therefore returns structured evidence with citations by default.
Synthesis into a narrative answer remains available as an explicitly requested
mode.

## Escape hatch for determinism

Agent-mediated contribution cannot satisfy every need. A factual error in a
specific sentence, or a compliance obligation to remove exact content, requires
a deterministic operation on an identified document.

A privileged steward or operator path provides this. It remains subject to space
policy and audit, and it is not the everyday contribution route.

## Long-running work is explicit

Ontology design, ontology migration, provisioning, reindexing, and deletion can
take longer than a caller will wait. These are modelled as jobs with observable
status, resumption after interruption, and defined compensation on failure. A
knowledge space's lifecycle state tells callers what is currently permitted.

## Deliberately deferred

The initial version does not include an administrative portal, MCP Apps visual
surfaces, bulk import, cross-space retrieval, or multi-tenancy. Each is recorded
in the
[assumptions and open questions register](../../product/assumptions-and-open-questions)
with the reason for deferral.

## Open questions

- What is the smallest end-to-end slice that would validate the structure-first
  bet: ontology creation, one contribution, and one scoped retrieval?
- Which strategy is most affected if the confirmed knowledge-space-level
  authorization model changes in a future version?
- Should the deterministic path reuse the change-set ledger, or is it a distinct
  audited operation?
