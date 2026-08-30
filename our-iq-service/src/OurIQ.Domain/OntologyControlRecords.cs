namespace OurIQ.Domain;

public static class OntologyControlRecordTypes
{
    public const string Proposal = "ontologyProposal";
    public const string CompatibilityAssessment = "ontologyCompatibilityAssessment";
    public const string Approval = "ontologyApproval";
    public const string ActivationEvidence = "ontologyActivationEvidence";
}

public sealed record OntologyProposal
{
    public string Id { get; init; } = string.Empty;

    public string KnowledgeSpaceId { get; init; } = string.Empty;

    public string OntologyVersionId { get; init; } = string.Empty;

    public DateTimeOffset CreatedAt { get; init; }

    public string CreatedBy { get; init; } = string.Empty;

    public IReadOnlyList<string> SourceReferences { get; init; } = [];

    public void Validate() => OntologyControlRecordValidator.ValidateImmutableRecord(
        Id,
        KnowledgeSpaceId,
        OntologyVersionId,
        CreatedAt,
        CreatedBy);
}

public sealed record OntologyCompatibilityAssessment
{
    public string Id { get; init; } = string.Empty;

    public string KnowledgeSpaceId { get; init; } = string.Empty;

    public string OntologyVersionId { get; init; } = string.Empty;

    public bool IsApproved { get; init; }

    public bool RequiresMigration { get; init; }

    public DateTimeOffset CreatedAt { get; init; }

    public string CreatedBy { get; init; } = string.Empty;

    public IReadOnlyList<string> Findings { get; init; } = [];

    public void Validate()
    {
        OntologyControlRecordValidator.ValidateImmutableRecord(
            Id,
            KnowledgeSpaceId,
            OntologyVersionId,
            CreatedAt,
            CreatedBy);

        if (IsApproved && RequiresMigration)
        {
            throw new OntologyPayloadValidationException(
                "A compatibility assessment that requires migration cannot be approved.");
        }
    }
}

public sealed record OntologyApproval
{
    public string Id { get; init; } = string.Empty;

    public string KnowledgeSpaceId { get; init; } = string.Empty;

    public string OntologyVersionId { get; init; } = string.Empty;

    public string CompatibilityAssessmentId { get; init; } = string.Empty;

    public string ActorId { get; init; } = string.Empty;

    public string Authority { get; init; } = string.Empty;

    public bool IsApproved { get; init; }

    public DateTimeOffset CreatedAt { get; init; }

    public void Validate()
    {
        OntologyControlRecordValidator.ValidateImmutableRecord(
            Id,
            KnowledgeSpaceId,
            OntologyVersionId,
            CreatedAt,
            ActorId);
        OntologyControlRecordValidator.ValidateRequired(
            CompatibilityAssessmentId,
            nameof(CompatibilityAssessmentId));
        OntologyControlRecordValidator.ValidateRequired(Authority, nameof(Authority));
    }
}

public sealed record OntologyActivationEvidence
{
    public string Id { get; init; } = string.Empty;

    public string KnowledgeSpaceId { get; init; } = string.Empty;

    public string OntologyVersionId { get; init; } = string.Empty;

    public string PayloadDigest { get; init; } = string.Empty;

    public string ApprovalId { get; init; } = string.Empty;

    public string CompatibilityAssessmentId { get; init; } = string.Empty;

    public DateTimeOffset ActivatedAt { get; init; }

    public void Validate()
    {
        OntologyControlRecordValidator.ValidateRequired(Id, nameof(Id));
        OntologyControlRecordValidator.ValidateRequired(
            KnowledgeSpaceId,
            nameof(KnowledgeSpaceId));
        OntologyControlRecordValidator.ValidateRequired(
            OntologyVersionId,
            nameof(OntologyVersionId));
        OntologyControlRecordValidator.ValidateRequired(ApprovalId, nameof(ApprovalId));
        OntologyControlRecordValidator.ValidateDigest(PayloadDigest);
        OntologyControlRecordValidator.ValidateRequired(
            CompatibilityAssessmentId,
            nameof(CompatibilityAssessmentId));

        if (ActivatedAt == default)
        {
            throw new OntologyPayloadValidationException("The activation timestamp is required.");
        }
    }
}

public sealed record OntologyActivationRequest(
    string KnowledgeSpaceId,
    string OntologyVersionId,
    string PayloadDigest,
    string ApprovalId,
    string? ExpectedActiveOntologyVersionId,
    string? ExpectedActiveOntologyDigest,
    string ActivationEvidenceId);

public sealed class OntologyActivationConflictException(string knowledgeSpaceId)
    : InvalidOperationException(
        $"The active ontology pointer for knowledge space '{knowledgeSpaceId}' changed before activation.")
{
    public string KnowledgeSpaceId { get; } = knowledgeSpaceId;
}

public sealed class OntologyControlRecordConflictException(
    string knowledgeSpaceId,
    string recordId)
    : InvalidOperationException(
        $"The immutable ontology control record '{recordId}' already exists in knowledge space '{knowledgeSpaceId}'.")
{
    public string KnowledgeSpaceId { get; } = knowledgeSpaceId;

    public string RecordId { get; } = recordId;
}

public static class OntologyControlRecordValidator
{
    public static void ValidateImmutableRecord(
        string id,
        string knowledgeSpaceId,
        string ontologyVersionId,
        DateTimeOffset timestamp,
        string actor)
    {
        ValidateRequired(id, nameof(id));
        ValidateRequired(knowledgeSpaceId, nameof(knowledgeSpaceId));
        ValidateRequired(ontologyVersionId, nameof(ontologyVersionId));
        ValidateRequired(actor, nameof(actor));

        if (timestamp == default)
        {
            throw new OntologyPayloadValidationException("The record timestamp is required.");
        }
    }

    public static void ValidateDigest(string digest)
    {
        if (digest.Length != 64
            || digest.Any(character =>
                character is not (>= '0' and <= '9')
                && character is not (>= 'a' and <= 'f')))
        {
            throw new OntologyPayloadValidationException(
                "The payload digest must be a lowercase SHA-256 digest.");
        }
    }

    public static void ValidateRequired(string? value, string valueName)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new OntologyPayloadValidationException(
                $"The {valueName} value is required.");
        }
    }
}
