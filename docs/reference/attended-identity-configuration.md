---
title: Attended identity configuration
status: Accepted
---

## Attended identity configuration

## Scope

The public MCP Server and private Tool Services hosts authenticate attended
requests with Microsoft Entra access tokens. This reference describes the
application configuration and claim mapping implemented by issue #37.

Entra app registrations, federated identity credentials, role assignments, and
other deployment resources are not provisioned by this implementation. They
remain infrastructure work.

## Identity mapping

| Context | Validated claims | Canonical identity |
| --- | --- | --- |
| Initiating user | Delegated `scp`, plus `tid` and `oid` | `{tid}:{oid}` |
| Acting agent | `azp`, or `appid` for a v1 token | Entra application client ID |

The access token is authoritative. Identity values in public and private
request envelopes must match the validated claims. Missing, duplicate,
malformed, or conflicting claims fail closed.

An `scp` claim is required so that an application-only token cannot be treated
as an attended user. Application roles do not satisfy this requirement.

Public MCP calls require the initiating user identity. Private Tool Services
calls require both the initiating user and an acting-agent client ID listed in
configuration. Health endpoints remain outside the authenticated MCP surfaces.
Management authorization remains a separate policy.

## MCP Server settings

| Configuration key | Purpose |
| --- | --- |
| `Entra:Instance` | Microsoft identity platform authority base URL. |
| `Entra:TenantId` | Tenant accepted by the MCP Server. |
| `Entra:ClientId` | MCP Server application client ID and token audience. |
| `ToolServicesDelegation:Scope` | Delegated Tool Services scope requested through the on-behalf-of flow. |
| `Entra:ClientCredentials` | Confidential-client proof used only to acquire downstream tokens. |

For an Azure-hosted MCP Server, use Microsoft Identity Web certificateless
credentials backed by managed identity and a federated identity credential.
For local development, keep any development-only client secret in .NET user
secrets or environment variables. Never add it to `appsettings*.json`.

Example environment-variable names:

```text
Entra__TenantId
Entra__ClientId
ToolServicesDelegation__Scope
Entra__ClientCredentials__0__SourceType
Entra__ClientCredentials__0__ManagedIdentityClientId
```

## Tool Services settings

| Configuration key | Purpose |
| --- | --- |
| `Entra:Instance` | Microsoft identity platform authority base URL. |
| `Entra:TenantId` | Tenant accepted by Tool Services. |
| `Entra:ClientId` | Tool Services application client ID and token audience. |
| `PrivateIdentity:AuthorizedAgentClientIds` | Allowlist of acting-agent application client IDs. |
| `AzureIdentity:ManagedIdentityClientId` | Optional user-assigned managed identity used for Azure data dependencies. |

`PrivateIdentity:AuthorizedAgentClientIds` is an array. Environment variables
use numeric indexes, for example:

```text
PrivateIdentity__AuthorizedAgentClientIds__0
```

Tool Services use `DefaultAzureCredential` in the local development
environment. Hosted environments use `ManagedIdentityCredential` directly,
selecting the configured user-assigned identity or the system-assigned identity.
User and agent tokens are authorization and audit context; they are never Azure
data-plane credentials.

## Deferred deployment work

Infrastructure work must create the two API audiences and delegated scope,
configure the allowed client applications, establish the MCP Server's
certificateless credential, assign the Tool Services managed identity only the
required data-plane roles, and supply the settings above. No shared key is the
default data-access path.

## References

- [ADR-0007: Agent identity and execution context](../design/decisions/adr-0007-agent-identity-and-execution-context)
- [ADR-0008: Service managed identities](../design/decisions/adr-0008-service-managed-identities)
- [ADR-0025: .NET technology and package baseline](../design/decisions/adr-0025-dotnet-technology-and-package-baseline)
- [Microsoft Identity Web protected web API configuration](https://learn.microsoft.com/entra/identity-platform/scenario-web-api-call-api-app-configuration)
- [Microsoft Identity Web credentials](https://learn.microsoft.com/entra/msidweb/authentication/credentials-overview)
- [Azure Identity credential chains](https://learn.microsoft.com/dotnet/azure/sdk/authentication/credential-chains)
