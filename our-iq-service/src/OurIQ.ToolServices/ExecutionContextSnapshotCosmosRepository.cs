using System.Net;
using Microsoft.Azure.Cosmos;
using Microsoft.Extensions.Options;
using OurIQ.Domain;

namespace OurIQ.ToolServices;

public sealed class ExecutionContextSnapshotCosmosRepository(
    CosmosClient cosmosClient,
    IOptions<KnowledgeSpaceCosmosOptions> options)
    : IExecutionContextSnapshotRepository
{
    private readonly KnowledgeSpaceCosmosOptions cosmosOptions = options.Value;

    public async Task<ExecutionContextSnapshot> CreateAsync(
        ExecutionContextSnapshot snapshot,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(snapshot);
        snapshot.Validate();
        var container = await GetContainerAsync(cancellationToken);

        try
        {
            await container.CreateItemAsync(
                OntologyControlRecordDocument.FromDomain(
                    snapshot,
                    snapshot.Id,
                    snapshot.KnowledgeSpaceId,
                    ExecutionContextSnapshot.RecordTypeValue),
                new PartitionKey(snapshot.KnowledgeSpaceId),
                cancellationToken: cancellationToken);
            return snapshot;
        }
        catch (CosmosException exception) when (exception.StatusCode == HttpStatusCode.Conflict)
        {
            throw new ExecutionContextSnapshotConflictException(
                snapshot.KnowledgeSpaceId,
                snapshot.Id);
        }
    }

    public async Task<ExecutionContextSnapshot?> GetAsync(
        string executionId,
        string knowledgeSpaceId,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(executionId);
        ArgumentException.ThrowIfNullOrWhiteSpace(knowledgeSpaceId);
        var container = await GetContainerAsync(cancellationToken);

        try
        {
            var response = await container.ReadItemAsync<OntologyControlRecordDocument>(
                executionId,
                new PartitionKey(knowledgeSpaceId),
                cancellationToken: cancellationToken);
            var document = response.Resource;
            if (!string.Equals(
                    document.RecordType,
                    ExecutionContextSnapshot.RecordTypeValue,
                    StringComparison.Ordinal))
            {
                return null;
            }

            var snapshot = document.ToDomain<ExecutionContextSnapshot>();
            snapshot.Validate();
            return snapshot;
        }
        catch (CosmosException exception) when (exception.StatusCode == HttpStatusCode.NotFound)
        {
            return null;
        }
    }

    private async Task<Container> GetContainerAsync(CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(cosmosOptions.DatabaseName);
        ArgumentException.ThrowIfNullOrWhiteSpace(cosmosOptions.ContainerName);

        var databaseResponse = await cosmosClient.CreateDatabaseIfNotExistsAsync(
            cosmosOptions.DatabaseName,
            cancellationToken: cancellationToken);
        var containerResponse = await databaseResponse.Database.CreateContainerIfNotExistsAsync(
            new ContainerProperties(cosmosOptions.ContainerName, "/knowledgeSpaceId"),
            cancellationToken: cancellationToken);
        return containerResponse.Container;
    }
}
