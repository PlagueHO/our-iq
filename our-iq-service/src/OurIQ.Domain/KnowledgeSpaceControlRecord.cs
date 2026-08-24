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

public static class KnowledgeSpaceRoles
{
    public const string Owner = "Owner";
    public const string OntologyManager = "Ontology Manager";
    public const string Contributor = "Contributor";
    public const string Reader = "Reader";

    private static readonly IReadOnlySet<string> DefinedRoles = new HashSet<string>(
        StringComparer.Ordinal)
    {
        Owner,
        OntologyManager,
        Contributor,
        Reader
    };

    public static bool IsDefined(string? role) => role is not null && DefinedRoles.Contains(role);
}

public sealed record KnowledgeSpaceRoleGrant(string UserId, string Role)
{
    public void Validate()
    {
        ValidateRequired(UserId, nameof(UserId));

        if (!KnowledgeSpaceRoles.IsDefined(Role))
        {
            throw new KnowledgeSpaceControlRecordValidationException(
                $"The role '{Role}' is not supported.");
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

public sealed record KnowledgeSpaceLifecycleTransition(
    string FromState,
    string ToState,
    IReadOnlyList<string> RequiredRoles);

public static class KnowledgeSpaceLifecycleTransitions
{
    private static readonly IReadOnlyList<KnowledgeSpaceLifecycleTransition> DefinedTransitions =
        Array.AsReadOnly(
        [
            new(
                KnowledgeSpaceLifecycleStates.Draft,
                KnowledgeSpaceLifecycleStates.Pending,
                [KnowledgeSpaceRoles.Owner, KnowledgeSpaceRoles.OntologyManager]),
            new(
                KnowledgeSpaceLifecycleStates.Pending,
                KnowledgeSpaceLifecycleStates.Active,
                [KnowledgeSpaceRoles.OntologyManager]),
            new(
                KnowledgeSpaceLifecycleStates.Active,
                KnowledgeSpaceLifecycleStates.Readonly,
                [KnowledgeSpaceRoles.Owner]),
            new(
                KnowledgeSpaceLifecycleStates.Active,
                KnowledgeSpaceLifecycleStates.Maintenance,
                [KnowledgeSpaceRoles.Owner]),
            new(
                KnowledgeSpaceLifecycleStates.Active,
                KnowledgeSpaceLifecycleStates.Retired,
                [KnowledgeSpaceRoles.Owner]),
            new(
                KnowledgeSpaceLifecycleStates.Readonly,
                KnowledgeSpaceLifecycleStates.Active,
                [KnowledgeSpaceRoles.Owner]),
            new(
                KnowledgeSpaceLifecycleStates.Readonly,
                KnowledgeSpaceLifecycleStates.Maintenance,
                [KnowledgeSpaceRoles.Owner]),
            new(
                KnowledgeSpaceLifecycleStates.Readonly,
                KnowledgeSpaceLifecycleStates.Retired,
                [KnowledgeSpaceRoles.Owner]),
            new(
                KnowledgeSpaceLifecycleStates.Maintenance,
                KnowledgeSpaceLifecycleStates.Active,
                [KnowledgeSpaceRoles.Owner]),
            new(
                KnowledgeSpaceLifecycleStates.Maintenance,
                KnowledgeSpaceLifecycleStates.Readonly,
                [KnowledgeSpaceRoles.Owner]),
            new(
                KnowledgeSpaceLifecycleStates.Maintenance,
                KnowledgeSpaceLifecycleStates.Retired,
                [KnowledgeSpaceRoles.Owner]),
            new(
                KnowledgeSpaceLifecycleStates.Retired,
                KnowledgeSpaceLifecycleStates.Deleting,
                [KnowledgeSpaceRoles.Owner]),
            new(
                KnowledgeSpaceLifecycleStates.Deleting,
                KnowledgeSpaceLifecycleStates.Deleted,
                [KnowledgeSpaceRoles.Owner])
        ]);

    private static readonly IReadOnlyDictionary<(string FromState, string ToState), KnowledgeSpaceLifecycleTransition>
        TransitionsByState = DefinedTransitions.ToDictionary(
            transition => (transition.FromState, transition.ToState));

    public static IReadOnlyList<KnowledgeSpaceLifecycleTransition> All => DefinedTransitions;

    public static KnowledgeSpaceLifecycleTransition GetRequiredTransition(
        string currentState,
        string targetState)
    {
        if (TransitionsByState.TryGetValue((currentState, targetState), out var transition))
        {
            return transition;
        }

        throw new KnowledgeSpaceStateConflictException(currentState, targetState);
    }
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

    public IReadOnlyList<KnowledgeSpaceRoleGrant> RoleGrants { get; init; } = [];

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
            CreatedBy = creation.CreatedBy,
            RoleGrants = string.IsNullOrWhiteSpace(creation.CreatedBy)
                ? []
                : [new KnowledgeSpaceRoleGrant(creation.CreatedBy, KnowledgeSpaceRoles.Owner)]
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

        var duplicateGrant = RoleGrants
            .GroupBy(grant => (grant.UserId, grant.Role))
            .FirstOrDefault(grants => grants.Count() > 1);

        if (duplicateGrant is not null)
        {
            throw new KnowledgeSpaceControlRecordValidationException(
                $"The role '{duplicateGrant.Key.Role}' is granted more than once to '{duplicateGrant.Key.UserId}'.");
        }

        foreach (var roleGrant in RoleGrants)
        {
            roleGrant.Validate();
        }
    }

    public KnowledgeSpaceControlRecord TransitionTo(
        string targetState,
        string initiatingUserId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(targetState);
        ArgumentException.ThrowIfNullOrWhiteSpace(initiatingUserId);
        Validate();
        var transition = KnowledgeSpaceLifecycleTransitions.GetRequiredTransition(
            LifecycleState,
            targetState);
        RequireAnyRole(initiatingUserId, transition.RequiredRoles);

        return this with
        {
            LifecycleState = targetState
        };
    }

    public KnowledgeSpaceControlRecord GrantRole(
        string grantingUserId,
        string grantedUserId,
        string role)
    {
        Validate();
        RequireOwner(grantingUserId);
        var grant = new KnowledgeSpaceRoleGrant(grantedUserId, role);
        grant.Validate();

        return RoleGrants.Any(existing => existing == grant)
            ? this
            : this with { RoleGrants = [.. RoleGrants, grant] };
    }

    public KnowledgeSpaceControlRecord RevokeRole(
        string revokingUserId,
        string revokedUserId,
        string role)
    {
        Validate();
        RequireOwner(revokingUserId);
        var grant = new KnowledgeSpaceRoleGrant(revokedUserId, role);
        grant.Validate();

        return this with
        {
            RoleGrants = RoleGrants.Where(existing => existing != grant).ToArray()
        };
    }

    public bool HasRole(string userId, string role)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(userId);
        ArgumentException.ThrowIfNullOrWhiteSpace(role);
        return RoleGrants.Any(grant =>
            string.Equals(grant.UserId, userId, StringComparison.Ordinal)
            && string.Equals(grant.Role, role, StringComparison.Ordinal));
    }

    private void RequireOwner(string userId)
    {
        RequireAnyRole(userId, [KnowledgeSpaceRoles.Owner]);
    }

    private void RequireAnyRole(string userId, IReadOnlyList<string> requiredRoles)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(userId);

        if (!requiredRoles.Any(role => HasRole(userId, role)))
        {
            throw new KnowledgeSpaceRoleAuthorizationException(
                $"The user '{userId}' lacks a role required for this operation.");
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

public sealed class KnowledgeSpaceRoleAuthorizationException(string message)
    : UnauthorizedAccessException(message);

public sealed class KnowledgeSpaceControlRecordConflictException(
    string knowledgeSpaceId,
    string expectedETag)
    : InvalidOperationException(
        $"The knowledge-space control record '{knowledgeSpaceId}' changed since ETag '{expectedETag}' was read.")
{
    public string KnowledgeSpaceId { get; } = knowledgeSpaceId;

    public string ExpectedETag { get; } = expectedETag;
}

public sealed class KnowledgeSpaceStateConflictException(
    string currentState,
    string targetState)
    : InvalidOperationException(
        $"The transition from '{currentState}' to '{targetState}' is not permitted.")
{
    public const string Code = "space_state_conflict";

    public string CurrentState { get; } = currentState;

    public string TargetState { get; } = targetState;
}
