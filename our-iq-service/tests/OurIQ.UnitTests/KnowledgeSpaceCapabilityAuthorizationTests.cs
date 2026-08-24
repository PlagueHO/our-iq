using OurIQ.Domain;

namespace OurIQ.UnitTests;

[TestClass]
public sealed class KnowledgeSpaceCapabilityAuthorizationTests
{
    private static readonly IReadOnlyDictionary<string, IReadOnlySet<string>> ExpectedUserCapabilities =
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

    private static readonly IReadOnlyDictionary<string, IReadOnlySet<string>> ExpectedAgentCapabilities =
        new Dictionary<string, IReadOnlySet<string>>(StringComparer.Ordinal)
        {
            [DomainAgentIdentities.Ontology] = new HashSet<string>(StringComparer.Ordinal)
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
            [DomainAgentIdentities.Contribution] = new HashSet<string>(StringComparer.Ordinal)
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
            [DomainAgentIdentities.Retrieval] = new HashSet<string>(StringComparer.Ordinal)
            {
                "get_space",
                "get_ontology",
                "search_evidence",
                "read_canonical_evidence"
            }
        };

    [TestMethod]
    public void CreateAssignsCreatorTheOwnerRole()
    {
        var record = CreateRecord("owner-001");

        CollectionAssert.AreEqual(
            new[] { new KnowledgeSpaceRoleGrant("owner-001", KnowledgeSpaceRoles.Owner) },
            record.RoleGrants.ToArray());
    }

    [TestMethod]
    public void OwnerCanGrantAndRevokeEveryFixedRole()
    {
        var record = CreateRecord("owner-001");

        foreach (var role in ExpectedUserCapabilities.Keys)
        {
            record = record.GrantRole("owner-001", "member-001", role);
            Assert.IsTrue(record.HasRole("member-001", role));
        }

        foreach (var role in ExpectedUserCapabilities.Keys)
        {
            record = record.RevokeRole("owner-001", "member-001", role);
            Assert.IsFalse(record.HasRole("member-001", role));
        }
    }

    [TestMethod]
    public void GrantAndRevokeAreIdempotent()
    {
        var record = CreateRecord("owner-001")
            .GrantRole("owner-001", "reader-001", KnowledgeSpaceRoles.Reader);

        var repeatedGrant = record.GrantRole(
            "owner-001",
            "reader-001",
            KnowledgeSpaceRoles.Reader);
        var repeatedRevocation = repeatedGrant
            .RevokeRole("owner-001", "reader-001", KnowledgeSpaceRoles.Reader)
            .RevokeRole("owner-001", "reader-001", KnowledgeSpaceRoles.Reader);

        Assert.AreEqual(record, repeatedGrant);
        Assert.IsFalse(repeatedRevocation.HasRole("reader-001", KnowledgeSpaceRoles.Reader));
    }

    [TestMethod]
    public void NonOwnerCannotGrantOrRevokeRoles()
    {
        var record = CreateRecord("owner-001")
            .GrantRole("owner-001", "reader-001", KnowledgeSpaceRoles.Reader);

        Assert.Throws<KnowledgeSpaceRoleAuthorizationException>(
            () => record.GrantRole("reader-001", "member-001", KnowledgeSpaceRoles.Contributor));
        Assert.Throws<KnowledgeSpaceRoleAuthorizationException>(
            () => record.RevokeRole("reader-001", "owner-001", KnowledgeSpaceRoles.Owner));
    }

    [TestMethod]
    public void InvalidRoleGrantsAreRejected()
    {
        var record = CreateRecord("owner-001");

        Assert.Throws<KnowledgeSpaceControlRecordValidationException>(
            () => record.GrantRole("owner-001", "member-001", "Administrator"));
        Assert.Throws<KnowledgeSpaceControlRecordValidationException>(
            () => record with
            {
                RoleGrants =
                [
                    new KnowledgeSpaceRoleGrant("member-001", KnowledgeSpaceRoles.Reader),
                    new KnowledgeSpaceRoleGrant("member-001", KnowledgeSpaceRoles.Reader)
                ]
            }.Validate());
    }

    [TestMethod]
    public void RoleCapabilitiesMatchTheAcceptedMatrix()
    {
        var record = CreateRecord("owner-001");
        var allCapabilities = ExpectedUserCapabilities.Values
            .SelectMany(capabilities => capabilities)
            .ToHashSet(StringComparer.Ordinal);

        foreach (var (role, expectedCapabilities) in ExpectedUserCapabilities)
        {
            var userId = $"user-{role.Replace(" ", "-", StringComparison.Ordinal).ToLowerInvariant()}";
            record = record.GrantRole("owner-001", userId, role);

            foreach (var capability in allCapabilities)
            {
                Assert.AreEqual(
                    expectedCapabilities.Contains(capability),
                    KnowledgeSpaceRoleCapabilities.HasCapability(record, userId, capability),
                    $"{role} capability '{capability}' did not match the accepted matrix.");
            }
        }
    }

    [TestMethod]
    public void AgentCapabilitiesMatchTheAcceptedManifests()
    {
        var record = CreateRecord("owner-001");
        var allCapabilities = ExpectedAgentCapabilities.Values
            .SelectMany(capabilities => capabilities)
            .ToHashSet(StringComparer.Ordinal);

        foreach (var (agentId, expectedCapabilities) in ExpectedAgentCapabilities)
        {
            foreach (var capability in allCapabilities)
            {
                var result = Authorize(
                    record,
                    KnowledgeSpaceUserCapabilities.ManageRoles,
                    agentId,
                    capability);

                Assert.IsTrue(result.IsUserAuthorized);
                Assert.AreEqual(
                    expectedCapabilities.Contains(capability),
                    result.IsAgentAuthorized,
                    $"{agentId} capability '{capability}' did not match the accepted manifest.");
            }
        }
    }

    [TestMethod]
    public void CapabilityAuthorizationRequiresBothUserAndAgentAuthority()
    {
        var record = CreateRecord("owner-001")
            .GrantRole("owner-001", "reader-001", KnowledgeSpaceRoles.Reader);

        var userDenied = Authorize(
            record,
            KnowledgeSpaceUserCapabilities.ManageRoles,
            DomainAgentIdentities.Ontology,
            "get_space",
            "reader-001");
        var agentDenied = Authorize(
            record,
            KnowledgeSpaceUserCapabilities.ManageRoles,
            DomainAgentIdentities.Retrieval,
            "stage_ontology_version");
        var authorized = Authorize(
            record,
            KnowledgeSpaceUserCapabilities.ReadEvidence,
            DomainAgentIdentities.Retrieval,
            "read_canonical_evidence",
            "reader-001");

        Assert.IsFalse(userDenied.IsUserAuthorized);
        Assert.IsTrue(userDenied.IsAgentAuthorized);
        Assert.IsFalse(userDenied.IsAuthorized);

        Assert.IsTrue(agentDenied.IsUserAuthorized);
        Assert.IsFalse(agentDenied.IsAgentAuthorized);
        Assert.IsFalse(agentDenied.IsAuthorized);

        Assert.IsTrue(authorized.IsAuthorized);
    }

    [TestMethod]
    public void CapabilityIntersectionDeniesEveryAuthorizationOutsideBothMatrices()
    {
        var record = CreateRecord("owner-001");
        var allUserCapabilities = ExpectedUserCapabilities.Values
            .SelectMany(capabilities => capabilities)
            .ToHashSet(StringComparer.Ordinal);
        var allAgentCapabilities = ExpectedAgentCapabilities.Values
            .SelectMany(capabilities => capabilities)
            .ToHashSet(StringComparer.Ordinal);

        foreach (var (role, expectedUserCapabilities) in ExpectedUserCapabilities)
        {
            var userId = $"user-{role.Replace(" ", "-", StringComparison.Ordinal).ToLowerInvariant()}";
            record = record.GrantRole("owner-001", userId, role);

            foreach (var userCapability in allUserCapabilities)
            {
                foreach (var (agentId, expectedAgentCapabilities) in ExpectedAgentCapabilities)
                {
                    foreach (var agentCapability in allAgentCapabilities)
                    {
                        var result = Authorize(
                            record,
                            userCapability,
                            agentId,
                            agentCapability,
                            userId);

                        Assert.AreEqual(
                            expectedUserCapabilities.Contains(userCapability)
                                && expectedAgentCapabilities.Contains(agentCapability),
                            result.IsAuthorized,
                            $"Unexpected authorization for {role}, {userCapability}, {agentId}, and {agentCapability}.");
                    }
                }
            }
        }
    }

    [TestMethod]
    public void AgentCapabilityRequiresTheExactDefinitionVersion()
    {
        var record = CreateRecord("owner-001");
        var result = KnowledgeSpaceCapabilityAuthorizer.Authorize(
            record,
            new KnowledgeSpaceCapabilityAuthorizationRequest(
                "owner-001",
                KnowledgeSpaceUserCapabilities.ManageRoles,
                DomainAgentIdentities.Ontology,
                "2.0",
                "get_space"));

        Assert.IsTrue(result.IsUserAuthorized);
        Assert.IsFalse(result.IsAgentAuthorized);
        Assert.IsFalse(result.IsAuthorized);
    }

    private static KnowledgeSpaceControlRecord CreateRecord(string createdBy) =>
        KnowledgeSpaceControlRecord.Create(
            new KnowledgeSpaceCreation("Product", "contributor confirmation", createdBy));

    private static KnowledgeSpaceCapabilityAuthorizationResult Authorize(
        KnowledgeSpaceControlRecord record,
        string requiredUserCapability,
        string agentId,
        string requiredAgentCapability,
        string initiatingUserId = "owner-001") =>
        KnowledgeSpaceCapabilityAuthorizer.Authorize(
            record,
            new KnowledgeSpaceCapabilityAuthorizationRequest(
                initiatingUserId,
                requiredUserCapability,
                agentId,
                DomainAgentIdentities.InitialDefinitionVersion,
                requiredAgentCapability));
}
