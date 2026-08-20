---
title: arc42 11 - Risks and technical debt
status: Proposed
---

## 11. Risks and technical debt

## Purpose

Make material architecture risks, uncertainties, and deferred work visible.

## Current risks

| Risk | Impact | Likelihood | Mitigation | Owner | Status |
| --- | --- | --- | --- | --- | --- |
| Prompt injection through knowledge content | Unauthorized tool use or unsupported canonical changes | High | ADR-0020 controls and adversarial evaluation gates | Project owner | Mitigated by design; implementation evidence required |
| Projection and canonical divergence | Stale or incorrect evidence | Medium | Resolve every result to an active canonical revision and report freshness | Project owner | Mitigated by design; implementation evidence required |
| New MCP or prerelease SDK behaviour changes | Contract or build churn | Medium | Pin versions, run compatibility tests, and use ADR-0018 deprecation rules | Project owner | Open |
| Cosmos DB hot partition or item-size constraints | Failed publication or ontology storage at larger scale | Low at pilot scale | Pilot limits, per-space partitioning, and measured item sizes | Project owner | Accepted for pilot |
| Prompt-based Foundry agents cannot satisfy required orchestration | Rework to Hosted Agents | Medium | Evaluate the three prompt-based agents against thin-slice gates before introducing hosted code | Project owner | Open |
| Production compliance controls are undefined | Unsafe use with sensitive data | High if scope expands | Enforce the non-sensitive pilot boundary until Q-21 is resolved | Project owner | Scope constrained |
| Long-running orchestration is unselected | Migration, rebuild, bootstrap, and deletion cannot ship | High for full release | Explicitly defer those flows beyond the synchronous thin slice and resolve Q-24 before implementing them | Project owner | Deferred blocker |

## Evidence needed

- Adversarial evaluation results for instruction and tool-manifest isolation.
- Atomic publication failure-injection results.
- Canonical citation and projection-freshness integration results.
- p50 and p95 latency and Azure cost observations for the selected thin slice.
- Foundry prompt-based agent evaluation results and any evidence requiring
  Hosted Agents.
