using OurIQ.Domain;

namespace OurIQ.UnitTests;

[TestClass]
public sealed class KnowledgeSpaceControlRecordTests
{
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
}
