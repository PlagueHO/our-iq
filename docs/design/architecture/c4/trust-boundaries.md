---
title: C4 trust boundaries and data flow
status: Proposed
---

## C4 trust boundaries and data flow

## Purpose

Show the public, agent-runtime, private application, data, management, and
observability boundaries. This `Proposed` view describes desired controls; it
does not describe a deployed network or security configuration.

```mermaid
flowchart LR
  client[Client Agent]
  entra[Microsoft Entra<br/>Required identity context]
  foundry[Microsoft Foundry Agent Service<br/>Required runtime]
  operator[Steward or operator]

  subgraph public[Public boundary]
    mcp[Our IQ MCP Server<br/>Intent-level MCP tools only]
  end

  subgraph agent[Agent-runtime boundary]
    domain[Our IQ Domain Agents<br/>Shared, versioned definitions]
  end

  subgraph private[Private application boundary]
    tools[Our IQ Tool Services<br/>Service managed identities]
    management[Management APIs<br/>Privileged deterministic correction]
  end

  subgraph data[Private data boundary]
    canonical[(Authoritative canonical Markdown)]
    control[(Authoritative control metadata)]
    projection((Derived search and graph projections))
  end

  subgraph observability[Observability boundary]
    audit[Audit, logs, metrics, and traces<br/>Service selection open]
  end

  client -->|authenticated intent| mcp
  mcp -->|user and agent authorization context| entra
  mcp -->|private invocation| domain
  domain -->|runs on| foundry
  domain -->|private tools| tools
  operator -->|privileged management path| management
  management --> tools
  tools -->|managed-identity access| canonical
  tools -->|managed-identity access| control
  canonical -. rebuild .-> projection
  mcp -. trace .-> audit
  domain -. trace .-> audit
  tools -. trace .-> audit
  management -. audit .-> audit
```

## Boundary rules

| Boundary | Permitted flow | Constraint |
| --- | --- | --- |
| Public | Client Agent to public intent MCP tools | No public document or ontology CRUD. |
| Agent runtime | MCP Server to Domain Agents; Domain Agents to private Tool Services | Agent content is untrusted input and must not alter instructions or tool permissions. |
| Private application | Tool and management operations to data dependencies | Tool Services use their own managed identities. |
| Data | Canonical and control writes; projection rebuilds | Canonical writes are atomic, versioned change sets; projections are non-authoritative. |
| Management | Privileged correction or removal of an identified document | Subject to policy, authorization, versioning, and audit. |
| Observability | Traces, metrics, logs, and audit evidence from every major flow | Retention, service, and alert rules remain open. |

## Open questions

- How are prompt-injection controls enforced at each agent-processing boundary
  (Q-07)?
- What storage and retention controls apply to audit and observability data
  (Q-21)?
- How is unattended authority represented and validated across boundaries
  (Q-05)?
