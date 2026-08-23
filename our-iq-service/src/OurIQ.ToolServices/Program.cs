using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using ModelContextProtocol.Server;
using OurIQ.ToolServices;

var builder = WebApplication.CreateBuilder(args);

builder.Services
    .AddMcpServer()
    .WithHttpTransport()
    .WithToolsFromAssembly();

builder.Services
    .AddAuthentication(ToolServicesPolicies.Authentication)
    .AddScheme<AuthenticationSchemeOptions, DenyAuthenticationHandler>(
        ToolServicesPolicies.Authentication,
        _ => { });

builder.Services.AddAuthorizationBuilder()
    .AddPolicy(ToolServicesPolicies.PrivateTools, policy =>
        policy.Requirements.Add(new PrivateExecutionContextRequirement()))
    .AddPolicy(ToolServicesPolicies.Management, policy =>
        policy.Requirements.Add(new ManagementAccessRequirement()));

builder.Services.AddSingleton<IAuthorizationHandler, PrivateExecutionContextAuthorizationHandler>();
builder.Services.AddSingleton<IAuthorizationHandler, ManagementAccessAuthorizationHandler>();
builder.Services.AddSingleton<IPrivateExecutionContextValidator, DenyPrivateExecutionContextValidator>();
builder.Services.AddSingleton<IManagementAccessValidator, DenyManagementAccessValidator>();

var app = builder.Build();

app.UseAuthentication();
app.UseAuthorization();

app.MapGet("/health", () => Results.Text("healthy"));
app.MapGet("/ready", () => Results.Text("ready"));

app.MapMcp("/mcp")
    .RequireAuthorization(ToolServicesPolicies.PrivateTools);

var management = app.MapGroup("/management");
management.RequireAuthorization(ToolServicesPolicies.Management);
management.MapGet("/status", () => Results.Ok(new { status = "available" }));

app.Run();

public partial class ToolServicesProgram;
