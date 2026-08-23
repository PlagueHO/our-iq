using System.Security.Cryptography;
using System.Text.Json;
using Json.Schema;

namespace OurIQ.UnitTests;

[TestClass]
[DoNotParallelize]
public sealed class ContractSchemaTests
{
    private static readonly string ContractsRoot = Path.Combine(AppContext.BaseDirectory, "contracts");
    private static readonly Lazy<JsonSchema> PublicSchema = new(() => LoadSchema("public"));
    private static readonly Lazy<JsonSchema> PrivateSchema = new(() => LoadSchema("private"));

    [TestMethod]
    public void PublishedSchemasMatchTheirManifestDigests()
    {
        foreach (var surface in new[] { "public", "private" })
        {
            var manifestPath = Path.Combine(ContractsRoot, surface, "manifest.json");
            using var manifest = JsonDocument.Parse(File.ReadAllText(manifestPath));

            Assert.AreEqual(surface, manifest.RootElement.GetProperty("surface").GetString());

            foreach (var version in manifest.RootElement.GetProperty("contractVersions").EnumerateArray())
            {
                Assert.AreEqual("1.0", version.GetProperty("contractVersion").GetString());
                Assert.AreEqual("supported", version.GetProperty("status").GetString());

                foreach (var schema in version.GetProperty("schemas").EnumerateArray())
                {
                    var assetPath = schema.GetProperty("assetPath").GetString()!;
                    StringAssert.StartsWith(assetPath, $"contracts/{surface}/");

                    var fullPath = Path.Combine(
                        AppContext.BaseDirectory,
                        assetPath.Replace('/', Path.DirectorySeparatorChar));
                    var bytes = File.ReadAllBytes(fullPath);
                    var digest = Convert.ToHexString(SHA256.HashData(bytes)).ToLowerInvariant();

                    Assert.AreEqual(schema.GetProperty("sha256").GetString(), digest);
                    using var schemaDocument = JsonDocument.Parse(bytes);
                    var schemaName = schema.GetProperty("schemaName").GetString();
                    Assert.AreEqual(
                        $"https://our-iq.dev/contracts/{surface}/v1.0/{schemaName}",
                        schemaDocument.RootElement.GetProperty("$id").GetString());
                    _ = GetSchema(surface);
                }
            }
        }
    }

    [TestMethod]
    public void ValidPublicContributionEnvelopeIsAccepted()
    {
        var instance = """
            {
              "contractVersion": "1.0",
              "operation": "contribute_knowledge",
              "request": {
                "knowledgeSpaceId": "ks-product",
                "content": {
                  "text": "Evidence must be returned before synthesis."
                },
                "idempotencyKey": "request-001",
                "identity": {
                  "initiatingUserId": "user-001"
                }
              },
              "response": {
                "outcome": "plan_ready",
                "knowledgeSpaceId": "ks-product",
                "planId": "plan-001",
                "correlationId": "corr-001",
                "pagination": "notApplicable"
              }
            }
            """;

        AssertValid("public", instance);
    }

    [TestMethod]
    public void ValidPublicQueryEnvelopeIsAccepted()
    {
        var instance = """
            {
              "contractVersion": "1.0",
              "operation": "query_knowledge",
              "request": {
                "knowledgeSpaceId": "ks-product",
                "query": "evidence",
                "pagination": {
                  "pageSize": 20,
                  "cursor": null
                },
                "identity": {
                  "initiatingUserId": "user-001"
                }
              },
              "response": {
                "outcome": "completed",
                "knowledgeSpaceId": "ks-product",
                "completeness": "complete",
                "evidence": [],
                "pagination": {
                  "pageSize": 20,
                  "nextCursor": null
                },
                "correlationId": "corr-001"
              }
            }
            """;

        AssertValid("public", instance);
    }

    [TestMethod]
    public void ValidPrivateToolEnvelopeIsAccepted()
    {
        var instance = """
            {
              "contractVersion": "1.0",
              "operation": "validate_change_plan",
              "knowledgeSpaceId": "ks-product",
              "identity": {
                "initiatingUserId": "user-001",
                "actingAgentId": "agent-contribution",
                "agentDefinitionVersion": "1.0",
                "requiredCapability": "validate_change_plan"
              },
              "executionContext": {
                "executionId": "exec-001",
                "traceId": "trace-001",
                "correlationId": "corr-001",
                "knowledgeSpaceId": "ks-product",
                "lifecycleState": "active",
                "ontologyVersion": "ontology-product-v1",
                "ontologyDigest": "aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa",
                "mutationPolicy": "confirmation",
                "canonicalHeadVersion": "head-001"
              },
              "arguments": {
                "planId": "plan-001"
              }
            }
            """;

        AssertValid("private", instance);
    }

    [TestMethod]
    public void ValidPublicErrorEnvelopeIsAccepted()
    {
        var instance = """
            {
              "contractVersion": "1.0",
              "operation": "approve_ontology",
              "request": {
                "knowledgeSpaceId": "ks-product",
                "ontologyVersionId": "ontology-product-v1",
                "approval": {
                  "decision": "approve"
                },
                "idempotencyKey": "request-002",
                "identity": {
                  "initiatingUserId": "user-001"
                }
              },
              "response": {
                "outcome": "error",
                "knowledgeSpaceId": "ks-product",
                "correlationId": "corr-002",
                "pagination": "notApplicable",
                "error": {
                  "code": "authorization_denied",
                  "category": "authorization",
                  "message": "The caller lacks the required capability.",
                  "remediation": "Request Ontology Manager access."
                }
              }
            }
            """;

        AssertValid("public", instance);
    }

    [TestMethod]
    public void IncompatiblePublicEnvelopeIsRejected()
    {
        var instance = """
            {
              "contractVersion": "2.0",
              "operation": "create_space",
              "request": {
                "displayName": "Product",
                "identity": {
                  "initiatingUserId": "user-001"
                }
              },
              "response": {
                "outcome": "accepted",
                "knowledgeSpaceId": "ks-product",
                "state": "draft",
                "operationId": "op-001",
                "correlationId": "corr-001",
                "pagination": "notApplicable"
              }
            }
            """;

        AssertInvalid("public", instance);
    }

    [TestMethod]
    public void IncompatiblePrivateEnvelopeIsRejected()
    {
        var instance = """
            {
              "contractVersion": "1.0",
              "operation": "commit_change_set",
              "knowledgeSpaceId": "ks-product",
              "identity": {
                "initiatingUserId": "user-001",
                "actingAgentId": "agent-contribution",
                "agentDefinitionVersion": "1.0",
                "requiredCapability": "commit_change_set"
              },
              "executionContext": {
                "executionId": "exec-001",
                "traceId": "trace-001",
                "correlationId": "corr-001",
                "knowledgeSpaceId": "ks-product",
                "lifecycleState": "active",
                "ontologyVersion": "ontology-product-v1",
                "ontologyDigest": "not-a-digest",
                "mutationPolicy": "confirmation",
                "canonicalHeadVersion": "head-001"
              }
            }
            """;

        AssertInvalid("private", instance);
    }

    [TestMethod]
    public void MissingPublicIdempotencyKeyIsRejected()
    {
        var instance = """
            {
              "contractVersion": "1.0",
              "operation": "create_space",
              "request": {
                "displayName": "Product",
                "identity": {
                  "initiatingUserId": "user-001"
                }
              },
              "response": {
                "outcome": "accepted",
                "knowledgeSpaceId": "ks-product",
                "state": "draft",
                "operationId": "op-001",
                "correlationId": "corr-001",
                "pagination": "notApplicable"
              }
            }
            """;

        AssertInvalid("public", instance);
    }

    [TestMethod]
    public void PublicManifestDoesNotReferencePrivateAssets()
    {
        var manifestPath = Path.Combine(ContractsRoot, "public", "manifest.json");
        var manifest = File.ReadAllText(manifestPath);

        Assert.IsFalse(manifest.Contains("private", StringComparison.OrdinalIgnoreCase));
    }

    private static void AssertValid(string surface, string instance)
    {
        var schema = GetSchema(surface);
        using var document = JsonDocument.Parse(instance);

        Assert.IsTrue(schema.Evaluate(document.RootElement).IsValid);
    }

    private static void AssertInvalid(string surface, string instance)
    {
        var schema = GetSchema(surface);
        using var document = JsonDocument.Parse(instance);

        Assert.IsFalse(schema.Evaluate(document.RootElement).IsValid);
    }

    private static JsonSchema LoadSchema(string surface)
    {
        var schemaPath = Path.Combine(
            ContractsRoot,
            surface,
            "v1.0",
            surface == "public"
                ? "public-thin-slice.schema.json"
                : "private-deterministic-tools.schema.json");

        return JsonSchema.FromFile(schemaPath);
    }

    private static JsonSchema GetSchema(string surface)
    {
        return surface == "public" ? PublicSchema.Value : PrivateSchema.Value;
    }
}
