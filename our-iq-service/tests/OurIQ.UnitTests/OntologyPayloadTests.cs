using System.Text.Json;
using OurIQ.Domain;

namespace OurIQ.UnitTests;

[TestClass]
public sealed class OntologyPayloadTests
{
    private const string TemplateDigest =
        "aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa";

    [TestMethod]
    public void ValidateAcceptsPayloadContainingEveryContractSection()
    {
        var payload = CreateValidPayload();

        payload.Validate(new OntologyIdentity("ontology-product", "ontology-product-v1"));

        Assert.AreEqual("decision-record", payload.DocumentTypes[0].DocumentTypeId);
        Assert.AreEqual("supersedes", payload.RelationshipTypes[0].RelationshipTypeId);
        Assert.AreEqual(OntologyRuleLevel.Required, payload.Rules[0].Level);
        Assert.AreEqual("metadata.status", payload.FilterableFields[0].Path);
        Assert.AreEqual("decision-template", payload.TemplateReferences[0].TemplateId);
    }

    [TestMethod]
    public void ValidateRejectsMismatchedExpectedIdentity()
    {
        var payload = CreateValidPayload();

        Assert.Throws<OntologyPayloadValidationException>(
            () => payload.Validate(new OntologyIdentity("ontology-product", "ontology-product-v2")));
    }

    [TestMethod]
    public void ValidateRejectsDuplicateStableIdentifiers()
    {
        var duplicateDocumentType = CreateValidPayload() with
        {
            DocumentTypes =
            [
                CreateDocumentType(),
                CreateDocumentType()
            ]
        };
        var duplicateRelationshipType = CreateValidPayload() with
        {
            RelationshipTypes =
            [
                new OntologyRelationshipType("supersedes", ["decision-record"], ["decision-record"]),
                new OntologyRelationshipType("supersedes", ["decision-record"], ["decision-record"])
            ]
        };
        var duplicateRule = CreateValidPayload() with
        {
            Rules =
            [
                new OntologyRule("decision-status-required", OntologyRuleLevel.Required, "First."),
                new OntologyRule("decision-status-required", OntologyRuleLevel.Recommended, "Second.")
            ]
        };
        var duplicateFilter = CreateValidPayload() with
        {
            FilterableFields =
            [
                new OntologyFilterableField("metadata.status", "string"),
                new OntologyFilterableField("metadata.status", "string")
            ]
        };
        var duplicateTemplate = CreateValidPayload() with
        {
            TemplateReferences =
            [
                new OntologyTemplateReference(
                    "decision-template",
                    "decision-template-v1",
                    "text/markdown",
                    TemplateDigest,
                    "templates/first.md"),
                new OntologyTemplateReference(
                    "decision-template",
                    "decision-template-v1",
                    "text/markdown",
                    TemplateDigest,
                    "templates/second.md")
            ]
        };

        Assert.Throws<OntologyPayloadValidationException>(() => duplicateDocumentType.Validate());
        Assert.Throws<OntologyPayloadValidationException>(() => duplicateRelationshipType.Validate());
        Assert.Throws<OntologyPayloadValidationException>(() => duplicateRule.Validate());
        Assert.Throws<OntologyPayloadValidationException>(() => duplicateFilter.Validate());
        Assert.Throws<OntologyPayloadValidationException>(() => duplicateTemplate.Validate());
    }

    [TestMethod]
    public void ValidateRejectsUndeclaredHierarchyAndRelationshipDocumentTypes()
    {
        var hierarchyPayload = CreateValidPayload() with
        {
            Hierarchy = new OntologyHierarchy(["unknown"], [])
        };
        var relationshipPayload = CreateValidPayload() with
        {
            RelationshipTypes =
            [
                new OntologyRelationshipType("supersedes", ["decision-record"], ["unknown"], 1)
            ]
        };

        Assert.Throws<OntologyPayloadValidationException>(() => hierarchyPayload.Validate());
        Assert.Throws<OntologyPayloadValidationException>(() => relationshipPayload.Validate());
    }

    [TestMethod]
    public void CanonicalizeMatchesRfc8785PrimitiveAndOrderingExample()
    {
        var json = """
            {
              "numbers": [333333333.33333329, 1E30, 4.50, 2e-3, 0.000000000000000000000000001],
              "string": "\u20ac$\u000f\nA'B\"\\\\\"/",
              "literals": [null, true, false]
            }
            """;

        var canonical = JsonCanonicalizer.Canonicalize(Parse(json));

        Assert.AreEqual(
            """{"literals":[null,true,false],"numbers":[333333333.3333333,1e+30,4.5,0.002,1e-27],"string":"€$\u000f\nA'B\"\\\\\"/"}""",
            canonical);
    }

    [TestMethod]
    public void CanonicalizeOrdersEquivalentObjectPropertiesIdentically()
    {
        var first = JsonCanonicalizer.Canonicalize(Parse("""{"z":1,"a":{"y":true,"x":null}}"""));
        var second = JsonCanonicalizer.Canonicalize(Parse("""{"a":{"x":null,"y":true},"z":1}"""));

        Assert.AreEqual("""{"a":{"x":null,"y":true},"z":1}""", first);
        Assert.AreEqual(first, second);
    }

    [TestMethod]
    public void CanonicalizeUsesDecimalNotationInsideRfc8785ExponentThresholds()
    {
        var canonical = JsonCanonicalizer.Canonicalize(
            Parse("""[0.000001,0.0000001,100000000000000000000,1e21]"""));

        Assert.AreEqual("""[0.000001,1e-7,100000000000000000000,1e+21]""", canonical);
    }

    [TestMethod]
    public void ComputeReturnsStableLowercaseSha256Digest()
    {
        var digest = OntologyPayloadDigest.Compute(CreateValidPayload());

        Assert.AreEqual(
            "d797c62f1349e9dc71e1e3f3d28ee7ee742f9de8ee625334ded0a754630afbc1",
            digest);
    }

    private static OntologyPayload CreateValidPayload() =>
        new()
        {
            OntologyId = "ontology-product",
            OntologyVersionId = "ontology-product-v1",
            Title = "Product knowledge",
            Description = "Structures product decisions and requirements.",
            DocumentTypes = [CreateDocumentType()],
            Hierarchy = new OntologyHierarchy(["decision-record"], []),
            RelationshipTypes =
            [
                new OntologyRelationshipType(
                    "supersedes",
                    ["decision-record"],
                    ["decision-record"],
                    1)
            ],
            Rules =
            [
                new OntologyRule(
                    "decision-status-required",
                    OntologyRuleLevel.Required,
                    "Every decision must expose its lifecycle state.")
            ],
            FilterableFields = [new OntologyFilterableField("metadata.status", "string")],
            TemplateReferences =
            [
                new OntologyTemplateReference(
                    "decision-template",
                    "decision-template-v1",
                    "text/markdown",
                    TemplateDigest,
                    "templates/decision-template-v1.md")
            ]
        };

    private static OntologyDocumentType CreateDocumentType() =>
        new(
            "decision-record",
            "A governed product or architecture decision.",
            Parse(
                """
                {
                  "$schema": "https://json-schema.org/draft/2020-12/schema",
                  "type": "object",
                  "required": ["status"],
                  "properties": {
                    "status": {
                      "type": "string",
                      "enum": ["proposed", "accepted", "superseded", "deprecated"]
                    }
                  },
                  "additionalProperties": false
                }
                """));

    private static JsonElement Parse(string json)
    {
        using var document = JsonDocument.Parse(json);
        return document.RootElement.Clone();
    }
}
