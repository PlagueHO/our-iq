using System.Net;
using System.Text.Json;
using Microsoft.Azure.Cosmos;
using Microsoft.Extensions.Options;
using OurIQ.Domain;
using OurIQ.ToolServices;

namespace OurIQ.ComponentTests;

[TestClass]
[TestCategory("Integration")]
[DoNotParallelize]
public sealed class IdempotencyCosmosRepositoryComponentTests
{
    [TestMethod]
    public async Task ConcurrentIdenticalStagingCommitsOneSetAndSurvivesRestartWithoutExpiry()
    {
        await using var fixture = await Fixture.CreateAsync();
        var (version, proposal) = fixture.StageRecords();
        var outcome = Outcome(fixture.Control);
        var results = await Task.WhenAll(
            fixture.Repository.CommitStagedVersionAsync(version, proposal, fixture.Control, outcome),
            fixture.NewRepository().CommitStagedVersionAsync(
                version, proposal, fixture.Control, outcome with { ResponseJson = """{"other":"response"}""" }));

        Assert.AreEqual(results[0], results[1]);
        Assert.AreEqual(results[0], await fixture.NewRepository().GetReplayAsync(outcome.Identity));
        Assert.AreEqual(1, await fixture.CountAsync("ontologyVersion"));
        Assert.AreEqual(1, await fixture.CountAsync(OntologyControlRecordTypes.Proposal));
        Assert.AreEqual(1, await fixture.CountAsync("idempotencyOutcome"));
        using var stored = await fixture.ReadAsync(outcome.Identity.Id);
        Assert.AreEqual(-1, stored.RootElement.GetProperty("ttl").GetInt32());
        Assert.AreEqual("1.0", stored.RootElement.GetProperty("recordVersion").GetString());
    }

    [TestMethod]
    public async Task ChangedInputRaceHasOneWinnerAndOneConflict()
    {
        await using var fixture = await Fixture.CreateAsync();
        var (version, proposal) = fixture.StageRecords();
        var original = Outcome(fixture.Control);
        var changed = Outcome(fixture.Control, input: "different");
        var results = await Task.WhenAll(Attempt(original), Attempt(changed));

        Assert.AreEqual(1, results.Count(result => result is not null));
        Assert.AreEqual(1, await fixture.CountAsync("idempotencyOutcome"));
        Assert.AreEqual(1, await fixture.CountAsync(OntologyControlRecordTypes.Proposal));
        var winner = results.Single(result => result is not null)!;
        Assert.AreEqual(winner, await fixture.NewRepository().GetReplayAsync(winner.Identity));

        async Task<IdempotencyRecord?> Attempt(IdempotencyRecord outcome)
        {
            try
            {
                return await fixture.Repository.CommitStagedVersionAsync(
                    version, proposal, fixture.Control, outcome);
            }
            catch (IdempotencyKeyConflictException)
            {
                return null;
            }
        }
    }

    [TestMethod]
    [DataRow(true)]
    [DataRow(false)]
    public async Task FailedStageRollsBackProposalVersionAndOutcome(bool staleControl)
    {
        await using var fixture = await Fixture.CreateAsync();
        var (version, proposal) = fixture.StageRecords();
        var outcome = Outcome(fixture.Control);
        if (staleControl)
        {
            await fixture.Spaces.UpdateAsync(
                fixture.Control with { DisplayName = "Changed" }, fixture.Control.ETag!);
            await Assert.ThrowsExactlyAsync<MutationConcurrencyException>(() =>
                fixture.Repository.CommitStagedVersionAsync(version, proposal, fixture.Control, outcome));
            Assert.IsNull(await fixture.Repository.GetVersionAsync(version.Id, version.KnowledgeSpaceId));
        }
        else
        {
            await fixture.Repository.CreateVersionAsync(version);
            await Assert.ThrowsExactlyAsync<OntologyControlRecordConflictException>(() =>
                fixture.Repository.CommitStagedVersionAsync(version, proposal, fixture.Control, outcome));
        }

        Assert.AreEqual(0, await fixture.CountAsync(OntologyControlRecordTypes.Proposal));
        Assert.IsNull(await fixture.Repository.GetReplayAsync(outcome.Identity));
    }

    [TestMethod]
    public async Task ActivationCommitsControlEvidenceAndOutcomeTogether()
    {
        await using var fixture = await Fixture.CreateAsync();
        var request = await fixture.SeedActivationAsync();
        var outcome = Outcome(fixture.Control, operation: "activate_ontology_version");
        var result = await fixture.Repository.CommitActivationAsync(request, fixture.Control, outcome);
        var current = await fixture.Spaces.GetAsync(fixture.Control.KnowledgeSpaceId);

        Assert.IsNotNull(current);
        Assert.AreEqual(KnowledgeSpaceLifecycleStates.Active, current.LifecycleState);
        Assert.AreEqual(request.OntologyVersionId, current.ActiveOntologyVersionId);
        using var evidence = await fixture.ReadAsync(request.ActivationEvidenceId);
        Assert.AreEqual(OntologyControlRecordTypes.ActivationEvidence,
            evidence.RootElement.GetProperty("recordType").GetString());
        Assert.AreEqual(result, await fixture.NewRepository().CommitActivationAsync(
            request, fixture.Control, outcome with { ResponseJson = """{"retry":"must not replace"}""" }));
        Assert.AreEqual(1, await fixture.CountAsync(OntologyControlRecordTypes.ActivationEvidence));
    }

    [TestMethod]
    public async Task FailedActivationLeavesNoEvidenceOrSuccessOutcome()
    {
        await using var fixture = await Fixture.CreateAsync();
        var request = await fixture.SeedActivationAsync();
        await fixture.Spaces.UpdateAsync(
            fixture.Control with { MutationPolicyVersion = "2.0" }, fixture.Control.ETag!);
        var outcome = Outcome(fixture.Control, operation: "activate_ontology_version");

        await Assert.ThrowsExactlyAsync<MutationConcurrencyException>(() =>
            fixture.Repository.CommitActivationAsync(request, fixture.Control, outcome));
        var current = await fixture.Spaces.GetAsync(fixture.Control.KnowledgeSpaceId);
        Assert.IsNotNull(current);
        Assert.IsNull(current.ActiveOntologyVersionId);
        Assert.AreEqual(0, await fixture.CountAsync(OntologyControlRecordTypes.ActivationEvidence));
        Assert.IsNull(await fixture.Repository.GetReplayAsync(outcome.Identity));
    }

    [TestMethod]
    public async Task SavedFailuresSurviveRestartAndScopesStaySeparate()
    {
        await using var fixture = await Fixture.CreateAsync();
        var otherSpace = await fixture.Spaces.CreateAsync(new("Other", "review", "owner-001"));
        var outcomes = new[]
        {
            Outcome(fixture.Control),
            Outcome(fixture.Control, user: "other-user"),
            Outcome(fixture.Control, operation: "record_approval"),
            Outcome(otherSpace)
        }.Select(outcome => outcome with { IsError = true }).ToArray();

        foreach (var outcome in outcomes)
        {
            Assert.AreEqual(outcome, await fixture.Repository.SaveFailureAsync(outcome));
            Assert.AreEqual(outcome, await fixture.NewRepository().GetReplayAsync(outcome.Identity));
            Assert.AreEqual(outcome, await fixture.NewRepository().SaveFailureAsync(
                outcome with { ResponseJson = """{"replacement":"not allowed"}""" }));
        }

        await Assert.ThrowsExactlyAsync<IdempotencyKeyConflictException>(() =>
            fixture.Repository.SaveFailureAsync(Outcome(fixture.Control, input: "changed") with { IsError = true }));
    }

    [TestMethod]
    public async Task StagingCannotOccupyAnotherKeysReplayId()
    {
        await using var fixture = await Fixture.CreateAsync();
        var outcomeA = Outcome(fixture.Control);
        var outcomeB = Outcome(fixture.Control, key: "other-key");
        var (version, proposal) = fixture.StageRecords();

        await Assert.ThrowsExactlyAsync<OntologyPayloadValidationException>(() =>
            fixture.Repository.CommitStagedVersionAsync(
                version, proposal with { Id = outcomeA.Identity.Id }, fixture.Control, outcomeB));

        Assert.AreEqual(0, await fixture.CountAsync("ontologyVersion"));
        Assert.AreEqual(0, await fixture.CountAsync(OntologyControlRecordTypes.Proposal));
        Assert.IsNull(await fixture.Repository.GetReplayAsync(outcomeA.Identity));
        Assert.IsNull(await fixture.Repository.GetReplayAsync(outcomeB.Identity));
        Assert.AreEqual(outcomeA, await fixture.Repository.CommitStagedVersionAsync(
            version, proposal, fixture.Control, outcomeA));
        Assert.AreEqual(outcomeA, await fixture.NewRepository().GetReplayAsync(outcomeA.Identity));
    }

    [TestMethod]
    [DataRow("ontologyVersion")]
    [DataRow("ontologyProposal")]
    [DataRow("ontologyCompatibilityAssessment")]
    [DataRow("ontologyApproval")]
    [DataRow("executionContextSnapshot")]
    [DataRow("knowledgeSpaceControl")]
    public async Task StandaloneWritersCannotOccupyReplayIds(string recordType)
    {
        await using var fixture = await Fixture.CreateAsync();
        var outcome = Outcome(fixture.Control) with { IsError = true };
        var id = outcome.Identity.Id;
        var (version, proposal) = fixture.StageRecords(versionId: id);
        switch (recordType)
        {
            case "ontologyVersion":
                await Assert.ThrowsExactlyAsync<OntologyPayloadValidationException>(() =>
                    fixture.Repository.CreateVersionAsync(version));
                break;
            case "ontologyProposal":
                await Assert.ThrowsExactlyAsync<OntologyPayloadValidationException>(() =>
                    fixture.Repository.CreateProposalAsync(proposal with { Id = id }));
                break;
            case "ontologyCompatibilityAssessment":
                await Assert.ThrowsExactlyAsync<OntologyPayloadValidationException>(() =>
                    fixture.Repository.CreateCompatibilityAssessmentAsync(new()
                    {
                        Id = id,
                        KnowledgeSpaceId = fixture.Control.KnowledgeSpaceId,
                        OntologyVersionId = version.Id,
                        CreatedBy = "owner-001",
                        CreatedAt = DateTimeOffset.UtcNow
                    }));
                break;
            case "ontologyApproval":
                await Assert.ThrowsExactlyAsync<OntologyPayloadValidationException>(() =>
                    fixture.Repository.CreateApprovalAsync(new()
                    {
                        Id = id,
                        KnowledgeSpaceId = fixture.Control.KnowledgeSpaceId,
                        OntologyVersionId = version.Id,
                        CompatibilityAssessmentId = "assessment-001",
                        ActorId = "owner-001",
                        Authority = KnowledgeSpaceRoles.OntologyManager,
                        CreatedAt = DateTimeOffset.UtcNow
                    }));
                break;
            case "executionContextSnapshot":
                var snapshot = ExecutionContextSnapshot.Create(
                    fixture.Control, id, "trace-001", DomainAgentIdentities.Ontology,
                    DomainAgentIdentities.InitialDefinitionVersion, "owner-001");
                await Assert.ThrowsExactlyAsync<OntologyPayloadValidationException>(() =>
                    fixture.Snapshots.CreateAsync(snapshot));
                break;
            case "knowledgeSpaceControl":
                await Assert.ThrowsExactlyAsync<KnowledgeSpaceControlRecordValidationException>(() =>
                    fixture.Spaces.UpdateAsync(
                        fixture.Control with { KnowledgeSpaceId = id }, fixture.Control.ETag!));
                break;
            default:
                Assert.Fail($"Unexpected record type '{recordType}'.");
                break;
        }

        Assert.IsNull(await fixture.Repository.GetReplayAsync(outcome.Identity));
        Assert.AreEqual(outcome, await fixture.Repository.SaveFailureAsync(outcome));
        Assert.AreEqual(outcome, await fixture.NewRepository().GetReplayAsync(outcome.Identity));
    }

    [TestMethod]
    [DataRow(false)]
    [DataRow(true)]
    public async Task ActivationWritersCannotOccupyReplayIds(bool atomic)
    {
        await using var fixture = await Fixture.CreateAsync();
        var request = await fixture.SeedActivationAsync();
        var outcome = Outcome(fixture.Control, operation: "activate_ontology_version");
        request = request with { ActivationEvidenceId = outcome.Identity.Id };

        await Assert.ThrowsExactlyAsync<OntologyPayloadValidationException>(async () =>
        {
            if (atomic)
            {
                await fixture.Repository.CommitActivationAsync(request, fixture.Control, outcome);
            }
            else
            {
                await fixture.Repository.ActivateAsync(request);
            }
        });

        Assert.AreEqual(0, await fixture.CountAsync(OntologyControlRecordTypes.ActivationEvidence));
        Assert.IsNull(await fixture.Repository.GetReplayAsync(outcome.Identity));
        var current = await fixture.Spaces.GetAsync(fixture.Control.KnowledgeSpaceId);
        Assert.IsNotNull(current);
        Assert.IsNull(current.ActiveOntologyVersionId);
        var failure = outcome with { IsError = true };
        Assert.AreEqual(failure, await fixture.Repository.SaveFailureAsync(failure));
    }

    [TestMethod]
    public async Task OversizedBatchIsRejectedBeforeWrites()
    {
        await using var fixture = await Fixture.CreateAsync();
        var (version, proposal) = fixture.StageRecords();
        var outcome = Outcome(fixture.Control) with { ResponseJson = new string('x', 2 * 1024 * 1024) };

        await Assert.ThrowsExactlyAsync<OntologyPayloadValidationException>(() =>
            fixture.Repository.CommitStagedVersionAsync(version, proposal, fixture.Control, outcome));
        Assert.AreEqual(0, await fixture.CountAsync("ontologyVersion"));
        Assert.AreEqual(0, await fixture.CountAsync(OntologyControlRecordTypes.Proposal));
        Assert.IsNull(await fixture.Repository.GetReplayAsync(outcome.Identity));
    }

    [TestMethod]
    public async Task ServiceSizeRejectionDoesNotBecomeADependencyFault()
    {
        var handler = new BatchFaultHandler(BatchFault.RejectSize);
        await using var fixture = await Fixture.CreateAsync(handler);
        var (version, proposal) = fixture.StageRecords();
        var outcome = Outcome(fixture.Control);

        await Assert.ThrowsExactlyAsync<OntologyPayloadValidationException>(() =>
            fixture.Repository.CommitStagedVersionAsync(version, proposal, fixture.Control, outcome));
        Assert.IsTrue(handler.Injected);
        Assert.AreEqual(0, await fixture.CountAsync("ontologyVersion"));
        Assert.AreEqual(0, await fixture.CountAsync(OntologyControlRecordTypes.Proposal));
        Assert.IsNull(await fixture.Repository.GetReplayAsync(outcome.Identity));
    }

    [TestMethod]
    [DataRow(false)]
    [DataRow(true)]
    public async Task LostBatchResponseIsReconciledFromTheCommittedOutcome(bool transportTimeout)
    {
        var handler = new BatchFaultHandler(
            transportTimeout ? BatchFault.LoseResponseWithTimeout : BatchFault.LoseResponse);
        await using var fixture = await Fixture.CreateAsync(handler);
        var (version, proposal) = fixture.StageRecords();
        var outcome = Outcome(fixture.Control);
        var result = await fixture.Repository.CommitStagedVersionAsync(
            version, proposal, fixture.Control, outcome);

        Assert.IsTrue(handler.Injected);
        Assert.AreEqual(outcome, result);
        Assert.AreEqual(outcome, await fixture.NewRepository().GetReplayAsync(outcome.Identity));
        Assert.AreEqual(1, await fixture.CountAsync("ontologyVersion"));
        Assert.AreEqual(1, await fixture.CountAsync(OntologyControlRecordTypes.Proposal));
    }

    private static IdempotencyRecord Outcome(
        KnowledgeSpaceControlRecord space,
        string input = "original",
        string user = "user-001",
        string operation = "stage_ontology_version",
        string key = "same-key")
    {
        var request = JsonSerializer.SerializeToElement(new
        {
            knowledgeSpaceId = space.KnowledgeSpaceId,
            idempotencyKey = key,
            identity = new { initiatingUserId = user },
            executionContext = new { correlationId = "original-correlation" },
            arguments = new { input }
        });
        return new(
            IdempotencyIdentity.Create(operation, user, DomainAgentIdentities.Ontology, request),
            """{"outcome":"completed","correlationId":"original-correlation"}""",
            false,
            DateTimeOffset.UtcNow);
    }

    private enum BatchFault { RejectSize, LoseResponse, LoseResponseWithTimeout }

    private sealed class BatchFaultHandler(BatchFault fault) : RequestHandler
    {
        public bool Injected { get; private set; }

        public override async Task<ResponseMessage> SendAsync(
            RequestMessage request, CancellationToken cancellationToken)
        {
            if (Injected || !string.Equals(
                    request.Headers["x-ms-cosmos-is-batch-request"], "True", StringComparison.OrdinalIgnoreCase))
            {
                return await base.SendAsync(request, cancellationToken);
            }

            Injected = true;
            if (fault == BatchFault.RejectSize)
            {
                return new ResponseMessage(HttpStatusCode.RequestEntityTooLarge, request);
            }

            using var response = await base.SendAsync(request, cancellationToken);
            Assert.IsTrue(response.IsSuccessStatusCode);
            if (fault == BatchFault.LoseResponseWithTimeout)
            {
                throw new TaskCanceledException("Synthetic transport timeout after commit.");
            }

            throw new HttpRequestException("Synthetic lost batch response.");
        }
    }

    private sealed class Fixture : IAsyncDisposable
    {
        private readonly CosmosClient client;
        private readonly KnowledgeSpaceCosmosOptions options;

        private Fixture(CosmosClient client, KnowledgeSpaceCosmosOptions options,
            KnowledgeSpaceCosmosRepository spaces, KnowledgeSpaceControlRecord control)
        {
            this.client = client;
            this.options = options;
            Spaces = spaces;
            Control = control;
            Repository = NewRepository();
        }

        public KnowledgeSpaceCosmosRepository Spaces { get; }
        public KnowledgeSpaceControlRecord Control { get; private set; }
        public OntologyVersionCosmosRepository Repository { get; }
        public ExecutionContextSnapshotCosmosRepository Snapshots => new(client, Options.Create(options));
        private Container Container => client.GetContainer(options.DatabaseName, options.ContainerName);

        public static async Task<Fixture> CreateAsync(RequestHandler? handler = null)
        {
            var connectionString = Environment.GetEnvironmentVariable("OURIQ_COSMOS_CONNECTION_STRING");
            if (string.IsNullOrWhiteSpace(connectionString))
            {
                Assert.Inconclusive("Set OURIQ_COSMOS_CONNECTION_STRING to run Cosmos emulator component tests.");
            }

            var clientOptions = new CosmosClientOptions { ConnectionMode = ConnectionMode.Gateway };
            if (handler is not null)
            {
                clientOptions.CustomHandlers.Add(handler);
            }

            var client = new CosmosClient(connectionString, clientOptions);
            var options = new KnowledgeSpaceCosmosOptions
            {
                DatabaseName = $"ouriq-idempotency-{Guid.NewGuid():N}",
                ContainerName = "knowledgeSpaceControl"
            };
            var spaces = new KnowledgeSpaceCosmosRepository(client, Options.Create(options));
            var draft = await spaces.CreateAsync(new("Product", "review", "owner-001"));
            var control = await spaces.UpdateAsync(
                draft.TransitionTo(KnowledgeSpaceLifecycleStates.Pending, "owner-001"), draft.ETag!);
            return new(client, options, spaces, control);
        }

        public OntologyVersionCosmosRepository NewRepository() => new(client, Options.Create(options));

        public (OntologyVersionEnvelope Version, OntologyProposal Proposal) StageRecords(
            string versionId = "version-001")
        {
            var payload = new OntologyPayload
            {
                OntologyId = "ontology-product",
                OntologyVersionId = versionId,
                Title = "Product knowledge",
                Description = "Product decisions.",
                DocumentTypes = [new("decision", "A decision.",
                    JsonSerializer.SerializeToElement(new Dictionary<string, string>
                    {
                        ["$schema"] = "https://json-schema.org/draft/2020-12/schema",
                        ["type"] = "object"
                    }))],
                Hierarchy = new(["decision"], [])
            };
            var version = new OntologyVersionEnvelope
            {
                Id = payload.OntologyVersionId,
                RecordType = "ontologyVersion",
                KnowledgeSpaceId = Control.KnowledgeSpaceId,
                OntologyId = payload.OntologyId,
                OntologyVersionId = payload.OntologyVersionId,
                SchemaVersion = "1.0",
                Payload = payload,
                PayloadDigest = OntologyPayloadDigest.Compute(payload),
                CreatedAt = DateTimeOffset.UtcNow,
                CreatedBy = "owner-001"
            };
            return (version, new OntologyProposal
            {
                Id = "proposal-001",
                KnowledgeSpaceId = Control.KnowledgeSpaceId,
                OntologyVersionId = version.Id,
                CreatedAt = version.CreatedAt,
                CreatedBy = "owner-001"
            });
        }

        public async Task<OntologyActivationRequest> SeedActivationAsync()
        {
            var (version, _) = StageRecords();
            await Repository.CreateVersionAsync(version);
            var assessment = new OntologyCompatibilityAssessment
            {
                Id = "assessment-001",
                KnowledgeSpaceId = Control.KnowledgeSpaceId,
                OntologyVersionId = version.Id,
                IsApproved = true,
                CreatedAt = version.CreatedAt,
                CreatedBy = "owner-001"
            };
            await Repository.CommitAssessmentAsync(
                assessment, Control, Outcome(Control, operation: "validate_ontology_compatibility"));
            Control = await Spaces.GetAsync(Control.KnowledgeSpaceId)
                ?? throw new InvalidOperationException("The assessment control fence is missing.");
            var approval = new OntologyApproval
            {
                Id = "approval-001",
                KnowledgeSpaceId = Control.KnowledgeSpaceId,
                OntologyVersionId = version.Id,
                CompatibilityAssessmentId = assessment.Id,
                ActorId = "owner-001",
                Authority = KnowledgeSpaceRoles.OntologyManager,
                IsApproved = true,
                CreatedAt = version.CreatedAt
            };
            await Repository.CommitApprovalAsync(
                approval, Control, Outcome(Control, operation: "record_approval"));
            Control = await Spaces.GetAsync(Control.KnowledgeSpaceId)
                ?? throw new InvalidOperationException("The approval control fence is missing.");
            return new(Control.KnowledgeSpaceId, version.Id, version.PayloadDigest, approval.Id,
                null, null, "activation-001");
        }

        public async Task<int> CountAsync(string recordType)
        {
            using var iterator = Container.GetItemQueryIterator<int>(
                new QueryDefinition("SELECT VALUE COUNT(1) FROM c WHERE c.recordType = @type")
                    .WithParameter("@type", recordType),
                requestOptions: new QueryRequestOptions { PartitionKey = new(Control.KnowledgeSpaceId) });
            return (await iterator.ReadNextAsync()).Single();
        }

        public async Task<JsonDocument> ReadAsync(string id)
        {
            using var response = await Container.ReadItemStreamAsync(id, new(Control.KnowledgeSpaceId));
            Assert.IsTrue(response.IsSuccessStatusCode);
            return await JsonDocument.ParseAsync(response.Content);
        }

        public async ValueTask DisposeAsync()
        {
            try
            {
                await client.GetDatabase(options.DatabaseName).DeleteAsync();
            }
            finally
            {
                client.Dispose();
            }
        }
    }
}
