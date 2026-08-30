using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using OurIQ.Domain;

namespace OurIQ.ToolServices;

public static class KnowledgeSpacePersistenceServiceCollectionExtensions
{
    public static IServiceCollection AddKnowledgeSpacePersistence(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services
            .AddOptions<KnowledgeSpaceCosmosOptions>()
            .Bind(configuration.GetSection(KnowledgeSpaceCosmosOptions.SectionName))
            .ValidateOnStart();
        services.AddSingleton<IKnowledgeSpaceControlRecordRepository, KnowledgeSpaceCosmosRepository>();
        services.AddSingleton<IOntologyVersionRepository, OntologyVersionCosmosRepository>();
        services.AddSingleton<IExecutionContextSnapshotRepository, ExecutionContextSnapshotCosmosRepository>();
        return services;
    }
}
