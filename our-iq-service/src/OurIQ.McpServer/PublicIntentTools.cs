using System.ComponentModel;
using System.Text.Json;
using ModelContextProtocol.Protocol;
using ModelContextProtocol.Server;

namespace OurIQ.McpServer;

[McpServerToolType]
public static class PublicIntentTools
{
    [McpServerTool(Name = "create_space")]
    [Description("Request creation of a knowledge space.")]
    public static CallToolResult CreateSpace(
        [Description("The public contract request envelope.")] JsonElement request) =>
        Unsupported("create_space");

    [McpServerTool(Name = "submit_space_setup")]
    [Description("Submit ontology and governance setup for a knowledge space.")]
    public static CallToolResult SubmitSpaceSetup(
        [Description("The public contract request envelope.")] JsonElement request) =>
        Unsupported("submit_space_setup");

    [McpServerTool(Name = "approve_ontology")]
    [Description("Approve a pending ontology version for a knowledge space.")]
    public static CallToolResult ApproveOntology(
        [Description("The public contract request envelope.")] JsonElement request) =>
        Unsupported("approve_ontology");

    [McpServerTool(Name = "contribute_knowledge")]
    [Description("Submit an attended contribution for governed planning.")]
    public static CallToolResult ContributeKnowledge(
        [Description("The public contract request envelope.")] JsonElement request) =>
        Unsupported("contribute_knowledge");

    [McpServerTool(Name = "approve_change_plan")]
    [Description("Approve or reject a governed change plan.")]
    public static CallToolResult ApproveChangePlan(
        [Description("The public contract request envelope.")] JsonElement request) =>
        Unsupported("approve_change_plan");

    [McpServerTool(Name = "query_knowledge")]
    [Description("Query authorized knowledge and return cited evidence.")]
    public static CallToolResult QueryKnowledge(
        [Description("The public contract request envelope.")] JsonElement request) =>
        Unsupported("query_knowledge");

    private static CallToolResult Unsupported(string operation) =>
        new()
        {
            IsError = true,
            Content =
            [
                new TextContentBlock
                {
                    Text = $"The public MCP host does not implement '{operation}' yet."
                }
            ]
        };
}
