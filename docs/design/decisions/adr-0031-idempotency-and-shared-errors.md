---
title: Idempotency and shared errors
status: Proposed
---

## ADR-0031: Idempotency and shared errors

## Status

Proposed. This record captures the approved implementation scope for issue #36.
Architecture review of this ADR is pending.

## Date and ownership

- Date: 2026-11-16
- Authors: Implementation agent
- Reviewers: Pending architecture review

## Context

An ontology mutation can commit before its caller receives a response.
Repeating individual writes can then create conflicts or duplicate work.
Activation also changes the lifecycle state and makes the original execution
snapshot stale. A retry must recover the original outcome without bypassing
current access checks.

Both MCP hosts need the same application error fields. This does not change
HTTP authentication, MCP protocol errors, or resource-read errors.

The existing Cosmos DB control container partitions records by knowledge space.
It can commit a mutation and its response in one transactional batch.

## Decision

Use durable replay for `stage_ontology_version`,
`validate_ontology_compatibility`, `record_approval`, and
`activate_ontology_version`. Public workflows and other private tools remain
unsupported.

1. Require a caller-generated key for each supported mutation. Scope it to the
   operation, initiating user, logical acting agent, and knowledge space.
   Store a hash of the scope, not the raw key. Keep keys case-sensitive.
2. Compare canonical JSON fingerprints. Ignore object property order, JSON
   whitespace, the key itself, and `executionContext.correlationId`. Normalize
   the initiating user identifier. Keep arguments, array order, explicit nulls,
   agent definition version, and the pinned snapshot significant. Reject
   duplicate JSON properties.
3. Check current user and agent access before reading a saved outcome.
   Return the original response JSON and error status on a match.
   Return `idempotency_key_conflict` for different input under the same key.
   Apply lifecycle and snapshot freshness checks only to a new execution.
   A new snapshot needs a new key.
4. Create domain records and the immutable replay outcome in one space-partition
   transaction. Fence each mutation with the control record's ETag.
   Stage the version and proposal together. Commit activation evidence and
   the active control pointer together. Never split one mutation across batches.
5. Save deterministic business failures only after a valid, authorized replay
   identity exists. Do not save authentication, authorization, malformed-envelope,
   dependency, cancellation, or unexpected internal failures.
   On a race, read a committed winner before recording a failure.
6. Bound internal ETag retries. A timeout does not prove rollback.
   Recover the committed outcome when possible; otherwise return
   `dependency_unavailable` and require retry with the original key.
   Handle batch sibling statuses, including `424`, and reject oversized batches.
7. Retain replay records while the space exists. Set `ttl = -1` so a container
   default cannot expire them. Classify saved responses as content-bearing
   control records. Apply
   [ADR-0030](adr-0030-pilot-data-governance-and-retention-controls) for the space's
   classification, geography, legal holds, and deletion deadlines.
   This issue adds no purge job.
   Production admission still requires purge evidence.
8. Share the application error object across hosts: `code`, `category`,
   `message`, and `remediation`. Keep correlation IDs on the response envelope.
   Use `operation_not_implemented` / `state`, `dependency_unavailable` /
   `availability`, and `internal_error` / `internal` for the new common failures.
   Errors may use a null space ID when no valid space ID is available.
   Log only safe type, stage, and correlation metadata for unexpected failures.

The original response body keeps its original correlation ID on replay.
A retry's HTTP correlation header can differ.

## Alternatives considered

| Alternative | Rejection or selection rationale |
| --- | --- |
| Save the mutation and replay record separately | Rejected: a process failure can separate the mutation from its response. |
| Keep replay only in process memory | Rejected: restart loses outcomes and concurrent hosts can repeat work. |
| Expire keys automatically | Rejected for this scope: a delayed retry could repeat a committed mutation. |
| Recheck lifecycle and freshness before replay | Rejected: successful activation makes its own original snapshot stale. |
| Atomic Cosmos DB batch with an access check before replay | Selected: uses the existing partition and preserves the original outcome. |

## Consequences

### Positive

- POS-001: Retries and process restarts recover one committed outcome.
- POS-002: Current authorization still controls access to saved responses.
- POS-003: Both hosts return machine-readable application errors.

### Negative

- NEG-001: Saved response content increases control-container storage.
- NEG-002: Each new mutation replaces the control record as an ETag fence.
  Concurrent mutations can need a bounded retry.
- NEG-003: Purge must include replay records and recovery copies.
- NEG-004: One mutation must fit the service's transaction limits.

## Implementation notes

- IMP-001: Private ontology handlers use `IOntologyVersionRepository` atomic
  commit methods. Public tools still return `operation_not_implemented`.
- IMP-002: Replay records have record type `idempotencyOutcome`, record version
  `1.0`, and the reserved ID prefix `ouriq-replay-`.
- IMP-003: Contract bundles remain pre-GA version `1.0`. Manifest digests identify
  their current schema bytes.
- IMP-004: Local tests use the Cosmos emulator. They do not prove production
  authentication, deployment, purge, or recovery-copy deletion.

## References

- REF-001: [Issue #36](https://github.com/PlagueHO/our-iq/issues/36).
- REF-002: [Immutable execution-context snapshots](adr-0016-immutable-execution-context-snapshots).
- REF-003: [Initial Azure data plane](adr-0022-initial-azure-data-plane).
- REF-004: [Contract schema publication](adr-0028-contract-schema-publication).
- REF-005: [Pilot data governance and retention controls](adr-0030-pilot-data-governance-and-retention-controls).
- REF-006: [Cosmos DB transactional batches](https://learn.microsoft.com/azure/cosmos-db/transactional-batch).
- REF-007: [Linux emulator support and limits](https://learn.microsoft.com/azure/cosmos-db/emulator-linux).

## Review record

The issue planning decisions established the four-operation scope, atomic
commit, canonical comparison, replay ordering, and no-expiry retention choice.
No independent architecture review or production approval is recorded here.
