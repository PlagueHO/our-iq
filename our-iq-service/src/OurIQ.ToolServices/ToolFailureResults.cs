using System.Diagnostics;
using System.Text.Json;
using Microsoft.Azure.Cosmos;
using ModelContextProtocol.Protocol;
using OurIQ.Contracts;
using OurIQ.Domain;

namespace OurIQ.ToolServices;

public static class ToolFailureResults
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    public static CallToolResult Create(
        string operation,
        JsonElement request,
        bool privateSurface,
        ApplicationError error,
        string? correlationId = null)
    {
        var spaceId = ReadString(request, "knowledgeSpaceId");
        correlationId ??= Activity.Current?.TraceId.ToHexString() ?? Guid.NewGuid().ToString("N");
        object response = privateSurface
            ? new
            {
                contractVersion = OntologyAgentToolService.ContractVersion,
                operation,
                knowledgeSpaceId = spaceId,
                outcome = "error",
                correlationId,
                pagination = "notApplicable",
                error
            }
            : new
            {
                outcome = "error",
                knowledgeSpaceId = spaceId,
                correlationId,
                pagination = "notApplicable",
                error
            };
        return new()
        {
            IsError = true,
            Content = [new TextContentBlock { Text = JsonSerializer.Serialize(response, JsonOptions) }]
        };
    }

    public static ApplicationError Classify(Exception exception) =>
        exception is CosmosException or HttpRequestException or MutationDependencyException or OperationCanceledException
            ? ApplicationError.DependencyUnavailable
            : ApplicationError.Internal;

    public static JsonElement ReadRequest(IDictionary<string, JsonElement>? arguments) =>
        arguments is not null && arguments.TryGetValue("request", out var request)
            ? request
            : default;

    private static string? ReadString(JsonElement value, string name) =>
        value.ValueKind == JsonValueKind.Object
        && value.TryGetProperty(name, out var property)
        && property.ValueKind == JsonValueKind.String
        && !string.IsNullOrWhiteSpace(property.GetString())
            ? property.GetString()
            : null;
}
