using System.Net;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using OurIQ.ToolServices;

namespace OurIQ.ComponentTests;

[TestClass]
[DoNotParallelize]
public sealed class PrivateToolServicesComponentTests
{
    [TestMethod]
    public async Task HealthAndReadinessAreSeparateFromPrivateMcp()
    {
        using var factory = new WebApplicationFactory<ToolServicesProgram>();
        using var client = factory.CreateClient();

        using var health = await client.GetAsync("/health");
        using var readiness = await client.GetAsync("/ready");

        Assert.AreEqual(HttpStatusCode.OK, health.StatusCode);
        Assert.AreEqual("healthy", await health.Content.ReadAsStringAsync());
        Assert.AreEqual(HttpStatusCode.OK, readiness.StatusCode);
        Assert.AreEqual("ready", await readiness.Content.ReadAsStringAsync());
    }

    [TestMethod]
    public async Task PrivateMcpDeniesCallersWithoutPrivateExecutionContext()
    {
        using var factory = new WebApplicationFactory<ToolServicesProgram>();
        using var client = factory.CreateClient();

        using var response = await SendHttpRequestAsync(client, InitializeRequest());

        Assert.AreEqual(HttpStatusCode.Forbidden, response.StatusCode);
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
        using var factory = new WebApplicationFactory<McpServerProgram>();
        using var client = factory.CreateClient();

        var initialize = await SendMcpRequestAsync(
            client,
            InitializeRequest(),
            includePrivateExecutionContext: false);
        var tools = await SendMcpRequestAsync(
            client,
            ToolsListRequest(),
            includePrivateExecutionContext: false,
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
                    arguments = new { request = new { } }
                }
            },
            includePrivateExecutionContext: true,
            initialize.SessionId);

        Assert.IsTrue(call.Document.RootElement.GetProperty("result").GetProperty("isError").GetBoolean());
    }

    [TestMethod]
    public async Task ManagementSurfaceUsesASeparateAuthorizationPolicy()
    {
        using var factory = CreateAuthorizedFactory();
        using var client = factory.CreateClient();

        client.DefaultRequestHeaders.Add("X-Test-Private-Execution-Context", "valid");
        using var privateContextManagement = await client.GetAsync("/management/status");
        Assert.AreEqual(HttpStatusCode.Forbidden, privateContextManagement.StatusCode);

        client.DefaultRequestHeaders.Remove("X-Test-Private-Execution-Context");
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

    private static WebApplicationFactory<ToolServicesProgram> CreateAuthorizedFactory() =>
        new WebApplicationFactory<ToolServicesProgram>().WithWebHostBuilder(builder =>
            builder.ConfigureServices(services =>
            {
                services.RemoveAll<IPrivateExecutionContextValidator>();
                services.RemoveAll<IManagementAccessValidator>();
                services.AddSingleton<IPrivateExecutionContextValidator, TestPrivateExecutionContextValidator>();
                services.AddSingleton<IManagementAccessValidator, TestManagementAccessValidator>();
            }));

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
            httpRequest.Headers.Add("X-Test-Private-Execution-Context", "valid");
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
                JsonSerializer.Serialize(request),
                Encoding.UTF8,
                "application/json")
        };

        return httpRequest.WithAcceptHeaders();
    }

    private sealed class TestPrivateExecutionContextValidator : IPrivateExecutionContextValidator
    {
        public ValueTask<bool> ValidateAsync(
            HttpContext httpContext,
            CancellationToken cancellationToken) =>
            ValueTask.FromResult(
                httpContext.Request.Headers.TryGetValue(
                    "X-Test-Private-Execution-Context",
                    out var value)
                && value.Count == 1
                && value[0] == "valid");
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
