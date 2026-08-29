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

    public async Task<KnowledgeSpaceControlRecordPage> ListAsync(
        KnowledgeSpaceControlRecordQuery query,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(query);
        query.Validate();

        var container = await GetContainerAsync(cancellationToken);
        var queryDefinition = new QueryDefinition(
            """
            SELECT * FROM c
            WHERE c.recordType = @recordType
              AND EXISTS(
                  SELECT VALUE grant
                  FROM grant IN c.roleGrants
                  WHERE grant.userId = @userId)
              AND (IS_NULL(@cursor) OR c.id > @cursor)
              AND (IS_NULL(@lifecycleState) OR c.lifecycleState = @lifecycleState)
            ORDER BY c.id
            """)
            .WithParameter("@recordType", KnowledgeSpaceControlRecord.RecordTypeValue)
            .WithParameter("@userId", query.UserId)
            .WithParameter("@cursor", query.Cursor)
            .WithParameter("@lifecycleState", query.LifecycleState);
        using var iterator = container.GetItemQueryIterator<KnowledgeSpaceControlRecordDocument>(
            queryDefinition,
            requestOptions: new QueryRequestOptions { MaxItemCount = query.PageSize + 1 });

        var documents = new List<KnowledgeSpaceControlRecordDocument>(query.PageSize + 1);
        while (iterator.HasMoreResults && documents.Count <= query.PageSize)
        {
            var response = await iterator.ReadNextAsync(cancellationToken);
            documents.AddRange(response.Resource);
        }

        var hasNextPage = documents.Count > query.PageSize;
        var pageDocuments = documents.Take(query.PageSize).ToArray();
        var records = pageDocuments
            .Select(document => document.ToDomain(null))
            .ToArray();
        foreach (var record in records)
        {
            record.Validate();
        }

        return new KnowledgeSpaceControlRecordPage(
            records,
            hasNextPage ? records[^1].KnowledgeSpaceId : null);
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
