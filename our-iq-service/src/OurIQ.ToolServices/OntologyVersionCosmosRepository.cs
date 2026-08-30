using System.Net;
using System.Text.Json;
using Microsoft.Azure.Cosmos;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using OurIQ.Domain;
using JsonSerializer = System.Text.Json.JsonSerializer;

namespace OurIQ.ToolServices;

public sealed class OntologyVersionCosmosRepository(
    CosmosClient cosmosClient,
    IOptions<KnowledgeSpaceCosmosOptions> options)
    : IOntologyVersionRepository
{
    private readonly KnowledgeSpaceCosmosOptions cosmosOptions = options.Value;

    public async Task<OntologyVersionEnvelope?> GetVersionAsync(
        string ontologyVersionId,
        string knowledgeSpaceId,
        CancellationToken cancellationToken = default)
    {
        var container = await GetContainerAsync(cancellationToken);
        var version = await TryReadImmutableAsync<OntologyVersionEnvelope>(
            container,
            ontologyVersionId,
            knowledgeSpaceId,
            "ontologyVersion",
            cancellationToken);
        version?.Validate();
        return version;
    }

    public Task<OntologyVersionEnvelope> CreateVersionAsync(
        OntologyVersionEnvelope version,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(version);
        version.Validate();
        return CreateImmutableAsync(
            version,
            version.Id,
            version.KnowledgeSpaceId,
            "ontologyVersion",
            cancellationToken);
    }

    public Task<OntologyProposal> CreateProposalAsync(
        OntologyProposal proposal,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(proposal);
        proposal.Validate();
        return CreateImmutableAsync(
            proposal,
            proposal.Id,
            proposal.KnowledgeSpaceId,
            OntologyControlRecordTypes.Proposal,
            cancellationToken);
    }

    public Task<OntologyCompatibilityAssessment> CreateCompatibilityAssessmentAsync(
        OntologyCompatibilityAssessment assessment,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(assessment);
        assessment.Validate();
        return CreateImmutableAsync(
            assessment,
            assessment.Id,
            assessment.KnowledgeSpaceId,
            OntologyControlRecordTypes.CompatibilityAssessment,
            cancellationToken);
    }

    public async Task<OntologyCompatibilityAssessment?> GetCompatibilityAssessmentAsync(
        string assessmentId,
        string knowledgeSpaceId,
        CancellationToken cancellationToken = default)
    {
        var container = await GetContainerAsync(cancellationToken);
        var assessment = await TryReadImmutableAsync<OntologyCompatibilityAssessment>(
            container,
            assessmentId,
            knowledgeSpaceId,
            OntologyControlRecordTypes.CompatibilityAssessment,
            cancellationToken);
        assessment?.Validate();
        return assessment;
    }

    public Task<OntologyApproval> CreateApprovalAsync(
        OntologyApproval approval,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(approval);
        approval.Validate();
        return CreateImmutableAsync(
            approval,
            approval.Id,
            approval.KnowledgeSpaceId,
            OntologyControlRecordTypes.Approval,
            cancellationToken);
    }

    public async Task<OntologyApproval?> GetApprovalAsync(
        string approvalId,
        string knowledgeSpaceId,
        CancellationToken cancellationToken = default)
    {
        var container = await GetContainerAsync(cancellationToken);
        var approval = await TryReadImmutableAsync<OntologyApproval>(
            container,
            approvalId,
            knowledgeSpaceId,
            OntologyControlRecordTypes.Approval,
            cancellationToken);
        approval?.Validate();
        return approval;
    }

    public async Task<KnowledgeSpaceControlRecord> ActivateAsync(
        OntologyActivationRequest request,
        CancellationToken cancellationToken = default)
    {
        ValidateActivationRequest(request);
        var container = await GetContainerAsync(cancellationToken);
        var version = await ReadImmutableAsync<OntologyVersionEnvelope>(
            container,
            request.OntologyVersionId,
            request.KnowledgeSpaceId,
            "ontologyVersion",
            cancellationToken);
        var approval = await ReadImmutableAsync<OntologyApproval>(
            container,
            request.ApprovalId,
            request.KnowledgeSpaceId,
            OntologyControlRecordTypes.Approval,
            cancellationToken);
        var assessment = await ReadImmutableAsync<OntologyCompatibilityAssessment>(
            container,
            approval.CompatibilityAssessmentId,
            request.KnowledgeSpaceId,
            OntologyControlRecordTypes.CompatibilityAssessment,
            cancellationToken);
        var controlResponse = await container.ReadItemAsync<KnowledgeSpaceControlRecordDocument>(
            request.KnowledgeSpaceId,
            new PartitionKey(request.KnowledgeSpaceId),
            cancellationToken: cancellationToken);
        var current = controlResponse.Resource.ToDomain(controlResponse.ETag);
        current.Validate();

        ValidateActivationInputs(request, version, approval, assessment, current);

        var activatedAt = DateTimeOffset.UtcNow;
        var evidence = new OntologyActivationEvidence
        {
            Id = request.ActivationEvidenceId,
            KnowledgeSpaceId = request.KnowledgeSpaceId,
            OntologyVersionId = request.OntologyVersionId,
            PayloadDigest = request.PayloadDigest,
            ApprovalId = approval.Id,
            CompatibilityAssessmentId = assessment.Id,
            ActivatedAt = activatedAt
        };
        evidence.Validate();
        var next = current with
        {
            ActiveOntologyVersionId = version.OntologyVersionId,
            ActiveOntologyDigest = version.PayloadDigest,
            LifecycleState = KnowledgeSpaceLifecycleStates.Active,
            UpdatedAt = activatedAt,
            ETag = null
        };
        next.Validate();

        using var response = await container
            .CreateTransactionalBatch(new PartitionKey(request.KnowledgeSpaceId))
            .CreateItem(OntologyControlRecordDocument.FromDomain(
                evidence,
                OntologyControlRecordTypes.ActivationEvidence))
            .ReplaceItem(
                next.KnowledgeSpaceId,
                KnowledgeSpaceControlRecordDocument.FromDomain(next),
                new TransactionalBatchItemRequestOptions { IfMatchEtag = current.ETag })
            .ExecuteAsync(cancellationToken);

        if (response.IsSuccessStatusCode)
        {
            return next;
        }

        if (response.StatusCode == HttpStatusCode.PreconditionFailed)
        {
            throw new OntologyActivationConflictException(request.KnowledgeSpaceId);
        }

        if (response.StatusCode == HttpStatusCode.Conflict)
        {
            throw new OntologyControlRecordConflictException(
                request.KnowledgeSpaceId,
                request.ActivationEvidenceId);
        }

        throw new InvalidOperationException(
            $"Ontology activation failed with Cosmos status code '{response.StatusCode}'.");
    }

    private async Task<T> CreateImmutableAsync<T>(
        T record,
        string recordId,
        string knowledgeSpaceId,
        string recordType,
        CancellationToken cancellationToken)
    {
        var container = await GetContainerAsync(cancellationToken);

        try
        {
            await container.CreateItemAsync(
                OntologyControlRecordDocument.FromDomain(record, recordId, knowledgeSpaceId, recordType),
                new PartitionKey(knowledgeSpaceId),
                cancellationToken: cancellationToken);
            return record;
        }
        catch (CosmosException exception) when (exception.StatusCode == HttpStatusCode.Conflict)
        {
            throw new OntologyControlRecordConflictException(knowledgeSpaceId, recordId);
        }
    }

    private static async Task<T> ReadImmutableAsync<T>(
        Container container,
        string recordId,
        string knowledgeSpaceId,
        string recordType,
        CancellationToken cancellationToken)
    {
        try
        {
            var response = await container.ReadItemAsync<OntologyControlRecordDocument>(
                recordId,
                new PartitionKey(knowledgeSpaceId),
                cancellationToken: cancellationToken);
            var document = response.Resource;
            if (!string.Equals(document.RecordType, recordType, StringComparison.Ordinal))
            {
                throw new OntologyPayloadValidationException(
                    $"The ontology control record '{recordId}' has an unexpected record type.");
            }

            return document.ToDomain<T>();
        }
        catch (CosmosException exception) when (exception.StatusCode == HttpStatusCode.NotFound)
        {
            throw new OntologyPayloadValidationException(
                $"The ontology control record '{recordId}' does not exist in knowledge space '{knowledgeSpaceId}'.");
        }
    }

    private static async Task<T?> TryReadImmutableAsync<T>(
        Container container,
        string recordId,
        string knowledgeSpaceId,
        string recordType,
        CancellationToken cancellationToken)
        where T : class
    {
        OntologyControlRecordValidator.ValidateRequired(recordId, nameof(recordId));
        OntologyControlRecordValidator.ValidateRequired(
            knowledgeSpaceId,
            nameof(knowledgeSpaceId));

        try
        {
            var response = await container.ReadItemAsync<OntologyControlRecordDocument>(
                recordId,
                new PartitionKey(knowledgeSpaceId),
                cancellationToken: cancellationToken);
            var document = response.Resource;
            return string.Equals(document.RecordType, recordType, StringComparison.Ordinal)
                ? document.ToDomain<T>()
                : null;
        }
        catch (CosmosException exception) when (exception.StatusCode == HttpStatusCode.NotFound)
        {
            return null;
        }
    }

    private async Task<Container> GetContainerAsync(CancellationToken cancellationToken)
    {
        ValidateConfiguration();
        var databaseResponse = await cosmosClient.CreateDatabaseIfNotExistsAsync(
            cosmosOptions.DatabaseName,
            cancellationToken: cancellationToken);
        var containerResponse = await databaseResponse.Database.CreateContainerIfNotExistsAsync(
            new ContainerProperties(cosmosOptions.ContainerName, "/knowledgeSpaceId"),
            cancellationToken: cancellationToken);
        return containerResponse.Container;
    }

    private void ValidateConfiguration()
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(cosmosOptions.DatabaseName);
        ArgumentException.ThrowIfNullOrWhiteSpace(cosmosOptions.ContainerName);
    }

    private static void ValidateActivationRequest(OntologyActivationRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        OntologyControlRecordValidator.ValidateRequired(
            request.KnowledgeSpaceId,
            nameof(request.KnowledgeSpaceId));
        OntologyControlRecordValidator.ValidateRequired(
            request.OntologyVersionId,
            nameof(request.OntologyVersionId));
        OntologyControlRecordValidator.ValidateDigest(request.PayloadDigest);
        OntologyControlRecordValidator.ValidateRequired(request.ApprovalId, nameof(request.ApprovalId));
        OntologyControlRecordValidator.ValidateRequired(
            request.ActivationEvidenceId,
            nameof(request.ActivationEvidenceId));

        if (string.IsNullOrWhiteSpace(request.ExpectedActiveOntologyVersionId)
            != string.IsNullOrWhiteSpace(request.ExpectedActiveOntologyDigest))
        {
            throw new OntologyPayloadValidationException(
                "The expected active ontology version and digest must be supplied together.");
        }
    }

    private static void ValidateActivationInputs(
        OntologyActivationRequest request,
        OntologyVersionEnvelope version,
        OntologyApproval approval,
        OntologyCompatibilityAssessment assessment,
        KnowledgeSpaceControlRecord current)
    {
        version.Validate();
        approval.Validate();
        assessment.Validate();
        if (!string.Equals(
                current.LifecycleState,
                KnowledgeSpaceLifecycleStates.Pending,
                StringComparison.Ordinal))
        {
            throw new KnowledgeSpaceStateConflictException(
                current.LifecycleState,
                KnowledgeSpaceLifecycleStates.Active);
        }

        if (version.OntologyVersionId != request.OntologyVersionId
            || version.PayloadDigest != request.PayloadDigest
            || approval.OntologyVersionId != request.OntologyVersionId
            || !approval.IsApproved
            || assessment.OntologyVersionId != request.OntologyVersionId
            || !assessment.IsApproved)
        {
            throw new OntologyPayloadValidationException(
                "The version, approved compatibility assessment, and approval evidence must agree.");
        }

        if (!string.Equals(
                current.ActiveOntologyVersionId,
                request.ExpectedActiveOntologyVersionId,
                StringComparison.Ordinal)
            || !string.Equals(
                current.ActiveOntologyDigest,
                request.ExpectedActiveOntologyDigest,
                StringComparison.Ordinal))
        {
            throw new OntologyActivationConflictException(request.KnowledgeSpaceId);
        }
    }
}

internal sealed class OntologyControlRecordDocument
{
    [JsonProperty("id")]
    public string Id { get; init; } = string.Empty;

    [JsonProperty("knowledgeSpaceId")]
    public string KnowledgeSpaceId { get; init; } = string.Empty;

    [JsonProperty("recordType")]
    public string RecordType { get; init; } = string.Empty;

    [JsonProperty("record")]
    public JObject Record { get; init; } = new();

    public static OntologyControlRecordDocument FromDomain<T>(
        T record,
        string id,
        string knowledgeSpaceId,
        string recordType) =>
        new()
        {
            Id = id,
            KnowledgeSpaceId = knowledgeSpaceId,
            RecordType = recordType,
            Record = JObject.Parse(JsonSerializer.Serialize(record))
        };

    public static OntologyControlRecordDocument FromDomain(
        OntologyActivationEvidence evidence,
        string recordType) =>
        FromDomain(evidence, evidence.Id, evidence.KnowledgeSpaceId, recordType);

    public T ToDomain<T>() =>
        JsonSerializer.Deserialize<T>(Record.ToString(Formatting.None))
        ?? throw new OntologyPayloadValidationException(
            $"The ontology control record '{Id}' is invalid.");
}
