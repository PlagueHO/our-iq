namespace OurIQ.Domain;

public static class KnowledgeSpaceLifecycleStates
{
    public const string Draft = "draft";
    public const string Pending = "pending";
    public const string Active = "active";
    public const string Readonly = "readonly";
    public const string Maintenance = "maintenance";
    public const string Retired = "retired";
    public const string Deleting = "deleting";
    public const string Deleted = "deleted";

    public static bool IsDefined(string? state) =>
        state is Draft
            or Pending
            or Active
            or Readonly
            or Maintenance
            or Retired
            or Deleting
            or Deleted;
}

public sealed record KnowledgeSpaceCreation(
    string DisplayName,
    string MutationPolicy,
    string? CreatedBy = null,
    string MutationPolicyVersion = "1.0");

public sealed record KnowledgeSpaceControlRecord
{
    public const string RecordTypeValue = "knowledgeSpace";

    public string KnowledgeSpaceId { get; init; } = string.Empty;

    public string RecordType { get; init; } = RecordTypeValue;

    public string DisplayName { get; init; } = string.Empty;

    public string LifecycleState { get; init; } = KnowledgeSpaceLifecycleStates.Draft;

    public string MutationPolicy { get; init; } = string.Empty;

    public string MutationPolicyVersion { get; init; } = string.Empty;

    public string? ActiveOntologyVersionId { get; init; }

    public string? ActiveOntologyDigest { get; init; }

    public string? CanonicalHeadVersion { get; init; }

    public string? ActiveChangeSetId { get; init; }

    public DateTimeOffset CreatedAt { get; init; }

    public DateTimeOffset UpdatedAt { get; init; }

    public string? CreatedBy { get; init; }

    public string? ETag { get; init; }

    public static KnowledgeSpaceControlRecord Create(
        KnowledgeSpaceCreation creation,
        Func<Guid>? identifierFactory = null,
        DateTimeOffset? now = null)
    {
        ArgumentNullException.ThrowIfNull(creation);
        ValidateRequired(creation.DisplayName, nameof(creation.DisplayName));
        ValidateRequired(creation.MutationPolicy, nameof(creation.MutationPolicy));
        ValidateRequired(creation.MutationPolicyVersion, nameof(creation.MutationPolicyVersion));

        var timestamp = now ?? DateTimeOffset.UtcNow;
        var identifier = identifierFactory?.Invoke() ?? Guid.NewGuid();

        return new KnowledgeSpaceControlRecord
        {
            KnowledgeSpaceId = $"ks-{identifier:N}",
            DisplayName = creation.DisplayName,
            MutationPolicy = creation.MutationPolicy,
            MutationPolicyVersion = creation.MutationPolicyVersion,
            CreatedAt = timestamp,
            UpdatedAt = timestamp,
            CreatedBy = creation.CreatedBy
        };
    }

    public void Validate()
    {
        ValidateRequired(KnowledgeSpaceId, nameof(KnowledgeSpaceId));
        ValidateRequired(DisplayName, nameof(DisplayName));
        ValidateRequired(MutationPolicy, nameof(MutationPolicy));
        ValidateRequired(MutationPolicyVersion, nameof(MutationPolicyVersion));

        if (RecordType != RecordTypeValue)
        {
            throw new KnowledgeSpaceControlRecordValidationException(
                $"The control record type must be '{RecordTypeValue}'.");
        }

        if (!KnowledgeSpaceLifecycleStates.IsDefined(LifecycleState))
        {
            throw new KnowledgeSpaceControlRecordValidationException(
                $"The lifecycle state '{LifecycleState}' is not supported.");
        }
    }

    private static void ValidateRequired(string? value, string name)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new KnowledgeSpaceControlRecordValidationException(
                $"The {name} value is required.");
        }
    }
}

public sealed class KnowledgeSpaceControlRecordValidationException(string message)
    : InvalidOperationException(message);

public sealed class KnowledgeSpaceControlRecordConflictException(
    string knowledgeSpaceId,
    string expectedETag)
    : InvalidOperationException(
        $"The knowledge-space control record '{knowledgeSpaceId}' changed since ETag '{expectedETag}' was read.")
{
    public string KnowledgeSpaceId { get; } = knowledgeSpaceId;

    public string ExpectedETag { get; } = expectedETag;
}
