# Our IQ local inner loop

The .NET Aspire AppHost starts the current local development topology:

- `mcp-server`: public MCP Server with external HTTP ingress.
- `tool-services`: private Tool Services with internal service discovery only.
- `cosmos`: local Cosmos DB emulator and the `ouriq` database.
- `storage` and `blobs`: Azurite-backed Blob Storage with persisted local data.
- `search`: Azure AI Search provisioned through Aspire local provisioning.

Aspire is the local orchestration and service-discovery tool. Azure
infrastructure remains defined and deployed through Bicep and the Azure
Developer CLI.

## Prerequisites

- .NET SDK `10.0.400` or a compatible SDK allowed by
  `our-iq-service/global.json`.
- Docker Desktop running for the Cosmos DB emulator and Azurite.
- Aspire CLI installed and available as `aspire`.
- Azure CLI or Azure Developer CLI signed in with permission to provision the
  development Azure AI Search resource.

The Search resource is required because Azure AI Search has no local emulator.
Aspire local provisioning uses the signed-in developer identity; do not add
connection keys or other credentials to the repository.

Configure the subscription and region in Aspire user secrets before the first
run:

```powershell
aspire secret set "Azure:SubscriptionId" "<subscription-id>"
aspire secret set "Azure:Location" "<azure-region>"
```

Optionally set `Azure:ResourceGroup` to reuse an existing development resource
group. Without it, Aspire creates a resource group for the provisioned Search
resource. Local provisioning can incur Azure charges.

## Start the system

From the repository root, run:

```powershell
aspire start --isolated --apphost .\our-iq-service\src\OurIQ.AppHost\OurIQ.AppHost.csproj
```

Open the Aspire dashboard URL printed by the command. The dashboard should
show `mcp-server`, `tool-services`, `cosmos`, `storage`, `blobs`, and `search`.
The MCP Server is the only application resource with an external endpoint.

Use the CLI to inspect the application and wait for resources to become
healthy:

```powershell
aspire describe --apphost .\our-iq-service\src\OurIQ.AppHost\OurIQ.AppHost.csproj
aspire wait mcp-server --apphost .\our-iq-service\src\OurIQ.AppHost\OurIQ.AppHost.csproj
```

The MCP health endpoint is `/health`. Tool Services exposes `/health` and
`/ready` through its internal endpoint; its MCP and management surfaces are not
publicly reachable.

## Local data and cleanup

Azurite data is stored in the local Aspire-managed volume. The Cosmos DB
emulator data is also local and is intended only for synthetic development
data. Stop the AppHost with `aspire stop`; remove local emulator state through
the Aspire or Docker tooling when a clean data set is required.

This local topology does not claim to be the Azure pilot or production
deployment. It does not replace the Bicep/azd deployment contract or select
production networking, identity, retention, or availability settings.
