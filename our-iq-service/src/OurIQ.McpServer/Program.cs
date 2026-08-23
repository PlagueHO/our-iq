var builder = WebApplication.CreateBuilder(args);

builder.Services
    .AddMcpServer()
    .WithHttpTransport()
    .WithToolsFromAssembly();

var app = builder.Build();

app.MapGet("/health", () => Results.Text("healthy"));
app.MapMcp("/mcp");

app.Run();

public partial class McpServerProgram;
