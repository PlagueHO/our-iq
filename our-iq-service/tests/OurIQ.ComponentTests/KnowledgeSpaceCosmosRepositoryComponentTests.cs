using System.Text.Json;
using Microsoft.Azure.Cosmos;
using Microsoft.Extensions.Options;
using OurIQ.Domain;
using OurIQ.ToolServices;

namespace OurIQ.ComponentTests;

[TestClass]
[TestCategory("Integration")]
[DoNotParallelize]
public sealed class KnowledgeSpaceCosmosRepositoryComponentTests
{
    [TestMethod]
    public async Task CosmosEmulatorPersistsAndGuardsControlRecords()
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
            new CosmosClientOptions
            {
                ConnectionMode = ConnectionMode.Gateway
            });
        var repository = new KnowledgeSpaceCosmosRepository(
            cosmosClient,
            Options.Create(options));

        var created = await repository.CreateAsync(
            new KnowledgeSpaceCreation(
                "Product",
                "contributor confirmation",
                "component-test"));
        var read = await repository.GetAsync(created.KnowledgeSpaceId);

        Assert.IsNotNull(read);
        Assert.AreEqual(created.KnowledgeSpaceId, read.KnowledgeSpaceId);
        Assert.AreEqual(KnowledgeSpaceControlRecord.RecordTypeValue, read.RecordType);
        Assert.AreEqual(KnowledgeSpaceLifecycleStates.Draft, read.LifecycleState);
        Assert.IsFalse(string.IsNullOrWhiteSpace(read.ETag));

        var updated = await repository.UpdateAsync(
            read
                .GrantRole("component-test", "reader-001", KnowledgeSpaceRoles.Reader)
                with { DisplayName = "Updated Product" },
            read.ETag!);
        Assert.AreEqual("Updated Product", updated.DisplayName);
        CollectionAssert.AreEquivalent(
            new[]
            {
                new KnowledgeSpaceRoleGrant("component-test", KnowledgeSpaceRoles.Owner),
                new KnowledgeSpaceRoleGrant("reader-001", KnowledgeSpaceRoles.Reader)
            },
            updated.RoleGrants.ToArray());

        var container = cosmosClient.GetContainer(options.DatabaseName, options.ContainerName);
        using var storedResponse = await container.ReadItemStreamAsync(
            updated.KnowledgeSpaceId,
            new PartitionKey(updated.KnowledgeSpaceId));
        Assert.IsTrue(storedResponse.IsSuccessStatusCode);

        using var storedDocument = await JsonDocument.ParseAsync(storedResponse.Content);
        var storedRecord = storedDocument.RootElement;
        Assert.AreEqual(
            updated.KnowledgeSpaceId,
            storedRecord.GetProperty("id").GetString());
        Assert.AreEqual(
            updated.KnowledgeSpaceId,
            storedRecord.GetProperty("knowledgeSpaceId").GetString());
        Assert.AreEqual(2, storedRecord.GetProperty("roleGrants").GetArrayLength());
        Assert.IsFalse(storedRecord.TryGetProperty("eTag", out _));

        await Assert.ThrowsAsync<KnowledgeSpaceControlRecordConflictException>(
            () => repository.UpdateAsync(
                read with { DisplayName = "Stale Update" },
                read.ETag!));

        await Assert.ThrowsAsync<KnowledgeSpaceControlRecordValidationException>(
            () => repository.UpdateAsync(
                updated with { LifecycleState = "invalid" },
                updated.ETag!));
    }
}
