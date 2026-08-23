---
title: ADR-0028 - Contract schema publication
status: Accepted
---

## ADR-0028: Contract schema publication

## Status

Accepted

## Date and ownership

- Date: 2026-08-23
- Authors: @PlagueHO
- Reviewers: @PlagueHO

## Context

The public Our IQ MCP Server and private Domain Agent MCP tools have separate
trust boundaries and deterministic compatibility requirements. The API contract
baseline identifies JSON Schema-compatible request and response shapes, but it
does not define where those schemas are authoritative, how a runtime resolves a
contract version, or how compatibility tests consume the same artifacts.

After the formal 1.0 GA release, ADR-0018 requires additive contract changes to
remain backward compatible. A breaking change requires a new major contract
version, and the immediately preceding minor version must remain supported
through its published deprecation window. Before that release, the project may
make breaking changes to any contract without preserving a prior minor version.
The publication design must make the post-GA policy testable without exposing
private schemas to public callers.

## Decision

Contract schemas are authoritative repository assets under a top-level
`contracts/` tree. The tree has separate `public/` and `private/` surfaces.
Each surface contains versioned schema bundles and a manifest that identifies
the contract version, schema names, content digests, and supported
deprecation range.

Contract versions use `major.minor` form and are scoped to their contract
surface. Public and private surfaces may advance independently. Before 1.0 GA,
any contract may change incompatibly without the post-GA major-version and
deprecation support guarantee. Changes still update their repository manifest
and declared version so development consumers and compatibility tests resolve a
known artifact.

After 1.0 GA, a minor version may add optional fields, operations, or schemas
without invalidating consumers. Required-field, meaning, removal, or other
incompatible changes require a new major version. A package may carry more than
one version while the immediately preceding minor version is within its
deprecation window.

The build publishes immutable runtime assets from the repository sources:

- The public MCP Server packages only the public schema bundle and may expose
  those schemas as versioned read-only MCP resources.
- Private Tool Services package only the private schema bundle. Private schemas
  are resolved by authorized Tool Services and are not exposed through the
  public MCP resource surface.
- Compatibility tests consume the repository manifests and the corresponding
  packaged assets. They do not retrieve schemas from a mutable external
  registry.

Resolution is explicit and deterministic. A consumer supplies the surface,
contract version, and schema name. The resolver returns the manifest entry and
schema only when that exact version is packaged and declared supported. It
does not silently fall back to the latest version or retarget a request to
another surface. Unsupported versions, missing schemas, and digest mismatches
fail closed.

## Alternatives considered

| Alternative | Rejection or selection rationale |
| --- | --- |
| Repository-owned schemas packaged as immutable runtime assets | Selected because the source, compatibility tests, and deployed consumers share reviewable versioned artifacts without introducing a new hosted dependency. |
| A separately versioned contract package | Rejected for the initial implementation because it adds release and dependency coordination before the contract surfaces have demonstrated that need. |
| An external schema registry or hosted schema service | Rejected because it adds a mutable runtime dependency and creates an additional authorization boundary, especially for private schemas. |
| One combined public/private schema bundle | Rejected because packaging mistakes could make private deterministic contracts discoverable to public callers. |

## Consequences

### Positive

- POS-001: Consumers can resolve a schema by an exact contract version.
- POS-002: Compatibility tests validate the same versioned artifacts that
  runtimes package.
- POS-003: Separate bundles make the public/private trust boundary reviewable
  and testable.
- POS-004: Version retention and deprecation can be enforced from manifests
  rather than inferred from deployment state.

### Negative

- NEG-001: Each public or private schema change requires manifest, compatibility
  test, and package review.
- NEG-002: Runtime packages must retain the immediately preceding supported
  contract version during its deprecation window.
- NEG-003: Public and private surface versions must be tracked independently.

## Implementation notes

- IMP-001: The proposed repository layout, manifest shape, resolver behavior,
  and compatibility-test responsibilities are described in the
  [contract schema publication reference](../architecture/contract-schema-publication).
- IMP-002: This ADR defines the publication and resolution boundary; it does
  not add executable schema files or a runtime resolver.
- IMP-003: Public and private packaging allowlists must be independently
  validated so a private schema cannot enter a public runtime asset.
- IMP-004: A schema digest mismatch is an integrity failure and must not produce
  a schema-shaped success response.
- IMP-005: The MCP specification target remains `2026-07-28`; this ADR does not
  change the public/private operation or compatibility policy in ADR-0018.

## References

- REF-001: [ADR-0018](adr-0018-mcp-contract-boundaries-and-compatibility).
- REF-002: [API contract baseline](../architecture/api-contract-baseline).
- REF-003: [Assumptions and open questions](../product/assumptions-and-open-questions).

## Review record

- 2026-08-23: Accepted by @PlagueHO for V1-D04.
