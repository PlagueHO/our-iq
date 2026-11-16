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
            if (OntologyAgentToolService.IsMutating(operation))
            {
                var replay = await harness.Service.ExecuteAsync(
                    operation,
                    CreateRequest(harness.Snapshot, operation, arguments, correlationId: "retry-correlation"));
                Assert.AreEqual(result.Content.OfType<TextContentBlock>().Single().Text,
                    replay.Content.OfType<TextContentBlock>().Single().Text);
                Assert.IsFalse(replay.IsError);
            }
        }

        Assert.HasCount(1, harness.Ontologies.Versions);
        Assert.HasCount(1, harness.Ontologies.Proposals);
        Assert.HasCount(1, harness.Ontologies.Assessments);
        Assert.HasCount(1, harness.Ontologies.Approvals);
        Assert.AreEqual(1, harness.Ontologies.ActivationCount);
        Assert.HasCount(4, harness.Ontologies.Replays);
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
            await harness.Service.ExecuteAsync("stage_ontology_version",
                CreateRequest(harness.Snapshot, "stage_ontology_version",
                    new { proposalId = "proposal-001", payload }, idempotencyKey: "new-key")),
            "validation_failed");
    }

    [TestMethod]
    [DataRow("stage_ontology_version")]
    [DataRow("validate_ontology_compatibility")]
    [DataRow("record_approval")]
    [DataRow("activate_ontology_version")]
    public async Task DeterministicFailuresReplayTheOriginalOutcome(string operation)
    {
        var harness = CreateHarness(KnowledgeSpaceLifecycleStates.Pending);
        var first = await harness.Service.ExecuteAsync(
            operation, CreateRequest(harness.Snapshot, operation, new { }));
        harness.Spaces.Record = harness.Spaces.Record with { MutationPolicyVersion = "2.0" };
        var replay = await harness.Service.ExecuteAsync(
            operation, CreateRequest(harness.Snapshot, operation, new { }, correlationId: "retry"));

        AssertError(first, "validation_failed");
        Assert.AreEqual(first.Content.OfType<TextContentBlock>().Single().Text,
            replay.Content.OfType<TextContentBlock>().Single().Text);
        Assert.HasCount(1, harness.Ontologies.Replays);
        Assert.HasCount(0, harness.Ontologies.Versions);
    }

    [TestMethod]
    [DataRow("abcdefab-1234-5678-90ab-abcdef123456")]
    [DataRow("ABCDEFAB-1234-5678-90AB-ABCDEF123456")]
    public async Task EntraCallerCasingMatchesAuthorizationSnapshotAndSavedReplay(string persistedUserId)
    {
        var harness = CreateHarness(
            KnowledgeSpaceLifecycleStates.Pending, initiatingUserId: persistedUserId);
        const string operation = "stage_ontology_version";
        var arguments = new { proposalId = "proposal-001", payload = CreatePayload() };
        var request = JsonNode.Parse(CreateRequest(harness.Snapshot, operation, arguments).GetRawText())!;
        request["identity"]!["initiatingUserId"] = persistedUserId == persistedUserId.ToUpperInvariant()
            ? persistedUserId.ToLowerInvariant()
            : persistedUserId.ToUpperInvariant();
        var first = await harness.Service.ExecuteAsync(operation, JsonSerializer.SerializeToElement(request));
        Assert.IsFalse(first.IsError);

        harness.Spaces.Record = harness.Spaces.Record with { MutationPolicyVersion = "2.0" };
        var replay = await harness.Service.ExecuteAsync(
            operation, CreateRequest(harness.Snapshot, operation, arguments, correlationId: "retry"));
        Assert.IsFalse(replay.IsError);
        Assert.AreEqual(first.Content.OfType<TextContentBlock>().Single().Text,
            replay.Content.OfType<TextContentBlock>().Single().Text);
        Assert.HasCount(1, harness.Ontologies.Versions);
        Assert.HasCount(1, harness.Ontologies.Replays);
    }

    [TestMethod]
    [DataRow("abcdefab-1234-5678-90ab-abcdef123456", "bbcdefab-1234-5678-90ab-abcdef123456", "agent-ontology")]
    [DataRow("ontology-manager-001", "ONTOLOGY-MANAGER-001", "agent-ontology")]
    [DataRow("abcdefab-1234-5678-90ab-abcdef123456", "abcdefab-1234-5678-90ab-abcdef123456", "AGENT-ONTOLOGY")]
    public async Task CallerNormalizationDoesNotAuthorizeDistinctUsersOrAgentCasing(
        string persistedUserId, string requestedUserId, string requestedAgentId)
    {
        var harness = CreateHarness(
            KnowledgeSpaceLifecycleStates.Pending, initiatingUserId: persistedUserId);
        var request = JsonNode.Parse(CreateRequest(harness.Snapshot, "stage_ontology_version",
            new { proposalId = "proposal-001", payload = CreatePayload() }).GetRawText())!;
        request["identity"]!["initiatingUserId"] = requestedUserId;
        request["identity"]!["actingAgentId"] = requestedAgentId;

        AssertError(await harness.Service.ExecuteAsync(
            "stage_ontology_version", JsonSerializer.SerializeToElement(request)), "authorization_denied");
        Assert.HasCount(0, harness.Ontologies.Replays);
        Assert.HasCount(0, harness.Ontologies.Versions);
    }

    [TestMethod]
    public async Task DistinctAuthorizedEntraUserCannotUseAnotherUsersSnapshot()
    {
        const string snapshotUser = "abcdefab-1234-5678-90ab-abcdef123456";
        const string otherUser = "bbcdefab-1234-5678-90ab-abcdef123456";
        var harness = CreateHarness(
            KnowledgeSpaceLifecycleStates.Pending, initiatingUserId: snapshotUser);
        harness.Spaces.Record = harness.Spaces.Record.GrantRole(
            "owner-001", otherUser, KnowledgeSpaceRoles.OntologyManager);
        var request = JsonNode.Parse(CreateRequest(harness.Snapshot, "get_space", new { }).GetRawText())!;
        request["identity"]!["initiatingUserId"] = otherUser;

        AssertError(await harness.Service.ExecuteAsync(
            "get_space", JsonSerializer.SerializeToElement(request)), "replan_required");
        Assert.HasCount(0, harness.Ontologies.Replays);
    }

    [TestMethod]
    public async Task SavedStateFailureNeedsFreshSnapshotAndNewKeyAfterCorrection()
    {
        var harness = CreateHarness(KnowledgeSpaceLifecycleStates.Active);
        const string operation = "stage_ontology_version";
        var arguments = new { proposalId = "proposal-001", payload = CreatePayload() };
        var request = CreateRequest(harness.Snapshot, operation, arguments);
        var first = await harness.Service.ExecuteAsync(operation, request);
        AssertError(first, "space_state_conflict");
        AssertSavedFailureRemediation(first);

        harness.Spaces.Record = harness.Spaces.Record with { LifecycleState = KnowledgeSpaceLifecycleStates.Pending };
        var fresh = ExecutionContextSnapshot.Create(
            harness.Spaces.Record, "execution-new", "trace-new",
            DomainAgentIdentities.Ontology, DomainAgentIdentities.InitialDefinitionVersion, OntologyManagerUser);
        harness.Snapshots.Snapshots.Add(fresh.ExecutionId, fresh);
        var replay = await harness.Service.ExecuteAsync(operation, request);
        Assert.AreEqual(first.Content.OfType<TextContentBlock>().Single().Text,
            replay.Content.OfType<TextContentBlock>().Single().Text);
        AssertError(await harness.Service.ExecuteAsync(
            operation, CreateRequest(fresh, operation, arguments)), "idempotency_key_conflict");
        Assert.IsFalse((await harness.Service.ExecuteAsync(
            operation, CreateRequest(fresh, operation, arguments, idempotencyKey: "corrected-key"))).IsError);
    }

    [TestMethod]
    public async Task SavedPrerequisiteFailureNeedsNewKeyEvenWhenRequestDoesNotChange()
    {
        var harness = CreateHarness(KnowledgeSpaceLifecycleStates.Pending);
        const string operation = "validate_ontology_compatibility";
        var version = CreateVersion(harness.Spaces.Record.KnowledgeSpaceId);
        var arguments = new { assessmentId = "assessment-001", ontologyVersionId = version.OntologyVersionId };
        var request = CreateRequest(harness.Snapshot, operation, arguments);
        var first = await harness.Service.ExecuteAsync(operation, request);
        AssertError(first, "validation_failed");
        AssertSavedFailureRemediation(first);

        harness.Ontologies.Versions.Add(version.OntologyVersionId, version);
        var replay = await harness.Service.ExecuteAsync(operation, request);
        Assert.AreEqual(first.Content.OfType<TextContentBlock>().Single().Text,
            replay.Content.OfType<TextContentBlock>().Single().Text);
        Assert.IsFalse((await harness.Service.ExecuteAsync(operation,
            CreateRequest(harness.Snapshot, operation, arguments, idempotencyKey: "corrected-key"))).IsError);
        Assert.HasCount(1, harness.Ontologies.Assessments);
    }

    [TestMethod]
    public async Task SavedStaleSnapshotFailureRequiresFreshSnapshotAndNewKeyButReadsDoNot()
    {
        var harness = CreateHarness(KnowledgeSpaceLifecycleStates.Pending);
        harness.Spaces.Record = harness.Spaces.Record with { MutationPolicyVersion = "2.0" };
        var mutation = await harness.Service.ExecuteAsync("stage_ontology_version",
            CreateRequest(harness.Snapshot, "stage_ontology_version", new { }));
        AssertError(mutation, "replan_required");
        AssertSavedFailureRemediation(mutation);

        var read = await harness.Service.ExecuteAsync(
            "get_space", CreateRequest(harness.Snapshot, "get_space", new { }));
        AssertError(read, "replan_required");
        using var response = ReadResponse(read);
        Assert.AreEqual("Create a fresh execution-context snapshot and retry.",
            response.RootElement.GetProperty("error").GetProperty("remediation").GetString());
        Assert.HasCount(1, harness.Ontologies.Replays);
    }

    private static void AssertSavedFailureRemediation(CallToolResult result)
    {
        using var response = ReadResponse(result);
        var remediation = response.RootElement.GetProperty("error").GetProperty("remediation").GetString()!;
        StringAssert.Contains(remediation, "new idempotency key");
        StringAssert.Contains(remediation, "execution-context snapshot");
    }

    [TestMethod]
    public async Task ReplayRequiresCurrentAccessAndRejectsChangedSnapshots()
    {
        var harness = CreateHarness(KnowledgeSpaceLifecycleStates.Pending);
        const string operation = "stage_ontology_version";
        var arguments = new { proposalId = "proposal-001", payload = CreatePayload() };
        var request = CreateRequest(harness.Snapshot, operation, arguments);
        Assert.IsFalse((await harness.Service.ExecuteAsync(operation, request)).IsError);
        var changed = JsonNode.Parse(request.GetRawText())!;
        changed["executionContext"]!["executionId"] = "new-execution";
        AssertError(await harness.Service.ExecuteAsync(operation,
            JsonSerializer.SerializeToElement(changed)), "idempotency_key_conflict");

        harness.Spaces.Record = harness.Spaces.Record.RevokeRole(
            "owner-001", OntologyManagerUser, KnowledgeSpaceRoles.OntologyManager);
        AssertError(await harness.Service.ExecuteAsync(operation, request), "authorization_denied");
        Assert.HasCount(1, harness.Ontologies.Replays);
    }

    [TestMethod]
    public async Task MalformedEnvelopesAndKeyFreeReadsDoNotSaveOutcomes()
    {
        var harness = CreateHarness(KnowledgeSpaceLifecycleStates.Pending);
        var request = JsonNode.Parse(
            CreateRequest(harness.Snapshot, "stage_ontology_version", new { }).GetRawText())!;
        foreach (var key in new[] { null, "", "   " })
        {
            request["idempotencyKey"] = key;
            AssertError(await harness.Service.ExecuteAsync(
                "stage_ontology_version", JsonSerializer.SerializeToElement(request)), "validation_failed");
        }

        request.AsObject().Remove("idempotencyKey");
        AssertError(await harness.Service.ExecuteAsync(
            "stage_ontology_version", JsonSerializer.SerializeToElement(request)), "validation_failed");
        request["contractVersion"] = "2.0";
        AssertError(await harness.Service.ExecuteAsync(
            "stage_ontology_version", JsonSerializer.SerializeToElement(request)), "contract_version_unsupported");

        var read = JsonNode.Parse(CreateRequest(harness.Snapshot, "get_space", new { }).GetRawText())!;
        read.AsObject().Remove("idempotencyKey");
        Assert.IsFalse((await harness.Service.ExecuteAsync(
            "get_space", JsonSerializer.SerializeToElement(read))).IsError);
        Assert.HasCount(0, harness.Ontologies.Replays);
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

    [TestMethod]
    public async Task LookupAndTransientCommitFailuresCannotCreateTerminalOutcomes()
    {
        var harness = CreateHarness(KnowledgeSpaceLifecycleStates.Pending);
        const string operation = "stage_ontology_version";
        var request = CreateRequest(harness.Snapshot, operation,
            new { proposalId = "proposal-001", payload = CreatePayload() });
        harness.Ontologies.LookupFailure = new HttpRequestException("Synthetic dependency failure.");
        AssertError(await harness.Service.ExecuteAsync(operation, request), "dependency_unavailable");
        Assert.AreEqual(0, harness.Ontologies.CommitAttempts);
        harness.Ontologies.LookupFailure = null;
        harness.Ontologies.CommitFailure = new MutationDependencyException();
        var dependencyFailure = await harness.Service.ExecuteAsync(operation, request);
        AssertError(dependencyFailure, "dependency_unavailable");
        using var dependencyResponse = ReadResponse(dependencyFailure);
        Assert.AreEqual("Retry the original request with the same idempotency key.",
            dependencyResponse.RootElement.GetProperty("error").GetProperty("remediation").GetString());
        Assert.HasCount(0, harness.Ontologies.Replays);
        harness.Ontologies.CommitFailure = null;
        Assert.IsFalse((await harness.Service.ExecuteAsync(operation, request)).IsError);
        Assert.HasCount(1, harness.Ontologies.Versions);
    }

    [TestMethod]
    public async Task ContentionIsBoundedAndCancellationIsNotSaved()
    {
        var harness = CreateHarness(KnowledgeSpaceLifecycleStates.Pending);
        const string operation = "stage_ontology_version";
        var request = CreateRequest(harness.Snapshot, operation,
            new { proposalId = "proposal-001", payload = CreatePayload() });
        harness.Ontologies.CommitFailure = new MutationConcurrencyException();
        AssertError(await harness.Service.ExecuteAsync(operation, request), "dependency_unavailable");
        Assert.AreEqual(3, harness.Ontologies.CommitAttempts);
        Assert.HasCount(0, harness.Ontologies.Replays);
        harness.Ontologies.CommitFailure = new TaskCanceledException("Synthetic transport timeout.");
        AssertError(await harness.Service.ExecuteAsync(operation, request), "dependency_unavailable");
        Assert.HasCount(0, harness.Ontologies.Replays);
        harness.Ontologies.CommitFailure = new OperationCanceledException();
        using var cancellation = new CancellationTokenSource();
        cancellation.Cancel();
        await Assert.ThrowsExactlyAsync<OperationCanceledException>(() =>
            harness.Service.ExecuteAsync(operation, request, cancellation.Token));
        Assert.HasCount(0, harness.Ontologies.Replays);
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
        string? requiredCapability = null,
        string? idempotencyKey = null,
        string correlationId = "correlation-001") =>
        JsonSerializer.SerializeToElement(new
        {
            contractVersion = OntologyAgentToolService.ContractVersion,
            operation,
            idempotencyKey = idempotencyKey ?? $"key-{operation}",
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
                correlationId,
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

    internal static OntologyPayload CreatePayload(string versionId = "ontology-v1") =>
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
        public Dictionary<string, IdempotencyRecord> Replays { get; } = new(StringComparer.Ordinal);
        public Exception? LookupFailure { get; set; }
        public Exception? CommitFailure { get; set; }
        public int CommitAttempts { get; private set; }

        public Task<IdempotencyRecord?> GetReplayAsync(
            IdempotencyIdentity identity, CancellationToken cancellationToken = default) =>
            LookupFailure is { } failure
                ? Task.FromException<IdempotencyRecord?>(failure)
                : Task.FromResult(Replays.TryGetValue(identity.Id, out var saved) ? saved.Match(identity) : null);

        public Task<IdempotencyRecord> SaveFailureAsync(
            IdempotencyRecord outcome, CancellationToken cancellationToken = default) =>
            CommitAsync(outcome, () => Task.CompletedTask);

        public Task<IdempotencyRecord> CommitStagedVersionAsync(
            OntologyVersionEnvelope version, OntologyProposal proposal,
            KnowledgeSpaceControlRecord expectedControl, IdempotencyRecord outcome,
            CancellationToken cancellationToken = default) =>
            CommitAsync(outcome, async () =>
            {
                if (Versions.ContainsKey(version.Id) || Proposals.ContainsKey(proposal.Id))
                {
                    throw new OntologyControlRecordConflictException(version.KnowledgeSpaceId, version.Id);
                }

                await CreateVersionAsync(version, cancellationToken);
                await CreateProposalAsync(proposal, cancellationToken);
            });

        public Task<IdempotencyRecord> CommitAssessmentAsync(
            OntologyCompatibilityAssessment assessment, KnowledgeSpaceControlRecord expectedControl,
            IdempotencyRecord outcome, CancellationToken cancellationToken = default) =>
            CommitAsync(outcome, () => CreateCompatibilityAssessmentAsync(assessment, cancellationToken));

        public Task<IdempotencyRecord> CommitApprovalAsync(
            OntologyApproval approval, KnowledgeSpaceControlRecord expectedControl,
            IdempotencyRecord outcome, CancellationToken cancellationToken = default) =>
            CommitAsync(outcome, () => CreateApprovalAsync(approval, cancellationToken));

        public Task<IdempotencyRecord> CommitActivationAsync(
            OntologyActivationRequest request, KnowledgeSpaceControlRecord expectedControl,
            IdempotencyRecord outcome, CancellationToken cancellationToken = default) =>
            CommitAsync(outcome, () => ActivateAsync(request, cancellationToken));

        private async Task<IdempotencyRecord> CommitAsync(IdempotencyRecord outcome, Func<Task> write)
        {
            CommitAttempts++;
            if (CommitFailure is { } failure)
            {
                throw failure;
            }

            if (Replays.TryGetValue(outcome.Identity.Id, out var saved))
            {
                return saved.Match(outcome.Identity);
            }

            await write();
            Replays.Add(outcome.Identity.Id, outcome);
            return outcome;
        }

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
