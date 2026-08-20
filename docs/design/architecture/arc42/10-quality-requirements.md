---
title: arc42 10 - Quality requirements
status: Proposed
---

## 10. Quality requirements

## Purpose

Link measurable quality goals to scenarios and architecture decisions.

## Current status

Correctness and security gates for the first increment are approved in the
[initial implementation readiness baseline](../implementation-readiness).
Numeric performance budgets remain evidence-driven. The thin slice records p50
and p95 latency for ontology approval, contribution planning and commit,
projection visibility, and evidence query before budgets are set.

## Quality scenarios

| Scenario | Stimulus | Required response |
| --- | --- | --- |
| Unauthorized operation | A user or agent requests a capability outside its intersection. | Deny before data access or mutation and retain auditable diagnostics. |
| Instruction injection | Knowledge content attempts to alter instructions, tools, identity, or policy. | Fail closed without capability or state change. |
| Partial publication | A failure occurs after revision staging but before pointer publication. | Readers continue to observe the previous complete change set. |
| Stale plan | Ontology, lifecycle, policy, or canonical head changes before commit. | Return `replan_required`; do not retarget the plan. |
| Projection lag | A query runs before the projection reaches the canonical head. | Report freshness and return only canonically verified evidence. |
| Ambiguous contribution | More than one materially different grounded interpretation exists. | Return `clarification_required`; create no plan or mutation. |

Measurements and traces use the execution and correlation identifiers defined by
the contract baseline. Production SLOs, retention, and alert thresholds remain
open beyond the pilot.
