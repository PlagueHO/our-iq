---
title: Non-functional requirements
status: Proposed
owner: "@PlagueHO"
reviewers: "@PlagueHO"
---

## Non-functional requirements

## Purpose

Capture measurable quality attributes and operational constraints independently
from implementation technology.

This document is `Proposed`. Each requirement states a quality attribute, a
measurement method, and a scope. Targets are marked `TBD` where scale, policy,
or cost input is not yet available; the requirement is still stated so a target
can be attached without restructuring the register. The governance targets for
NFR-0005, NFR-0007, and NFR-0008 are the accepted baseline in
[ADR-0030](../decisions/adr-0030-pilot-data-governance-and-retention-controls);
the requirement status remains `Proposed` until implementation evidence exists.

## Requirement format

```text
NFR-0001
Quality attribute: TBD
Target: TBD
Measurement: TBD
Scope: TBD
Status: Proposed
```

## Security and privacy

| ID | Quality attribute | Requirement | Target | Status |
| --- | --- | --- | --- | --- |
| NFR-0001 | Authentication | Every request that reads or changes knowledge is attributable to an authenticated user, an authenticated agent identity, or both. | 100% of requests | Proposed |
| NFR-0002 | Authorization | No operation succeeds that exceeds the intersection of user permissions and acting agent capabilities. | Zero unauthorized successes | Proposed |
| NFR-0003 | Least privilege | Each service component holds only the platform permissions required for its own responsibilities. | Verified per component | Proposed |
| NFR-0004 | Auditability | Security-relevant and knowledge-changing operations produce immutable audit records. | 100% of such operations | Proposed |
| NFR-0005 | Audit retention | Audit records are retained for a defined period. | At least 365 days for 100% of audit records; diagnostic telemetry defaults to no more than 30 days | Proposed |
| NFR-0006 | Encryption | Knowledge is encrypted in transit and at rest. | All stores and transports | Proposed |
| NFR-0007 | Data classification | Each knowledge space declares the classification of the content it may hold, and the system enforces the resulting handling constraints. | 100% of spaces have a classification before activation; zero writes exceed the declared policy; pilot data is non-sensitive synthetic or internal test data | Proposed |
| NFR-0008 | Data residency | Knowledge remains within a declared geography. | 100% of knowledge, projections, backups, and recovery copies remain within the deployment's approved geography set; zero unapproved cross-geography copies or exports | Proposed |
| NFR-0009 | Content safety | Contributed content and generated output are screened against a defined safety policy. | TBD | Proposed |
| NFR-0010 | Untrusted content | Knowledge content is treated as untrusted input to any agent that processes it, and cannot alter that agent's instructions or permitted tool set. | Zero successful instruction injections | Proposed |

NFR-0010 is release-blocking. Our IQ stores content contributed by users and
feeds it back into agents, which is a direct prompt-injection path.

## Correctness and trustworthiness

| ID | Quality attribute | Requirement | Target | Status |
| --- | --- | --- | --- | --- |
| NFR-0020 | Atomicity | No partial change set is observable in canonical knowledge. | Zero observable partial commits | Proposed |
| NFR-0021 | Provenance | Every canonical knowledge item can be traced to the change sets that produced it, including source material and acting identities. | 100% of items | Proposed |
| NFR-0022 | Groundedness | Every evidence item returned by retrieval cites the canonical knowledge it came from. | 100% of evidence items | Proposed |
| NFR-0023 | Rebuildability | Every derived projection can be rebuilt from canonical knowledge with no loss of retrievable information. | Verified per projection | Proposed |
| NFR-0024 | Ontology conformance | Committed knowledge conforms to the ontology version in force at commit time. | 100% of commits | Proposed |
| NFR-0025 | Agent behaviour regression | Changes to an agent definition, prompt, or model are evaluated against a fixed set of representative cases before rollout. | No unreviewed regressions | Proposed |

NFR-0025 depends on the agent evaluation strategy, which is defined in a later
design slice.

## Performance and scale

| ID | Quality attribute | Requirement | Target | Status |
| --- | --- | --- | --- | --- |
| NFR-0030 | Retrieval latency | Time from question to returned evidence. | TBD | Proposed |
| NFR-0031 | Contribution latency | Time from contribution to a returned change plan or commit confirmation. | TBD | Proposed |
| NFR-0032 | Read-after-write visibility | Time before a committed change is reflected in retrieval results. | TBD | Proposed |
| NFR-0033 | Concurrent users | Users concurrently interacting with one instance. | Under 20 (pilot scale; C-17) | Proposed |
| NFR-0034 | Knowledge spaces per instance | Spaces one instance can host without degradation. | Low single digits (pilot: one team) | Proposed |
| NFR-0035 | Knowledge items per space | Items one space can hold while meeting retrieval latency targets. | Under 5,000 (pilot scale; C-17) | Proposed |
| NFR-0036 | Knowledge item size | Maximum size of a single canonical knowledge item. | TBD | Proposed |
| NFR-0037 | Migration duration | Time to migrate a space to a new ontology version, and the maximum acceptable write outage. | TBD | Proposed |

Pilot-scale targets are set (NFR-0033 to NFR-0035; see C-17 in the
[assumptions and open questions register](assumptions-and-open-questions)).
Retrieval latency, contribution latency, item size, and migration duration
still depend on architecture decisions made in later slices.

## Availability and recoverability

| ID | Quality attribute | Requirement | Target | Status |
| --- | --- | --- | --- | --- |
| NFR-0040 | Availability | Availability of retrieval for a space in the `active` state. | Best-effort; no formal target for pilot (C-20) | Proposed |
| NFR-0041 | Degraded operation | Canonical knowledge remains readable when a derived projection is unavailable. | Required | Proposed |
| NFR-0042 | Recovery point objective | Maximum acceptable loss of committed knowledge. | Best-effort; no formal target for pilot (C-20) | Proposed |
| NFR-0043 | Recovery time objective | Maximum acceptable time to restore service after failure. | Best-effort; no formal target for pilot (C-20) | Proposed |
| NFR-0044 | Resumability | A long-running operation resumes or compensates after an interruption rather than leaving inconsistent state. | Required | Proposed |
| NFR-0045 | Projection failure isolation | Failure of a projection update does not roll back or corrupt a committed change set. | Required | Proposed |

## Operability and observability

| ID | Quality attribute | Requirement | Target | Status |
| --- | --- | --- | --- | --- |
| NFR-0050 | Traceability | A single user request can be traced across every component and agent invocation it triggers. | 100% of requests | Proposed |
| NFR-0051 | Diagnosability | A failed operation reports enough detail to identify the failing stage without access to raw logs. | Required | Proposed |
| NFR-0052 | Job visibility | Long-running operations expose progress and outcome. | Required | Proposed |
| NFR-0053 | Alerting | Failures affecting knowledge integrity or availability raise an actionable alert. | Required | Proposed |

## Maintainability and evolvability

| ID | Quality attribute | Requirement | Target | Status |
| --- | --- | --- | --- | --- |
| NFR-0060 | Interface versioning | The public interface has an explicit version and compatibility policy. | Required | Proposed |
| NFR-0061 | Agent versioning | Agent definitions are versioned, and a rollout can be rolled back. | Required | Proposed |
| NFR-0062 | Ontology versioning | Ontology versions are immutable and a space records which version is active. | Required | Proposed |
| NFR-0063 | Deprecation policy | Removal of a public capability is preceded by a defined notice period. | TBD | Proposed |
| NFR-0064 | Protocol compatibility | The supported range of Model Context Protocol specification versions is declared. | Targets spec `2026-07-28`; compatibility and deprecation follow ADR-0018 | Proposed |

## Accessibility and usability

| ID | Quality attribute | Requirement | Target | Status |
| --- | --- | --- | --- | --- |
| NFR-0070 | Agent usability | A calling agent can determine, from the interface alone, which operations exist, what they require, and how they fail. | Required | Proposed |
| NFR-0071 | Error clarity | Every failure identifies the cause and whether retrying, changing input, or seeking authorization would help. | Required | Proposed |
| NFR-0072 | Human surfaces | Any human-facing surface meets the applicable accessibility standard. | TBD | Proposed |

## Cost

| ID | Quality attribute | Requirement | Target | Status |
| --- | --- | --- | --- | --- |
| NFR-0080 | Cost attribution | Operational cost can be attributed to a knowledge space. | Required | Proposed |
| NFR-0081 | Cost envelope | Cost per instance and per knowledge space stays within a defined budget. | TBD | Proposed |
| NFR-0082 | Unattended cost control | Unattended maintenance has a bounded cost per execution and per period. | TBD | Proposed |

### Cost drivers

This note is directional and is not a cost model. It exists so that architecture
options are compared on cost as well as capability.

| Driver | Why it costs | Where cost is controlled |
| --- | --- | --- |
| Contribution planning | An agent reads the ontology and existing knowledge, then reasons over both to plan changes | Scope of context supplied to the agent; caching the ontology; limiting retrieval breadth during planning |
| Retrieval | Embedding the question, executing hybrid search, and assembling evidence | Result size limits; avoiding synthesis by default |
| Optional synthesis | An additional generation pass over retrieved evidence | Kept opt-in rather than default |
| Projection maintenance | Embedding and indexing knowledge on every commit | Amortized at commit; incremental rather than full rebuild |
| Ontology migration | Potentially reprocessing every item in a space | Rare, scheduled, bounded by space size |
| Unattended maintenance | Recurring agent execution without a user waiting on it | Explicit authorization scope, frequency limits, and budget caps |

Returning structured evidence rather than a synthesized answer by default avoids
a second generation pass in Our IQ and avoids the calling agent summarizing a
summary. This is a cost and correctness decision, not only a contract choice.

## Release-blocking attributes

**Proposed.** These must have agreed targets before implementation is
considered complete.

- Authentication, authorization, and audit: NFR-0001 to NFR-0004.
- Untrusted content handling: NFR-0010.
- Atomicity, provenance, and groundedness: NFR-0020 to NFR-0022.
- Degraded operation and projection failure isolation: NFR-0041, NFR-0045.

For the first implementation increment, these attributes use deterministic
pass/fail gates. Retrieval, contribution, projection-visibility, and ontology
approval latency are instrumented at p50 and p95; numeric latency budgets are
set from the first representative baseline rather than guessed before evidence
exists.

## Open questions

- Which additional regulated or restricted classifications, if any, should a
  later decision authorize?
- What is the acceptable cost envelope per instance and per knowledge space?
- What is the maximum size of a single canonical knowledge item?

See the [assumptions and open questions register](assumptions-and-open-questions)
for the full list.
