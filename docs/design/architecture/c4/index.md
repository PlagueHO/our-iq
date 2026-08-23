# C4 views

**Status:** Proposed

The [C4 model](https://c4model.com/) describes architecture at multiple levels.
The structural views below derive from accepted ADRs while clearly separating
required constraints, selected services, open decisions, and deployed
behaviour.

- [System context](system-context)
- [Container](container)
- [Trust boundaries and data flow](trust-boundaries)
- [Pilot Azure deployment](azure-deployment)
- [Component](component) - proposed public orchestration, change-set,
  retrieval, and ontology-lifecycle responsibilities.
- [Agentic execution model](../agentic-execution-model) - detailed runtime,
  identity, authorization, and state diagrams.
- [Logical knowledge model](../logical-knowledge-model) - canonical knowledge,
  ontology-rule, and projection-flow diagrams.
- [Ontology storage contract](../ontology-storage-contract) - immutable
  ontology payload, version, digest, and activation records.
- [Initial implementation readiness](../implementation-readiness) - selected
  thin slice, release gates, and explicit deferrals.
