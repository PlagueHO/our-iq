using System.Net;
using System.Text;
using Microsoft.Azure.Cosmos;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using OurIQ.Domain;

namespace OurIQ.ToolServices;

public sealed partial class OntologyVersionCosmosRepository
{
    private const string ReplayRecordType = "idempotencyOutcome";
    private const int MaximumBatchItemBytes = 2 * 1024 * 1024;

    public async Task<IdempotencyRecord?> GetReplayAsync(
        IdempotencyIdentity identity,
        CancellationToken cancellationToken = default)
    {
        var container = await GetContainerAsync(cancellationToken);
        return await ReadReplayAsync(container, identity, cancellationToken);
    }

    public async Task<IdempotencyRecord> SaveFailureAsync(
        IdempotencyRecord outcome,
        CancellationToken cancellationToken = default)
    {
        if (!outcome.IsError)
        {
            throw new ArgumentException("Only a failed outcome can be saved without a mutation.", nameof(outcome));
        }

        var container = await GetContainerAsync(cancellationToken);
        var existing = await ReadReplayAsync(container, outcome.Identity, cancellationToken);
        if (existing is not null)
        {
            return existing;
        }

        try
        {
            await container.CreateItemAsync(
                ReplayDocument(outcome),
                new PartitionKey(outcome.Identity.KnowledgeSpaceId),
                cancellationToken: cancellationToken);
            return outcome;
        }
        catch (CosmosException exception) when (exception.StatusCode == HttpStatusCode.Conflict)
        {
            return await ReadReplayAsync(container, outcome.Identity, cancellationToken)
                ?? throw new MutationDependencyException();
        }
    }

    public Task<IdempotencyRecord> CommitStagedVersionAsync(
        OntologyVersionEnvelope version,
        OntologyProposal proposal,
        KnowledgeSpaceControlRecord expectedControl,
        IdempotencyRecord outcome,
        CancellationToken cancellationToken = default)
    {
        version.Validate();
        proposal.Validate();
        RequireSpace(expectedControl, version.KnowledgeSpaceId, proposal.KnowledgeSpaceId);
        return CommitMutationAsync(
            expectedControl, expectedControl, outcome,
            [
                OntologyControlRecordDocument.FromDomain(
                    version, version.Id, version.KnowledgeSpaceId, "ontologyVersion"),
                OntologyControlRecordDocument.FromDomain(
                    proposal, proposal.Id, proposal.KnowledgeSpaceId, OntologyControlRecordTypes.Proposal)
            ],
            cancellationToken);
    }

    public Task<IdempotencyRecord> CommitAssessmentAsync(
        OntologyCompatibilityAssessment assessment,
        KnowledgeSpaceControlRecord expectedControl,
        IdempotencyRecord outcome,
        CancellationToken cancellationToken = default)
    {
        assessment.Validate();
        RequireSpace(expectedControl, assessment.KnowledgeSpaceId);
        return CommitMutationAsync(
            expectedControl, expectedControl, outcome,
            [
                OntologyControlRecordDocument.FromDomain(
                    assessment, assessment.Id, assessment.KnowledgeSpaceId,
                    OntologyControlRecordTypes.CompatibilityAssessment)
            ],
            cancellationToken);
    }

    public Task<IdempotencyRecord> CommitApprovalAsync(
        OntologyApproval approval,
        KnowledgeSpaceControlRecord expectedControl,
        IdempotencyRecord outcome,
        CancellationToken cancellationToken = default)
    {
        approval.Validate();
        RequireSpace(expectedControl, approval.KnowledgeSpaceId);
        return CommitMutationAsync(
            expectedControl, expectedControl, outcome,
            [
                OntologyControlRecordDocument.FromDomain(
                    approval, approval.Id, approval.KnowledgeSpaceId, OntologyControlRecordTypes.Approval)
            ],
            cancellationToken);
    }

    public async Task<IdempotencyRecord> CommitActivationAsync(
        OntologyActivationRequest request,
        KnowledgeSpaceControlRecord expectedControl,
        IdempotencyRecord outcome,
        CancellationToken cancellationToken = default)
    {
        ValidateActivationRequest(request);
        RequireSpace(expectedControl, request.KnowledgeSpaceId);
        var container = await GetContainerAsync(cancellationToken);
        var version = await ReadImmutableAsync<OntologyVersionEnvelope>(
            container, request.OntologyVersionId, request.KnowledgeSpaceId,
            "ontologyVersion", cancellationToken);
        var approval = await ReadImmutableAsync<OntologyApproval>(
            container, request.ApprovalId, request.KnowledgeSpaceId,
            OntologyControlRecordTypes.Approval, cancellationToken);
        var assessment = await ReadImmutableAsync<OntologyCompatibilityAssessment>(
            container, approval.CompatibilityAssessmentId, request.KnowledgeSpaceId,
            OntologyControlRecordTypes.CompatibilityAssessment, cancellationToken);
        var (next, evidence) = PrepareActivation(request, version, approval, assessment, expectedControl);
        return await CommitMutationAsync(
            expectedControl, next, outcome,
            [OntologyControlRecordDocument.FromDomain(evidence, OntologyControlRecordTypes.ActivationEvidence)],
            cancellationToken);
    }

    private async Task<IdempotencyRecord> CommitMutationAsync(
        KnowledgeSpaceControlRecord expectedControl,
        KnowledgeSpaceControlRecord nextControl,
        IdempotencyRecord outcome,
        OntologyControlRecordDocument[] records,
        CancellationToken cancellationToken)
    {
        RequireSpace(expectedControl, outcome.Identity.KnowledgeSpaceId, nextControl.KnowledgeSpaceId);
        ArgumentException.ThrowIfNullOrWhiteSpace(expectedControl.ETag);
        expectedControl.Validate();
        nextControl.Validate();
        var replay = ReplayDocument(outcome);
        var control = KnowledgeSpaceControlRecordDocument.FromDomain(nextControl);
        var itemBytes = records.Sum(record => SerializedBytes(record))
            + SerializedBytes(replay) + SerializedBytes(control);
        if (itemBytes > MaximumBatchItemBytes)
        {
            throw new OntologyPayloadValidationException("The atomic mutation is too large.");
        }

        var container = await GetContainerAsync(cancellationToken);
        try
        {
            _ = await container.ReadItemAsync<KnowledgeSpaceControlRecordDocument>(
                expectedControl.KnowledgeSpaceId,
                new PartitionKey(expectedControl.KnowledgeSpaceId),
                cancellationToken: cancellationToken);
        }
        catch (CosmosException exception) when (exception.StatusCode == HttpStatusCode.NotFound)
        {
            throw new OntologyPayloadValidationException("The knowledge-space control record does not exist.");
        }

        var batch = container.CreateTransactionalBatch(new PartitionKey(expectedControl.KnowledgeSpaceId))
            .CreateItem(replay);
        foreach (var record in records)
        {
            batch.CreateItem(record);
        }

        batch.ReplaceItem(
            expectedControl.KnowledgeSpaceId, control,
            new TransactionalBatchItemRequestOptions { IfMatchEtag = expectedControl.ETag });
        TransactionalBatchResponse response;
        try
        {
            response = await batch.ExecuteAsync(cancellationToken);
        }
        catch (Exception exception) when (
            exception is HttpRequestException
            || exception is OperationCanceledException && !cancellationToken.IsCancellationRequested
            || exception is CosmosException cosmos
                && (cosmos.StatusCode is HttpStatusCode.RequestTimeout or HttpStatusCode.TooManyRequests
                    || (int)cosmos.StatusCode >= 500))
        {
            return await ReadReplayAsync(container, outcome.Identity, cancellationToken)
                ?? throw new MutationDependencyException();
        }

        using (response)
        {
            if (response.IsSuccessStatusCode)
            {
                return outcome;
            }

            var winner = await ReadReplayAsync(container, outcome.Identity, cancellationToken);
            if (winner is not null)
            {
                return winner;
            }

            var failure = Enumerable.Range(0, response.Count)
                .Select(index => response[index].StatusCode)
                .FirstOrDefault(status => (int)status >= 400 && status != HttpStatusCode.FailedDependency);
            if (failure == default)
            {
                failure = response.StatusCode;
            }

            switch (failure)
            {
                case HttpStatusCode.PreconditionFailed:
                    throw new MutationConcurrencyException();
                case HttpStatusCode.Conflict:
                    if (response.Count > 0 && response[0].StatusCode == HttpStatusCode.Conflict)
                    {
                        throw new MutationDependencyException();
                    }

                    throw new OntologyControlRecordConflictException(
                        expectedControl.KnowledgeSpaceId, "existing-domain-record");
                case HttpStatusCode.RequestEntityTooLarge:
                case HttpStatusCode.BadRequest:
                    throw new OntologyPayloadValidationException("The atomic mutation was rejected.");
                default:
                    throw new MutationDependencyException();
            }
        }
    }

    private static int SerializedBytes(object value) =>
        Encoding.UTF8.GetByteCount(JsonConvert.SerializeObject(value));

    private static async Task<IdempotencyRecord?> ReadReplayAsync(
        Container container,
        IdempotencyIdentity identity,
        CancellationToken cancellationToken)
    {
        try
        {
            var response = await container.ReadItemAsync<OntologyControlRecordDocument>(
                identity.Id, new PartitionKey(identity.KnowledgeSpaceId),
                cancellationToken: cancellationToken);
            if (response.Resource.RecordType != ReplayRecordType
                || response.Resource.RecordVersion != "1.0")
            {
                throw new MutationDependencyException();
            }

            return response.Resource.ToDomain<IdempotencyRecord>().Match(identity);
        }
        catch (CosmosException exception) when (exception.StatusCode == HttpStatusCode.NotFound)
        {
            return null;
        }
    }

    private static OntologyControlRecordDocument ReplayDocument(IdempotencyRecord outcome) =>
        new()
        {
            Id = outcome.Identity.Id,
            KnowledgeSpaceId = outcome.Identity.KnowledgeSpaceId,
            RecordType = ReplayRecordType,
            RecordVersion = "1.0",
            Record = JObject.Parse(System.Text.Json.JsonSerializer.Serialize(outcome)),
            TimeToLive = -1
        };

    private static void RequireSpace(KnowledgeSpaceControlRecord control, params string[] spaces)
    {
        if (spaces.Any(space => !string.Equals(
                control.KnowledgeSpaceId, space, StringComparison.Ordinal)))
        {
            throw new OntologyPayloadValidationException("The mutation must stay within one knowledge space.");
        }
    }
}
