using System.Text.Json;
using OurIQ.Domain;

namespace OurIQ.UnitTests;

[TestClass]
public sealed class CanonicalMarkdownTests
{
    public static IEnumerable<object[]> InvalidFixtures =>
        Directory.EnumerateFiles(FixtureDirectory, "invalid-*.md")
            .Order(StringComparer.Ordinal)
            .Select(path => new object[] { Path.GetFileName(path) });

    private static string FixtureDirectory =>
        Path.Combine(AppContext.BaseDirectory, "TestData", "CanonicalMarkdown");

    [TestMethod]
    public void ParseRepresentsCompleteLogicalKnowledgeModel()
    {
        var revision = CanonicalMarkdown.Parse(
            ReadFixture("valid-complete.md").TrimEnd('\r', '\n'));

        Assert.AreEqual("ki-product-decisions", revision.KnowledgeItemId);
        Assert.AreEqual("rev-2026-08-18-001", revision.RevisionId);
        Assert.AreEqual("decision-record", revision.DocumentType);
        Assert.AreEqual("Prefer evidence before synthesis", revision.Title);
        Assert.AreEqual("ki-product", revision.PrimaryParent!.KnowledgeItemId);
        Assert.HasCount(2, revision.Relationships);
        Assert.AreEqual("ki-grounded-evidence", revision.Relationships[0].Target.KnowledgeItemId);
        Assert.AreEqual("retrieval usability", revision.Relationships[1].Target.UnresolvedConcept);
        Assert.AreEqual("asserted", revision.Relationships[0].AssertionStatus);
        Assert.AreEqual(
            0.9m,
            revision.Relationships[0].Qualifier!.Value
                .GetProperty("confidence")
                .GetDecimal());
        Assert.AreEqual("accepted", revision.Metadata.GetProperty("status").GetString());
        Assert.IsTrue(revision.Metadata.GetProperty("reviewed").GetBoolean());
        Assert.AreEqual(
            "knowledge-stewards",
            revision.Extensions
                .GetProperty("example.org")
                .GetProperty("owner_group")
                .GetString());
        Assert.AreEqual("cs-2026-08-18-015", revision.Provenance.ChangeSetId);
        Assert.AreEqual("ontology-product-v3", revision.Provenance.OntologyVersion);
        Assert.AreEqual(
            "# Evidence first\n\nKeep this line's trailing spaces.  \n\n---\n\nEnd without a newline.",
            revision.Body);
    }

    [TestMethod]
    public void ParseDefaultsOptionalCollectionsAndReferences()
    {
        var revision = CanonicalMarkdown.Parse(ReadFixture("valid-minimal.md"));

        Assert.IsNull(revision.PrimaryParent);
        Assert.IsEmpty(revision.Relationships);
        Assert.AreEqual(JsonValueKind.Object, revision.Metadata.ValueKind);
        Assert.AreEqual(0, revision.Metadata.GetPropertyCount());
        Assert.AreEqual(JsonValueKind.Object, revision.Extensions.ValueKind);
        Assert.AreEqual(0, revision.Extensions.GetPropertyCount());
        Assert.EndsWith("Minimal body.\n", revision.Body, StringComparison.Ordinal);
    }

    [TestMethod]
    public void ParseNormalizesOnlyBodyLineEndings()
    {
        var fixture = ReadFixture("valid-complete.md")
            .Replace("\r\n", "\n", StringComparison.Ordinal)
            .TrimEnd('\n');
        var mixedLineEndings = fixture
            .Replace("\n", "\r\n", StringComparison.Ordinal)
            .Replace(
                "Keep this line's trailing spaces.  \r\n",
                "Keep this line's trailing spaces.  \r",
                StringComparison.Ordinal);

        var revision = CanonicalMarkdown.Parse(mixedLineEndings);

        Assert.AreEqual(
            "# Evidence first\n\nKeep this line's trailing spaces.  \n\n---\n\nEnd without a newline.",
            revision.Body);
    }

    [TestMethod]
    public void SerializeProducesStableSemanticRoundTrip()
    {
        var parsed = CanonicalMarkdown.Parse(
            ReadFixture("valid-complete.md").TrimEnd('\r', '\n'));

        var serialized = CanonicalMarkdown.Serialize(parsed);
        var reparsed = CanonicalMarkdown.Parse(serialized);

        Assert.AreEqual(serialized, CanonicalMarkdown.Serialize(reparsed));
        Assert.AreEqual(parsed.KnowledgeItemId, reparsed.KnowledgeItemId);
        Assert.AreEqual(parsed.RevisionId, reparsed.RevisionId);
        Assert.AreEqual(parsed.DocumentType, reparsed.DocumentType);
        Assert.AreEqual(parsed.Title, reparsed.Title);
        Assert.AreEqual(parsed.Body, reparsed.Body);
        Assert.IsTrue(JsonElement.DeepEquals(parsed.Metadata, reparsed.Metadata));
        Assert.IsTrue(JsonElement.DeepEquals(parsed.Extensions, reparsed.Extensions));
    }

    [TestMethod]
    [DynamicData(nameof(InvalidFixtures))]
    public void ParseRejectsInvalidFixture(string fixtureName)
    {
        var fixture = ReadFixture(fixtureName);

        Assert.Throws<CanonicalMarkdownValidationException>(
            () => CanonicalMarkdown.Parse(fixture));
    }

    [TestMethod]
    public void SerializeRejectsAmbiguousRelationshipTarget()
    {
        var revision = CanonicalMarkdown.Parse(ReadFixture("valid-minimal.md")) with
        {
            Relationships =
            [
                new CanonicalRelationship(
                    "related-to",
                    new CanonicalRelationshipTarget("ki-target", "target concept"))
            ]
        };

        Assert.Throws<CanonicalMarkdownValidationException>(
            () => CanonicalMarkdown.Serialize(revision));
    }

    [TestMethod]
    public void SerializeRejectsWhitespaceRelationshipTarget()
    {
        var revision = CanonicalMarkdown.Parse(ReadFixture("valid-minimal.md")) with
        {
            Relationships =
            [
                new CanonicalRelationship(
                    "related-to",
                    new CanonicalRelationshipTarget("   ", "target concept"))
            ]
        };

        Assert.Throws<CanonicalMarkdownValidationException>(
            () => CanonicalMarkdown.Serialize(revision));
    }

    [TestMethod]
    public void SerializeOrdersStructuredObjectProperties()
    {
        var revision = CanonicalMarkdown.Parse(ReadFixture("valid-minimal.md")) with
        {
            Metadata = JsonSerializer.SerializeToElement(
                new Dictionary<string, object?>
                {
                    ["z"] = 1,
                    ["a"] = new Dictionary<string, object?>
                    {
                        ["y"] = true,
                        ["x"] = null
                    }
                })
        };

        var serialized = CanonicalMarkdown.Serialize(revision);

        StringAssert.Contains(
            serialized,
            "metadata:\n  a:\n    x: \n    y: true\n  z: 1",
            StringComparison.Ordinal);
    }

    [TestMethod]
    public void ParseTreatsYamlNullFormsAsAbsentOptionalValues()
    {
        var fixture = ReadFixture("valid-minimal.md").Replace(
            "provenance:",
            "primary_parent: ~\nrelationships:\nmetadata: null\nextensions:\nprovenance:",
            StringComparison.Ordinal);

        var revision = CanonicalMarkdown.Parse(fixture);

        Assert.IsNull(revision.PrimaryParent);
        Assert.IsEmpty(revision.Relationships);
        Assert.AreEqual(0, revision.Metadata.GetPropertyCount());
        Assert.AreEqual(0, revision.Extensions.GetPropertyCount());
    }

    private static string ReadFixture(string fixtureName) =>
        File.ReadAllText(Path.Combine(FixtureDirectory, fixtureName));
}
