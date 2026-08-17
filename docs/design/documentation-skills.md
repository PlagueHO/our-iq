---
title: Documentation skills
status: Proposed
---

## Documentation skills

This repository may use selected skills from
[`github/awesome-copilot`](https://github.com/github/awesome-copilot/tree/main/skills)
to make documentation workflows repeatable. Skills are optional reviewed
dependencies, not automatic installations or architecture decisions.

| Skill | Use in Our IQ | Lifecycle |
| --- | --- | --- |
| [`documentation-writer`](https://github.com/github/awesome-copilot/tree/main/skills/documentation-writer) | Apply Diátaxis distinctions and clarify audience, goal, and scope before drafting. | Use for reader-facing docs. |
| [`create-architectural-decision-record`](https://github.com/github/awesome-copilot/tree/main/skills/create-architectural-decision-record) | Draft structured ADRs with consequences and alternatives. | Reconcile its default path with `docs/design/decisions/` before adoption. |
| [`architecture-blueprint-generator`](https://github.com/github/awesome-copilot/tree/main/skills/architecture-blueprint-generator) | Analyze an implemented codebase and produce evidence-based architecture and C4 documentation. | Use after implementation exists. |
| [`update-markdown-file-index`](https://github.com/github/awesome-copilot/tree/main/skills/update-markdown-file-index) | Maintain documentation indexes as the tree grows. | Use when navigation or registers change. |
| [`github-actions-hardening`](https://github.com/github/awesome-copilot/tree/main/skills/github-actions-hardening) | Review workflow permissions, triggers, injection risks, and action supply chain. | Use for every workflow addition or security review. |
| [`github-actions-efficiency`](https://github.com/github/awesome-copilot/tree/main/skills/github-actions-efficiency) | Review caching, path filters, concurrency, and CI cost. | Use after workflow history exists. |
| [`suggest-awesome-github-copilot-skills`](https://github.com/github/awesome-copilot/tree/main/skills/suggest-awesome-github-copilot-skills) | Re-evaluate skill relevance as repository needs evolve. | Use periodically; do not install automatically. |

## Arc42 gap

No arc42-specific skill was found in the current Awesome-Copilot skills
directory. The arc42 templates remain local to this repository. The
[MSiccDev/arc42-toolkit](https://github.com/MSiccDev/arc42-toolkit) is a
separate external option that may be evaluated later; it is not represented as
an Awesome-Copilot skill.
