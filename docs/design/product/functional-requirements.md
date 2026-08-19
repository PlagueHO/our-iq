---
title: Functional requirements
status: Proposed
owner: TBD
reviewers: TBD
---

## Functional requirements

## Purpose

Capture verifiable behaviour for the initial version without selecting a
technical implementation.

This document is `Proposed`. Every requirement below is `Proposed` until
reviewed. Requirements derive from confirmed product direction recorded in
[Vision and scope](vision-and-scope); requirements that depend on an unresolved
decision are marked **Blocked** and name the blocking question.

## Requirement format

Each requirement has a stable identifier, statement, priority, acceptance
criteria, and status.

```text
FR-0001
Status: Proposed
Priority: TBD
The system shall [observable behavior].
Acceptance criteria:
- [observable result]
```

Priority uses `Must`, `Should`, or `Could` for the initial version. `Deferred`
marks behaviour intentionally excluded from the initial version.

## Knowledge space lifecycle

| ID | Requirement | Priority | Status |
| --- | --- | --- | --- |
| FR-0001 | The system shall allow an authorized user to create a knowledge space within an instance. | Must | Proposed |
| FR-0002 | The system shall assign every knowledge space a stable identifier that is used by all operations targeting that space. | Must | Proposed |
| FR-0003 | The system shall record and expose the lifecycle state of every knowledge space. | Must | Proposed |
| FR-0004 | The system shall allow an authorized user to discover the knowledge spaces they may access, together with each space's lifecycle state. | Must | Proposed |
| FR-0005 | The system shall reject or defer any operation that is not permitted in the target space's current lifecycle state, and shall explain why. | Must | Proposed |
| FR-0006 | The system shall allow an authorized user to place a knowledge space into a read-only or offline state and to return it to normal service. | Must | Proposed |
| FR-0007 | The system shall allow an authorized user to retire a knowledge space so that normal use is disabled while retention policy applies. | Should | Proposed |
| FR-0008 | The system shall allow an authorized user to delete a knowledge space, and shall report progress of the irreversible cleanup. | Should | Proposed |

Acceptance criteria for FR-0003 and FR-0005 use the lifecycle states and
transition authorities defined in ADR-0019.

## Roles and access

| ID | Requirement | Priority | Status |
| --- | --- | --- | --- |
| FR-0010 | The system shall authenticate the human user initiating an interaction. | Must | Proposed |
| FR-0011 | The system shall authenticate the agent identity performing work on a user's behalf, distinctly from the user. | Must | Proposed |
| FR-0012 | The system shall allow an authorized user to grant and revoke roles for another user within an instance or a knowledge space. | Must | Proposed |
| FR-0013 | The system shall authorize each operation against the intersection of the user's permissions and the acting agent's permitted capabilities, such that neither can extend the other's effective authority. | Must | Proposed |
| FR-0014 | The system shall permit unattended execution only for maintenance work already authorized by an attended request or by explicit knowledge-space policy. | Must | Proposed |
| FR-0015 | The system shall record both the initiating user and the acting agent identity for every operation that reads or changes knowledge. | Must | Proposed |

The exact role taxonomy and capability granularity are open questions. The
working capability model is recorded in [Vision and scope](vision-and-scope).

## Ontology lifecycle

| ID | Requirement | Priority | Status |
| --- | --- | --- | --- |
| FR-0020 | The system shall accept grounding material describing a team's domain as input to ontology design. | Must | Proposed |
| FR-0021 | The system shall allow ontology design to proceed across multiple sessions without losing prior context or proposals. | Must | Proposed |
| FR-0022 | The system shall produce a reviewable ontology proposal describing document types, primary hierarchy, typed relationships, required metadata, and fields intended to be filterable. | Must | Proposed |
| FR-0023 | The system shall commit an approved ontology as an immutable, identified version. | Must | Proposed |
| FR-0024 | The system shall allow an authorized user to inspect the active ontology of a knowledge space they may access. | Must | Proposed |
| FR-0025 | The system shall determine whether a proposed ontology version is compatible with the existing knowledge in the space, and shall report incompatibilities before commitment. | Must | Proposed |
| FR-0026 | The system shall produce a migration plan when a proposed ontology version is incompatible with existing knowledge. | Must | Proposed |
| FR-0027 | The system shall execute an ontology migration as a monitored, resumable operation and shall report its outcome. | Must | Proposed |
| FR-0028 | The system shall not expose low-level ontology create, update, or delete operations on its public interface. | Must | Proposed |

## Knowledge contribution

| ID | Requirement | Priority | Status |
| --- | --- | --- | --- |
| FR-0030 | The system shall accept unstructured or semi-structured input as a contribution of knowledge, without requiring the contributor to identify a target document. | Must | Proposed |
| FR-0031 | The system shall interpret a contribution against the active ontology and produce a plan describing the resulting changes, which may affect zero or more documents. | Must | Proposed |
| FR-0032 | The system shall be able to conclude that a contribution requires no change to canonical knowledge, and shall report that outcome. | Must | Proposed |
| FR-0033 | The system shall evaluate every change plan against the target space's mutation policy, which may be automatic commit, contributor confirmation, or review. | Must | Proposed |
| FR-0034 | The system shall present a change plan for confirmation or review in a form that identifies affected documents and the nature of each change. | Must | Proposed |
| FR-0035 | The system shall commit an approved change plan as a single atomic, versioned change set, such that no partial change to canonical knowledge is observable. | Must | Proposed |
| FR-0036 | The system shall validate a change plan against the active ontology before commitment and shall reject plans that would violate it. | Must | Proposed |
| FR-0037 | The system shall record, for every change set, the initiating user, acting agent, ontology version, source material, approval evidence, and resulting version. | Must | Proposed |
| FR-0038 | The system shall provide a privileged path allowing an authorized steward or operator to deterministically change or remove an identified document, subject to policy and audit. | Must | Proposed |
| FR-0039 | The system shall not expose document-level create, update, or delete operations to ordinary contributors on its public interface. | Must | Proposed |
| FR-0040 | The system shall detect and reject a change plan that was produced against a superseded ontology version or superseded document state. | Must | Proposed |

Acceptance criteria for FR-0035 depend on the change-set commit mechanism, which
is an open decision.

## Knowledge retrieval

| ID | Requirement | Priority | Status |
| --- | --- | --- | --- |
| FR-0050 | The system shall accept a natural-language question scoped to a knowledge space. | Must | Proposed |
| FR-0051 | The system shall return structured evidence with citations identifying the canonical knowledge each item came from. | Must | Proposed |
| FR-0052 | The system shall support retrieval that combines semantic similarity, lexical matching, hierarchy, typed relationships, and deterministic metadata filters. | Must | Proposed |
| FR-0053 | The system shall support retrieving all knowledge related to a given ontology-defined entity by metadata filter, without depending on that entity being mentioned in document text. | Must | Proposed |
| FR-0054 | The system shall support traversing typed relationships between knowledge items during retrieval. | Should | Proposed |
| FR-0055 | The system shall offer synthesis of retrieved evidence into a narrative answer as an explicitly requested mode, not as the default response. | Should | Proposed |
| FR-0056 | The system shall report when retrieval found insufficient evidence to answer, rather than returning an unsupported answer. | Must | Proposed |
| FR-0057 | The system shall exclude from results any knowledge the requesting user is not authorized to read. | Must | Proposed |

## Operations and observability

| ID | Requirement | Priority | Status |
| --- | --- | --- | --- |
| FR-0060 | The system shall report the health of a knowledge space, including whether derived projections are current. | Must | Proposed |
| FR-0061 | The system shall report the size and composition of a knowledge space. | Should | Proposed |
| FR-0062 | The system shall expose the status, progress, and outcome of long-running operations. | Must | Proposed |
| FR-0063 | The system shall allow an authorized user to cancel a long-running operation where cancellation is safe. | Should | Proposed |
| FR-0064 | The system shall allow derived projections to be rebuilt from canonical knowledge without loss. | Must | Proposed |
| FR-0065 | The system shall record an immutable audit trail of security-relevant and knowledge-changing operations. | Must | Proposed |
| FR-0066 | The system shall emit metrics, logs, and traces sufficient to diagnose a failed operation end to end. | Must | Proposed |
| FR-0067 | The system shall provide a command-line tool covering maintenance operations. | Should | Proposed |
| FR-0068 | The system shall provide management APIs for operator and platform capabilities, separate from the public knowledge interface. | Must | Proposed |

## Deferred behaviour

| ID | Requirement | Priority | Status |
| --- | --- | --- | --- |
| FR-0070 | The system shall provide an administrative web portal including visual knowledge-graph exploration. | Deferred | Proposed |
| FR-0071 | The system shall render graph, status, or review experiences through MCP Apps where the host supports them. | Deferred | Proposed |
| FR-0072 | The system shall support external-system import connectors into a knowledge space. | Deferred | Proposed |
| FR-0073 | The system shall support retrieval spanning more than one knowledge space in a single operation. | Deferred | Proposed |

FR-0072 is deferred. Agent-mediated source-asset bootstrap is an initial
contract, while source-specific external connectors require later design.

## Open questions

- Which lifecycle states exist and which transitions are legal?
- Who may configure a space's mutation policy, and may it vary by operation risk?
- Who may confirm or approve a change plan, and does a confirmation expire?
- What is the exact role taxonomy, and can permissions attach below a space?
- What is the response when a contribution is ambiguous rather than invalid?
- Must FR-0072 be pulled into the initial version to make a new space usable?

See the [assumptions and open questions register](assumptions-and-open-questions)
for the full list.
