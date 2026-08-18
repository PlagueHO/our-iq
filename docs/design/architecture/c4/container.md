---
title: C4 container view
status: Proposed
---

## C4 container view

## Purpose

Show the major deployable or independently meaningful parts inside the system
boundary after they are decided.

This view is `Proposed`. The named Azure services are status-labelled
candidates, except for required Microsoft Foundry Agent Service and Microsoft
Entra constraints and selected Cosmos DB control metadata. It does not
describe a deployment.

```mermaid
flowchart LR
  client[Client Agent]
  entra[Microsoft Entra<br/>Required]
  foundry[Microsoft Foundry Agent Service<br/>Required]

  subgraph public[Public intent boundary]
    mcp[Our IQ MCP Server<br/>Candidate Azure Container Apps compute]
  end

  subgraph private[Private application boundary]
    tools[Our IQ Tool Services<br/>Candidate Azure Container Apps compute]
    management[Management APIs and maintenance commands<br/>Candidate Azure Container Apps compute]
  end

  subgraph agent[Agent-runtime boundary]
    domain[Our IQ Domain Agents<br/>Shared and versioned]
  end

  subgraph data[Private data boundary]
    canonical[(Canonical knowledge<br/>Markdown and front matter<br/>Candidate Azure Blob Storage)]
    control[(Control metadata<br/>Selected Cosmos DB)]
    projection((Derived retrieval projection<br/>Candidate Azure AI Search))
    graph((Derived graph projection<br/>Candidate service open))
  end

  client -->|public intent MCP tools| mcp
  mcp --> entra
  mcp -->|private invocation| domain
  domain --> foundry
  domain -->|private domain tools| tools
  management -->|private management APIs| tools
  tools --> canonical
  tools --> control
  canonical -. rebuild .-> projection
  canonical -. rebuild .-> graph
```

**Notation:** rounded database shapes represent authoritative canonical or
control data. Double-circle shapes represent derived, rebuildable projections.
Solid arrows are request or private service calls; dotted arrows are rebuild
flows. A projection may lag a canonical commit and is never authoritative.

| Container | Responsibility | Technology | Status |
| --- | --- | --- | --- |
| Our IQ MCP Server | Public intent-level MCP operation handling. | Azure Container Apps | Candidate compute |
| Our IQ Domain Agents | Agent-mediated contribution, retrieval, and ontology reasoning. | Microsoft Foundry Agent Service | Required |
| Our IQ Tool Services | Deterministic private domain operations and data coordination. | Azure Container Apps | Candidate compute |
| Management APIs and maintenance commands | Privileged operator and steward capability. | Azure Container Apps | Candidate compute |
| Canonical knowledge store | Stores authoritative Markdown and front matter. | Azure Blob Storage | Candidate |
| Control metadata store | Stores governance and per-space change-set coordination metadata. | Cosmos DB | Selected |
| Retrieval projection | Supports hybrid retrieval from rebuildable derived data. | Azure AI Search | Candidate |
| Graph projection | Supports relationship traversal if justified. | Open | Candidate service undecided |

## Open questions

- Which Tool Services become separate deployable units and what private contracts
  do they expose?
- Is a dedicated graph projection justified, and if so, which service supports
  it (D-08 and Q-24)?
- Which candidate services require private endpoints and which network
  constraints govern them (Q-21 and Q-24)?
