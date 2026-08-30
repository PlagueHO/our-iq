using Microsoft.Azure.Cosmos;
using Microsoft.Extensions.Options;
using OurIQ.Domain;
using OurIQ.ToolServices;

namespace OurIQ.ComponentTests;

[TestClass]
[TestCategory("Integration")]
[DoNotParallelize]
public sealed class ExecutionContextSnapshotCosmosRepositoryComponentTests
{
    [TestMethod]
    public async Task CosmosEmulatorPersistsAndGuardsExecutionContextSnapshots()
    {
        var connectionString = Environment.GetEnvironmentVariable("OURIQ_COSMOS_CONNECTION_STRING");
        if (string.IsNullOrWhiteSpace(connectionString))
        {
            Assert.Inconclusive(
                "Set OURIQ_COSMOS_CONNECTION_STRING to run Cosmos emulator component tests.");
        }

        var options = new KnowledgeSpaceCosmosOptions
        {
            DatabaseName = $"ouriq-test-{Guid.NewGuid():N}",
            ContainerName = "knowledgeSpaceControl"
        };
        using var cosmosClient = new CosmosClient(
            connectionString,
            new CosmosClientOptions { ConnectionMode = ConnectionMode.Gateway });
        var repository = new ExecutionContextSnapshotCosmosRepository(
            cosmosClient,
            Options.Create(options));
        var controlRecord = KnowledgeSpaceControlRecord.Create(
            new KnowledgeSpaceCreation("Product", "contributor confirmation", "owner-001"));
        var snapshot = ExecutionContextSnapshot.Create(
            controlRecord,
            "execution-001",
            "trace-001",
            DomainAgentIdentities.Contribution,
            DomainAgentIdentities.InitialDefinitionVersion,
            "owner-001");

        await repository.CreateAsync(snapshot);
        var read = await repository.GetAsync(
            snapshot.ExecutionId,
            snapshot.KnowledgeSpaceId);

        Assert.IsNotNull(read);
        Assert.AreEqual(snapshot, read);
        await Assert.ThrowsAsync<ExecutionContextSnapshotConflictException>(
            () => repository.CreateAsync(snapshot));
    }
}
