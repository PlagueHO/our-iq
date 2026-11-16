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
        retry["executionContext"]!["correlationId"] = "retry-correlation";
        retry["identity"]!["initiatingUserId"] = "USER-001";
        var reordered = new JsonObject(retry.Reverse()
            .Select(property => KeyValuePair.Create(property.Key, property.Value?.DeepClone())));

        Assert.AreEqual(Identity(first), Identity(reordered, user: "USER-001"));
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
