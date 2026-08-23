var builder = DistributedApplication.CreateBuilder(args);

#pragma warning disable ASPIRECOSMOSDB001
var cosmos = builder.AddAzureCosmosDB("cosmos")
    .RunAsPreviewEmulator();
#pragma warning restore ASPIRECOSMOSDB001

var database = cosmos.AddCosmosDatabase("ouriq");

var storage = builder.AddAzureStorage("storage")
    .RunAsEmulator(emulator => emulator.WithDataVolume());
var blobs = storage.AddBlobs("blobs");

var search = builder.AddAzureSearch("search");

var toolServices = builder.AddProject<Projects.OurIQ_ToolServices>("tool-services")
    .WithReference(cosmos)
    .WithReference(blobs)
    .WithReference(search)
    .WaitFor(cosmos)
    .WaitFor(database)
    .WaitFor(storage)
    .WaitFor(search);

builder.AddProject<Projects.OurIQ_McpServer>("mcp-server")
    .WithReference(toolServices)
    .WaitFor(toolServices)
    .WithExternalHttpEndpoints();

builder.Build().Run();
