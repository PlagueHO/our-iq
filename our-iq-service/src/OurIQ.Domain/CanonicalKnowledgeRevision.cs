using System.Text.Json;

namespace OurIQ.Domain;

public sealed record CanonicalKnowledgeItemReference(string KnowledgeItemId)
{
    public void Validate() =>
        CanonicalKnowledgeRevisionValidator.ValidateRequired(
            KnowledgeItemId,
            nameof(KnowledgeItemId));
}

public sealed record CanonicalRelationshipTarget(
    string? KnowledgeItemId = null,
    string? UnresolvedConcept = null)
{
    public void Validate()
    {
        CanonicalKnowledgeRevisionValidator.ValidateOptional(
            KnowledgeItemId,
            nameof(KnowledgeItemId));
        CanonicalKnowledgeRevisionValidator.ValidateOptional(
            UnresolvedConcept,
            nameof(UnresolvedConcept));

        var targetCount = 0;

        if (!string.IsNullOrWhiteSpace(KnowledgeItemId))
        {
            targetCount++;
        }

        if (!string.IsNullOrWhiteSpace(UnresolvedConcept))
        {
            targetCount++;
        }

        if (targetCount != 1)
        {
            throw new CanonicalMarkdownValidationException(
                "A relationship target must declare exactly one supported reference.");
        }
    }
}

public sealed record CanonicalRelationship(
    string Type,
    CanonicalRelationshipTarget Target,
    string? Note = null,
    JsonElement? Qualifier = null,
    string? AssertionStatus = null)
{
    public void Validate()
    {
        CanonicalKnowledgeRevisionValidator.ValidateRequired(Type, nameof(Type));
        ArgumentNullException.ThrowIfNull(Target);
        Target.Validate();
        CanonicalKnowledgeRevisionValidator.ValidateOptional(Note, nameof(Note));
        CanonicalKnowledgeRevisionValidator.ValidateOptional(
            AssertionStatus,
            nameof(AssertionStatus));

        if (Qualifier is { ValueKind: JsonValueKind.Undefined })
        {
            throw new CanonicalMarkdownValidationException(
                "A relationship qualifier cannot be undefined.");
        }
    }
}

public sealed record CanonicalProvenance(string ChangeSetId, string OntologyVersion)
{
    public void Validate()
    {
        CanonicalKnowledgeRevisionValidator.ValidateRequired(
            ChangeSetId,
            nameof(ChangeSetId));
        CanonicalKnowledgeRevisionValidator.ValidateRequired(
            OntologyVersion,
            nameof(OntologyVersion));
    }
}

public sealed record CanonicalKnowledgeRevision
{
    public string KnowledgeItemId { get; init; } = string.Empty;

    public string RevisionId { get; init; } = string.Empty;

    public string DocumentType { get; init; } = string.Empty;

    public string Title { get; init; } = string.Empty;

    public CanonicalKnowledgeItemReference? PrimaryParent { get; init; }

    public IReadOnlyList<CanonicalRelationship> Relationships { get; init; } = [];

    public JsonElement Metadata { get; init; } = EmptyObject();

    public JsonElement Extensions { get; init; } = EmptyObject();

    public CanonicalProvenance Provenance { get; init; } = new(string.Empty, string.Empty);

    public string Body { get; init; } = string.Empty;

    public void Validate()
    {
        CanonicalKnowledgeRevisionValidator.ValidateRequired(
            KnowledgeItemId,
            nameof(KnowledgeItemId));
        CanonicalKnowledgeRevisionValidator.ValidateRequired(RevisionId, nameof(RevisionId));
        CanonicalKnowledgeRevisionValidator.ValidateRequired(
            DocumentType,
            nameof(DocumentType));
        CanonicalKnowledgeRevisionValidator.ValidateRequired(Title, nameof(Title));

        PrimaryParent?.Validate();

        ArgumentNullException.ThrowIfNull(Relationships);
        foreach (var relationship in Relationships)
        {
            ArgumentNullException.ThrowIfNull(relationship);
            relationship.Validate();
        }

        ValidateObject(Metadata, "Metadata");
        ValidateObject(Extensions, "Extensions");

        foreach (var extension in Extensions.EnumerateObject())
        {
            CanonicalKnowledgeRevisionValidator.ValidateRequired(
                extension.Name,
                "extension namespace");

            if (extension.Value.ValueKind != JsonValueKind.Object)
            {
                throw new CanonicalMarkdownValidationException(
                    $"The extension namespace '{extension.Name}' must contain an object.");
            }
        }

        ArgumentNullException.ThrowIfNull(Provenance);
        Provenance.Validate();
        ArgumentNullException.ThrowIfNull(Body);
    }

    private static JsonElement EmptyObject() =>
        JsonSerializer.SerializeToElement(new Dictionary<string, object?>());

    private static void ValidateObject(JsonElement value, string valueName)
    {
        if (value.ValueKind != JsonValueKind.Object)
        {
            throw new CanonicalMarkdownValidationException(
                $"The {valueName} value must be an object.");
        }
    }
}

internal static class CanonicalKnowledgeRevisionValidator
{
    public static void ValidateRequired(string? value, string valueName)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new CanonicalMarkdownValidationException(
                $"The {valueName} value is required.");
        }
    }

    public static void ValidateOptional(string? value, string valueName)
    {
        if (value is not null && string.IsNullOrWhiteSpace(value))
        {
            throw new CanonicalMarkdownValidationException(
                $"The {valueName} value cannot be empty.");
        }
    }
}

public sealed class CanonicalMarkdownValidationException : InvalidOperationException
{
    public CanonicalMarkdownValidationException(string message)
        : base(message)
    {
    }

    public CanonicalMarkdownValidationException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}
