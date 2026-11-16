using System.Text.Json;
using System.Text.Json.Nodes;
using OurIQ.Domain;

namespace OurIQ.UnitTests;

[TestClass]
public sealed class IdempotencyTests
{
    [TestMethod]
    public void EquivalentJsonAndRetryCorrelationProduceTheSameIdentity()
    {
        var first = Request("""{"count":1.0,"values":["a","b"]}""");
        var retry = Request("""{ "values": [ "a", "b" ], "count": 1e0 }""");
        first["identity"]!["initiatingUserId"] = "abcdefab-1234-5678-90ab-abcdefabcdef";
        retry["executionContext"]!["correlationId"] = "retry-correlation";
        retry["identity"]!["initiatingUserId"] = "{ABCDEFAB-1234-5678-90AB-ABCDEFABCDEF}";
        var reordered = new JsonObject(retry.Reverse()
            .Select(property => KeyValuePair.Create(property.Key, property.Value?.DeepClone())));

        Assert.AreEqual(
            Identity(first, user: "abcdefab-1234-5678-90ab-abcdefabcdef"),
            Identity(reordered, user: "{ABCDEFAB-1234-5678-90AB-ABCDEFABCDEF}"));
    }

    [TestMethod]
    [DataRow("9007199254740993", "9007199254740993.000")]
    [DataRow("9007199254740993", "9.007199254740993e15")]
    [DataRow("0.1234567890123456789012345678901", "123456789012345678901234567890100e-33")]
    [DataRow("1e-9999", "10e-10000")]
    [DataRow("1e-6", "0.000001000")]
    [DataRow("1e21", "1000000000000000000000")]
    [DataRow("-0.0", "0e-99999999999999999999")]
    public void EquivalentLosslessNumbersProduceTheSameFingerprint(string firstNumber, string retryNumber)
    {
        Assert.AreEqual(
            Identity(Request($$$"""{"frontMatterSchema":{"const":{{{firstNumber}}}}}""")),
            Identity(Request($$$"""{"frontMatterSchema":{"const":{{{retryNumber}}}}}""")));
    }

    [TestMethod]
    [DataRow("9007199254740992", "9007199254740993")]
    [DataRow("0.1234567890123456789012345678901", "0.1234567890123456789012345678902")]
    [DataRow("1e-9999", "0")]
    [DataRow("-9007199254740992", "-9007199254740993")]
    public void DistinctLosslessNumbersConflict(string originalNumber, string changedNumber)
    {
        var original = Identity(Request($$$"""{"frontMatterSchema":{"const":{{{originalNumber}}}}}"""));
        var changed = Identity(Request($$$"""{"frontMatterSchema":{"const":{{{changedNumber}}}}}"""));

        Assert.AreEqual(original.Id, changed.Id);
        Assert.AreNotEqual(original.RequestFingerprint, changed.RequestFingerprint);
        Assert.ThrowsExactly<IdempotencyKeyConflictException>(() =>
            new IdempotencyRecord(original, "{}", false, DateTimeOffset.UtcNow).Match(changed));
    }

    [TestMethod]
    public void NonGuidCallerIdsRemainCaseSensitive()
    {
        var request = Request("{}");

        Assert.AreNotEqual(Identity(request).Id, Identity(request, user: "USER-001").Id);
    }

    [TestMethod]
    [DataRow("""{"count":2,"values":["a","b"]}""")]
    [DataRow("""{"count":1,"values":["b","a"]}""")]
    [DataRow("""{"count":1,"values":["a","b"],"optional":null}""")]
    public void MeaningfulArgumentsConflict(string changedArguments)
    {
        var original = Identity(Request("""{"count":1,"values":["a","b"]}"""));
        var changed = Identity(Request(changedArguments));

        Assert.AreEqual(original.Id, changed.Id);
        Assert.AreNotEqual(original.RequestFingerprint, changed.RequestFingerprint);
        Assert.ThrowsExactly<IdempotencyKeyConflictException>(() =>
            new IdempotencyRecord(original, "{}", false, DateTimeOffset.UtcNow).Match(changed));
    }

    [TestMethod]
    [DataRow("executionContext", "executionId")]
    [DataRow("executionContext", "ontologyDigest")]
    [DataRow("identity", "agentDefinitionVersion")]
    public void PinnedContextAndDefinitionChangesRemainSignificant(string section, string field)
    {
        var request = Request("{}");
        var original = Identity(request);
        request[section]![field] = "changed";

        Assert.AreNotEqual(original.RequestFingerprint, Identity(request).RequestFingerprint);
    }

    [TestMethod]
    public void ScopeUsesOperationCallerLogicalAgentSpaceAndCaseSensitiveKey()
    {
        var request = Request("{}");
        var original = Identity(request);
        Assert.AreNotEqual(original.Id, Identity(request, operation: "record_approval").Id);
        Assert.AreNotEqual(original.Id, Identity(request, user: "user-002").Id);
        Assert.AreNotEqual(original.Id, Identity(request, agent: "agent-other").Id);
        request["idempotencyKey"] = "KEY-001";
        Assert.AreNotEqual(original.Id, Identity(request).Id);
        request["idempotencyKey"] = "key-001";
        request["knowledgeSpaceId"] = "space-other";
        var otherSpace = Identity(request);
        Assert.AreEqual(original.Id, otherSpace.Id);
        Assert.AreNotEqual(original.KnowledgeSpaceId, otherSpace.KnowledgeSpaceId);
    }

    [TestMethod]
    public void DuplicateMembersAndUnboundedNumbersAreRejected()
    {
        foreach (var arguments in new[] { """{"same":1,"same":2}""", """{"number":1e9999}""" })
        {
            using var request = JsonDocument.Parse(
                $$"""
                {
                  "idempotencyKey":"key-001",
                  "knowledgeSpaceId":"space-001",
                  "identity":{"initiatingUserId":"user-001"},
                  "executionContext":{"correlationId":"correlation"},
                  "arguments":{{arguments}}
                }
                """);
            Assert.ThrowsExactly<ArgumentException>(() =>
                IdempotencyIdentity.Create("stage_ontology_version", "user-001", "agent-ontology", request.RootElement));
        }
    }

    private static JsonObject Request(string arguments) =>
        JsonNode.Parse(
            $$"""
            {
              "contractVersion":"1.0",
              "operation":"stage_ontology_version",
              "idempotencyKey":"key-001",
              "knowledgeSpaceId":"space-001",
              "identity":{"initiatingUserId":"user-001","agentDefinitionVersion":"1.0"},
              "executionContext":{"executionId":"execution-001","ontologyDigest":null,"correlationId":"original"},
              "arguments":{{arguments}}
            }
            """)!.AsObject();

    private static IdempotencyIdentity Identity(
        JsonObject request,
        string operation = "stage_ontology_version",
        string user = "user-001",
        string agent = "agent-ontology") =>
        IdempotencyIdentity.Create(operation, user, agent, JsonSerializer.SerializeToElement(request));
}
