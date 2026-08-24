namespace OurIQ.Domain;

public static class KnowledgeSpaceUserCapabilities
{
    public const string ManageRoles = "manage_roles";
    public const string ConfigureMutationPolicy = "configure_mutation_policy";
    public const string TransitionLifecycle = "transition_lifecycle";
    public const string StartDeletion = "start_deletion";
    public const string ApproveReviewPlan = "approve_review_plan";
    public const string InspectSpace = "inspect_space";
    public const string SubmitSpaceSetup = "submit_space_setup";
    public const string ApproveOntology = "approve_ontology";
    public const string StageOntologyVersion = "stage_ontology_version";
    public const string InspectOntology = "inspect_ontology";
    public const string InspectPlan = "inspect_plan";
    public const string InspectOperation = "inspect_operation";
    public const string ReadEvidence = "read_evidence";
    public const string ContributeKnowledge = "contribute_knowledge";
    public const string BootstrapKnowledge = "bootstrap_knowledge";
    public const string ConfirmPlan = "confirm_plan";
    public const string InspectPublicSpace = "inspect_public_space";
    public const string InspectPublicOperation = "inspect_public_operation";
}

public static class KnowledgeSpaceRoleCapabilities
{
    private static readonly IReadOnlyDictionary<string, IReadOnlySet<string>> CapabilitiesByRole =
        new Dictionary<string, IReadOnlySet<string>>(StringComparer.Ordinal)
        {
            [KnowledgeSpaceRoles.Owner] = new HashSet<string>(StringComparer.Ordinal)
            {
                KnowledgeSpaceUserCapabilities.ManageRoles,
                KnowledgeSpaceUserCapabilities.ConfigureMutationPolicy,
                KnowledgeSpaceUserCapabilities.TransitionLifecycle,
                KnowledgeSpaceUserCapabilities.StartDeletion,
                KnowledgeSpaceUserCapabilities.ApproveReviewPlan,
                KnowledgeSpaceUserCapabilities.InspectSpace
            },
            [KnowledgeSpaceRoles.OntologyManager] = new HashSet<string>(StringComparer.Ordinal)
            {
                KnowledgeSpaceUserCapabilities.SubmitSpaceSetup,
                KnowledgeSpaceUserCapabilities.ApproveOntology,
                KnowledgeSpaceUserCapabilities.StageOntologyVersion,
                KnowledgeSpaceUserCapabilities.InspectOntology,
                KnowledgeSpaceUserCapabilities.InspectPlan,
                KnowledgeSpaceUserCapabilities.InspectOperation,
                KnowledgeSpaceUserCapabilities.ReadEvidence
            },
            [KnowledgeSpaceRoles.Contributor] = new HashSet<string>(StringComparer.Ordinal)
            {
                KnowledgeSpaceUserCapabilities.ContributeKnowledge,
                KnowledgeSpaceUserCapabilities.BootstrapKnowledge,
                KnowledgeSpaceUserCapabilities.ConfirmPlan,
                KnowledgeSpaceUserCapabilities.ReadEvidence
            },
            [KnowledgeSpaceRoles.Reader] = new HashSet<string>(StringComparer.Ordinal)
            {
                KnowledgeSpaceUserCapabilities.ReadEvidence,
                KnowledgeSpaceUserCapabilities.InspectPublicSpace,
                KnowledgeSpaceUserCapabilities.InspectPublicOperation
            }
        };

    public static bool HasCapability(
        KnowledgeSpaceControlRecord record,
        string userId,
        string requiredCapability)
    {
        ArgumentNullException.ThrowIfNull(record);
        ArgumentException.ThrowIfNullOrWhiteSpace(userId);
        ArgumentException.ThrowIfNullOrWhiteSpace(requiredCapability);

        return record.RoleGrants.Any(grant =>
            string.Equals(grant.UserId, userId, StringComparison.Ordinal)
            && CapabilitiesByRole[grant.Role].Contains(requiredCapability));
    }
}

public static class DomainAgentIdentities
{
    public const string Ontology = "agent-ontology";
    public const string Contribution = "agent-contribution";
    public const string Retrieval = "agent-retrieval";
    public const string InitialDefinitionVersion = "1.0";
}

public static class DomainAgentCapabilities
{
    private static readonly IReadOnlyDictionary<(string AgentId, string DefinitionVersion), IReadOnlySet<string>>
        CapabilitiesByAgentDefinition =
            new Dictionary<(string AgentId, string DefinitionVersion), IReadOnlySet<string>>
            {
                [(DomainAgentIdentities.Ontology, DomainAgentIdentities.InitialDefinitionVersion)] =
                    new HashSet<string>(StringComparer.Ordinal)
                    {
                        "get_space",
                        "get_ontology",
                        "list_all_templates",
                        "get_template",
                        "stage_ontology_version",
                        "validate_ontology_compatibility",
                        "record_approval",
                        "activate_ontology_version"
                    },
                [(DomainAgentIdentities.Contribution, DomainAgentIdentities.InitialDefinitionVersion)] =
                    new HashSet<string>(StringComparer.Ordinal)
                    {
                        "get_space",
                        "get_ontology",
                        "get_canonical_snapshot",
                        "search_evidence",
                        "read_canonical_evidence",
                        "validate_change_plan",
                        "stage_knowledge_revisions",
                        "commit_change_set"
                    },
                [(DomainAgentIdentities.Retrieval, DomainAgentIdentities.InitialDefinitionVersion)] =
                    new HashSet<string>(StringComparer.Ordinal)
                    {
                        "get_space",
                        "get_ontology",
                        "search_evidence",
                        "read_canonical_evidence"
                    }
            };

    public static bool HasCapability(
        string agentId,
        string definitionVersion,
        string requiredCapability)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(agentId);
        ArgumentException.ThrowIfNullOrWhiteSpace(definitionVersion);
        ArgumentException.ThrowIfNullOrWhiteSpace(requiredCapability);

        return CapabilitiesByAgentDefinition.TryGetValue(
            (agentId, definitionVersion),
            out var capabilities)
            && capabilities.Contains(requiredCapability);
    }
}

public sealed record KnowledgeSpaceCapabilityAuthorizationRequest(
    string InitiatingUserId,
    string RequiredUserCapability,
    string ActingAgentId,
    string AgentDefinitionVersion,
    string RequiredAgentCapability)
{
    public void Validate()
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(InitiatingUserId);
        ArgumentException.ThrowIfNullOrWhiteSpace(RequiredUserCapability);
        ArgumentException.ThrowIfNullOrWhiteSpace(ActingAgentId);
        ArgumentException.ThrowIfNullOrWhiteSpace(AgentDefinitionVersion);
        ArgumentException.ThrowIfNullOrWhiteSpace(RequiredAgentCapability);
    }
}

public sealed record KnowledgeSpaceCapabilityAuthorizationResult(
    bool IsUserAuthorized,
    bool IsAgentAuthorized)
{
    public bool IsAuthorized => IsUserAuthorized && IsAgentAuthorized;
}

public static class KnowledgeSpaceCapabilityAuthorizer
{
    public static KnowledgeSpaceCapabilityAuthorizationResult Authorize(
        KnowledgeSpaceControlRecord record,
        KnowledgeSpaceCapabilityAuthorizationRequest request)
    {
        ArgumentNullException.ThrowIfNull(record);
        ArgumentNullException.ThrowIfNull(request);
        record.Validate();
        request.Validate();

        return new(
            KnowledgeSpaceRoleCapabilities.HasCapability(
                record,
                request.InitiatingUserId,
                request.RequiredUserCapability),
            DomainAgentCapabilities.HasCapability(
                request.ActingAgentId,
                request.AgentDefinitionVersion,
                request.RequiredAgentCapability));
    }
}
