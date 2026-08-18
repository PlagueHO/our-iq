---
title: arc42 12 - Glossary
status: Proposed
---

## 12. Glossary

## Purpose

Define terms so that requirements, architecture, contracts, and reader-facing
documentation use one vocabulary.

This section is `Proposed`. Terms marked `Confirmed` reflect agreed direction.
Terms marked `Proposed` are working definitions that may change as the design
develops.

## Product concepts

| Term | Definition | Status |
| --- | --- | --- |
| Our IQ | An agent-first platform that lets a team build and govern a shared knowledge base whose structure makes knowledge reliably retrievable by agents. | Confirmed |
| Instance | One single-tenant deployment of Our IQ, serving multiple users and hosting multiple knowledge spaces. | Confirmed |
| Knowledge space | An independently governed, discoverable, lifecycle-managed body of knowledge within an instance. Often shortened to *space*. | Confirmed |
| Team | The working term for any group that owns a knowledge space: a team, project, organization, or community. | Confirmed |
| Ontology | The team-defined description of a knowledge space's document types, primary hierarchy, typed relationships, required metadata, and filterable fields. | Confirmed |
| Ontology version | An immutable, identified snapshot of an ontology. A knowledge space records which version is active. | Confirmed |
| Grounding material | Documents or descriptions a team supplies to inform ontology design. | Confirmed |
| Knowledge item | A canonical unit of knowledge, expressed as Markdown with structured front matter. | Confirmed |
| Front matter | The structured metadata block at the head of a knowledge item, carrying both ontology-required and team-extensible fields. | Confirmed |
| Primary hierarchy | The single ontology-defined position each knowledge item occupies. | Confirmed |
| Typed relationship | A named, directional link between knowledge items, recorded canonically in front matter. | Confirmed |
| Contribution | Unstructured or semi-structured input offered as knowledge, without identifying a target document. | Confirmed |
| Change plan | An agent's proposal describing the document changes a contribution should produce. May affect zero or many documents. | Confirmed |
| Change set | An atomic, versioned group of knowledge mutations committed together, with its provenance and approval evidence. | Confirmed |
| Mutation policy | A knowledge space's configured rule for handling a change plan: automatic commit, contributor confirmation, or review. | Confirmed |
| Evidence | A retrieval result comprising knowledge content and a citation identifying the canonical item it came from. | Confirmed |
| Synthesis | The opt-in mode in which retrieved evidence is composed into a narrative answer. | Confirmed |
| Projection | A derived, rebuildable representation of canonical knowledge, such as a search index or graph, used to make retrieval efficient. | Confirmed |
| Lifecycle state | The current condition of a knowledge space, determining which operations are permitted. | Confirmed |

## Architecture vocabulary

| Term | Definition | Status |
| --- | --- | --- |
| Client Agent | An external agent or Model Context Protocol host acting on a user's behalf. Outside the Our IQ boundary. | Confirmed |
| Our IQ MCP Server | The public protocol and authentication boundary exposing intent-level operations. | Confirmed |
| Our IQ Domain Agents | Shared, versioned backend agents, parameterized by knowledge-space identifier, that interpret intent and plan work. | Confirmed |
| Our IQ Tool Services | Private services exposing narrow domain capabilities to Domain Agents. Not publicly reachable. | Confirmed |
| Our IQ Data Services | Canonical stores, control metadata, projections, messaging, and other platform dependencies. | Confirmed |
| Canonical store | The authoritative store of knowledge items. Never a projection. | Confirmed |
| Change-set ledger | The immutable record of change plans, approvals, provenance, and committed versions. | Proposed |
| Control metadata | Knowledge-space identity, lifecycle state, membership, policy, and job records. | Proposed |
| Visibility fence | The committed active pointer and manifest that make one complete change set visible to canonical readers. | Confirmed |
| Execution context | An immutable snapshot that pins a space's state, governing versions, identities, and trace information for one invocation. | Confirmed |
| Execution grant | Immutable, bounded authorization evidence for unattended work. | Confirmed |
| Required rule | An ontology rule whose violation blocks a change set. | Confirmed |
| Recommended rule | An ontology rule surfaced for review that may proceed only with recorded rationale. | Confirmed |
| Informational rule | Ontology guidance that influences planning or review but never blocks commitment. | Confirmed |

## Identity and execution

| Term | Definition | Status |
| --- | --- | --- |
| User identity | The authenticated human initiating an interaction. | Confirmed |
| Agent identity | The Microsoft Entra identity under which an Our IQ Domain Agent acts, distinct from any user. | Confirmed |
| Managed identity | The platform identity a service uses to reach its own platform dependencies. Not used to represent a user or an agent. | Confirmed |
| Attended execution | Work performed while an initiating user's delegated authority is present. | Confirmed |
| Unattended execution | Work performed under an agent identity alone, limited to maintenance already authorized by an attended request or space policy. | Confirmed |
| Deterministic correction | A privileged operation that changes or removes an identified document without agent interpretation, subject to policy and audit. | Confirmed |
| Job | A long-running operation with observable status, resumption, and defined compensation on failure. | Proposed |

## External terms

| Term | Definition | Status |
| --- | --- | --- |
| Model Context Protocol | The open protocol by which agents and hosts consume tools, resources, and prompts. Abbreviated MCP. | Confirmed |
| MCP Apps | A Model Context Protocol extension allowing a server to return interactive visual surfaces to a supporting host. Deferred beyond the initial version. | Proposed |
| Microsoft Foundry Agent Service | The required runtime hosting Our IQ Domain Agents. | Confirmed |
| Microsoft Entra ID | The identity provider authenticating users and issuing agent identities. | Confirmed |
| arc42 | The architecture documentation template used by this repository. | Confirmed |
| C4 | The architecture diagram notation used for context, container, and component views. | Confirmed |
| Architecture Decision Record | A dated, status-marked record of one consequential decision and its alternatives. Abbreviated ADR. | Confirmed |
| Diataxis | The framework classifying reader-facing documentation as tutorial, how-to, reference, or explanation. | Confirmed |

## Terms deliberately avoided

| Avoided term | Use instead | Reason |
| --- | --- | --- |
| Document | Knowledge item | *Document* suggests a file a user edits directly, which the public interface does not permit. |
| MCP Gateway | Our IQ MCP Server | The component is a protocol server, not a pass-through gateway. |
| Database | Canonical store or projection | The distinction between authoritative and derived state matters more than the storage technology. |
| Knowledge graph | Typed relationships, graph projection | The domain model is graph-shaped, but no graph database is selected. |
| CRUD | Intent-level operation | The public interface has no create, read, update, and delete surface over knowledge. |

## Open questions

- Is *knowledge space* the term the first target team would use, or is *space*
  or another word clearer?
- Do change plans and change sets need distinct user-facing names?
- What are the individual names of each Domain Agent and Tool Service?
