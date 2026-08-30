using System.Net;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;
using OurIQ.Domain;
using OurIQ.ToolServices;

namespace OurIQ.ComponentTests;

[TestClass]
[DoNotParallelize]
public sealed class PrivateToolServicesComponentTests
{
    [TestMethod]
    public async Task HealthAndReadinessAreSeparateFromPrivateMcp()
    {
        using var factory = CreateAuthorizedFactory();
        using var client = factory.CreateClient();

        using var health = await client.GetAsync("/health");
        using var readiness = await client.GetAsync("/ready");

        Assert.AreEqual(HttpStatusCode.OK, health.StatusCode);
        Assert.AreEqual("healthy", await health.Content.ReadAsStringAsync());
        Assert.AreEqual(HttpStatusCode.OK, readiness.StatusCode);
        Assert.AreEqual("ready", await readiness.Content.ReadAsStringAsync());
    }

    [TestMethod]
    public void RealBearerHandlerPreservesRawEntraClaimNames()
    {
        using var factory = new WebApplicationFactory<ToolServicesProgram>()
            .WithWebHostBuilder(builder =>
                builder.ConfigureAppConfiguration((_, configuration) =>
                    configuration.AddInMemoryCollection(new Dictionary<string, string?>
                    {
                        ["Entra:Instance"] = "https://login.microsoftonline.com/",
                        ["Entra:TenantId"] = TestIdentity.TenantId,
                        ["Entra:ClientId"] = TestIdentity.AgentId
                    })));

        var options = factory.Services
            .GetRequiredService<IOptionsMonitor<JwtBearerOptions>>()
            .Get(JwtBearerDefaults.AuthenticationScheme);

        Assert.IsFalse(options.MapInboundClaims);
    }

    [TestMethod]
    public async Task PrivateMcpDeniesCallersWithoutPrivateExecutionContext()
    {
        using var factory = CreateAuthorizedFactory();
        using var client = factory.CreateClient();

        using var response = await SendHttpRequestAsync(client, InitializeRequest());

        Assert.AreEqual(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [TestMethod]
    public async Task PrivateMcpDiscoveryContainsOnlyPrivateDeterministicTools()
    {
        using var factory = CreateAuthorizedFactory();
        using var client = factory.CreateClient();

        var initialize = await SendMcpRequestAsync(
            client,
            InitializeRequest(),
            includePrivateExecutionContext: true);
        var tools = await SendMcpRequestAsync(
            client,
            ToolsListRequest(),
            includePrivateExecutionContext: true,
            initialize.SessionId);

        var toolNames = tools.Document.RootElement
            .GetProperty("result")
            .GetProperty("tools")
            .EnumerateArray()
            .Select(tool => tool.GetProperty("name").GetString())
            .ToHashSet(StringComparer.Ordinal);

        CollectionAssert.AreEquivalent(
            new[]
            {
                "activate_ontology_version",
                "authorize_capability",
                "cancel_operation",
                "commit_change_set",
                "create_operation",
                "get_canonical_snapshot",
                "get_change_set",
                "get_extraction_result",
                "get_ontology",
                "get_operation",
                "get_source_asset",
                "get_space",
                "get_template",
                "list_all_templates",
                "list_spaces",
                "read_canonical_evidence",
                "record_approval",
                "search_evidence",
                "stage_knowledge_revisions",
                "stage_ontology_version",
                "stage_source_asset",
                "transition_space",
                "validate_change_plan",
                "validate_execution_grant",
                "validate_ontology_compatibility"
            },
            toolNames.ToArray());

        Assert.DoesNotContain("query_knowledge", toolNames);
        Assert.DoesNotContain("contribute_knowledge", toolNames);
    }

    [TestMethod]
    public async Task PublicMcpServerDoesNotExposePrivateDeterministicTools()
    {
        using var factory = CreatePublicAuthorizedFactory();
        using var client = factory.CreateClient();

        var initialize = await SendMcpRequestAsync(
            client,
            InitializeRequest(),
            includePrivateExecutionContext: true);
        var tools = await SendMcpRequestAsync(
            client,
            ToolsListRequest(),
            includePrivateExecutionContext: true,
            initialize.SessionId);

        var toolNames = tools.Document.RootElement
            .GetProperty("result")
            .GetProperty("tools")
            .EnumerateArray()
            .Select(tool => tool.GetProperty("name").GetString())
            .ToHashSet(StringComparer.Ordinal);

        Assert.DoesNotContain("get_space", toolNames);
        Assert.DoesNotContain("commit_change_set", toolNames);
        Assert.DoesNotContain("validate_execution_grant", toolNames);
    }

    [TestMethod]
    public async Task AuthorizedPrivateToolCallReachesThePrivateHostBoundary()
    {
        using var factory = CreateAuthorizedFactory();
        using var client = factory.CreateClient();

        var initialize = await SendMcpRequestAsync(
            client,
            InitializeRequest(),
            includePrivateExecutionContext: true);
        var call = await SendMcpRequestAsync(
            client,
            new
            {
                jsonrpc = "2.0",
                id = 2,
                method = "tools/call",
                @params = new
                {
                    name = "get_space",
                    arguments = new
                    {
                        request = new
                        {
                            identity = new
                            {
                                initiatingUserId = TestIdentity.InitiatingUserId,
                                actingAgentId = TestIdentity.AgentId
                            }
                        }
                    }
                }
            },
            includePrivateExecutionContext: true,
            initialize.SessionId);

        Assert.IsTrue(call.Document.RootElement.GetProperty("result").GetProperty("isError").GetBoolean());
    }

    [TestMethod]
    public async Task OntologyAgentExecutesExactlyItsEightPrivateOperations()
    {
        var state = OntologyComponentState.Create();
        using var factory = CreateAuthorizedFactory(services =>
        {
            services.RemoveAll<IKnowledgeSpaceControlRecordRepository>();
            services.RemoveAll<IOntologyVersionRepository>();
            services.RemoveAll<IExecutionContextSnapshotRepository>();
            services.AddSingleton<IKnowledgeSpaceControlRecordRepository>(state);
            services.AddSingleton<IOntologyVersionRepository>(state);
            services.AddSingleton<IExecutionContextSnapshotRepository>(state);
        });
        using var client = factory.CreateClient();
        var initialize = await SendMcpRequestAsync(
            client,
            InitializeRequest(),
            includePrivateExecutionContext: true);
        var payload = CreateOntologyPayload();
        var calls = new (string Operation, object Arguments)[]
        {
            ("get_space", new { }),
            ("stage_ontology_version", new { proposalId = "proposal-001", payload }),
            ("get_ontology", new { ontologyVersionId = payload.OntologyVersionId }),
            ("list_all_templates", new { ontologyVersionId = payload.OntologyVersionId }),
            ("get_template", new
            {
                ontologyVersionId = payload.OntologyVersionId,
                templateId = "template-001",
                revisionId = "revision-001"
            }),
            ("validate_ontology_compatibility", new
            {
                assessmentId = "assessment-001",
                ontologyVersionId = payload.OntologyVersionId
            }),
            ("record_approval", new
            {
                approvalId = "approval-001",
                ontologyVersionId = payload.OntologyVersionId,
                compatibilityAssessmentId = "assessment-001",
                decision = "approve"
            }),
            ("activate_ontology_version", new
            {
                ontologyVersionId = payload.OntologyVersionId,
                payloadDigest = OntologyPayloadDigest.Compute(payload),
                approvalId = "approval-001",
                activationEvidenceId = "activation-001",
                expectedActiveOntologyVersionId = (string?)null,
                expectedActiveOntologyDigest = (string?)null
            })
        };

        var requestId = 2;
        foreach (var (operation, arguments) in calls)
        {
            var call = await SendMcpRequestAsync(
                client,
                OntologyToolCallRequest(requestId++, state.Snapshot, operation, arguments),
                includePrivateExecutionContext: true,
                initialize.SessionId);
            var result = call.Document.RootElement.GetProperty("result");
            Assert.IsFalse(
                result.GetProperty("isError").GetBoolean(),
                result.GetProperty("content")[0].GetProperty("text").GetString());
        }

        Assert.AreEqual(1, state.ActivationCount);
        Assert.AreEqual(KnowledgeSpaceLifecycleStates.Active, state.Space.LifecycleState);
    }

    [TestMethod]
    public async Task PrivateMcpRejectsAnUnauthorizedAgent()
    {
        using var factory = CreateAuthorizedFactory();
        using var client = factory.CreateClient();
        using var request = CreateMcpHttpRequest(InitializeRequest());
        TestIdentity.AddTo(request, "44444444-4444-4444-4444-444444444444");

        using var response = await client.SendAsync(request);

        Assert.AreEqual(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [TestMethod]
    public async Task PrivateToolRejectsAgentIdentitySubstitution()
    {
        var state = OntologyComponentState.Create();
        using var factory = CreateAuthorizedFactory(services =>
        {
            services.RemoveAll<IKnowledgeSpaceControlRecordRepository>();
            services.RemoveAll<IOntologyVersionRepository>();
            services.RemoveAll<IExecutionContextSnapshotRepository>();
            services.AddSingleton<IKnowledgeSpaceControlRecordRepository>(state);
            services.AddSingleton<IOntologyVersionRepository>(state);
            services.AddSingleton<IExecutionContextSnapshotRepository>(state);
        });
        using var client = factory.CreateClient();
        var initialize = await SendMcpRequestAsync(
            client,
            InitializeRequest(),
            includePrivateExecutionContext: true);

        var substituted = new
        {
            jsonrpc = "2.0",
            id = 2,
            method = "tools/call",
            @params = new
            {
                name = "get_space",
                arguments = new
                {
                    request = new
                    {
                        contractVersion = "1.0",
                        operation = "get_space",
                        knowledgeSpaceId = state.Snapshot.KnowledgeSpaceId,
                        identity = new
                        {
                            initiatingUserId = TestIdentity.InitiatingUserId,
                            actingAgentId = DomainAgentIdentities.Contribution,
                            agentDefinitionVersion = "1.0",
                            requiredCapability = "get_space"
                        },
                        executionContext = new
                        {
                            executionId = state.Snapshot.ExecutionId,
                            traceId = state.Snapshot.TraceId,
                            correlationId = "correlation-001",
                            knowledgeSpaceId = state.Snapshot.KnowledgeSpaceId,
                            lifecycleState = state.Snapshot.LifecycleState,
                            ontologyVersion = state.Snapshot.ActiveOntologyVersionId,
                            ontologyDigest = state.Snapshot.ActiveOntologyDigest,
                            mutationPolicy = state.Snapshot.MutationPolicy,
                            mutationPolicyVersion = state.Snapshot.MutationPolicyVersion,
                            canonicalHeadVersion = state.Snapshot.CanonicalHeadVersion
                        },
                        arguments = new { }
                    }
                }
            }
        };
        var call = await SendMcpRequestAsync(
            client,
            substituted,
            includePrivateExecutionContext: true,
            initialize.SessionId);

        var text = call.Document.RootElement
            .GetProperty("result")
            .GetProperty("content")[0]
            .GetProperty("text")
            .GetString();
        using var error = JsonDocument.Parse(text!);
        Assert.AreEqual(
            "authorization_denied",
            error.RootElement.GetProperty("error").GetProperty("code").GetString());
    }

    [TestMethod]
    public async Task ManagementSurfaceUsesASeparateAuthorizationPolicy()
    {
        using var factory = CreateAuthorizedFactory();
        using var client = factory.CreateClient();

        client.DefaultRequestHeaders.Add(TestAuthenticationHandler.AuthenticatedHeader, "true");
        client.DefaultRequestHeaders.Add(TestAuthenticationHandler.TenantIdHeader, TestIdentity.TenantId);
        client.DefaultRequestHeaders.Add(TestAuthenticationHandler.ObjectIdHeader, TestIdentity.ObjectId);
        client.DefaultRequestHeaders.Add(TestAuthenticationHandler.AgentIdHeader, TestIdentity.AgentId);
        using var privateContextManagement = await client.GetAsync("/management/status");
        Assert.AreEqual(HttpStatusCode.Forbidden, privateContextManagement.StatusCode);

        client.DefaultRequestHeaders.Remove(TestAuthenticationHandler.AgentIdHeader);
        client.DefaultRequestHeaders.Add("X-Test-Management-Access", "valid");
        using var management = await client.GetAsync("/management/status");
        Assert.AreEqual(HttpStatusCode.OK, management.StatusCode);
        Assert.AreEqual(
            """{"status":"available"}""",
            await management.Content.ReadAsStringAsync());

        using var managementContextPrivateMcp = await SendHttpRequestAsync(
            client,
            InitializeRequest());
        Assert.AreEqual(HttpStatusCode.Forbidden, managementContextPrivateMcp.StatusCode);
    }

    private static WebApplicationFactory<ToolServicesProgram> CreateAuthorizedFactory(
        Action<IServiceCollection>? configureServices = null) =>
        new WebApplicationFactory<ToolServicesProgram>().WithTestAuthentication(
            services =>
            {
                services.RemoveAll<IManagementAccessValidator>();
                services.AddSingleton<IManagementAccessValidator, TestManagementAccessValidator>();
                services.Configure<PrivateIdentityOptions>(options =>
                    options.AuthorizedAgentClientIds = [TestIdentity.AgentId]);
                configureServices?.Invoke(services);
            });

    private static WebApplicationFactory<McpServerProgram> CreatePublicAuthorizedFactory() =>
        new WebApplicationFactory<McpServerProgram>().WithTestAuthentication();

    private static object InitializeRequest() =>
        new
        {
            jsonrpc = "2.0",
            id = 1,
            method = "initialize",
            @params = new
            {
                protocolVersion = "2026-07-28",
                capabilities = new { },
                clientInfo = new { name = "component-tests", version = "1.0.0" }
            }
        };

    private static object ToolsListRequest() =>
        new
        {
            jsonrpc = "2.0",
            id = 2,
            method = "tools/list",
            @params = new { }
        };

    private static object OntologyToolCallRequest(
        int id,
        ExecutionContextSnapshot snapshot,
        string operation,
        object arguments) =>
        new
        {
            jsonrpc = "2.0",
            id,
            method = "tools/call",
            @params = new
            {
                name = operation,
                arguments = new
                {
                    request = new
                    {
                        contractVersion = OntologyAgentToolService.ContractVersion,
                        operation,
                        knowledgeSpaceId = snapshot.KnowledgeSpaceId,
                        identity = new
                        {
                            initiatingUserId = snapshot.InitiatingUserId,
                            actingAgentId = DomainAgentIdentities.Ontology,
                            agentDefinitionVersion = DomainAgentIdentities.InitialDefinitionVersion,
                            requiredCapability = operation
                        },
                        executionContext = new
                        {
                            executionId = snapshot.ExecutionId,
                            traceId = snapshot.TraceId,
                            correlationId = "correlation-001",
                            knowledgeSpaceId = snapshot.KnowledgeSpaceId,
                            lifecycleState = snapshot.LifecycleState,
                            ontologyVersion = snapshot.ActiveOntologyVersionId,
                            ontologyDigest = snapshot.ActiveOntologyDigest,
                            mutationPolicy = snapshot.MutationPolicy,
                            mutationPolicyVersion = snapshot.MutationPolicyVersion,
                            canonicalHeadVersion = snapshot.CanonicalHeadVersion
                        },
                        arguments
                    }
                }
            }
        };

    private static OntologyPayload CreateOntologyPayload() =>
        new()
        {
            OntologyId = "ontology-product",
            OntologyVersionId = "ontology-v1",
            Title = "Product knowledge",
            Description = "Structures product decisions.",
            DocumentTypes =
            [
                new(
                    "decision-record",
                    "A decision.",
                    ParseJson(
                        """
                        {
                          "$schema": "https://json-schema.org/draft/2020-12/schema",
                          "type": "object"
                        }
                        """))
            ],
            Hierarchy = new(["decision-record"], []),
            TemplateReferences =
            [
                new(
                    "template-001",
                    "revision-001",
                    "text/markdown",
                    new string('a', 64),
                    "asset://template-001/revision-001")
            ]
        };

    private static JsonElement ParseJson(string json)
    {
        using var document = JsonDocument.Parse(json);
        return document.RootElement.Clone();
    }

    private static async Task<HttpResponseMessage> SendHttpRequestAsync(
        HttpClient client,
        object request)
    {
        using var httpRequest = CreateMcpHttpRequest(request);
        return await client.SendAsync(httpRequest);
    }

    private static async Task<McpResponse> SendMcpRequestAsync(
        HttpClient client,
        object request,
        bool includePrivateExecutionContext,
        string? sessionId = null)
    {
        using var httpRequest = CreateMcpHttpRequest(request);

        if (includePrivateExecutionContext)
        {
            TestIdentity.AddTo(httpRequest);
        }

        if (sessionId is not null)
        {
            httpRequest.Headers.Add("Mcp-Session-Id", sessionId);
        }

        using var response = await client.SendAsync(httpRequest);
        var responseBody = await response.Content.ReadAsStringAsync();
        Assert.IsTrue(
            response.IsSuccessStatusCode,
            $"MCP request failed with {(int)response.StatusCode}: {responseBody}");

        var json = responseBody
            .Split('\n')
            .Select(line => line.StartsWith("data: ", StringComparison.Ordinal)
                ? line["data: ".Length..]
                : line)
            .FirstOrDefault(line => line.TrimStart().StartsWith("{", StringComparison.Ordinal))
            ?? throw new AssertFailedException("MCP response did not contain a JSON message.");

        var responseSessionId = response.Headers.TryGetValues("Mcp-Session-Id", out var values)
            ? values.Single()
            : sessionId;

        return new McpResponse(JsonDocument.Parse(json), responseSessionId);
    }

    private static HttpRequestMessage CreateMcpHttpRequest(object request)
    {
        var httpRequest = new HttpRequestMessage(HttpMethod.Post, "/mcp")
        {
            Content = new StringContent(
                JsonSerializer.Serialize(request, JsonOptions),
                Encoding.UTF8,
                "application/json")
        };

        return httpRequest.WithAcceptHeaders();
    }

    private sealed class TestManagementAccessValidator : IManagementAccessValidator
    {
        public ValueTask<bool> ValidateAsync(
            HttpContext httpContext,
            CancellationToken cancellationToken) =>
            ValueTask.FromResult(
                httpContext.Request.Headers.TryGetValue(
                    "X-Test-Management-Access",
                    out var value)
                && value.Count == 1
                && value[0] == "valid");
    }

    private sealed record McpResponse(JsonDocument Document, string? SessionId);

    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        Converters = { new JsonStringEnumConverter(JsonNamingPolicy.CamelCase) }
    };

    private sealed class OntologyComponentState :
        IKnowledgeSpaceControlRecordRepository,
        IOntologyVersionRepository,
        IExecutionContextSnapshotRepository
    {
        private readonly Dictionary<string, OntologyVersionEnvelope> versions = new();
        private readonly Dictionary<string, OntologyProposal> proposals = new();
        private readonly Dictionary<string, OntologyCompatibilityAssessment> assessments = new();
        private readonly Dictionary<string, OntologyApproval> approvals = new();

        private OntologyComponentState(
            KnowledgeSpaceControlRecord space,
            ExecutionContextSnapshot snapshot)
        {
            Space = space;
            Snapshot = snapshot;
        }

        public KnowledgeSpaceControlRecord Space { get; private set; }

        public ExecutionContextSnapshot Snapshot { get; }

        public int ActivationCount { get; private set; }

        public static OntologyComponentState Create()
        {
            var space = KnowledgeSpaceControlRecord.Create(
                new KnowledgeSpaceCreation("Product", "review", "owner-001"))
                .GrantRole(
                    "owner-001",
                    TestIdentity.InitiatingUserId,
                    KnowledgeSpaceRoles.OntologyManager) with
            {
                LifecycleState = KnowledgeSpaceLifecycleStates.Pending,
                ETag = "etag-001"
            };
            var snapshot = ExecutionContextSnapshot.Create(
                space,
                "execution-001",
                "trace-001",
                DomainAgentIdentities.Ontology,
                DomainAgentIdentities.InitialDefinitionVersion,
                TestIdentity.InitiatingUserId);
            return new(space, snapshot);
        }

        public Task<KnowledgeSpaceControlRecord?> GetAsync(
            string knowledgeSpaceId,
            CancellationToken cancellationToken = default) =>
            Task.FromResult<KnowledgeSpaceControlRecord?>(
                knowledgeSpaceId == Space.KnowledgeSpaceId ? Space : null);

        public Task<ExecutionContextSnapshot?> GetAsync(
            string executionId,
            string knowledgeSpaceId,
            CancellationToken cancellationToken = default) =>
            Task.FromResult<ExecutionContextSnapshot?>(
                executionId == Snapshot.ExecutionId
                && knowledgeSpaceId == Snapshot.KnowledgeSpaceId
                    ? Snapshot
                    : null);

        public Task<OntologyVersionEnvelope?> GetVersionAsync(
            string ontologyVersionId,
            string knowledgeSpaceId,
            CancellationToken cancellationToken = default) =>
            Task.FromResult<OntologyVersionEnvelope?>(
                versions.TryGetValue(ontologyVersionId, out var version)
                && version.KnowledgeSpaceId == knowledgeSpaceId
                    ? version
                    : null);

        public Task<OntologyVersionEnvelope> CreateVersionAsync(
            OntologyVersionEnvelope version,
            CancellationToken cancellationToken = default)
        {
            versions.Add(version.Id, version);
            return Task.FromResult(version);
        }

        public Task<OntologyProposal> CreateProposalAsync(
            OntologyProposal proposal,
            CancellationToken cancellationToken = default)
        {
            proposals.Add(proposal.Id, proposal);
            return Task.FromResult(proposal);
        }

        public Task<OntologyCompatibilityAssessment> CreateCompatibilityAssessmentAsync(
            OntologyCompatibilityAssessment assessment,
            CancellationToken cancellationToken = default)
        {
            assessments.Add(assessment.Id, assessment);
            return Task.FromResult(assessment);
        }

        public Task<OntologyCompatibilityAssessment?> GetCompatibilityAssessmentAsync(
            string assessmentId,
            string knowledgeSpaceId,
            CancellationToken cancellationToken = default) =>
            Task.FromResult<OntologyCompatibilityAssessment?>(
                assessments.TryGetValue(assessmentId, out var assessment)
                && assessment.KnowledgeSpaceId == knowledgeSpaceId
                    ? assessment
                    : null);

        public Task<OntologyApproval> CreateApprovalAsync(
            OntologyApproval approval,
            CancellationToken cancellationToken = default)
        {
            approvals.Add(approval.Id, approval);
            return Task.FromResult(approval);
        }

        public Task<OntologyApproval?> GetApprovalAsync(
            string approvalId,
            string knowledgeSpaceId,
            CancellationToken cancellationToken = default) =>
            Task.FromResult<OntologyApproval?>(
                approvals.TryGetValue(approvalId, out var approval)
                && approval.KnowledgeSpaceId == knowledgeSpaceId
                    ? approval
                    : null);

        public Task<KnowledgeSpaceControlRecord> ActivateAsync(
            OntologyActivationRequest request,
            CancellationToken cancellationToken = default)
        {
            var version = versions[request.OntologyVersionId];
            var approval = approvals[request.ApprovalId];
            var assessment = assessments[approval.CompatibilityAssessmentId];
            Assert.IsTrue(approval.IsApproved);
            Assert.IsTrue(assessment.IsApproved);
            Assert.AreEqual(request.ExpectedActiveOntologyVersionId, Space.ActiveOntologyVersionId);
            Assert.AreEqual(request.ExpectedActiveOntologyDigest, Space.ActiveOntologyDigest);
            ActivationCount++;
            Space = Space with
            {
                LifecycleState = KnowledgeSpaceLifecycleStates.Active,
                ActiveOntologyVersionId = version.OntologyVersionId,
                ActiveOntologyDigest = version.PayloadDigest
            };
            return Task.FromResult(Space);
        }

        Task<KnowledgeSpaceControlRecord> IKnowledgeSpaceControlRecordRepository.CreateAsync(
            KnowledgeSpaceCreation creation,
            CancellationToken cancellationToken) =>
            throw new NotSupportedException();

        Task<KnowledgeSpaceControlRecordPage> IKnowledgeSpaceControlRecordRepository.ListAsync(
            KnowledgeSpaceControlRecordQuery query,
            CancellationToken cancellationToken) =>
            throw new NotSupportedException();

        Task<KnowledgeSpaceControlRecord> IKnowledgeSpaceControlRecordRepository.UpdateAsync(
            KnowledgeSpaceControlRecord record,
            string expectedETag,
            CancellationToken cancellationToken) =>
            throw new NotSupportedException();

        Task<ExecutionContextSnapshot> IExecutionContextSnapshotRepository.CreateAsync(
            ExecutionContextSnapshot snapshot,
            CancellationToken cancellationToken) =>
            throw new NotSupportedException();
    }
}

internal static class HttpRequestMessageExtensions
{
    public static HttpRequestMessage WithAcceptHeaders(this HttpRequestMessage request)
    {
        request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
        request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("text/event-stream"));
        return request;
    }
}
