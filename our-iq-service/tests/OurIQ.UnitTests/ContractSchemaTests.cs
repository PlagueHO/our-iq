using System.Security.Cryptography;
using System.Text.Json;
using System.Text.Json.Nodes;
using Json.Schema;
using ModelContextProtocol.Protocol;
using OurIQ.Contracts;
using OurIQ.ToolServices;

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
                "mutationPolicyVersion": "1.0",
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
                "mutationPolicyVersion": "1.0",
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

    [TestMethod]
    public void PublicSpaceRepresentationExcludesControlMetadata()
    {
        var schemaPath = Path.Combine(
            ContractsRoot,
            "public",
            "v1.0",
            "public-thin-slice.schema.json");
        using var document = JsonDocument.Parse(File.ReadAllBytes(schemaPath));
        var publicSpace = document.RootElement
            .GetProperty("$defs")
            .GetProperty("publicKnowledgeSpace");
        var properties = publicSpace.GetProperty("properties");

        Assert.IsTrue(properties.TryGetProperty("knowledgeSpaceId", out _));
        Assert.IsTrue(properties.TryGetProperty("lifecycleState", out _));
        Assert.IsFalse(properties.TryGetProperty("roleGrants", out _));
        Assert.IsFalse(properties.TryGetProperty("mutationPolicy", out _));
        Assert.IsFalse(properties.TryGetProperty("activeOntologyVersionId", out _));
        Assert.IsFalse(properties.TryGetProperty("canonicalHeadVersion", out _));
        Assert.IsFalse(properties.TryGetProperty("eTag", out _));
    }

    [TestMethod]
    [DataRow("stage_ontology_version")]
    [DataRow("validate_ontology_compatibility")]
    [DataRow("record_approval")]
    [DataRow("activate_ontology_version")]
    [DataRow("get_space")]
    public void PrivateMutationKeysAreRequiredAndReadsRemainKeyFree(string operation)
    {
        object arguments = operation switch
        {
            "stage_ontology_version" => new
            {
                proposalId = "proposal-001",
                payload = OntologyAgentToolServiceTests.CreatePayload()
            },
            "validate_ontology_compatibility" => new
            {
                assessmentId = "assessment-001", ontologyVersionId = "ontology-v1"
            },
            "record_approval" => new
            {
                approvalId = "approval-001", ontologyVersionId = "ontology-v1",
                compatibilityAssessmentId = "assessment-001", decision = "approve"
            },
            "activate_ontology_version" => new
            {
                ontologyVersionId = "ontology-v1", payloadDigest = new string('a', 64),
                approvalId = "approval-001", activationEvidenceId = "activation-001",
                expectedActiveOntologyVersionId = (string?)null,
                expectedActiveOntologyDigest = (string?)null
            },
            _ => new { }
        };
        var request = JsonSerializer.SerializeToNode(new
        {
            contractVersion = "1.0", operation, knowledgeSpaceId = "space-001", idempotencyKey = "key-001",
            identity = new
            {
                initiatingUserId = "user-001", actingAgentId = "agent-ontology",
                agentDefinitionVersion = "1.0", requiredCapability = operation
            },
            executionContext = new
            {
                executionId = "execution-001", traceId = "trace-001", correlationId = "correlation-001",
                knowledgeSpaceId = "space-001", lifecycleState = "pending",
                ontologyVersion = (string?)null, ontologyDigest = (string?)null,
                mutationPolicy = "review", mutationPolicyVersion = "1.0", canonicalHeadVersion = (string?)null
            },
            arguments
        }, new JsonSerializerOptions(JsonSerializerDefaults.Web))!.AsObject();
        AssertValid("private", request.ToJsonString());
        request.Remove("idempotencyKey");
        Assert.AreEqual(operation == "get_space",
            GetSchema("private").Evaluate(JsonSerializer.SerializeToElement(request)).IsValid);
        foreach (var invalid in new[] { "", "   " })
        {
            request["idempotencyKey"] = invalid;
            AssertInvalid("private", request.ToJsonString());
        }
    }

    [TestMethod]
    public void SharedErrorEnvelopesMatchBothSchemasAndRequireRemediation()
    {
        foreach (var error in new[]
                 {
                     ApplicationError.IdentityMismatch, ApplicationError.NotImplemented,
                     ApplicationError.DependencyUnavailable, ApplicationError.Internal,
                     ApplicationError.IdempotencyConflict
                 })
        {
            foreach (var privateSurface in new[] { false, true })
            {
                var result = ToolFailureResults.Create("get_space", default, privateSurface, error);
                var response = JsonNode.Parse(result.Content.OfType<TextContentBlock>().Single().Text)!;
                Assert.IsTrue(result.IsError);
                Assert.IsNull(response["knowledgeSpaceId"]);
                Assert.IsFalse(string.IsNullOrWhiteSpace(response["correlationId"]!.GetValue<string>()));
                JsonNode envelope = privateSurface
                    ? response
                    : JsonSerializer.SerializeToNode(new
                    {
                        contractVersion = "1.0", operation = "query_knowledge",
                        request = new
                        {
                            knowledgeSpaceId = "space-001", query = "evidence",
                            pagination = new { pageSize = 20, cursor = (string?)null },
                            identity = new { initiatingUserId = "user-001" }
                        },
                        response
                    })!;
                var surface = privateSurface ? "private" : "public";
                AssertValid(surface, envelope.ToJsonString());
                var errorNode = privateSurface ? envelope["error"]! : envelope["response"]!["error"]!;
                errorNode.AsObject().Remove("remediation");
                AssertInvalid(surface, envelope.ToJsonString());
            }
        }
    }

    private static void AssertValid(string surface, string instance)
    {
        var schema = GetSchema(surface);
        using var document = JsonDocument.Parse(instance);

        Assert.IsTrue(schema.Evaluate(document.RootElement).IsValid, $"{surface}: {instance}");
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
