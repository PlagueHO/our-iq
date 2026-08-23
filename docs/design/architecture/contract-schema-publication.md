---
title: Contract schema publication
status: Proposed
---

## Contract schema publication

## Purpose

This document describes the proposed repository and runtime publication model
for the public and private JSON Schemas defined by the
[API contract baseline](api-contract-baseline). It is an implementation
reference for V1-D04, not evidence that schema bundles, runtime resources, or
resolvers have been implemented.

The governing decision is
[ADR-0028](../decisions/adr-0028-contract-schema-publication). The public and
private trust boundary and compatibility policy remain governed by
[ADR-0018](../decisions/adr-0018-mcp-contract-boundaries-and-compatibility).

## Selected publication model

Repository-owned schemas are the authoritative source. A future implementation
packages immutable runtime assets from that source and keeps public and private
surfaces separate:

| Surface | Repository source | Runtime publication | Allowed consumers |
| --- | --- | --- | --- |
| Public | `contracts/public/` | Versioned read-only MCP resources from the public MCP Server | Client Agents and other authorized public consumers |
| Private | `contracts/private/` | Package-local schema assets in private Tool Services | Authorized Domain Agents through private Tool Services |

The runtime package is a release artifact, not a second source of truth. A
deployment must not fetch a mutable "latest" schema or reconstruct a schema
from a service's current code. The public runtime has no private bundle, and
the private runtime does not publish private schemas through the public MCP
resource surface.

## Proposed repository layout

The following layout is the planned contract source layout. It is intentionally
documented before executable schemas are added:

```text
contracts/
  public/
    manifest.json
    v1.0/
      <schema-name>.schema.json
  private/
    manifest.json
    v1.0/
      <schema-name>.schema.json
```

Each manifest is scoped to one surface and lists the contract versions and
schema names available in that surface. A manifest entry records:

| Entry | Requirement |
| --- | --- |
| `surface` | `public` or `private`; it must match the bundle location. |
| `contractVersion` | Exact `major.minor` version used for resolution. |
| `schemaName` | Stable name unique within the surface and version. |
| `assetPath` | Repository-relative source path for the schema. |
| `sha256` | Digest of the canonical schema bytes packaged at runtime. |
| `status` | Supported or deprecated state for the published version. |
| `deprecatedAfter` | Reviewable end of the deprecation window when applicable. |

Schema documents use JSON Schema 2020-12, consistent with the existing
ontology and contract direction. The schema `$id` and manifest identity must
refer to the same surface, version, and schema name. Digests are checked after
packaging so a changed or substituted asset cannot be served as the declared
schema.

## Version and compatibility policy

Contract versions are scoped to a surface and use `major.minor` form. Package
build numbers and deployment revisions do not change the contract version.

Until Our IQ publishes `1.0` and declares a formal GA release, any contract may
change incompatibly without a backward-compatibility or deprecation guarantee.
Development consumers and compatibility tests still resolve a known declared
version; they must not assume that pre-GA versions are stable.

After 1.0 GA, an additive change may be published as a new minor version when
existing consumers remain valid. A breaking change requires a new major
version. A runtime that supports a newer minor version retains the immediately
preceding minor version until its published deprecation window ends.
Compatibility tests must verify the declared support range rather than assume
that the newest bundle is compatible.

The resolver uses an exact `(surface, contractVersion, schemaName)` key. It
never falls back to the newest version, crosses from public to private, or
silently upgrades a requested contract. A request for an unsupported version
returns `contract_version_unsupported`; a missing schema returns
`contract_schema_not_found`; a digest mismatch returns
`contract_schema_integrity_failure`. These failures do not return a partial
schema or a success-shaped response.

## Runtime consumption

Public consumers resolve a public schema through a versioned, read-only MCP
resource. The resource identity includes the exact contract version and schema
name. Public resource resolution is limited to entries in the public manifest.
It does not reveal private manifest entries, private asset paths, or private
schema content.

Private Domain Agents do not resolve schemas through the public server. Private
Tool Services resolve the exact private manifest entry from their package
before validating a deterministic tool request or response. The agent's
versioned tool manifest and execution context remain the authorization source;
schema resolution does not grant a capability.

The resolver and resource layer are planned runtime responsibilities. This
document does not claim that either exists today or select an HTTP management
API transport.

## Compatibility-test responsibilities

Compatibility tests consume the repository manifest and the generated runtime
bundle for each surface. They should verify:

1. Every manifest entry points to a valid JSON Schema 2020-12 document.
2. The manifest digest matches the packaged schema bytes.
3. A consumer can resolve every declared schema by its exact surface, version,
   and name.
4. Declared additive minor versions accept the compatibility fixtures for the
   supported preceding version.
5. Breaking changes are not published under an existing major version.
6. The public bundle contains no private manifest entry, schema path, or schema
   content.
7. Unsupported versions, missing names, and integrity failures fail closed.

Tests should run against files and package outputs in the repository build
without requiring a deployed schema service. Runtime resource tests can be
added when the public MCP Server and private Tool Services exist.

## Publication and review flow

The intended flow is:

1. Author or update a schema in the surface-specific repository tree.
2. Update that surface's manifest with the contract version and digest.
3. Run structural, compatibility, and trust-boundary checks.
4. Package the validated surface bundle as an immutable runtime asset.
5. Publish only the bundle allowed for the target runtime.
6. Retain the immediately preceding supported minor version until its
   deprecation window has ended.

The repository review must treat a public/private path change as a trust-boundary
change. A schema is not considered published merely because it exists in the
repository; it is published for consumers only when its manifest entry and
immutable runtime bundle pass the compatibility checks.

## Open implementation details

The following details remain implementation work rather than decisions in this
document:

- The concrete build task that validates and packages each bundle.
- The concrete resolver type and dependency-injection registration.
- The exact MCP resource URI syntax and public resource authorization checks.
- The test framework and fixture organization used by the .NET services.
- The release automation that retains or removes deprecated bundles.
