using System.Net;
using Microsoft.Azure.Cosmos;
using Microsoft.Extensions.Options;
using OurIQ.Domain;

namespace OurIQ.ToolServices;

public sealed class KnowledgeSpaceCosmosRepository(
    CosmosClient cosmosClient,
    IOptions<KnowledgeSpaceCosmosOptions> options)
    : IKnowledgeSpaceControlRecordRepository
{
    private readonly KnowledgeSpaceCosmosOptions cosmosOptions = options.Value;

    public async Task<KnowledgeSpaceControlRecord> CreateAsync(
        KnowledgeSpaceCreation creation,
        CancellationToken cancellationToken = default)
    {
        var record = KnowledgeSpaceControlRecord.Create(creation);
        var container = await GetContainerAsync(cancellationToken);

        try
        {
            var response = await container.CreateItemAsync(
                KnowledgeSpaceControlRecordDocument.FromDomain(record),
                new PartitionKey(record.KnowledgeSpaceId),
                cancellationToken: cancellationToken);
            return record with { ETag = response.ETag };
        }
        catch (CosmosException exception) when (exception.StatusCode == HttpStatusCode.Conflict)
        {
            throw new KnowledgeSpaceControlRecordConflictException(
                record.KnowledgeSpaceId,
                "none");
        }
    }

    public async Task<KnowledgeSpaceControlRecord?> GetAsync(
        string knowledgeSpaceId,
        CancellationToken cancellationToken = default)
    {
        ValidateKnowledgeSpaceId(knowledgeSpaceId);
        var container = await GetContainerAsync(cancellationToken);

        try
        {
            var response = await container.ReadItemAsync<KnowledgeSpaceControlRecordDocument>(
                knowledgeSpaceId,
                new PartitionKey(knowledgeSpaceId),
                cancellationToken: cancellationToken);
            var record = response.Resource.ToDomain(response.ETag);
            record.Validate();
            return record;
        }
        catch (CosmosException exception) when (exception.StatusCode == HttpStatusCode.NotFound)
        {
            return null;
        }
    }

    public async Task<KnowledgeSpaceControlRecord> UpdateAsync(
        KnowledgeSpaceControlRecord record,
        string expectedETag,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(record);
        ValidateRequired(expectedETag, nameof(expectedETag));
        record.Validate();

        var updatedRecord = record with
        {
            UpdatedAt = DateTimeOffset.UtcNow,
            ETag = null
        };
        var container = await GetContainerAsync(cancellationToken);

        try
        {
            var response = await container.ReplaceItemAsync(
                KnowledgeSpaceControlRecordDocument.FromDomain(updatedRecord),
                updatedRecord.KnowledgeSpaceId,
                new PartitionKey(updatedRecord.KnowledgeSpaceId),
                new ItemRequestOptions { IfMatchEtag = expectedETag },
                cancellationToken);
            return updatedRecord with { ETag = response.ETag };
        }
        catch (CosmosException exception) when (exception.StatusCode == HttpStatusCode.PreconditionFailed)
        {
            throw new KnowledgeSpaceControlRecordConflictException(
                updatedRecord.KnowledgeSpaceId,
                expectedETag);
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
        ValidateRequired(cosmosOptions.DatabaseName, nameof(cosmosOptions.DatabaseName));
        ValidateRequired(cosmosOptions.ContainerName, nameof(cosmosOptions.ContainerName));
    }

    private static void ValidateKnowledgeSpaceId(string knowledgeSpaceId) =>
        ValidateRequired(knowledgeSpaceId, nameof(knowledgeSpaceId));

    private static void ValidateRequired(string? value, string name)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new KnowledgeSpaceControlRecordValidationException(
                $"The {name} value is required.");
        }
    }
}
