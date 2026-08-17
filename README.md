# Our IQ (RIQ)

Our IQ is a proposed team-level, project-level, or organizational MCP knowledge server for creating and maintaining a shared, repository-backed knowledge store: a governed “second brain” for teams and their work.

This repository is intentionally at the concept stage. It describes the desired direction for Our IQ without making technology, storage, deployment, indexing, authentication, or cloud architecture decisions.

## Problem

Teams accumulate important knowledge across issues, pull requests, documents, code reviews, design notes, chat, and operational experience. That knowledge is often hard to find, inconsistently structured, disconnected from its source context, and difficult to govern over time.

Our IQ is intended to help teams turn that distributed knowledge into a trusted, maintained body of shared context. The goal is to support agreement about how knowledge is captured, structured, reviewed, owned, retrieved, and evolved.

## Vision and Intended Capabilities

Our IQ should eventually help teams:

- Define knowledge schemas and ontologies for one or more repositories.
- Use MCP and agentic tools to capture, validate, organize, and store knowledge.
- Enforce agreed repository structure and contribution rules.
- Preserve provenance, source context, ownership, and contribution history.
- Retrieve, search, and extract trusted knowledge from repository-backed stores.
- Support multiple contributors and maintainers governing a shared knowledge base.
- Make team knowledge explicit, reviewable, versioned, and reusable.

## Conceptual Model

### MCP Agentic Layer

The future MCP layer is expected to provide controlled agentic access to the knowledge store. Agents may help propose new knowledge entries, validate contributions against schemas, retrieve relevant context, summarize trusted records, and assist with maintenance workflows.

Agentic behavior should remain bounded by team-defined rules. Agents should support human review, preserve source grounding, and operate within explicit permissions rather than silently rewriting shared knowledge.

### Repository-Backed Knowledge

An Our IQ knowledge store is intended to live in one or more repositories. Repository history, review processes, ownership rules, and contribution workflows should provide the foundation for trust and governance.

Knowledge entries may be structured around explicit schemas and ontologies so that teams can describe concepts, relationships, evidence, ownership, lifecycle state, and source references consistently. The repository remains the durable system of record, while MCP tools provide structured access and assistance.

## Example Team Use Cases

- Capture architectural decisions with links to source discussions, owners, and affected systems.
- Maintain a project glossary and ontology for domain concepts used across repositories.
- Record operational runbooks, known issues, mitigations, and provenance for production knowledge.
- Build a curated onboarding knowledge base grounded in reviewed repository content.
- Track product, engineering, or research knowledge with contribution and review history.
- Help agents answer project questions from trusted, governed knowledge rather than ad hoc context.

## Design Principles

- **Repository-backed knowledge:** Shared knowledge should be stored in repositories where changes can be reviewed, versioned, audited, and governed.
- **Explicit schemas and ontology:** Teams should define the structures, concepts, relationships, and validation rules that make their knowledge reliable.
- **Human and team governance:** People and teams should remain responsible for ownership, review, approval, and lifecycle management.
- **Source grounding and provenance:** Knowledge should retain links to its origin, supporting evidence, contributors, timestamps, and change history.
- **Controlled agentic access:** MCP tools and agents should operate within explicit boundaries, permissions, and contribution rules.
- **Technology-neutral design:** This repository should avoid premature choices about implementation technology, storage, indexing, hosting, or cloud architecture.
- **Secure-by-default boundaries:** Future designs should treat access control, trust boundaries, sensitive information, and safe agent behavior as foundational concerns.

## Non-Goals for This Initial Repository

This initial repository does not:

- Provide a working MCP server implementation.
- Define a final storage, indexing, retrieval, authentication, or deployment architecture.
- Introduce source code, package manifests, dependencies, configuration, CI workflows, infrastructure, tests, or generated assets.
- Choose a schema language, ontology format, repository layout, or governance workflow.
- Claim that any described capability is already implemented.

## Open Design Questions

Future discovery should answer questions such as:

- What storage model should back the knowledge store while preserving repository history?
- What schema and ontology formats should teams use?
- Which MCP interfaces, tools, prompts, and resources should be exposed?
- How should retrieval, search, ranking, extraction, and summarization work?
- How should authentication, authorization, and trust boundaries be enforced?
- How should knowledge entries be versioned, reviewed, deprecated, and archived?
- How should conflicting contributions or competing interpretations be resolved?
- What indexing strategy is appropriate, and how should indexes relate to repository state?
- What deployment models should be supported for local, team, project, or organizational use?
- How should provenance, source context, ownership, and contribution history be represented?

## Initial Scope and Next Steps

The initial scope is to establish the project intent and shared vocabulary in this README only.

Next steps may include:

1. Gather team requirements and representative knowledge-management scenarios.
2. Explore schema, ontology, governance, and repository-layout options.
3. Define candidate MCP interactions and contribution workflows.
4. Evaluate storage, retrieval, indexing, authentication, and deployment choices in later design work.
