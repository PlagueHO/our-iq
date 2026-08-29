using System.Net;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;
using OurIQ.Domain;

namespace OurIQ.ComponentTests;

[TestClass]
[DoNotParallelize]
public sealed class PublicMcpServerComponentTests
{
    private static WebApplicationFactory<McpServerProgram> _factory = null!;

    [ClassInitialize]
    public static void ClassInitialize(TestContext _)
    {
        _factory = CreateAuthenticatedFactory();
    }

    [TestMethod]
    public async Task PublicMcpRequiresAnAuthenticatedUser()
    {
        using var client = _factory.CreateClient();
        using var request = CreateMcpHttpRequest(InitializeRequest());

        using var response = await client.SendAsync(request);

        Assert.AreEqual(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [ClassCleanup]
    public static void ClassCleanup()
    {
        _factory.Dispose();
    }

    [TestMethod]
    public async Task HealthEndpointIsSeparateFromMcpEndpoint()
    {
        using var client = _factory.CreateClient();

        using var response = await client.GetAsync("/health");

        Assert.AreEqual(HttpStatusCode.OK, response.StatusCode);
        Assert.AreEqual("healthy", await response.Content.ReadAsStringAsync());
    }

    [TestMethod]
    public void RealBearerHandlerPreservesRawEntraClaimNames()
    {
        using var factory = new WebApplicationFactory<McpServerProgram>()
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
    public async Task McpDiscoveryContainsOnlyPublicIntentTools()
    {
        using var client = _factory.CreateClient();

        var initialize = await SendRequestAsync(
            client,
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
            });

        var tools = await SendRequestAsync(
            client,
            new
            {
                jsonrpc = "2.0",
                id = 2,
                method = "tools/list",
                @params = new { }
            },
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
                "approve_change_plan",
                "approve_ontology",
                "contribute_knowledge",
                "create_space",
                "query_knowledge",
                "submit_space_setup"
            },
            toolNames.ToArray());

        Assert.DoesNotContain("create_knowledge_item", toolNames);
        Assert.DoesNotContain("read_knowledge_item", toolNames);
        Assert.DoesNotContain("update_knowledge_item", toolNames);
        Assert.DoesNotContain("delete_knowledge_item", toolNames);
        Assert.DoesNotContain("create_ontology", toolNames);
        Assert.DoesNotContain("update_ontology", toolNames);
        Assert.DoesNotContain("delete_ontology", toolNames);
    }

    [TestMethod]
    public async Task PublicMcpDiscoversAuthorizedSpaceResources()
    {
        using var client = _factory.CreateClient();
        var initialize = await SendRequestAsync(client, InitializeRequest());

        var templates = await SendRequestAsync(
            client,
            new
            {
                jsonrpc = "2.0",
                id = 2,
                method = "resources/templates/list",
                @params = new { }
            },
            initialize.SessionId);

        var uris = templates.Document.RootElement
            .GetProperty("result")
            .GetProperty("resourceTemplates")
            .EnumerateArray()
            .Select(template => template.GetProperty("uriTemplate").GetString())
            .ToArray();

        CollectionAssert.Contains(uris, "ouriq://spaces{?cursor,pageSize,lifecycleState}");
        CollectionAssert.Contains(uris, "ouriq://spaces/{knowledgeSpaceId}");
    }

    [TestMethod]
    public async Task PublicSpaceResourceReturnsOnlyVisiblePublicState()
    {
        var record = KnowledgeSpaceControlRecord.Create(
            new KnowledgeSpaceCreation(
                "Product",
                "contributor confirmation",
                TestIdentity.InitiatingUserId),
            () => Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa"),
            new DateTimeOffset(2026, 8, 30, 0, 0, 0, TimeSpan.Zero)) with
            {
                LifecycleState = KnowledgeSpaceLifecycleStates.Active,
                ActiveOntologyVersionId = "ontology-001",
                MutationPolicyVersion = "2.0"
            };
        using var factory = CreateAuthenticatedFactory(new InMemoryKnowledgeSpaceRepository([record]));
        using var client = factory.CreateClient();
        var initialize = await SendRequestAsync(client, InitializeRequest());

        var resource = await SendRequestAsync(
            client,
            new
            {
                jsonrpc = "2.0",
                id = 2,
                method = "resources/read",
                @params = new { uri = $"ouriq://spaces/{record.KnowledgeSpaceId}" }
            },
            initialize.SessionId);

        var text = resource.Document.RootElement
            .GetProperty("result")
            .GetProperty("contents")[0]
            .GetProperty("text")
            .GetString();
        using var document = JsonDocument.Parse(text!);
        var space = document.RootElement;

        Assert.IsTrue(space.TryGetProperty("knowledgeSpaceId", out _), space.GetRawText());
        Assert.AreEqual(record.KnowledgeSpaceId, space.GetProperty("knowledgeSpaceId").GetString());
        Assert.AreEqual("Product", space.GetProperty("displayName").GetString());
        Assert.AreEqual("active", space.GetProperty("lifecycleState").GetString());
        Assert.IsFalse(space.TryGetProperty("roleGrants", out _));
        Assert.IsFalse(space.TryGetProperty("mutationPolicy", out _));
        Assert.IsFalse(space.TryGetProperty("activeOntologyVersionId", out _));
    }

    [TestMethod]
    public async Task PublicSpaceCollectionUsesVisibilityFilteringAndCursorPaging()
    {
        var first = CreateSpace("ks-001", TestIdentity.InitiatingUserId);
        var second = CreateSpace("ks-002", TestIdentity.InitiatingUserId);
        var inaccessible = CreateSpace("ks-003", "other-user");
        using var factory = CreateAuthenticatedFactory(
            new InMemoryKnowledgeSpaceRepository([inaccessible, second, first]));
        using var client = factory.CreateClient();
        var initialize = await SendRequestAsync(client, InitializeRequest());

        var firstPage = await ReadResourceAsync(
            client,
            initialize.SessionId,
            "ouriq://spaces?pageSize=1&lifecycleState=active");
        var firstPageSpaces = firstPage.GetProperty("spaces");
        Assert.AreEqual(1, firstPageSpaces.GetArrayLength());
        Assert.AreEqual("ks-001", firstPageSpaces[0].GetProperty("knowledgeSpaceId").GetString());
        Assert.AreEqual(1, firstPage.GetProperty("pagination").GetProperty("pageSize").GetInt32());
        var cursor = firstPage.GetProperty("pagination").GetProperty("nextCursor").GetString();
        Assert.AreEqual("ks-001", cursor);

        var secondPage = await ReadResourceAsync(
            client,
            initialize.SessionId,
            $"ouriq://spaces?cursor={cursor}&pageSize=1&lifecycleState=active");
        var secondPageSpaces = secondPage.GetProperty("spaces");
        Assert.AreEqual(1, secondPageSpaces.GetArrayLength());
        Assert.AreEqual("ks-002", secondPageSpaces[0].GetProperty("knowledgeSpaceId").GetString());
        Assert.IsNull(secondPage.GetProperty("pagination").GetProperty("nextCursor").GetString());
    }

    [TestMethod]
    public async Task PublicIntentToolReportsUnsupportedDomainBehavior()
    {
        using var client = _factory.CreateClient();

        var initialize = await SendRequestAsync(
            client,
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
            });

        var call = await SendRequestAsync(
            client,
            new
            {
                jsonrpc = "2.0",
                id = 2,
                method = "tools/call",
                @params = new
                {
                    name = "contribute_knowledge",
                    arguments = new
                    {
                        request = new
                        {
                            identity = new { initiatingUserId = TestIdentity.InitiatingUserId }
                        }
                    }
                }
            },
            initialize.SessionId);

        Assert.IsTrue(call.Document.RootElement.GetProperty("result").GetProperty("isError").GetBoolean());
    }

    [TestMethod]
    public async Task PublicToolRejectsInitiatingUserSubstitution()
    {
        using var client = _factory.CreateClient();
        var initialize = await SendRequestAsync(client, InitializeRequest());

        var call = await SendRequestAsync(
            client,
            new
            {
                jsonrpc = "2.0",
                id = 2,
                method = "tools/call",
                @params = new
                {
                    name = "contribute_knowledge",
                    arguments = new
                    {
                        request = new
                        {
                            identity = new
                            {
                                initiatingUserId =
                                    "11111111-1111-1111-1111-111111111111:"
                                    + "99999999-9999-9999-9999-999999999999"
                            }
                        }
                    }
                }
            },
            initialize.SessionId);

        var text = call.Document.RootElement
            .GetProperty("result")
            .GetProperty("content")[0]
            .GetProperty("text")
            .GetString();
        Assert.AreEqual(
            "The authenticated identity does not match the request identity.",
            text);
    }

    [TestMethod]
    public async Task DirectCrudToolCallIsUnsupported()
    {
        using var client = _factory.CreateClient();

        var initialize = await SendRequestAsync(
            client,
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
            });

        var call = await SendRequestAsync(
            client,
            new
            {
                jsonrpc = "2.0",
                id = 2,
                method = "tools/call",
                @params = new
                {
                    name = "delete_knowledge_item",
                    arguments = new
                    {
                        request = new
                        {
                            identity = new { initiatingUserId = TestIdentity.InitiatingUserId }
                        }
                    }
                }
            },
            initialize.SessionId);

        Assert.IsTrue(call.Document.RootElement.TryGetProperty("error", out _));
    }

    private static async Task<McpResponse> SendRequestAsync(
        HttpClient client,
        object request,
        string? sessionId = null)
    {
        using var httpRequest = CreateMcpHttpRequest(request);
        TestIdentity.AddTo(httpRequest, agentId: null);

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

    private static WebApplicationFactory<McpServerProgram> CreateAuthenticatedFactory(
        IKnowledgeSpaceControlRecordRepository? repository = null) =>
        new WebApplicationFactory<McpServerProgram>().WithTestAuthentication(
            services =>
            {
                if (repository is not null)
                {
                    services.RemoveAll<IKnowledgeSpaceControlRecordRepository>();
                    services.AddSingleton(repository);
                }
            });

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

    private static HttpRequestMessage CreateMcpHttpRequest(object request)
    {
        var httpRequest = new HttpRequestMessage(HttpMethod.Post, "/mcp")
        {
            Content = new StringContent(
                JsonSerializer.Serialize(request),
                Encoding.UTF8,
                "application/json")
        };
        httpRequest.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
        httpRequest.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("text/event-stream"));
        return httpRequest;
    }

    private static async Task<JsonElement> ReadResourceAsync(
        HttpClient client,
        string? sessionId,
        string uri)
    {
        var resource = await SendRequestAsync(
            client,
            new
            {
                jsonrpc = "2.0",
                id = 2,
                method = "resources/read",
                @params = new { uri }
            },
            sessionId);
        var text = resource.Document.RootElement
            .GetProperty("result")
            .GetProperty("contents")[0]
            .GetProperty("text")
            .GetString();
        using var document = JsonDocument.Parse(text!);
        return document.RootElement.Clone();
    }

    private static KnowledgeSpaceControlRecord CreateSpace(string knowledgeSpaceId, string userId) =>
        KnowledgeSpaceControlRecord.Create(
            new KnowledgeSpaceCreation("Product", "contributor confirmation", userId)) with
        {
            KnowledgeSpaceId = knowledgeSpaceId,
            LifecycleState = KnowledgeSpaceLifecycleStates.Active
        };

    private sealed record McpResponse(JsonDocument Document, string? SessionId);

    private sealed class InMemoryKnowledgeSpaceRepository(
        IReadOnlyList<KnowledgeSpaceControlRecord> records)
        : IKnowledgeSpaceControlRecordRepository
    {
        public Task<KnowledgeSpaceControlRecord> CreateAsync(
            KnowledgeSpaceCreation creation,
            CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();

        public Task<KnowledgeSpaceControlRecord?> GetAsync(
            string knowledgeSpaceId,
            CancellationToken cancellationToken = default) =>
            Task.FromResult(records.SingleOrDefault(record =>
                string.Equals(record.KnowledgeSpaceId, knowledgeSpaceId, StringComparison.Ordinal)));

        public Task<KnowledgeSpaceControlRecordPage> ListAsync(
            KnowledgeSpaceControlRecordQuery query,
            CancellationToken cancellationToken = default)
        {
            query.Validate();
            var visible = records
                .Where(record => record.RoleGrants.Any(grant =>
                    string.Equals(grant.UserId, query.UserId, StringComparison.Ordinal)))
                .Where(record => query.LifecycleState is null
                    || string.Equals(record.LifecycleState, query.LifecycleState, StringComparison.Ordinal))
                .OrderBy(record => record.KnowledgeSpaceId, StringComparer.Ordinal)
                .Where(record => query.Cursor is null
                    || string.CompareOrdinal(record.KnowledgeSpaceId, query.Cursor) > 0)
                .Take(query.PageSize + 1)
                .ToArray();
            var page = visible.Take(query.PageSize).ToArray();
            var nextCursor = visible.Length > query.PageSize
                ? page[^1].KnowledgeSpaceId
                : null;
            return Task.FromResult(new KnowledgeSpaceControlRecordPage(page, nextCursor));
        }

        public Task<KnowledgeSpaceControlRecord> UpdateAsync(
            KnowledgeSpaceControlRecord record,
            string expectedETag,
            CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();
    }
}
