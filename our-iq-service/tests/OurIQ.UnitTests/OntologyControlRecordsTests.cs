using OurIQ.Domain;

namespace OurIQ.UnitTests;

[TestClass]
public sealed class OntologyControlRecordsTests
{
    private const string Digest =
        "aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa";

    [TestMethod]
    public void ApprovalRequiresCompatibilityAssessmentReference()
    {
        var approval = CreateApproval() with { CompatibilityAssessmentId = string.Empty };

        Assert.Throws<OntologyPayloadValidationException>(approval.Validate);
    }

    [TestMethod]
    public void ActivationEvidenceRequiresLowercasePayloadDigest()
    {
        var evidence = CreateEvidence() with { PayloadDigest = Digest.ToUpperInvariant() };

        Assert.Throws<OntologyPayloadValidationException>(evidence.Validate);
    }

    [TestMethod]
    public void ControlRecordRejectsIncompleteActiveOntologyPointer()
    {
        var record = KnowledgeSpaceControlRecord.Create(
            new KnowledgeSpaceCreation("Product", "review", "owner"));

        Assert.Throws<KnowledgeSpaceControlRecordValidationException>(
            () => (record with { ActiveOntologyVersionId = "ontology-product-v1" }).Validate());
    }

    [TestMethod]
    public void CompatibilityAssessmentCannotApproveARequiredMigration()
    {
        var assessment = new OntologyCompatibilityAssessment
        {
            Id = "assessment-001",
            KnowledgeSpaceId = "ks-product",
            OntologyVersionId = "ontology-v2",
            IsApproved = true,
            RequiresMigration = true,
            CreatedAt = DateTimeOffset.UtcNow,
            CreatedBy = "ontology-manager-001"
        };

        Assert.Throws<OntologyPayloadValidationException>(assessment.Validate);
    }

    private static OntologyApproval CreateApproval() =>
        new()
        {
            Id = "approval-001",
            KnowledgeSpaceId = "ks-product",
            OntologyVersionId = "ontology-product-v1",
            CompatibilityAssessmentId = "assessment-001",
            ActorId = "owner-001",
            Authority = "Ontology Manager",
            IsApproved = true,
            CreatedAt = DateTimeOffset.UtcNow
        };

    private static OntologyActivationEvidence CreateEvidence() =>
        new()
        {
            Id = "activation-001",
            KnowledgeSpaceId = "ks-product",
            OntologyVersionId = "ontology-product-v1",
            PayloadDigest = Digest,
            ApprovalId = "approval-001",
            CompatibilityAssessmentId = "assessment-001",
            ActivatedAt = DateTimeOffset.UtcNow
        };
}
