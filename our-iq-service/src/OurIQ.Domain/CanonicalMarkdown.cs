using System.Globalization;
using System.Numerics;
using System.Text;
using System.Text.Json;
using YamlDotNet.Core;
using YamlDotNet.Core.Events;
using YamlDotNet.RepresentationModel;
using YamlDotNet.Serialization;

namespace OurIQ.Domain;

public static class CanonicalMarkdown
{
    private const int MaximumStructuredValueDepth = 64;

    private static readonly IReadOnlySet<string> TopLevelFields = new HashSet<string>(
        [
            "knowledge_item_id",
            "revision_id",
            "document_type",
            "title",
            "primary_parent",
            "relationships",
            "metadata",
            "extensions",
            "provenance"
        ],
        StringComparer.Ordinal);

    public static CanonicalKnowledgeRevision Parse(string markdown)
    {
        ArgumentNullException.ThrowIfNull(markdown);

        var normalizedMarkdown = NormalizeLineEndings(markdown);
        var (frontMatter, body) = SplitDocument(normalizedMarkdown);
        var root = ParseFrontMatter(frontMatter);
        var fields = ReadMapping(root, "front matter", TopLevelFields);

        var revision = new CanonicalKnowledgeRevision
        {
            KnowledgeItemId = ReadRequiredString(fields, "knowledge_item_id"),
            RevisionId = ReadRequiredString(fields, "revision_id"),
            DocumentType = ReadRequiredString(fields, "document_type"),
            Title = ReadRequiredString(fields, "title"),
            PrimaryParent = ReadPrimaryParent(fields),
            Relationships = ReadRelationships(fields),
            Metadata = ReadObject(fields, "metadata"),
            Extensions = ReadObject(fields, "extensions"),
            Provenance = ReadProvenance(fields),
            Body = body
        };

        revision.Validate();
        return revision;
    }

    public static string Serialize(CanonicalKnowledgeRevision revision)
    {
        ArgumentNullException.ThrowIfNull(revision);
        revision.Validate();

        var frontMatter = new Dictionary<string, object?>(StringComparer.Ordinal)
        {
            ["knowledge_item_id"] = revision.KnowledgeItemId,
            ["revision_id"] = revision.RevisionId,
            ["document_type"] = revision.DocumentType,
            ["title"] = revision.Title
        };

        if (revision.PrimaryParent is not null)
        {
            frontMatter["primary_parent"] = new Dictionary<string, object?>(StringComparer.Ordinal)
            {
                ["knowledge_item_id"] = revision.PrimaryParent.KnowledgeItemId
            };
        }

        frontMatter["relationships"] = revision.Relationships
            .Select(SerializeRelationship)
            .ToArray();
        frontMatter["metadata"] = ConvertJsonValue(revision.Metadata);
        frontMatter["extensions"] = ConvertJsonValue(revision.Extensions);
        frontMatter["provenance"] = new Dictionary<string, object?>(StringComparer.Ordinal)
        {
            ["change_set_id"] = revision.Provenance.ChangeSetId,
            ["ontology_version"] = revision.Provenance.OntologyVersion
        };

        var serializer = new SerializerBuilder()
            .DisableAliases()
            .Build();
        var yaml = NormalizeLineEndings(serializer.Serialize(frontMatter)).TrimEnd('\n');
        var body = NormalizeLineEndings(revision.Body);

        return $"---\n{yaml}\n---\n{body}";
    }

    private static Dictionary<string, object?> SerializeRelationship(
        CanonicalRelationship relationship)
    {
        var target = new Dictionary<string, object?>(StringComparer.Ordinal);
        if (!string.IsNullOrWhiteSpace(relationship.Target.KnowledgeItemId))
        {
            target["knowledge_item_id"] = relationship.Target.KnowledgeItemId;
        }
        else
        {
            target["unresolved_concept"] = relationship.Target.UnresolvedConcept;
        }

        var serialized = new Dictionary<string, object?>(StringComparer.Ordinal)
        {
            ["type"] = relationship.Type,
            ["target"] = target
        };

        if (relationship.Qualifier is not null)
        {
            serialized["qualifier"] = ConvertJsonValue(relationship.Qualifier.Value);
        }

        if (relationship.AssertionStatus is not null)
        {
            serialized["assertion_status"] = relationship.AssertionStatus;
        }

        if (relationship.Note is not null)
        {
            serialized["note"] = relationship.Note;
        }

        return serialized;
    }

    private static (string FrontMatter, string Body) SplitDocument(string markdown)
    {
        if (!markdown.StartsWith("---\n", StringComparison.Ordinal))
        {
            throw new CanonicalMarkdownValidationException(
                "Canonical Markdown must start with a front-matter delimiter.");
        }

        var delimiterStart = FindClosingDelimiter(markdown);
        if (delimiterStart < 0)
        {
            throw new CanonicalMarkdownValidationException(
                "Canonical Markdown must contain a closing front-matter delimiter.");
        }

        var delimiterEnd = delimiterStart + 4;
        var bodyStart = delimiterEnd < markdown.Length && markdown[delimiterEnd] == '\n'
            ? delimiterEnd + 1
            : delimiterEnd;

        return (
            markdown[4..delimiterStart],
            markdown[bodyStart..]);
    }

    private static int FindClosingDelimiter(string markdown)
    {
        var searchStart = 3;

        while (searchStart < markdown.Length)
        {
            var delimiterStart = markdown.IndexOf("\n---", searchStart, StringComparison.Ordinal);
            if (delimiterStart < 0)
            {
                return -1;
            }

            var delimiterEnd = delimiterStart + 4;
            if (delimiterEnd == markdown.Length || markdown[delimiterEnd] == '\n')
            {
                return delimiterStart;
            }

            searchStart = delimiterEnd;
        }

        return -1;
    }

    private static YamlMappingNode ParseFrontMatter(string frontMatter)
    {
        try
        {
            var stream = new YamlStream();
            using var reader = new StringReader(frontMatter);
            stream.Load(reader);

            if (stream.Documents.Count != 1
                || stream.Documents[0].RootNode is not YamlMappingNode root)
            {
                throw new CanonicalMarkdownValidationException(
                    "Canonical front matter must contain exactly one mapping.");
            }

            ValidateYamlFeatures(root);
            return root;
        }
        catch (CanonicalMarkdownValidationException)
        {
            throw;
        }
        catch (Exception exception) when (
            exception is YamlException or ArgumentException or InvalidOperationException)
        {
            throw new CanonicalMarkdownValidationException(
                "Canonical front matter is not valid YAML.",
                exception);
        }
    }

    private static IReadOnlyDictionary<string, YamlNode> ReadMapping(
        YamlMappingNode mapping,
        string context,
        IReadOnlySet<string>? allowedFields = null)
    {
        var values = new Dictionary<string, YamlNode>(StringComparer.Ordinal);

        foreach (var pair in mapping.Children)
        {
            if (pair.Key is not YamlScalarNode { Value: not null } key
                || string.IsNullOrWhiteSpace(key.Value))
            {
                throw new CanonicalMarkdownValidationException(
                    $"Every field in {context} must have a non-empty string name.");
            }

            if (!values.TryAdd(key.Value, pair.Value))
            {
                throw new CanonicalMarkdownValidationException(
                    $"The field '{key.Value}' is declared more than once in {context}.");
            }

            if (allowedFields is not null && !allowedFields.Contains(key.Value))
            {
                throw new CanonicalMarkdownValidationException(
                    $"The field '{key.Value}' is not declared for {context}.");
            }
        }

        return values;
    }

    private static string ReadRequiredString(
        IReadOnlyDictionary<string, YamlNode> fields,
        string fieldName)
    {
        if (!fields.TryGetValue(fieldName, out var node)
            || node is not YamlScalarNode { Value: not null } scalar
            || string.IsNullOrWhiteSpace(scalar.Value)
            || IsPlainJsonPrimitive(scalar))
        {
            throw new CanonicalMarkdownValidationException(
                $"The '{fieldName}' field must be a non-empty string.");
        }

        return scalar.Value;
    }

    private static CanonicalKnowledgeItemReference? ReadPrimaryParent(
        IReadOnlyDictionary<string, YamlNode> fields)
    {
        if (!fields.TryGetValue("primary_parent", out var node) || IsNull(node))
        {
            return null;
        }

        if (node is not YamlMappingNode mapping)
        {
            throw new CanonicalMarkdownValidationException(
                "The 'primary_parent' field must be a reference mapping.");
        }

        var parent = ReadMapping(
            mapping,
            "primary_parent",
            new HashSet<string>(["knowledge_item_id"], StringComparer.Ordinal));
        return new CanonicalKnowledgeItemReference(
            ReadRequiredString(parent, "knowledge_item_id"));
    }

    private static IReadOnlyList<CanonicalRelationship> ReadRelationships(
        IReadOnlyDictionary<string, YamlNode> fields)
    {
        if (!fields.TryGetValue("relationships", out var node) || IsNull(node))
        {
            return [];
        }

        if (node is not YamlSequenceNode sequence)
        {
            throw new CanonicalMarkdownValidationException(
                "The 'relationships' field must be a sequence.");
        }

        var relationships = new List<CanonicalRelationship>(sequence.Children.Count);
        foreach (var relationshipNode in sequence.Children)
        {
            if (relationshipNode is not YamlMappingNode mapping)
            {
                throw new CanonicalMarkdownValidationException(
                    "Every relationship must be a mapping.");
            }

            var relationship = ReadMapping(
                mapping,
                "relationship",
                new HashSet<string>(
                    ["type", "target", "qualifier", "assertion_status", "note"],
                    StringComparer.Ordinal));

            if (!relationship.TryGetValue("target", out var targetNode)
                || targetNode is not YamlMappingNode targetMapping)
            {
                throw new CanonicalMarkdownValidationException(
                    "Every relationship must contain a target mapping.");
            }

            var target = ReadMapping(
                targetMapping,
                "relationship target",
                new HashSet<string>(
                    ["knowledge_item_id", "unresolved_concept"],
                    StringComparer.Ordinal));
            var knowledgeItemId = ReadOptionalString(target, "knowledge_item_id");
            var unresolvedConcept = ReadOptionalString(target, "unresolved_concept");

            relationships.Add(
                new CanonicalRelationship(
                    ReadRequiredString(relationship, "type"),
                    new CanonicalRelationshipTarget(knowledgeItemId, unresolvedConcept),
                    ReadOptionalString(relationship, "note"),
                    ReadOptionalJson(relationship, "qualifier"),
                    ReadOptionalString(relationship, "assertion_status")));
        }

        return relationships;
    }

    private static CanonicalProvenance ReadProvenance(
        IReadOnlyDictionary<string, YamlNode> fields)
    {
        if (!fields.TryGetValue("provenance", out var node)
            || node is not YamlMappingNode mapping)
        {
            throw new CanonicalMarkdownValidationException(
                "The 'provenance' field must be a mapping.");
        }

        var provenance = ReadMapping(
            mapping,
            "provenance",
            new HashSet<string>(
                ["change_set_id", "ontology_version"],
                StringComparer.Ordinal));
        return new CanonicalProvenance(
            ReadRequiredString(provenance, "change_set_id"),
            ReadRequiredString(provenance, "ontology_version"));
    }

    private static JsonElement ReadObject(
        IReadOnlyDictionary<string, YamlNode> fields,
        string fieldName)
    {
        if (!fields.TryGetValue(fieldName, out var node) || IsNull(node))
        {
            return JsonSerializer.SerializeToElement(
                new Dictionary<string, object?>(StringComparer.Ordinal));
        }

        if (node is not YamlMappingNode)
        {
            throw new CanonicalMarkdownValidationException(
                $"The '{fieldName}' field must be a mapping.");
        }

        return ConvertYamlValue(node);
    }

    private static string? ReadOptionalString(
        IReadOnlyDictionary<string, YamlNode> fields,
        string fieldName)
    {
        if (!fields.TryGetValue(fieldName, out var node) || IsNull(node))
        {
            return null;
        }

        if (node is not YamlScalarNode { Value: not null } scalar
            || string.IsNullOrWhiteSpace(scalar.Value)
            || IsPlainJsonPrimitive(scalar))
        {
            throw new CanonicalMarkdownValidationException(
                $"The '{fieldName}' field must be a non-empty string when present.");
        }

        return scalar.Value;
    }

    private static JsonElement? ReadOptionalJson(
        IReadOnlyDictionary<string, YamlNode> fields,
        string fieldName) =>
        fields.TryGetValue(fieldName, out var node) && !IsNull(node)
            ? ConvertYamlValue(node)
            : null;

    private static JsonElement ConvertYamlValue(YamlNode node)
    {
        using var stream = new MemoryStream();
        using (var writer = new Utf8JsonWriter(stream))
        {
            WriteJsonValue(writer, node, 0);
        }

        using var document = JsonDocument.Parse(stream.ToArray());
        return document.RootElement.Clone();
    }

    private static void WriteJsonValue(Utf8JsonWriter writer, YamlNode node, int depth)
    {
        if (depth > MaximumStructuredValueDepth)
        {
            throw new CanonicalMarkdownValidationException(
                $"Structured front-matter values cannot exceed {MaximumStructuredValueDepth} levels.");
        }

        switch (node)
        {
            case YamlMappingNode mapping:
                writer.WriteStartObject();
                var names = new HashSet<string>(StringComparer.Ordinal);
                foreach (var pair in mapping.Children)
                {
                    if (pair.Key is not YamlScalarNode { Value: not null } key
                        || string.IsNullOrWhiteSpace(key.Value))
                    {
                        throw new CanonicalMarkdownValidationException(
                            "Structured front-matter mappings require non-empty string keys.");
                    }

                    if (!names.Add(key.Value))
                    {
                        throw new CanonicalMarkdownValidationException(
                            $"The structured field '{key.Value}' is declared more than once.");
                    }

                    writer.WritePropertyName(key.Value);
                    WriteJsonValue(writer, pair.Value, depth + 1);
                }

                writer.WriteEndObject();
                break;

            case YamlSequenceNode sequence:
                writer.WriteStartArray();
                foreach (var child in sequence.Children)
                {
                    WriteJsonValue(writer, child, depth + 1);
                }

                writer.WriteEndArray();
                break;

            case YamlScalarNode scalar:
                WriteJsonScalar(writer, scalar);
                break;

            default:
                throw new CanonicalMarkdownValidationException(
                    "Aliases and other non-JSON YAML values are not supported.");
        }
    }

    private static void ValidateYamlFeatures(YamlNode node)
    {
        if (!node.Anchor.IsEmpty || !node.Tag.IsEmpty)
        {
            throw new CanonicalMarkdownValidationException(
                "YAML anchors, aliases, and explicit tags are not supported.");
        }

        switch (node)
        {
            case YamlMappingNode mapping:
                foreach (var pair in mapping.Children)
                {
                    ValidateYamlFeatures(pair.Key);
                    ValidateYamlFeatures(pair.Value);
                }

                break;

            case YamlSequenceNode sequence:
                foreach (var child in sequence.Children)
                {
                    ValidateYamlFeatures(child);
                }

                break;
        }
    }

    private static void WriteJsonScalar(Utf8JsonWriter writer, YamlScalarNode scalar)
    {
        var value = scalar.Value;
        if (IsNull(scalar))
        {
            writer.WriteNullValue();
            return;
        }

        if (scalar.Style == ScalarStyle.Plain
            && value is not null
            && TryWriteJsonPrimitive(writer, value))
        {
            return;
        }

        writer.WriteStringValue(value);
    }

    private static bool TryWriteJsonPrimitive(Utf8JsonWriter writer, string value)
    {
        if (value is "null")
        {
            writer.WriteNullValue();
            return true;
        }

        if (bool.TryParse(value, out var boolean))
        {
            writer.WriteBooleanValue(boolean);
            return true;
        }

        if (long.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out var integer))
        {
            writer.WriteNumberValue(integer);
            return true;
        }

        if (decimal.TryParse(
                value,
                NumberStyles.Float,
                CultureInfo.InvariantCulture,
                out var decimalNumber)
            && IsExactDecimal(value, decimalNumber))
        {
            writer.WriteNumberValue(decimalNumber);
            return true;
        }

        if (IsJsonNumber(value)
            || decimal.TryParse(
                value,
                NumberStyles.Float,
                CultureInfo.InvariantCulture,
                out _))
        {
            throw new CanonicalMarkdownValidationException(
                $"The numeric value '{value}' is outside the supported range.");
        }

        return false;
    }

    private static bool IsExactDecimal(string source, decimal parsed)
    {
        var sourceNumber = NormalizeNumber(source);
        var parsedNumber = NormalizeNumber(parsed.ToString("G29", CultureInfo.InvariantCulture));

        return sourceNumber == parsedNumber;
    }

    private static (BigInteger Coefficient, int Exponent) NormalizeNumber(string value)
    {
        var exponentSeparator = value.IndexOfAny(['e', 'E']);
        var mantissa = exponentSeparator >= 0 ? value[..exponentSeparator] : value;
        var exponent = exponentSeparator >= 0
            ? int.Parse(
                value[(exponentSeparator + 1)..],
                NumberStyles.AllowLeadingSign,
                CultureInfo.InvariantCulture)
            : 0;
        var isNegative = mantissa.StartsWith('-');
        mantissa = mantissa.TrimStart('+', '-');

        var decimalPoint = mantissa.IndexOf('.');
        if (decimalPoint >= 0)
        {
            exponent -= mantissa.Length - decimalPoint - 1;
            mantissa = mantissa.Remove(decimalPoint, 1);
        }

        var coefficient = BigInteger.Parse(
            mantissa,
            NumberStyles.None,
            CultureInfo.InvariantCulture);
        if (isNegative)
        {
            coefficient = -coefficient;
        }

        if (coefficient.IsZero)
        {
            return (BigInteger.Zero, 0);
        }

        while (coefficient % 10 == 0)
        {
            coefficient /= 10;
            exponent++;
        }

        return (coefficient, exponent);
    }

    private static bool IsPlainJsonPrimitive(YamlScalarNode scalar)
    {
        if (scalar.Style != ScalarStyle.Plain || scalar.Value is null)
        {
            return false;
        }

        return IsNull(scalar)
            || bool.TryParse(scalar.Value, out _)
            || long.TryParse(
                scalar.Value,
                NumberStyles.Integer,
                CultureInfo.InvariantCulture,
                out _)
            || decimal.TryParse(
                scalar.Value,
                NumberStyles.Float,
                CultureInfo.InvariantCulture,
                out _)
            || IsJsonNumber(scalar.Value);
    }

    private static object? ConvertJsonValue(JsonElement value) =>
        value.ValueKind switch
        {
            JsonValueKind.Object => value.EnumerateObject()
                .OrderBy(property => property.Name, StringComparer.Ordinal)
                .ToDictionary(
                    property => property.Name,
                    property => ConvertJsonValue(property.Value),
                    StringComparer.Ordinal),
            JsonValueKind.Array => value.EnumerateArray().Select(ConvertJsonValue).ToArray(),
            JsonValueKind.String => value.GetString(),
            JsonValueKind.Number when value.TryGetInt64(out var integer) => integer,
            JsonValueKind.Number when value.TryGetDecimal(out var decimalNumber) => decimalNumber,
            JsonValueKind.Number => value.GetDouble(),
            JsonValueKind.True => true,
            JsonValueKind.False => false,
            JsonValueKind.Null => null,
            _ => throw new CanonicalMarkdownValidationException(
                "Structured front-matter values must be JSON-compatible.")
        };

    private static bool IsNull(YamlNode node) =>
        node is YamlScalarNode scalar
        && (scalar.Value is null
            || scalar.Style == ScalarStyle.Plain
            && (scalar.Value.Length == 0
                || scalar.Value is "~"
                || string.Equals(scalar.Value, "null", StringComparison.OrdinalIgnoreCase)));

    private static bool IsJsonNumber(string value)
    {
        try
        {
            using var document = JsonDocument.Parse(value);
            return document.RootElement.ValueKind == JsonValueKind.Number;
        }
        catch (JsonException)
        {
            return false;
        }
    }

    private static string NormalizeLineEndings(string value) =>
        value.Replace("\r\n", "\n", StringComparison.Ordinal).Replace('\r', '\n');
}
