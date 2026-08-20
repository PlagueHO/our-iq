---
title: Vision and scope
status: Proposed
owner: "@PlagueHO"
reviewers: "@PlagueHO"
---

## Vision and scope

## Purpose

Define the problem Our IQ addresses, the audience it serves, and the boundary of
the initial version.

This document is `Proposed`. Statements marked **Confirmed** reflect direction
agreed with the project owner. Statements marked **Proposed** are working
hypotheses. Nothing here describes implemented behaviour.

## Vision

**Confirmed.** Our IQ enables a team, project, organization, or other group to
build and govern a shared knowledge base — a collective second brain — whose
structure makes knowledge reliable, fast, and effective for agents to retrieve.

Teams already accumulate knowledge in documents, wikis, chat threads, and
individual memory. Agents cannot use that knowledge reliably because it is
unstructured, scattered, and undescribed. A flat pile of files is not enough: an
agent has no way to know how documents relate, where new knowledge belongs, or
which subset is relevant to a question.

Our IQ addresses this by making structure explicit. Like an encyclopedia, each
knowledge space defines how its content is organized before content is added.
That structure — the ontology — is what allows an agent to place new knowledge
correctly and retrieve relevant knowledge precisely.

Throughout this documentation, *team* is the working term for any such group.

## Audience

**Confirmed.** Our IQ is agent-first. Its primary consumer is an agent acting on
behalf of a person, not a person browsing a website.

| Audience | Relationship to Our IQ |
| --- | --- |
| Client Agent | Calls the Our IQ MCP Server on a user's behalf to contribute and retrieve knowledge |
| Knowledge contributor | Supplies knowledge through their agent, without needing to know where it is stored |
| Knowledge consumer | Asks questions through their agent and receives grounded, cited evidence |
| Ontology steward | Shapes and evolves the structure of a knowledge space |
| Space administrator | Governs membership, lifecycle, and maintenance of a space |
| Instance administrator | Creates knowledge spaces and sets instance-level policy |
| Operator or auditor | Reviews health, jobs, history, and audit records |

## Value proposition

**Confirmed.**

- Knowledge is retrievable by structure, not only by keyword or similarity. A
  question scoped to a specific entity does not depend on that entity's name
  appearing in the text.
- Contribution requires no knowledge of the storage layout. A contributor
  supplies information; Our IQ decides where it belongs.
- Structure is defined by the team that owns the knowledge, not imposed by the
  platform.
- Every change is attributable, versioned, and governed.
- Retrieval returns evidence with citations, so a calling agent can reason over
  grounded material rather than trusting a summary.

## Core concepts

**Confirmed.**

| Concept | Definition |
| --- | --- |
| Instance | One single-tenant deployment of Our IQ, serving multiple users |
| Knowledge space | An independently governed, discoverable, lifecycle-managed body of knowledge within an instance |
| Ontology | The team-defined description of a space's document types, hierarchy, relationships, required metadata, and filterable fields |
| Knowledge item | A canonical unit of knowledge, expressed as Markdown with structured front matter |
| Change set | An atomic, versioned group of knowledge mutations committed together |
| Projection | A derived, rebuildable index or graph built from canonical knowledge |

## In scope for the initial version

**Confirmed.**

### Knowledge spaces

- Create, discover, inspect, administer, retire, and delete knowledge spaces.
- Query the lifecycle state of a space before interacting with it.
- Assign users roles within an instance or a space.

### Ontology

- Design and refine an ontology from grounding material supplied by the team,
  through an agent-mediated conversation that may span multiple sessions.
- Commit an ontology as an immutable version with a migration plan.
- Take a space offline or read-only while an incompatible migration is applied.

### Contribution

- Accept unstructured or semi-structured knowledge as an expression of intent
  rather than a document edit.
- Let a backend agent interpret that input against the active ontology and plan
  zero or more coordinated document changes.
- Evaluate the plan against the space's mutation policy, which may commit
  automatically, require the contributor's confirmation, or route to review.
- Commit approved changes as one atomic, versioned change set.
- Provide a privileged steward or operator path that deterministically corrects
  or removes a specific document, so factual corrections and compliance
  takedowns do not depend on agent interpretation.

### Retrieval

- Interpret a question, plan retrieval, and return structured grounded evidence
  with citations.
- Combine semantic similarity, lexical matching, hierarchy, typed
  relationships, and deterministic metadata filters.
- Offer synthesis into a narrative answer as an opt-in mode.

### Operations

- Inspect space structure, ontology, size, health, and lifecycle state.
- Run and monitor long-running provisioning, migration, and reindexing work.
- Record logging, metrics, audit trails, and failure information.

### Interfaces

- An MCP server exposing intent-level tools as the primary interface.
- Management APIs for operator and platform capabilities.
- A command-line tool for maintenance workflows.

## Out of scope for the initial version

**Proposed.** These are deferred rather than rejected.

- An administrative web portal with visual knowledge-graph exploration.
- MCP Apps surfaces for visual responses within a host.
- Bulk import of existing team knowledge from external systems.
- Federation or search across multiple knowledge spaces in one operation.
- Multi-tenancy. The initial version is single-tenant.
- External identity federation beyond the instance's own tenant.
- Public or anonymous read access.

## Deliberate non-goals

**Confirmed.**

- Our IQ is not a document editor. It exposes no public CRUD surface over
  knowledge documents. Contribution is expressed as intent and resolved by an
  agent; deterministic edits are a governed operator capability, not the
  everyday path.
- Our IQ is not a general-purpose file store. Content that does not fit the
  ontology of a space does not belong in that space.
- Our IQ does not attempt to be the authoritative system of record for data that
  another system already owns.

## Measures of success

**Proposed.** Targets are set in the non-functional requirements once scale
input is available.

| Outcome | Signal |
| --- | --- |
| Knowledge is findable | Share of queries returning evidence judged relevant by the requester |
| Contribution is low-friction | Share of contributions committed without manual correction |
| Structure holds up | Rate of ontology violations detected at validation |
| Change is trustworthy | Share of committed change sets with complete provenance |
| The platform is operable | Time to complete an ontology migration without data loss |

## Open questions

- Which single workflow should the first validated increment prove: ontology
  creation, contribution, or retrieval?
- What is the expected number of knowledge spaces, contributors, and documents
  per space?
- Which deferred item is most likely to be pulled forward, and why?
- What evidence would justify introducing cross-space retrieval?

See the [assumptions and open questions register](assumptions-and-open-questions)
for the full list.
