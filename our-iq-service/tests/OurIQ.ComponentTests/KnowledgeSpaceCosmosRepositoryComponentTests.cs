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

    [TestMethod]
    public async Task CosmosEmulatorActivatesOntologyWithOneVisibilityBoundary()
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
        var spaces = new KnowledgeSpaceCosmosRepository(cosmosClient, Options.Create(options));
        var ontologies = new OntologyVersionCosmosRepository(cosmosClient, Options.Create(options));
        var draftSpace = await spaces.CreateAsync(
            new KnowledgeSpaceCreation("Product", "review", "owner-001"));
        var space = await spaces.UpdateAsync(
            draftSpace.TransitionTo(KnowledgeSpaceLifecycleStates.Pending, "owner-001"),
            draftSpace.ETag!);
        var version = CreateVersion(space.KnowledgeSpaceId);
        var assessment = new OntologyCompatibilityAssessment
        {
            Id = "assessment-001",
            KnowledgeSpaceId = space.KnowledgeSpaceId,
            OntologyVersionId = version.OntologyVersionId,
            IsApproved = true,
            CreatedAt = DateTimeOffset.UtcNow,
            CreatedBy = "owner-001"
        };
        var approval = new OntologyApproval
        {
            Id = "approval-001",
            KnowledgeSpaceId = space.KnowledgeSpaceId,
            OntologyVersionId = version.OntologyVersionId,
            CompatibilityAssessmentId = assessment.Id,
            ActorId = "owner-001",
            Authority = "Ontology Manager",
            IsApproved = true,
            CreatedAt = DateTimeOffset.UtcNow
        };

        await ontologies.CreateVersionAsync(version);
        Assert.AreEqual(
            version,
            await ontologies.GetVersionAsync(
                version.OntologyVersionId,
                version.KnowledgeSpaceId));
        await Assert.ThrowsAsync<OntologyControlRecordConflictException>(
            () => ontologies.CreateVersionAsync(version));
        await ontologies.CreateCompatibilityAssessmentAsync(assessment);
        Assert.AreEqual(
            assessment,
            await ontologies.GetCompatibilityAssessmentAsync(
                assessment.Id,
                assessment.KnowledgeSpaceId));
        await ontologies.CreateApprovalAsync(approval);
        Assert.AreEqual(
            approval,
            await ontologies.GetApprovalAsync(
                approval.Id,
                approval.KnowledgeSpaceId));
        var activated = await ontologies.ActivateAsync(
            new OntologyActivationRequest(
                space.KnowledgeSpaceId,
                version.OntologyVersionId,
                version.PayloadDigest,
                approval.Id,
                null,
                null,
                "activation-001"));

        Assert.AreEqual(version.OntologyVersionId, activated.ActiveOntologyVersionId);
        Assert.AreEqual(version.PayloadDigest, activated.ActiveOntologyDigest);

        await Assert.ThrowsAsync<OntologyActivationConflictException>(
            () => ontologies.ActivateAsync(
                new OntologyActivationRequest(
                    space.KnowledgeSpaceId,
                    version.OntologyVersionId,
                    version.PayloadDigest,
                    approval.Id,
                    null,
                    null,
                    "activation-stale")));

        var current = await spaces.GetAsync(space.KnowledgeSpaceId);
        Assert.IsNotNull(current);
        Assert.AreEqual(version.OntologyVersionId, current.ActiveOntologyVersionId);
        Assert.AreEqual(version.PayloadDigest, current.ActiveOntologyDigest);

        var container = cosmosClient.GetContainer(options.DatabaseName, options.ContainerName);
        using var response = await container.ReadItemStreamAsync(
            "activation-001",
            new PartitionKey(space.KnowledgeSpaceId));
        Assert.IsTrue(response.IsSuccessStatusCode);

        using var evidence = await JsonDocument.ParseAsync(response.Content);
        Assert.AreEqual(
            OntologyControlRecordTypes.ActivationEvidence,
            evidence.RootElement.GetProperty("recordType").GetString());
    }

    private static OntologyVersionEnvelope CreateVersion(string knowledgeSpaceId)
    {
        var payload = new OntologyPayload
        {
            OntologyId = "ontology-product",
            OntologyVersionId = "ontology-product-v1",
            Title = "Product knowledge",
            Description = "Structures product decisions.",
            DocumentTypes =
            [
                new OntologyDocumentType(
                    "decision-record",
                    "A decision.",
                    Parse(
                        """
                        {
                          "$schema": "https://json-schema.org/draft/2020-12/schema",
                          "type": "object"
                        }
                        """))
            ],
            Hierarchy = new OntologyHierarchy(["decision-record"], []),
            RelationshipTypes = [],
            Rules = [],
            FilterableFields = [],
            TemplateReferences = []
        };
        return new OntologyVersionEnvelope
        {
            Id = payload.OntologyVersionId,
            RecordType = "ontologyVersion",
            KnowledgeSpaceId = knowledgeSpaceId,
            OntologyId = payload.OntologyId,
            OntologyVersionId = payload.OntologyVersionId,
            SchemaVersion = "1",
            Payload = payload,
            PayloadDigest = OntologyPayloadDigest.Compute(payload),
            CreatedAt = DateTimeOffset.UtcNow,
            CreatedBy = "owner-001"
        };
    }

    private static JsonElement Parse(string json)
    {
        using var document = JsonDocument.Parse(json);
        return document.RootElement.Clone();
    }
}
