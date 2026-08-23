using OurIQ.Observability;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddOurIQTelemetry(builder.Configuration);
builder.Services
    .AddMcpServer()
    .WithHttpTransport()
    .WithToolsFromAssembly();

var app = builder.Build();

app.UseMiddleware<TelemetryContextMiddleware>();
app.MapGet("/health", () => Results.Text("healthy"));
app.MapMcp("/mcp");

app.Run();

public partial class McpServerProgram;
