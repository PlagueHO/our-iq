using System.Diagnostics;
using Microsoft.AspNetCore.Http;

namespace OurIQ.Observability;

public sealed record TelemetryContext(
    string ExecutionId,
    string CorrelationId,
    string? KnowledgeSpaceId,
    string? OperationId,
    string TraceId)
{
    public static TelemetryContext Create(Activity activity, IHeaderDictionary headers)
    {
        var values = headers.ToDictionary(
            header => header.Key,
            header => header.Value.Count == 1 ? header.Value[0] : null,
            StringComparer.OrdinalIgnoreCase);
        return Create(activity, values);
    }

    public static TelemetryContext Create(
        Activity activity,
        IReadOnlyDictionary<string, string?> headers)
    {
        return new TelemetryContext(
            ReadOrGenerate(headers, TelemetryConstants.ExecutionIdHeader),
            ReadOrGenerate(headers, TelemetryConstants.CorrelationIdHeader),
            ReadOptional(headers, TelemetryConstants.KnowledgeSpaceIdHeader),
            ReadOptional(headers, TelemetryConstants.OperationIdHeader),
            activity.TraceId.ToHexString());
    }

    public void Enrich(Activity activity)
    {
        activity.SetTag(TelemetryConstants.ExecutionIdTag, ExecutionId);
        activity.SetTag(TelemetryConstants.CorrelationIdTag, CorrelationId);
        activity.SetTag(TelemetryConstants.KnowledgeSpaceIdTag, KnowledgeSpaceId);
        activity.SetTag(TelemetryConstants.OperationIdTag, OperationId);
    }

    public void ApplyTo(HttpResponse response)
    {
        response.Headers[TelemetryConstants.ExecutionIdHeader] = ExecutionId;
        response.Headers[TelemetryConstants.CorrelationIdHeader] = CorrelationId;

        if (KnowledgeSpaceId is not null)
        {
            response.Headers[TelemetryConstants.KnowledgeSpaceIdHeader] = KnowledgeSpaceId;
        }

        if (OperationId is not null)
        {
            response.Headers[TelemetryConstants.OperationIdHeader] = OperationId;
        }
    }

    public void ApplyTo(HttpRequestMessage request)
    {
        request.Headers.TryAddWithoutValidation(TelemetryConstants.ExecutionIdHeader, ExecutionId);
        request.Headers.TryAddWithoutValidation(TelemetryConstants.CorrelationIdHeader, CorrelationId);

        if (KnowledgeSpaceId is not null)
        {
            request.Headers.TryAddWithoutValidation(TelemetryConstants.KnowledgeSpaceIdHeader, KnowledgeSpaceId);
        }

        if (OperationId is not null)
        {
            request.Headers.TryAddWithoutValidation(TelemetryConstants.OperationIdHeader, OperationId);
        }
    }

    public IReadOnlyDictionary<string, object?> ToLogState() =>
        new Dictionary<string, object?>
        {
            [TelemetryConstants.ExecutionIdTag] = ExecutionId,
            [TelemetryConstants.CorrelationIdTag] = CorrelationId,
            [TelemetryConstants.KnowledgeSpaceIdTag] = KnowledgeSpaceId,
            [TelemetryConstants.OperationIdTag] = OperationId,
            ["trace_id"] = TraceId
        };

    private static string ReadOrGenerate(IReadOnlyDictionary<string, string?> headers, string name) =>
        ReadOptional(headers, name) ?? Guid.NewGuid().ToString("N");

    private static string? ReadOptional(IReadOnlyDictionary<string, string?> headers, string name)
    {
        if (!headers.TryGetValue(name, out var value))
        {
            return null;
        }

        return IsSafeIdentifier(value) ? value : null;
    }

    private static bool IsSafeIdentifier(string? value)
    {
        if (string.IsNullOrEmpty(value) || value.Length > 128)
        {
            return false;
        }

        return value.All(character =>
            char.IsAsciiLetterOrDigit(character)
            || character is '.' or '_' or ':' or '-');
    }
}
