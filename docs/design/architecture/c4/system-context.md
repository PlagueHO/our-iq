---
title: C4 system context
status: Proposed
---

## C4 system context

## Purpose

Show Our IQ, its users, and external systems without assuming internal
technology.

This view is `Proposed`. It is based on accepted decisions and confirmed
constraints, not a deployed system.

```mermaid
flowchart LR
  user[Knowledge contributor or consumer]
  client[Client Agent]
  entra[Microsoft Entra<br/>Required identity service]
  foundry[Microsoft Foundry Agent Service<br/>Required agent runtime]
  source[Source material supplied by a team<br/>Optional input]

  subgraph ouriq[Our IQ]
    mcp[Our IQ MCP Server<br/>Public intent interface]
    domain[Our IQ Domain Agents]
    tools[Our IQ Tool Services]
    data[Our IQ Data Services]
  end

  user --> client
  client -->|intent-level MCP operations| mcp
  mcp -->|authenticate and authorize| entra
  mcp -->|invoke| domain
  domain -->|runs on| foundry
  domain -->|private tool invocation| tools
  tools -->|private data access| data
  source -->|grounding or contribution material| client
```

The Client Agent is the public consumer. Microsoft Entra and Microsoft Foundry
Agent Service are confirmed external systems. Source material is an optional
input, not an integration with a named external system.

## Open questions

- What management clients and operational integrations require explicit
  context-level relationships?
- Which external source integrations, if any, belong in scope after the
  deferred bulk-import decision is revisited?
