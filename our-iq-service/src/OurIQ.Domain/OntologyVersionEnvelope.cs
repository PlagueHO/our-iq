namespace OurIQ.Domain;

public sealed record OntologyVersionEnvelope
{
    public string Id { get; init; } = string.Empty;

    public string RecordType { get; init; } = string.Empty;

    public string KnowledgeSpaceId { get; init; } = string.Empty;

    public string OntologyId { get; init; } = string.Empty;

    public string OntologyVersionId { get; init; } = string.Empty;

    public string SchemaVersion { get; init; } = string.Empty;

    public OntologyPayload Payload { get; init; } = new();

    public string PayloadDigest { get; init; } = string.Empty;

    public DateTimeOffset CreatedAt { get; init; }

    public string CreatedBy { get; init; } = string.Empty;

    public IReadOnlyList<string> SourceReferences { get; init; } = [];

    public void Validate()
    {
        ValidateRequired(Id, nameof(Id));
        ValidateRequired(KnowledgeSpaceId, nameof(KnowledgeSpaceId));
        ValidateRequired(OntologyId, nameof(OntologyId));
        ValidateRequired(OntologyVersionId, nameof(OntologyVersionId));
        ValidateRequired(SchemaVersion, nameof(SchemaVersion));
        ValidateRequired(CreatedBy, nameof(CreatedBy));

        if (!string.Equals(RecordType, "ontologyVersion", StringComparison.Ordinal))
        {
            throw new OntologyPayloadValidationException(
                "The ontology version envelope record type must be 'ontologyVersion'.");
        }

        if (!string.Equals(Id, OntologyVersionId, StringComparison.Ordinal))
        {
            throw new OntologyPayloadValidationException(
                "The ontology version envelope identifier must equal its ontology version identifier.");
        }

        ArgumentNullException.ThrowIfNull(Payload);
        Payload.Validate(new OntologyIdentity(OntologyId, OntologyVersionId));

        if (!string.Equals(
                PayloadDigest,
                OntologyPayloadDigest.Compute(Payload),
                StringComparison.Ordinal))
        {
            throw new OntologyPayloadValidationException(
                "The ontology version envelope payload digest does not match the payload.");
        }
    }

    private static void ValidateRequired(string? value, string valueName)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new OntologyPayloadValidationException(
                $"The {valueName} value is required.");
        }
    }
}
