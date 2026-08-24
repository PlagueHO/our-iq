using OurIQ.Domain;

namespace OurIQ.UnitTests;

[TestClass]
public sealed class KnowledgeSpaceControlRecordTests
{
    private static readonly IReadOnlyList<KnowledgeSpaceLifecycleTransition> ExpectedTransitions =
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
    ];

    private static readonly IReadOnlySet<(string FromState, string ToState)> ExpectedTransitionPairs =
        ExpectedTransitions
            .Select(transition => (transition.FromState, transition.ToState))
            .ToHashSet();

    [TestMethod]
    public void CreateProducesStableDraftControlRecordDefaults()
    {
        var createdAt = new DateTimeOffset(2026, 8, 24, 0, 0, 0, TimeSpan.Zero);
        var record = KnowledgeSpaceControlRecord.Create(
            new KnowledgeSpaceCreation(
                "Product",
                "contributor confirmation",
                "user-001"),
            () => Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa"),
            createdAt);

        Assert.AreEqual("ks-aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa", record.KnowledgeSpaceId);
        Assert.AreEqual(KnowledgeSpaceControlRecord.RecordTypeValue, record.RecordType);
        Assert.AreEqual("Product", record.DisplayName);
        Assert.AreEqual(KnowledgeSpaceLifecycleStates.Draft, record.LifecycleState);
        Assert.AreEqual("contributor confirmation", record.MutationPolicy);
        Assert.AreEqual("1.0", record.MutationPolicyVersion);
        Assert.AreEqual(createdAt, record.CreatedAt);
        Assert.AreEqual(createdAt, record.UpdatedAt);
        Assert.IsNull(record.ActiveOntologyVersionId);
        Assert.IsNull(record.CanonicalHeadVersion);
        Assert.IsNull(record.ETag);
    }

    [TestMethod]
    public void CreateProducesUniqueIdentifiers()
    {
        var first = KnowledgeSpaceControlRecord.Create(
            new KnowledgeSpaceCreation("First", "automatic"));
        var second = KnowledgeSpaceControlRecord.Create(
            new KnowledgeSpaceCreation("Second", "review"));

        Assert.AreNotEqual(first.KnowledgeSpaceId, second.KnowledgeSpaceId);
    }

    [TestMethod]
    public void ValidateRejectsUnknownLifecycleState()
    {
        var record = KnowledgeSpaceControlRecord.Create(
            new KnowledgeSpaceCreation("Product", "contributor confirmation"))
            with
            {
                LifecycleState = "unknown"
            };

        Assert.Throws<KnowledgeSpaceControlRecordValidationException>(
            record.Validate);
    }

    [TestMethod]
    public void CreateRejectsMissingMutationPolicy()
    {
        Assert.Throws<KnowledgeSpaceControlRecordValidationException>(
            () => KnowledgeSpaceControlRecord.Create(new KnowledgeSpaceCreation("Product", "")));
    }

    [TestMethod]
    [DynamicData(nameof(AllowedTransitions), DynamicDataSourceType.Method)]
    public void TransitionToAllowsEveryDefinedTransition(
        string currentState,
        string targetState,
        string[] requiredRoles)
    {
        var (record, initiatingUserId) = CreateRecordWithRequiredRole(
            currentState,
            requiredRoles[0]);

        var transition = KnowledgeSpaceLifecycleTransitions.GetRequiredTransition(
            currentState,
            targetState);
        var transitionedRecord = record.TransitionTo(targetState, initiatingUserId);

        CollectionAssert.AreEquivalent(requiredRoles, transition.RequiredRoles.ToArray());
        Assert.AreEqual(targetState, transitionedRecord.LifecycleState);
        Assert.AreEqual(record.KnowledgeSpaceId, transitionedRecord.KnowledgeSpaceId);
        Assert.AreEqual(record.CreatedAt, transitionedRecord.CreatedAt);
        Assert.AreEqual(record.UpdatedAt, transitionedRecord.UpdatedAt);
    }

    [TestMethod]
    [DynamicData(nameof(RejectedTransitions), DynamicDataSourceType.Method)]
    public void TransitionToRejectsEveryUnlistedTransition(
        string currentState,
        string targetState)
    {
        var exception = Assert.Throws<KnowledgeSpaceStateConflictException>(
            () => CreateRecord(currentState).TransitionTo(targetState, "owner-001"));

        Assert.AreEqual(KnowledgeSpaceStateConflictException.Code, "space_state_conflict");
        Assert.AreEqual(currentState, exception.CurrentState);
        Assert.AreEqual(targetState, exception.TargetState);
    }

    public static IEnumerable<object[]> AllowedTransitions() =>
        ExpectedTransitions.Select(
            transition =>
            [
                transition.FromState,
                transition.ToState,
                transition.RequiredRoles.ToArray()
            ]);

    public static IEnumerable<object[]> RejectedTransitions() =>
        ExpectedTransitions
            .Select(transition => transition.FromState)
            .Append(KnowledgeSpaceLifecycleStates.Deleted)
            .Distinct()
            .SelectMany(
                currentState => ExpectedTransitions
                    .Select(transition => transition.ToState)
                    .Append(KnowledgeSpaceLifecycleStates.Draft)
                    .Distinct()
                    .Where(targetState => !ExpectedTransitionPairs.Contains((currentState, targetState)))
                    .Select(targetState => new object[] { currentState, targetState }));

    [TestMethod]
    [DynamicData(nameof(AllowedTransitions), DynamicDataSourceType.Method)]
    public void TransitionToRejectsUsersWithoutTheRequiredRole(
        string currentState,
        string targetState,
        string[] requiredRoles)
    {
        var record = CreateRecord(currentState);

        Assert.Throws<KnowledgeSpaceRoleAuthorizationException>(
            () => record.TransitionTo(targetState, "unassigned-user"));
    }

    private static (KnowledgeSpaceControlRecord Record, string InitiatingUserId) CreateRecordWithRequiredRole(
        string lifecycleState,
        string requiredRole)
    {
        var record = CreateRecord(lifecycleState);

        if (requiredRole == KnowledgeSpaceRoles.Owner)
        {
            return (record, "owner-001");
        }

        return (
            record.GrantRole("owner-001", "authorized-user", requiredRole),
            "authorized-user");
    }

    private static KnowledgeSpaceControlRecord CreateRecord(string lifecycleState) =>
        KnowledgeSpaceControlRecord.Create(
            new KnowledgeSpaceCreation("Product", "contributor confirmation", "owner-001")) with
        {
            LifecycleState = lifecycleState
        };
}
