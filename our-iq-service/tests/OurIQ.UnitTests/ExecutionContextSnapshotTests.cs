using OurIQ.Domain;

namespace OurIQ.UnitTests;

[TestClass]
public sealed class ExecutionContextSnapshotTests
{
    [TestMethod]
    public void CreatePinsTheCurrentControlRecordState()
    {
        var controlRecord = CreateRecord() with
        {
            LifecycleState = KnowledgeSpaceLifecycleStates.Active,
            ActiveOntologyVersionId = "ontology-v1",
            ActiveOntologyDigest = "digest-v1",
            CanonicalHeadVersion = "head-v3"
        };
        var createdAt = new DateTimeOffset(2026, 8, 30, 0, 0, 0, TimeSpan.Zero);

        var snapshot = ExecutionContextSnapshot.Create(
            controlRecord,
            "execution-001",
            "trace-001",
            DomainAgentIdentities.Contribution,
            DomainAgentIdentities.InitialDefinitionVersion,
            "user-001",
            now: createdAt);

        Assert.AreEqual(snapshot.ExecutionId, snapshot.Id);
        Assert.AreEqual("trace-001", snapshot.TraceId);
        Assert.AreEqual(controlRecord.KnowledgeSpaceId, snapshot.KnowledgeSpaceId);
        Assert.AreEqual(controlRecord.LifecycleState, snapshot.LifecycleState);
        Assert.AreEqual(controlRecord.MutationPolicy, snapshot.MutationPolicy);
        Assert.AreEqual(controlRecord.MutationPolicyVersion, snapshot.MutationPolicyVersion);
        Assert.AreEqual(controlRecord.ActiveOntologyVersionId, snapshot.ActiveOntologyVersionId);
        Assert.AreEqual(controlRecord.ActiveOntologyDigest, snapshot.ActiveOntologyDigest);
        Assert.AreEqual(controlRecord.CanonicalHeadVersion, snapshot.CanonicalHeadVersion);
        Assert.AreEqual(createdAt, snapshot.CreatedAt);
    }

    [TestMethod]
    public void CreateAllowsUnattendedExecutionOnlyWithGrant()
    {
        var snapshot = CreateSnapshot(executionGrantId: "grant-001");

        Assert.IsNull(snapshot.InitiatingUserId);
        Assert.AreEqual("grant-001", snapshot.ExecutionGrantId);
    }

    [TestMethod]
    public void ValidateRejectsInvalidIdentityAndOntologyCombinations()
    {
        var snapshot = CreateSnapshot();

        Assert.Throws<ExecutionContextSnapshotValidationException>(
            () => (snapshot with
            {
                InitiatingUserId = null,
                ExecutionGrantId = null
            }).Validate());
        Assert.Throws<ExecutionContextSnapshotValidationException>(
            () => (snapshot with
            {
                ExecutionGrantId = "grant-001"
            }).Validate());
        Assert.Throws<ExecutionContextSnapshotValidationException>(
            () => (snapshot with
            {
                ActiveOntologyVersionId = "ontology-v1",
                ActiveOntologyDigest = null
            }).Validate());
        Assert.Throws<ExecutionContextSnapshotValidationException>(
            () => (snapshot with
            {
                Id = "different-id"
            }).Validate());
    }

    [TestMethod]
    public void CheckFreshnessAcceptsUnchangedState()
    {
        var controlRecord = CreateRecord();
        var result = CreateSnapshot(controlRecord).CheckFreshness(controlRecord);

        Assert.IsTrue(result.IsFresh);
        Assert.IsNull(result.Code);
        CollectionAssert.AreEqual(Array.Empty<string>(), result.ChangedFields.ToArray());
    }

    [TestMethod]
    public void CheckFreshnessRequiresReplanWhenLifecycleChanges()
    {
        AssertStale(
            record => record with { LifecycleState = KnowledgeSpaceLifecycleStates.Pending },
            "lifecycleState");
    }

    [TestMethod]
    public void CheckFreshnessRequiresReplanWhenKnowledgeSpaceChanges()
    {
        AssertStale(
            record => record with { KnowledgeSpaceId = "ks-other" },
            "knowledgeSpaceId");
    }

    [TestMethod]
    public void CheckFreshnessRequiresReplanWhenOntologyChanges()
    {
        AssertStale(
            record => record with
            {
                ActiveOntologyVersionId = "ontology-v2",
                ActiveOntologyDigest = "digest-v2"
            },
            "activeOntologyVersionId",
            "activeOntologyDigest");
    }

    [TestMethod]
    public void CheckFreshnessRequiresReplanWhenMutationPolicyChanges()
    {
        AssertStale(
            record => record with
            {
                MutationPolicy = "owner confirmation",
                MutationPolicyVersion = "2.0"
            },
            "mutationPolicy",
            "mutationPolicyVersion");
    }

    [TestMethod]
    public void CheckFreshnessRequiresReplanWhenCanonicalHeadChanges()
    {
        AssertStale(
            record => record with { CanonicalHeadVersion = "head-v2" },
            "canonicalHeadVersion");
    }

    private static void AssertStale(
        Func<KnowledgeSpaceControlRecord, KnowledgeSpaceControlRecord> update,
        params string[] changedFields)
    {
        var controlRecord = CreateRecord();
        var result = CreateSnapshot(controlRecord).CheckFreshness(update(controlRecord));

        Assert.IsFalse(result.IsFresh);
        Assert.AreEqual(ExecutionContextFreshnessResult.ReplanRequiredCode, result.Code);
        CollectionAssert.AreEqual(changedFields, result.ChangedFields.ToArray());
    }

    private static ExecutionContextSnapshot CreateSnapshot(
        KnowledgeSpaceControlRecord? controlRecord = null,
        string? executionGrantId = null) =>
        ExecutionContextSnapshot.Create(
            controlRecord ?? CreateRecord(),
            "execution-001",
            "trace-001",
            DomainAgentIdentities.Contribution,
            DomainAgentIdentities.InitialDefinitionVersion,
            executionGrantId is null ? "user-001" : null,
            executionGrantId);

    private static KnowledgeSpaceControlRecord CreateRecord() =>
        KnowledgeSpaceControlRecord.Create(
            new KnowledgeSpaceCreation(
                "Product",
                "contributor confirmation",
                "owner-001"));
}
