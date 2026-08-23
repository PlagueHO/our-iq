namespace OurIQ.ToolServices;

public sealed class KnowledgeSpaceCosmosOptions
{
    public const string SectionName = "OurIQ:Cosmos";

    public string DatabaseName { get; set; } = "ouriq";

    public string ContainerName { get; set; } = "knowledgeSpaceControl";
}
