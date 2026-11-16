using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Azure.Cosmos;
using Microsoft.Identity.Web;
using ModelContextProtocol;
using ModelContextProtocol.Server;
using OurIQ.Contracts;
using OurIQ.Observability;
using OurIQ.ToolServices;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddOurIQTelemetry(builder.Configuration);
var azureIdentityOptions = builder.Configuration
    .GetSection(AzureIdentityOptions.SectionName)
    .Get<AzureIdentityOptions>()
    ?? new AzureIdentityOptions();
builder.AddAzureCosmosClient(
    "cosmos",
    settings => settings.Credential = AzureCredentialFactory.Create(
        builder.Environment,
        azureIdentityOptions),
    options =>
    {
        if (builder.Environment.IsDevelopment())
        {
            options.ConnectionMode = ConnectionMode.Gateway;
        }
    });
builder.Services.AddKnowledgeSpacePersistence(builder.Configuration);
builder.Services.AddSingleton<AttendedIdentityEnvelopeValidator>();
builder.Services.AddOptions<PrivateIdentityOptions>()
    .Bind(builder.Configuration.GetSection(PrivateIdentityOptions.SectionName));
builder.Services
    .AddMcpServer()
    .WithHttpTransport()
    .WithToolsFromAssembly()
    .WithRequestFilters(filters =>
        filters.AddCallToolFilter(next => async (context, cancellationToken) =>
        {
            var request = ToolFailureResults.ReadRequest(context.Params.Arguments);
            var correlationId = context.Services?.GetService<ITelemetryContextAccessor>()?.Current?.CorrelationId;
            if (context.Services is null || context.User is null)
            {
                return ToolFailureResults.Create(
                    context.Params.Name, request, true, ApplicationError.IdentityMismatch, correlationId);
            }

            var validator = context.Services.GetRequiredService<AttendedIdentityEnvelopeValidator>();
            if (!validator.MatchesPrivate(context.User, context.Params.Arguments))
            {
                return ToolFailureResults.Create(
                    context.Params.Name, request, true, ApplicationError.IdentityMismatch, correlationId);
            }

            try
            {
                return await next(context, cancellationToken);
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                throw;
            }
            catch (McpException)
            {
                throw;
            }
            catch (Exception exception)
            {
                context.Services.GetRequiredService<ILoggerFactory>()
                    .CreateLogger("OurIQ.ToolErrors")
                    .LogError("Tool failed at {Stage} with {ErrorType}. Correlation {CorrelationId}.",
                        "private", exception.GetType().Name, correlationId);
                return ToolFailureResults.Create(
                    context.Params.Name, request, true, ToolFailureResults.Classify(exception), correlationId);
            }
        }));

builder.Services
    .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddMicrosoftIdentityWebApi(builder.Configuration.GetSection("Entra"));
builder.Services.Configure<JwtBearerOptions>(
    JwtBearerDefaults.AuthenticationScheme,
    options => options.MapInboundClaims = false);

builder.Services.AddAuthorizationBuilder()
    .AddPolicy(ToolServicesPolicies.PrivateTools, policy =>
    {
        policy.RequireAuthenticatedUser();
        policy.Requirements.Add(new PrivateExecutionContextRequirement());
    })
    .AddPolicy(ToolServicesPolicies.Management, policy =>
        policy.Requirements.Add(new ManagementAccessRequirement()));

builder.Services.AddSingleton<IAuthorizationHandler, PrivateExecutionContextAuthorizationHandler>();
builder.Services.AddSingleton<IAuthorizationHandler, ManagementAccessAuthorizationHandler>();
builder.Services.AddSingleton<IPrivateExecutionContextValidator, EntraPrivateExecutionContextValidator>();
builder.Services.AddSingleton<IManagementAccessValidator, DenyManagementAccessValidator>();

var app = builder.Build();

app.UseMiddleware<TelemetryContextMiddleware>();
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
