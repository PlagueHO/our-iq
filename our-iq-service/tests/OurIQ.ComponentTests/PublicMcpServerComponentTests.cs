using System.Net;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

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

    private static WebApplicationFactory<McpServerProgram> CreateAuthenticatedFactory() =>
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

    private sealed record McpResponse(JsonDocument Document, string? SessionId);
}
