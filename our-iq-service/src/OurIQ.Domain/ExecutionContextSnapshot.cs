namespace OurIQ.Domain;

public sealed record ExecutionContextSnapshot
{
    public const string RecordTypeValue = "executionContextSnapshot";

    public string Id { get; init; } = string.Empty;

    public string ExecutionId { get; init; } = string.Empty;

    public string TraceId { get; init; } = string.Empty;

    public string KnowledgeSpaceId { get; init; } = string.Empty;

    public string LifecycleState { get; init; } = string.Empty;

    public string ActingAgentId { get; init; } = string.Empty;

    public string AgentDefinitionVersion { get; init; } = string.Empty;

    public string? ActiveOntologyVersionId { get; init; }

    public string? ActiveOntologyDigest { get; init; }

    public string MutationPolicy { get; init; } = string.Empty;

    public string MutationPolicyVersion { get; init; } = string.Empty;

    public string? CanonicalHeadVersion { get; init; }

    public string? InitiatingUserId { get; init; }

    public string? ExecutionGrantId { get; init; }

    public DateTimeOffset CreatedAt { get; init; }

    public static ExecutionContextSnapshot Create(
        KnowledgeSpaceControlRecord controlRecord,
        string executionId,
        string traceId,
        string actingAgentId,
        string agentDefinitionVersion,
        string? initiatingUserId = null,
        string? executionGrantId = null,
        DateTimeOffset? now = null)
    {
        ArgumentNullException.ThrowIfNull(controlRecord);
        ArgumentException.ThrowIfNullOrWhiteSpace(executionId);
        ArgumentException.ThrowIfNullOrWhiteSpace(traceId);
        ArgumentException.ThrowIfNullOrWhiteSpace(actingAgentId);
        ArgumentException.ThrowIfNullOrWhiteSpace(agentDefinitionVersion);

        controlRecord.Validate();

        var snapshot = new ExecutionContextSnapshot
        {
            Id = executionId,
            ExecutionId = executionId,
            TraceId = traceId,
            KnowledgeSpaceId = controlRecord.KnowledgeSpaceId,
            LifecycleState = controlRecord.LifecycleState,
            ActingAgentId = actingAgentId,
            AgentDefinitionVersion = agentDefinitionVersion,
            ActiveOntologyVersionId = controlRecord.ActiveOntologyVersionId,
            ActiveOntologyDigest = controlRecord.ActiveOntologyDigest,
            MutationPolicy = controlRecord.MutationPolicy,
            MutationPolicyVersion = controlRecord.MutationPolicyVersion,
            CanonicalHeadVersion = controlRecord.CanonicalHeadVersion,
            InitiatingUserId = initiatingUserId,
            ExecutionGrantId = executionGrantId,
            CreatedAt = now ?? DateTimeOffset.UtcNow
        };
        snapshot.Validate();
        return snapshot;
    }

    public void Validate()
    {
        ValidateRequired(Id, nameof(Id));
        ValidateRequired(ExecutionId, nameof(ExecutionId));
        ValidateRequired(TraceId, nameof(TraceId));
        ValidateRequired(KnowledgeSpaceId, nameof(KnowledgeSpaceId));
        ValidateRequired(LifecycleState, nameof(LifecycleState));
        ValidateRequired(ActingAgentId, nameof(ActingAgentId));
        ValidateRequired(AgentDefinitionVersion, nameof(AgentDefinitionVersion));
        ValidateRequired(MutationPolicy, nameof(MutationPolicy));
        ValidateRequired(MutationPolicyVersion, nameof(MutationPolicyVersion));

        if (!string.Equals(Id, ExecutionId, StringComparison.Ordinal))
        {
            throw new ExecutionContextSnapshotValidationException(
                "The snapshot ID must equal the execution ID.");
        }

        if (!KnowledgeSpaceLifecycleStates.IsDefined(LifecycleState))
        {
            throw new ExecutionContextSnapshotValidationException(
                $"The lifecycle state '{LifecycleState}' is not supported.");
        }

        if (string.IsNullOrWhiteSpace(ActiveOntologyVersionId)
            != string.IsNullOrWhiteSpace(ActiveOntologyDigest))
        {
            throw new ExecutionContextSnapshotValidationException(
                "The active ontology version and digest must be set together.");
        }

        if (string.IsNullOrWhiteSpace(InitiatingUserId)
            && string.IsNullOrWhiteSpace(ExecutionGrantId))
        {
            throw new ExecutionContextSnapshotValidationException(
                "Unattended execution requires an execution grant.");
        }

        if (!string.IsNullOrWhiteSpace(InitiatingUserId)
            && !string.IsNullOrWhiteSpace(ExecutionGrantId))
        {
            throw new ExecutionContextSnapshotValidationException(
                "Attended execution cannot include an execution grant.");
        }
    }

    public ExecutionContextFreshnessResult CheckFreshness(
        KnowledgeSpaceControlRecord current)
    {
        ArgumentNullException.ThrowIfNull(current);
        Validate();
        current.Validate();

        var changedFields = new List<string>();
        AddIfChanged(changedFields, "knowledgeSpaceId", KnowledgeSpaceId, current.KnowledgeSpaceId);
        AddIfChanged(changedFields, "lifecycleState", LifecycleState, current.LifecycleState);
        AddIfChanged(
            changedFields,
            "activeOntologyVersionId",
            ActiveOntologyVersionId,
            current.ActiveOntologyVersionId);
        AddIfChanged(
            changedFields,
            "activeOntologyDigest",
            ActiveOntologyDigest,
            current.ActiveOntologyDigest);
        AddIfChanged(changedFields, "mutationPolicy", MutationPolicy, current.MutationPolicy);
        AddIfChanged(
            changedFields,
            "mutationPolicyVersion",
            MutationPolicyVersion,
            current.MutationPolicyVersion);
        AddIfChanged(
            changedFields,
            "canonicalHeadVersion",
            CanonicalHeadVersion,
            current.CanonicalHeadVersion);

        return new ExecutionContextFreshnessResult(changedFields);
    }

    private static void AddIfChanged(
        ICollection<string> changedFields,
        string fieldName,
        string? expected,
        string? actual)
    {
        if (!string.Equals(expected, actual, StringComparison.Ordinal))
        {
            changedFields.Add(fieldName);
        }
    }

    private static void ValidateRequired(string? value, string name)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new ExecutionContextSnapshotValidationException(
                $"The {name} value is required.");
        }
    }
}

public sealed record ExecutionContextFreshnessResult(IReadOnlyList<string> ChangedFields)
{
    public const string ReplanRequiredCode = "replan_required";

    public bool IsFresh => ChangedFields.Count == 0;

    public string? Code => IsFresh ? null : ReplanRequiredCode;
}

public sealed class ExecutionContextSnapshotValidationException(string message)
    : InvalidOperationException(message);

public sealed class ExecutionContextSnapshotConflictException(
    string knowledgeSpaceId,
    string snapshotId)
    : InvalidOperationException(
        $"The immutable execution-context snapshot '{snapshotId}' already exists in knowledge space '{knowledgeSpaceId}'.")
{
    public string KnowledgeSpaceId { get; } = knowledgeSpaceId;

    public string SnapshotId { get; } = snapshotId;
}
