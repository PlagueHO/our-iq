using System.ComponentModel;
using System.Text.Json;
using ModelContextProtocol.Protocol;
using ModelContextProtocol.Server;

namespace OurIQ.ToolServices;

[McpServerToolType]
public static class PrivateDeterministicTools
{
    [McpServerTool(Name = "get_space")]
    [Description("Read a knowledge-space control record.")]
    public static CallToolResult GetSpace(JsonElement request) => Unsupported("get_space");

    [McpServerTool(Name = "list_spaces")]
    [Description("List authorized knowledge-space control records.")]
    public static CallToolResult ListSpaces(JsonElement request) => Unsupported("list_spaces");

    [McpServerTool(Name = "transition_space")]
    [Description("Apply an authorized knowledge-space lifecycle transition.")]
    public static CallToolResult TransitionSpace(JsonElement request) => Unsupported("transition_space");

    [McpServerTool(Name = "get_ontology")]
    [Description("Read an immutable ontology version.")]
    public static CallToolResult GetOntology(JsonElement request) => Unsupported("get_ontology");

    [McpServerTool(Name = "list_all_templates")]
    [Description("List private ontology templates.")]
    public static CallToolResult ListAllTemplates(JsonElement request) => Unsupported("list_all_templates");

    [McpServerTool(Name = "get_template")]
    [Description("Read a private ontology template.")]
    public static CallToolResult GetTemplate(JsonElement request) => Unsupported("get_template");

    [McpServerTool(Name = "stage_ontology_version")]
    [Description("Stage an immutable ontology version.")]
    public static CallToolResult StageOntologyVersion(JsonElement request) => Unsupported("stage_ontology_version");

    [McpServerTool(Name = "validate_ontology_compatibility")]
    [Description("Validate ontology compatibility.")]
    public static CallToolResult ValidateOntologyCompatibility(JsonElement request) =>
        Unsupported("validate_ontology_compatibility");

    [McpServerTool(Name = "activate_ontology_version")]
    [Description("Activate an approved ontology version.")]
    public static CallToolResult ActivateOntologyVersion(JsonElement request) =>
        Unsupported("activate_ontology_version");

    [McpServerTool(Name = "stage_source_asset")]
    [Description("Stage an immutable source asset.")]
    public static CallToolResult StageSourceAsset(JsonElement request) => Unsupported("stage_source_asset");

    [McpServerTool(Name = "get_source_asset")]
    [Description("Read an immutable source asset.")]
    public static CallToolResult GetSourceAsset(JsonElement request) => Unsupported("get_source_asset");

    [McpServerTool(Name = "get_extraction_result")]
    [Description("Read a source extraction result.")]
    public static CallToolResult GetExtractionResult(JsonElement request) => Unsupported("get_extraction_result");

    [McpServerTool(Name = "get_canonical_snapshot")]
    [Description("Read a pinned canonical snapshot.")]
    public static CallToolResult GetCanonicalSnapshot(JsonElement request) =>
        Unsupported("get_canonical_snapshot");

    [McpServerTool(Name = "validate_change_plan")]
    [Description("Validate a governed change plan.")]
    public static CallToolResult ValidateChangePlan(JsonElement request) =>
        Unsupported("validate_change_plan");

    [McpServerTool(Name = "stage_knowledge_revisions")]
    [Description("Stage immutable candidate knowledge revisions.")]
    public static CallToolResult StageKnowledgeRevisions(JsonElement request) =>
        Unsupported("stage_knowledge_revisions");

    [McpServerTool(Name = "commit_change_set")]
    [Description("Commit a governed change set.")]
    public static CallToolResult CommitChangeSet(JsonElement request) => Unsupported("commit_change_set");

    [McpServerTool(Name = "get_change_set")]
    [Description("Read a committed change set.")]
    public static CallToolResult GetChangeSet(JsonElement request) => Unsupported("get_change_set");

    [McpServerTool(Name = "search_evidence")]
    [Description("Search authorized evidence candidates.")]
    public static CallToolResult SearchEvidence(JsonElement request) => Unsupported("search_evidence");

    [McpServerTool(Name = "read_canonical_evidence")]
    [Description("Read cited canonical evidence.")]
    public static CallToolResult ReadCanonicalEvidence(JsonElement request) =>
        Unsupported("read_canonical_evidence");

    [McpServerTool(Name = "create_operation")]
    [Description("Create a monitored private operation.")]
    public static CallToolResult CreateOperation(JsonElement request) => Unsupported("create_operation");

    [McpServerTool(Name = "get_operation")]
    [Description("Read a monitored private operation.")]
    public static CallToolResult GetOperation(JsonElement request) => Unsupported("get_operation");

    [McpServerTool(Name = "cancel_operation")]
    [Description("Cancel an authorized private operation.")]
    public static CallToolResult CancelOperation(JsonElement request) => Unsupported("cancel_operation");

    [McpServerTool(Name = "authorize_capability")]
    [Description("Authorize a capability for a governed operation.")]
    public static CallToolResult AuthorizeCapability(JsonElement request) =>
        Unsupported("authorize_capability");

    [McpServerTool(Name = "record_approval")]
    [Description("Record approval evidence.")]
    public static CallToolResult RecordApproval(JsonElement request) => Unsupported("record_approval");

    [McpServerTool(Name = "validate_execution_grant")]
    [Description("Validate a bounded execution grant.")]
    public static CallToolResult ValidateExecutionGrant(JsonElement request) =>
        Unsupported("validate_execution_grant");

    private static CallToolResult Unsupported(string operation) =>
        new()
        {
            IsError = true,
            Content =
            [
                new TextContentBlock
                {
                    Text = $"The private Tool Services host does not implement '{operation}' yet."
                }
            ]
        };
}
