using Azure.Core;
using Azure.Identity;

namespace OurIQ.ToolServices;

public sealed class AzureIdentityOptions
{
    public const string SectionName = "AzureIdentity";

    public string? ManagedIdentityClientId { get; init; }
}

public static class AzureCredentialFactory
{
    public static TokenCredential Create(
        IHostEnvironment environment,
        AzureIdentityOptions options)
    {
        if (environment.IsDevelopment())
        {
            return new DefaultAzureCredential();
        }

        return string.IsNullOrWhiteSpace(options.ManagedIdentityClientId)
            ? new ManagedIdentityCredential(ManagedIdentityId.SystemAssigned)
            : new ManagedIdentityCredential(
                ManagedIdentityId.FromUserAssignedClientId(options.ManagedIdentityClientId));
    }
}
