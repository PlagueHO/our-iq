using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;
using Json.Schema;
using ModelContextProtocol.Protocol;
using OurIQ.Domain;
using OurIQ.ToolServices;

namespace OurIQ.UnitTests;

[TestClass]
public sealed class OntologyAgentToolServiceTests
{
    private static readonly Lazy<JsonSchema> PrivateSchema = new(LoadPrivateSchema);
    private static readonly JsonSerializerOptions RequestJsonOptions = new(JsonSerializerDefaults.Web)
    {
        Converters = { new JsonStringEnumConverter(JsonNamingPolicy.CamelCase) }
    };

    private static readonly string[] Operations =
    [
        "get_space",
        "get_ontology",
        "list_all_templates",
        "get_template",
        "stage_ontology_version",
        "validate_ontology_compatibility",
        "record_approval",
        "activate_ontology_version"
    ];

    [TestMethod]
    public async Task AllEightOperationsCompleteWithSchemaConformantEnvelopes()
    {
        var harness = CreateHarness(KnowledgeSpaceLifecycleStates.Pending);
        var payload = CreatePayload();
        var calls = new (string Operation, object Arguments)[]
        {
            ("get_space", new { }),
            ("stage_ontology_version", new
            {
                proposalId = "proposal-001",
                payload,
                sourceReferences = new[] { "source-001" }
            }),
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

        foreach (var (operation, arguments) in calls)
        {
            var request = CreateRequest(harness.Snapshot, operation, arguments);
            AssertSchemaValid(request);

            var result = await harness.Service.ExecuteAsync(operation, request);
            var response = ReadResponse(result);

            Assert.IsFalse(result.IsError, response.RootElement.GetRawText());
            Assert.AreEqual("completed", response.RootElement.GetProperty("outcome").GetString());
            AssertSchemaValid(response.RootElement);
        }

        Assert.HasCount(1, harness.Ontologies.Versions);
        Assert.HasCount(1, harness.Ontologies.Proposals);
        Assert.HasCount(1, harness.Ontologies.Assessments);
        Assert.HasCount(1, harness.Ontologies.Approvals);
        Assert.AreEqual(1, harness.Ontologies.ActivationCount);
        Assert.AreEqual(
            KnowledgeSpaceLifecycleStates.Active,
            harness.Spaces.Record.LifecycleState);
    }

    [TestMethod]
    [DynamicData(nameof(GetOperationNames))]
    public async Task EveryOperationDeniesAUserWithoutOntologyManagerCapability(string operation)
    {
        var harness = CreateHarness(
            KnowledgeSpaceLifecycleStates.Pending,
            initiatingUserId: "reader-001",
            role: KnowledgeSpaceRoles.Reader);

        var result = await harness.Service.ExecuteAsync(
            operation,
            CreateRequest(harness.Snapshot, operation, new { }));

        AssertError(result, "authorization_denied");
    }

    [TestMethod]
    public async Task FixedAgentIdentityDefinitionManifestAndCapabilityAreEnforced()
    {
        var harness = CreateHarness(KnowledgeSpaceLifecycleStates.Pending);
        var wrongAgent = CreateRequest(
            harness.Snapshot,
            "get_space",
            new { },
            actingAgentId: DomainAgentIdentities.Contribution);
        var wrongVersion = CreateRequest(
            harness.Snapshot,
            "get_space",
            new { },
            agentDefinitionVersion: "2.0");
        var wrongCapability = CreateRequest(
            harness.Snapshot,
            "get_space",
            new { },
            requiredCapability: "get_ontology");

        AssertError(await harness.Service.ExecuteAsync("get_space", wrongAgent), "authorization_denied");
        AssertError(await harness.Service.ExecuteAsync("get_space", wrongVersion), "authorization_denied");
        AssertError(await harness.Service.ExecuteAsync("get_space", wrongCapability), "authorization_denied");
    }

    [TestMethod]
    public async Task MutatingOperationsRejectIllegalLifecycleStates()
    {
        var activeHarness = CreateHarness(KnowledgeSpaceLifecycleStates.Active);
        var draftHarness = CreateHarness(KnowledgeSpaceLifecycleStates.Draft);

        foreach (var operation in new[]
                 {
                     "stage_ontology_version",
                     "validate_ontology_compatibility"
                 })
        {
            AssertError(
                await activeHarness.Service.ExecuteAsync(
                    operation,
                    CreateRequest(activeHarness.Snapshot, operation, new { })),
                "space_state_conflict");
        }

        foreach (var operation in new[] { "record_approval", "activate_ontology_version" })
        {
            AssertError(
                await draftHarness.Service.ExecuteAsync(
                    operation,
                    CreateRequest(draftHarness.Snapshot, operation, new { })),
                "space_state_conflict");
        }
    }

    [TestMethod]
    public async Task EveryOperationRejectsAStaleExecutionSnapshot()
    {
        foreach (var operation in Operations)
        {
            var harness = CreateHarness(KnowledgeSpaceLifecycleStates.Pending);
            harness.Spaces.Record = harness.Spaces.Record with
            {
                MutationPolicyVersion = "2.0"
            };

            var result = await harness.Service.ExecuteAsync(
                operation,
                CreateRequest(harness.Snapshot, operation, new { }));

            AssertError(result, "replan_required");
        }
    }

    [TestMethod]
    public async Task FirstVersionCompatibilitySucceedsWithoutMigration()
    {
        var harness = CreateHarness(KnowledgeSpaceLifecycleStates.Pending);
        var version = CreateVersion(harness.Spaces.Record.KnowledgeSpaceId);
        harness.Ontologies.Versions.Add(version.OntologyVersionId, version);

        var result = await harness.Service.ExecuteAsync(
            "validate_ontology_compatibility",
            CreateRequest(
                harness.Snapshot,
                "validate_ontology_compatibility",
                new
                {
                    assessmentId = "assessment-first",
                    ontologyVersionId = version.OntologyVersionId
                }));
        var response = ReadResponse(result);
        var compatibility = response.RootElement.GetProperty("result");

        Assert.IsFalse(result.IsError);
        Assert.IsTrue(compatibility.GetProperty("compatible").GetBoolean());
        Assert.IsFalse(compatibility.GetProperty("requiresMigration").GetBoolean());
        Assert.AreEqual(0, compatibility.GetProperty("findings").GetArrayLength());
    }

    [TestMethod]
    public async Task ExistingActiveVersionRequiresDeferredMigrationAndCannotBeApproved()
    {
        var activeVersion = CreateVersion("ks-test", "ontology-v1");
        var harness = CreateHarness(
            KnowledgeSpaceLifecycleStates.Maintenance,
            activeVersion.OntologyVersionId,
            activeVersion.PayloadDigest);
        var candidate = CreateVersion(harness.Spaces.Record.KnowledgeSpaceId, "ontology-v2");
        harness.Ontologies.Versions.Add(candidate.OntologyVersionId, candidate);

        var compatibility = await harness.Service.ExecuteAsync(
            "validate_ontology_compatibility",
            CreateRequest(
                harness.Snapshot,
                "validate_ontology_compatibility",
                new
                {
                    assessmentId = "assessment-migration",
                    ontologyVersionId = candidate.OntologyVersionId
                }));
        var response = ReadResponse(compatibility);

        Assert.IsFalse(compatibility.IsError);
        Assert.IsTrue(response.RootElement
            .GetProperty("result")
            .GetProperty("requiresMigration")
            .GetBoolean());
        Assert.IsFalse(harness.Ontologies.Assessments["assessment-migration"].IsApproved);

        harness.Spaces.Record = harness.Spaces.Record with
        {
            LifecycleState = KnowledgeSpaceLifecycleStates.Pending,
            ActiveOntologyVersionId = null,
            ActiveOntologyDigest = null
        };
        var approvalSnapshot = ExecutionContextSnapshot.Create(
            harness.Spaces.Record,
            "execution-approval",
            "trace-approval",
            DomainAgentIdentities.Ontology,
            DomainAgentIdentities.InitialDefinitionVersion,
            OntologyManagerUser);
        harness.Snapshots.Snapshots[approvalSnapshot.ExecutionId] = approvalSnapshot;
        var approval = await harness.Service.ExecuteAsync(
            "record_approval",
            CreateRequest(
                approvalSnapshot,
                "record_approval",
                new
                {
                    approvalId = "approval-invalid",
                    ontologyVersionId = candidate.OntologyVersionId,
                    compatibilityAssessmentId = "assessment-migration",
                    decision = "approve"
                }));

        AssertError(approval, "validation_failed");
    }

    [TestMethod]
    public async Task ImmutableStageRecordsRejectDuplicateIdentifiers()
    {
        var harness = CreateHarness(KnowledgeSpaceLifecycleStates.Pending);
        var payload = CreatePayload();
        var request = CreateRequest(
            harness.Snapshot,
            "stage_ontology_version",
            new { proposalId = "proposal-001", payload });

        var first = await harness.Service.ExecuteAsync("stage_ontology_version", request);
        using var firstResponse = ReadResponse(first);
        Assert.IsFalse(first.IsError, firstResponse.RootElement.GetRawText());
        AssertError(
            await harness.Service.ExecuteAsync("stage_ontology_version", request),
            "validation_failed");
    }

    [TestMethod]
    public async Task SnapshotEnvelopeMustMatchPersistedSnapshot()
    {
        var harness = CreateHarness(KnowledgeSpaceLifecycleStates.Pending);
        var request = CreateRequest(harness.Snapshot, "get_space", new { });
        var root = request;
        var changed = JsonSerializer.SerializeToElement(new
        {
            contractVersion = root.GetProperty("contractVersion").GetString(),
            operation = "get_space",
            knowledgeSpaceId = root.GetProperty("knowledgeSpaceId").GetString(),
            identity = root.GetProperty("identity"),
            executionContext = new
            {
                executionId = harness.Snapshot.ExecutionId,
                traceId = "substituted-trace",
                correlationId = "correlation-001",
                knowledgeSpaceId = harness.Snapshot.KnowledgeSpaceId,
                lifecycleState = harness.Snapshot.LifecycleState,
                ontologyVersion = harness.Snapshot.ActiveOntologyVersionId,
                ontologyDigest = harness.Snapshot.ActiveOntologyDigest,
                mutationPolicy = harness.Snapshot.MutationPolicy,
                mutationPolicyVersion = harness.Snapshot.MutationPolicyVersion,
                canonicalHeadVersion = harness.Snapshot.CanonicalHeadVersion
            },
            arguments = new { }
        });

        AssertError(
            await harness.Service.ExecuteAsync("get_space", changed),
            "replan_required");
    }

    public static IEnumerable<object[]> GetOperationNames() =>
        Operations.Select(operation => new object[] { operation });

    private const string OntologyManagerUser = "ontology-manager-001";

    private static Harness CreateHarness(
        string lifecycleState,
        string? activeOntologyVersionId = null,
        string? activeOntologyDigest = null,
        string initiatingUserId = OntologyManagerUser,
        string role = KnowledgeSpaceRoles.OntologyManager)
    {
        var record = KnowledgeSpaceControlRecord.Create(
            new KnowledgeSpaceCreation("Product", "review", "owner-001"),
            () => Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa"),
            DateTimeOffset.Parse("2026-08-30T00:00:00Z"))
            .GrantRole("owner-001", initiatingUserId, role) with
        {
            LifecycleState = lifecycleState,
            ActiveOntologyVersionId = activeOntologyVersionId,
            ActiveOntologyDigest = activeOntologyDigest,
            ETag = "etag-001"
        };
        var snapshot = ExecutionContextSnapshot.Create(
            record,
            "execution-001",
            "trace-001",
            DomainAgentIdentities.Ontology,
            DomainAgentIdentities.InitialDefinitionVersion,
            initiatingUserId,
            now: DateTimeOffset.Parse("2026-08-30T00:01:00Z"));
        var spaces = new InMemorySpaceRepository(record);
        var ontologies = new InMemoryOntologyRepository(spaces);
        var snapshots = new InMemorySnapshotRepository(snapshot);
        return new(
            new OntologyAgentToolService(spaces, ontologies, snapshots),
            spaces,
            ontologies,
            snapshots,
            snapshot);
    }

    private static JsonElement CreateRequest(
        ExecutionContextSnapshot snapshot,
        string operation,
        object arguments,
        string? actingAgentId = null,
        string? agentDefinitionVersion = null,
        string? requiredCapability = null) =>
        JsonSerializer.SerializeToElement(new
        {
            contractVersion = OntologyAgentToolService.ContractVersion,
            operation,
            knowledgeSpaceId = snapshot.KnowledgeSpaceId,
            identity = new
            {
                initiatingUserId = snapshot.InitiatingUserId,
                actingAgentId = actingAgentId ?? DomainAgentIdentities.Ontology,
                agentDefinitionVersion =
                    agentDefinitionVersion ?? DomainAgentIdentities.InitialDefinitionVersion,
                requiredCapability = requiredCapability ?? operation
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
        }, RequestJsonOptions);

    private static OntologyPayload CreatePayload(string versionId = "ontology-v1") =>
        new()
        {
            OntologyId = "ontology-product",
            OntologyVersionId = versionId,
            Title = "Product knowledge",
            Description = "Structures product decisions.",
            DocumentTypes =
            [
                new(
                    "decision-record",
                    "A decision.",
                    JsonSerializer.SerializeToElement(new
                    {
                        schema = "https://json-schema.org/draft/2020-12/schema",
                        type = "object"
                    }).RenameSchemaProperty())
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

    private static OntologyVersionEnvelope CreateVersion(
        string knowledgeSpaceId,
        string versionId = "ontology-v1")
    {
        var payload = CreatePayload(versionId);
        return new()
        {
            Id = versionId,
            RecordType = "ontologyVersion",
            KnowledgeSpaceId = knowledgeSpaceId,
            OntologyId = payload.OntologyId,
            OntologyVersionId = versionId,
            SchemaVersion = "1.0",
            Payload = payload,
            PayloadDigest = OntologyPayloadDigest.Compute(payload),
            CreatedAt = DateTimeOffset.Parse("2026-08-30T00:02:00Z"),
            CreatedBy = OntologyManagerUser
        };
    }

    private static JsonDocument ReadResponse(CallToolResult result)
    {
        var text = result.Content.OfType<TextContentBlock>().Single().Text;
        return JsonDocument.Parse(text);
    }

    private static void AssertError(CallToolResult result, string code)
    {
        using var response = ReadResponse(result);
        Assert.IsTrue(result.IsError, response.RootElement.GetRawText());
        Assert.AreEqual(
            code,
            response.RootElement.GetProperty("error").GetProperty("code").GetString());
        AssertSchemaValid(response.RootElement);
    }

    private static void AssertSchemaValid(JsonElement envelope)
    {
        var evaluation = PrivateSchema.Value.Evaluate(
            envelope,
            new EvaluationOptions { OutputFormat = OutputFormat.List });
        Assert.IsTrue(evaluation.IsValid, envelope.GetRawText());
    }

    private static JsonSchema LoadPrivateSchema()
    {
        var schemaPath = Path.Combine(
            AppContext.BaseDirectory,
            "contracts",
            "private",
            "v1.0",
            "private-deterministic-tools.schema.json");
        var schemaNode = JsonNode.Parse(File.ReadAllText(schemaPath))!.AsObject();
        schemaNode["$id"] = "https://our-iq.dev/contracts/private/v1.0/ontology-agent-tool-tests";
        return JsonSchema.FromText(schemaNode.ToJsonString());
    }

    private sealed record Harness(
        OntologyAgentToolService Service,
        InMemorySpaceRepository Spaces,
        InMemoryOntologyRepository Ontologies,
        InMemorySnapshotRepository Snapshots,
        ExecutionContextSnapshot Snapshot);

    private sealed class InMemorySpaceRepository(KnowledgeSpaceControlRecord record)
        : IKnowledgeSpaceControlRecordRepository
    {
        public KnowledgeSpaceControlRecord Record { get; set; } = record;

        public Task<KnowledgeSpaceControlRecord> CreateAsync(
            KnowledgeSpaceCreation creation,
            CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();

        public Task<KnowledgeSpaceControlRecord?> GetAsync(
            string knowledgeSpaceId,
            CancellationToken cancellationToken = default) =>
            Task.FromResult<KnowledgeSpaceControlRecord?>(
                knowledgeSpaceId == Record.KnowledgeSpaceId ? Record : null);

        public Task<KnowledgeSpaceControlRecordPage> ListAsync(
            KnowledgeSpaceControlRecordQuery query,
            CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();

        public Task<KnowledgeSpaceControlRecord> UpdateAsync(
            KnowledgeSpaceControlRecord updated,
            string expectedETag,
            CancellationToken cancellationToken = default)
        {
            Record = updated;
            return Task.FromResult(updated);
        }
    }

    private sealed class InMemorySnapshotRepository(ExecutionContextSnapshot snapshot)
        : IExecutionContextSnapshotRepository
    {
        public Dictionary<string, ExecutionContextSnapshot> Snapshots { get; } =
            new(StringComparer.Ordinal) { [snapshot.ExecutionId] = snapshot };

        public Task<ExecutionContextSnapshot> CreateAsync(
            ExecutionContextSnapshot created,
            CancellationToken cancellationToken = default)
        {
            Snapshots.Add(created.ExecutionId, created);
            return Task.FromResult(created);
        }

        public Task<ExecutionContextSnapshot?> GetAsync(
            string executionId,
            string knowledgeSpaceId,
            CancellationToken cancellationToken = default) =>
            Task.FromResult(
                Snapshots.TryGetValue(executionId, out var found)
                    && found.KnowledgeSpaceId == knowledgeSpaceId
                        ? found
                        : null);
    }

    private sealed class InMemoryOntologyRepository(InMemorySpaceRepository spaces)
        : IOntologyVersionRepository
    {
        public Dictionary<string, OntologyVersionEnvelope> Versions { get; } =
            new(StringComparer.Ordinal);

        public Dictionary<string, OntologyProposal> Proposals { get; } =
            new(StringComparer.Ordinal);

        public Dictionary<string, OntologyCompatibilityAssessment> Assessments { get; } =
            new(StringComparer.Ordinal);

        public Dictionary<string, OntologyApproval> Approvals { get; } =
            new(StringComparer.Ordinal);

        public int ActivationCount { get; private set; }

        public Task<OntologyVersionEnvelope?> GetVersionAsync(
            string ontologyVersionId,
            string knowledgeSpaceId,
            CancellationToken cancellationToken = default) =>
            Task.FromResult(
                Versions.TryGetValue(ontologyVersionId, out var version)
                    && version.KnowledgeSpaceId == knowledgeSpaceId
                        ? version
                        : null);

        public Task<OntologyVersionEnvelope> CreateVersionAsync(
            OntologyVersionEnvelope version,
            CancellationToken cancellationToken = default)
        {
            AddImmutable(Versions, version.Id, version, version.KnowledgeSpaceId);
            return Task.FromResult(version);
        }

        public Task<OntologyProposal> CreateProposalAsync(
            OntologyProposal proposal,
            CancellationToken cancellationToken = default)
        {
            AddImmutable(Proposals, proposal.Id, proposal, proposal.KnowledgeSpaceId);
            return Task.FromResult(proposal);
        }

        public Task<OntologyCompatibilityAssessment> CreateCompatibilityAssessmentAsync(
            OntologyCompatibilityAssessment assessment,
            CancellationToken cancellationToken = default)
        {
            AddImmutable(Assessments, assessment.Id, assessment, assessment.KnowledgeSpaceId);
            return Task.FromResult(assessment);
        }

        public Task<OntologyCompatibilityAssessment?> GetCompatibilityAssessmentAsync(
            string assessmentId,
            string knowledgeSpaceId,
            CancellationToken cancellationToken = default) =>
            Task.FromResult(
                Assessments.TryGetValue(assessmentId, out var assessment)
                    && assessment.KnowledgeSpaceId == knowledgeSpaceId
                        ? assessment
                        : null);

        public Task<OntologyApproval> CreateApprovalAsync(
            OntologyApproval approval,
            CancellationToken cancellationToken = default)
        {
            AddImmutable(Approvals, approval.Id, approval, approval.KnowledgeSpaceId);
            return Task.FromResult(approval);
        }

        public Task<OntologyApproval?> GetApprovalAsync(
            string approvalId,
            string knowledgeSpaceId,
            CancellationToken cancellationToken = default) =>
            Task.FromResult(
                Approvals.TryGetValue(approvalId, out var approval)
                    && approval.KnowledgeSpaceId == knowledgeSpaceId
                        ? approval
                        : null);

        public Task<KnowledgeSpaceControlRecord> ActivateAsync(
            OntologyActivationRequest request,
            CancellationToken cancellationToken = default)
        {
            if (spaces.Record.ActiveOntologyVersionId != request.ExpectedActiveOntologyVersionId
                || spaces.Record.ActiveOntologyDigest != request.ExpectedActiveOntologyDigest)
            {
                throw new OntologyActivationConflictException(request.KnowledgeSpaceId);
            }

            var version = Versions[request.OntologyVersionId];
            var approval = Approvals[request.ApprovalId];
            var assessment = Assessments[approval.CompatibilityAssessmentId];
            if (!approval.IsApproved || !assessment.IsApproved)
            {
                throw new OntologyPayloadValidationException("Approval is incomplete.");
            }

            ActivationCount++;
            spaces.Record = spaces.Record with
            {
                ActiveOntologyVersionId = version.OntologyVersionId,
                ActiveOntologyDigest = version.PayloadDigest,
                LifecycleState = KnowledgeSpaceLifecycleStates.Active
            };
            return Task.FromResult(spaces.Record);
        }

        private static void AddImmutable<T>(
            IDictionary<string, T> records,
            string id,
            T record,
            string knowledgeSpaceId)
        {
            if (!records.TryAdd(id, record))
            {
                throw new OntologyControlRecordConflictException(knowledgeSpaceId, id);
            }
        }
    }
}

internal static class JsonElementTestExtensions
{
    public static JsonElement RenameSchemaProperty(this JsonElement source)
    {
        using var document = JsonDocument.Parse(
            source.GetRawText().Replace("\"schema\":", "\"$schema\":", StringComparison.Ordinal));
        return document.RootElement.Clone();
    }
}
