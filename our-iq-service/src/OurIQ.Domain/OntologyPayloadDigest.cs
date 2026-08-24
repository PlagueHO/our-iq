using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace OurIQ.Domain;

public static class OntologyPayloadDigest
{
    public static string Compute(OntologyPayload payload)
    {
        ArgumentNullException.ThrowIfNull(payload);
        var canonicalPayload = Canonicalize(payload);
        var digest = SHA256.HashData(Encoding.UTF8.GetBytes(canonicalPayload));
        return Convert.ToHexString(digest).ToLowerInvariant();
    }

    public static string Canonicalize(OntologyPayload payload)
    {
        ArgumentNullException.ThrowIfNull(payload);
        payload.Validate();
        return JsonCanonicalizer.Canonicalize(ToJson(payload));
    }

    private static JsonElement ToJson(OntologyPayload payload) =>
        JsonSerializer.SerializeToElement(
            new
            {
                ontologyId = payload.OntologyId,
                ontologyVersionId = payload.OntologyVersionId,
                title = payload.Title,
                description = payload.Description,
                documentTypes = payload.DocumentTypes.Select(
                    documentType => new
                    {
                        documentTypeId = documentType.DocumentTypeId,
                        description = documentType.Description,
                        frontMatterSchema = documentType.FrontMatterSchema
                    }),
                hierarchy = new
                {
                    roots = payload.Hierarchy.Roots,
                    allowedParents = payload.Hierarchy.AllowedParents
                },
                relationshipTypes = payload.RelationshipTypes.Select(
                    relationshipType => new
                    {
                        relationshipTypeId = relationshipType.RelationshipTypeId,
                        sourceDocumentTypes = relationshipType.SourceDocumentTypes,
                        targetDocumentTypes = relationshipType.TargetDocumentTypes,
                        maximumTargets = relationshipType.MaximumTargets
                    }),
                rules = payload.Rules.Select(
                    rule => new
                    {
                        code = rule.Code,
                        level = rule.Level.ToString().ToLowerInvariant(),
                        rationale = rule.Rationale
                    }),
                filterableFields = payload.FilterableFields.Select(
                    field => new
                    {
                        path = field.Path,
                        valueType = field.ValueType
                    }),
                templateReferences = payload.TemplateReferences.Select(
                    template => new
                    {
                        templateId = template.TemplateId,
                        revisionId = template.RevisionId,
                        mediaType = template.MediaType,
                        contentDigest = template.ContentDigest,
                        assetReference = template.AssetReference
                    })
            });
}

public static class JsonCanonicalizer
{
    public static string Canonicalize(JsonElement value)
    {
        var builder = new StringBuilder();
        WriteValue(value, builder);
        return builder.ToString();
    }

    private static void WriteValue(JsonElement value, StringBuilder builder)
    {
        switch (value.ValueKind)
        {
            case JsonValueKind.Object:
                WriteObject(value, builder);
                return;
            case JsonValueKind.Array:
                WriteArray(value, builder);
                return;
            case JsonValueKind.String:
                WriteString(value.GetString()!, builder);
                return;
            case JsonValueKind.Number:
                builder.Append(FormatNumber(value));
                return;
            case JsonValueKind.True:
                builder.Append("true");
                return;
            case JsonValueKind.False:
                builder.Append("false");
                return;
            case JsonValueKind.Null:
                builder.Append("null");
                return;
            default:
                throw new ArgumentException(
                    $"The JSON value kind '{value.ValueKind}' cannot be canonicalized.",
                    nameof(value));
        }
    }

    private static void WriteObject(JsonElement value, StringBuilder builder)
    {
        var properties = value.EnumerateObject().ToArray();
        var duplicate = properties
            .GroupBy(property => property.Name, StringComparer.Ordinal)
            .FirstOrDefault(group => group.Count() > 1);

        if (duplicate is not null)
        {
            throw new ArgumentException(
                $"The JSON object contains duplicate property '{duplicate.Key}'.",
                nameof(value));
        }

        builder.Append('{');
        foreach (var property in properties.OrderBy(property => property.Name, StringComparer.Ordinal))
        {
            if (builder[^1] != '{')
            {
                builder.Append(',');
            }

            WriteString(property.Name, builder);
            builder.Append(':');
            WriteValue(property.Value, builder);
        }

        builder.Append('}');
    }

    private static void WriteArray(JsonElement value, StringBuilder builder)
    {
        builder.Append('[');
        foreach (var element in value.EnumerateArray())
        {
            if (builder[^1] != '[')
            {
                builder.Append(',');
            }

            WriteValue(element, builder);
        }

        builder.Append(']');
    }

    private static void WriteString(string value, StringBuilder builder)
    {
        builder.Append('"');

        for (var index = 0; index < value.Length; index++)
        {
            var character = value[index];

            switch (character)
            {
                case '"':
                    builder.Append("\\\"");
                    break;
                case '\\':
                    builder.Append("\\\\");
                    break;
                case '\b':
                    builder.Append("\\b");
                    break;
                case '\t':
                    builder.Append("\\t");
                    break;
                case '\n':
                    builder.Append("\\n");
                    break;
                case '\f':
                    builder.Append("\\f");
                    break;
                case '\r':
                    builder.Append("\\r");
                    break;
                case < '\u0020':
                    builder.Append("\\u");
                    builder.Append(((int)character).ToString("x4", CultureInfo.InvariantCulture));
                    break;
                case >= '\ud800' and <= '\udbff' when index + 1 < value.Length
                    && value[index + 1] is >= '\udc00' and <= '\udfff':
                    builder.Append(character);
                    builder.Append(value[++index]);
                    break;
                case >= '\ud800' and <= '\udfff':
                    throw new ArgumentException(
                        "The JSON string contains a surrogate code unit.",
                        nameof(value));
                default:
                    builder.Append(character);
                    break;
            }
        }

        builder.Append('"');
    }

    private static string FormatNumber(JsonElement value)
    {
        if (!value.TryGetDouble(out var number) || !double.IsFinite(number))
        {
            throw new ArgumentException(
                $"The JSON number '{value.GetRawText()}' is not an IEEE 754 finite number.",
                nameof(value));
        }

        if (number == 0)
        {
            return "0";
        }

        var roundTrip = number.ToString("R", CultureInfo.InvariantCulture);
        var exponentIndex = roundTrip.IndexOfAny(['E', 'e']);

        if (exponentIndex < 0)
        {
            return roundTrip;
        }

        var mantissa = roundTrip[..exponentIndex];
        var exponent = int.Parse(
            roundTrip[(exponentIndex + 1)..],
            NumberStyles.AllowLeadingSign,
            CultureInfo.InvariantCulture);
        var sign = mantissa.StartsWith("-", StringComparison.Ordinal) ? "-" : string.Empty;
        var unsignedMantissa = sign.Length == 0 ? mantissa : mantissa[1..];
        var digits = unsignedMantissa.Replace(".", string.Empty, StringComparison.Ordinal);
        var decimalIndex = unsignedMantissa.IndexOf('.');

        if (exponent is >= 0 and < 21)
        {
            var decimalPosition = (decimalIndex < 0 ? digits.Length : decimalIndex) + exponent;
            return sign + (decimalPosition >= digits.Length
                ? digits.PadRight(decimalPosition, '0')
                : digits.Insert(decimalPosition, "."));
        }

        if (exponent is < 0 and >= -6)
        {
            var decimalPosition = (decimalIndex < 0 ? digits.Length : decimalIndex) + exponent;
            return sign + (decimalPosition > 0
                ? digits.Insert(decimalPosition, ".")
                : $"0.{new string('0', -decimalPosition)}{digits}");
        }

        return $"{mantissa.ToLowerInvariant()}e{exponent.ToString("+0;-0", CultureInfo.InvariantCulture)}";
    }
}
