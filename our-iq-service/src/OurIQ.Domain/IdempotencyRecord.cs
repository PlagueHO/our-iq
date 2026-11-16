using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace OurIQ.Domain;

public sealed record IdempotencyIdentity(
    string Id,
    string KnowledgeSpaceId,
    string RequestFingerprint)
{
    public static IdempotencyIdentity Create(
        string operation,
        string initiatingUserId,
        string actingAgentId,
        JsonElement request)
    {
        _ = JsonCanonicalizer.Canonicalize(request);
        var key = request.GetProperty("idempotencyKey").GetString();
        ArgumentException.ThrowIfNullOrWhiteSpace(key);
        var spaceId = request.GetProperty("knowledgeSpaceId").GetString();
        ArgumentException.ThrowIfNullOrWhiteSpace(spaceId);
        var scope = JsonSerializer.SerializeToElement(new[]
        {
            operation, initiatingUserId.ToLowerInvariant(), actingAgentId, key
        });
        var normalized = JsonNode.Parse(request.GetRawText())!.AsObject();
        normalized.Remove("idempotencyKey");
        normalized["executionContext"]!.AsObject().Remove("correlationId");
        normalized["identity"]!["initiatingUserId"] = initiatingUserId.ToLowerInvariant();
        return new(
            $"ouriq-replay-{Hash(JsonCanonicalizer.Canonicalize(scope))}",
            spaceId,
            Hash(JsonCanonicalizer.Canonicalize(
                JsonSerializer.SerializeToElement(normalized))));
    }

    private static string Hash(string value) =>
        Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(value))).ToLowerInvariant();
}

public sealed record IdempotencyRecord(
    IdempotencyIdentity Identity,
    string ResponseJson,
    bool IsError,
    DateTimeOffset CreatedAt)
{
    public IdempotencyRecord Match(IdempotencyIdentity expected) =>
        Identity == expected
            ? this
            : throw new IdempotencyKeyConflictException();
}

public sealed class IdempotencyKeyConflictException : InvalidOperationException;

public sealed class MutationConcurrencyException : InvalidOperationException;

public sealed class MutationDependencyException : InvalidOperationException;
