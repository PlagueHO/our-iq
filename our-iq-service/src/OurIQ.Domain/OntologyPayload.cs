using System.Text.Json;
using Json.Schema;

namespace OurIQ.Domain;

public sealed record OntologyIdentity(string OntologyId, string OntologyVersionId);

public sealed record OntologyDocumentType(
    string DocumentTypeId,
    string Description,
    JsonElement FrontMatterSchema);

public sealed record OntologyHierarchy(
    IReadOnlyList<string> Roots,
    IReadOnlyList<string> AllowedParents);

public sealed record OntologyRelationshipType(
    string RelationshipTypeId,
    IReadOnlyList<string> SourceDocumentTypes,
    IReadOnlyList<string> TargetDocumentTypes,
    int? MaximumTargets = null);

public enum OntologyRuleLevel
{
    Required,
    Recommended,
    Informational
}

public sealed record OntologyRule(
    string Code,
    OntologyRuleLevel Level,
    string Rationale);

public sealed record OntologyFilterableField(string Path, string ValueType);

public sealed record OntologyTemplateReference(
    string TemplateId,
    string RevisionId,
    string MediaType,
    string ContentDigest,
    string AssetReference);

public sealed record OntologyPayload
{
    public string OntologyId { get; init; } = string.Empty;

    public string OntologyVersionId { get; init; } = string.Empty;

    public string Title { get; init; } = string.Empty;

    public string Description { get; init; } = string.Empty;

    public IReadOnlyList<OntologyDocumentType> DocumentTypes { get; init; } = [];

    public OntologyHierarchy Hierarchy { get; init; } = new([], []);

    public IReadOnlyList<OntologyRelationshipType> RelationshipTypes { get; init; } = [];

    public IReadOnlyList<OntologyRule> Rules { get; init; } = [];

    public IReadOnlyList<OntologyFilterableField> FilterableFields { get; init; } = [];

    public IReadOnlyList<OntologyTemplateReference> TemplateReferences { get; init; } = [];

    public OntologyIdentity Identity => new(OntologyId, OntologyVersionId);

    public void Validate(OntologyIdentity? expectedIdentity = null)
    {
        ValidateRequired(OntologyId, nameof(OntologyId));
        ValidateRequired(OntologyVersionId, nameof(OntologyVersionId));
        ValidateRequired(Title, nameof(Title));
        ValidateRequired(Description, nameof(Description));

        if (expectedIdentity is not null && Identity != expectedIdentity)
        {
            throw new OntologyPayloadValidationException(
                "The ontology payload identity does not match the expected identity.");
        }

        if (DocumentTypes.Count == 0)
        {
            throw new OntologyPayloadValidationException(
                "At least one document type is required.");
        }

        ValidateDocumentTypes();
        var documentTypeIds = DocumentTypes
            .Select(documentType => documentType.DocumentTypeId)
            .ToHashSet(StringComparer.Ordinal);

        ValidateHierarchy(documentTypeIds);
        ValidateRelationshipTypes(documentTypeIds);
        ValidateRules();
        ValidateFilterableFields();
        ValidateTemplateReferences();
    }

    private void ValidateDocumentTypes()
    {
        ValidateUnique(
            DocumentTypes,
            documentType => documentType.DocumentTypeId,
            "document type identifier");

        foreach (var documentType in DocumentTypes)
        {
            ValidateRequired(documentType.DocumentTypeId, nameof(documentType.DocumentTypeId));
            ValidateRequired(documentType.Description, nameof(documentType.Description));

            if (documentType.FrontMatterSchema.ValueKind != JsonValueKind.Object)
            {
                throw new OntologyPayloadValidationException(
                    $"The front-matter schema for document type '{documentType.DocumentTypeId}' must be a JSON object.");
            }

            ValidateFrontMatterSchema(documentType);
        }
    }

    private static void ValidateFrontMatterSchema(OntologyDocumentType documentType)
    {
        var schemaVersion = documentType.FrontMatterSchema.TryGetProperty("$schema", out var schemaProperty)
            ? schemaProperty.GetString()
            : null;

        if (!string.Equals(
                schemaVersion,
                "https://json-schema.org/draft/2020-12/schema",
                StringComparison.Ordinal))
        {
            throw new OntologyPayloadValidationException(
                $"The front-matter schema for document type '{documentType.DocumentTypeId}' must declare JSON Schema 2020-12.");
        }

        try
        {
            _ = JsonSchema.FromText(documentType.FrontMatterSchema.GetRawText());
        }
        catch (Exception exception) when (exception is JsonException or JsonSchemaException)
        {
            throw new OntologyPayloadValidationException(
                $"The front-matter schema for document type '{documentType.DocumentTypeId}' is not a valid JSON Schema 2020-12 document.");
        }
    }

    private void ValidateHierarchy(IReadOnlySet<string> documentTypeIds)
    {
        ArgumentNullException.ThrowIfNull(Hierarchy);

        ValidateReferences(
            Hierarchy.Roots,
            documentTypeIds,
            "hierarchy root document type");
        ValidateReferences(
            Hierarchy.AllowedParents,
            documentTypeIds,
            "allowed parent document type");
    }

    private void ValidateRelationshipTypes(IReadOnlySet<string> documentTypeIds)
    {
        ValidateUnique(
            RelationshipTypes,
            relationshipType => relationshipType.RelationshipTypeId,
            "relationship type identifier");

        foreach (var relationshipType in RelationshipTypes)
        {
            ValidateRequired(
                relationshipType.RelationshipTypeId,
                nameof(relationshipType.RelationshipTypeId));

            if (relationshipType.MaximumTargets is <= 0)
            {
                throw new OntologyPayloadValidationException(
                    $"The maximum target count for relationship type '{relationshipType.RelationshipTypeId}' must be positive.");
            }

            ValidateReferences(
                relationshipType.SourceDocumentTypes,
                documentTypeIds,
                $"source document type for relationship '{relationshipType.RelationshipTypeId}'");
            ValidateReferences(
                relationshipType.TargetDocumentTypes,
                documentTypeIds,
                $"target document type for relationship '{relationshipType.RelationshipTypeId}'");
        }
    }

    private void ValidateRules()
    {
        ValidateUnique(Rules, rule => rule.Code, "rule code");

        foreach (var rule in Rules)
        {
            ValidateRequired(rule.Code, nameof(rule.Code));
            ValidateRequired(rule.Rationale, nameof(rule.Rationale));
        }
    }

    private void ValidateFilterableFields()
    {
        ValidateUnique(FilterableFields, field => field.Path, "filterable field path");

        foreach (var field in FilterableFields)
        {
            ValidateRequired(field.Path, nameof(field.Path));
            ValidateRequired(field.ValueType, nameof(field.ValueType));
        }
    }

    private void ValidateTemplateReferences()
    {
        ValidateUnique(
            TemplateReferences,
            template => $"{template.TemplateId}\u001f{template.RevisionId}",
            "template revision");

        foreach (var template in TemplateReferences)
        {
            ValidateRequired(template.TemplateId, nameof(template.TemplateId));
            ValidateRequired(template.RevisionId, nameof(template.RevisionId));
            ValidateRequired(template.MediaType, nameof(template.MediaType));
            ValidateRequired(template.ContentDigest, nameof(template.ContentDigest));
            ValidateRequired(template.AssetReference, nameof(template.AssetReference));

            if (!string.Equals(template.MediaType, "text/markdown", StringComparison.Ordinal))
            {
                throw new OntologyPayloadValidationException(
                    $"The template '{template.TemplateId}' must use media type 'text/markdown'.");
            }

            if (!IsLowercaseSha256(template.ContentDigest))
            {
                throw new OntologyPayloadValidationException(
                    $"The template '{template.TemplateId}' content digest must be a lowercase SHA-256 digest.");
            }
        }
    }

    private static void ValidateReferences(
        IReadOnlyList<string> references,
        IReadOnlySet<string> documentTypeIds,
        string referenceName)
    {
        ValidateUnique(references, reference => reference, referenceName);

        foreach (var reference in references)
        {
            ValidateRequired(reference, referenceName);

            if (!documentTypeIds.Contains(reference))
            {
                throw new OntologyPayloadValidationException(
                    $"The {referenceName} '{reference}' does not identify a declared document type.");
            }
        }
    }

    private static void ValidateUnique<T>(
        IEnumerable<T> values,
        Func<T, string> keySelector,
        string valueName)
    {
        var duplicate = values
            .GroupBy(keySelector, StringComparer.Ordinal)
            .FirstOrDefault(group => group.Count() > 1);

        if (duplicate is not null)
        {
            throw new OntologyPayloadValidationException(
                $"The {valueName} '{duplicate.Key}' is declared more than once.");
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

    private static bool IsLowercaseSha256(string value) =>
        value.Length == 64
        && value.All(character =>
            character is >= '0' and <= '9'
            || character is >= 'a' and <= 'f');
}

public sealed class OntologyPayloadValidationException(string message)
    : InvalidOperationException(message);
