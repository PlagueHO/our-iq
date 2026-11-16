using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Identity.Web;
using ModelContextProtocol;
using ModelContextProtocol.Server;
using OurIQ.Contracts;
using OurIQ.Domain;
using OurIQ.McpServer;
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
        azureIdentityOptions));
builder.Services.AddKnowledgeSpacePersistence(builder.Configuration);
builder.Services.AddHttpContextAccessor();
builder.Services.AddSingleton<AttendedIdentityEnvelopeValidator>();
builder.Services.AddSingleton<IAuthorizationHandler, AttendedUserAuthorizationHandler>();
builder.Services.AddScoped<IToolServicesTokenAcquirer, ToolServicesTokenAcquirer>();
builder.Services.AddOptions<ToolServicesDelegationOptions>()
    .Bind(builder.Configuration.GetSection(ToolServicesDelegationOptions.SectionName));

builder.Services
    .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddMicrosoftIdentityWebApi(builder.Configuration.GetSection("Entra"))
    .EnableTokenAcquisitionToCallDownstreamApi()
    .AddInMemoryTokenCaches();
builder.Services.Configure<JwtBearerOptions>(
    JwtBearerDefaults.AuthenticationScheme,
    options => options.MapInboundClaims = false);

builder.Services.AddAuthorizationBuilder()
    .AddPolicy(McpServerPolicies.AttendedUser, policy =>
    {
        policy.RequireAuthenticatedUser();
        policy.Requirements.Add(new AttendedUserRequirement());
    });

builder.Services
    .AddMcpServer()
    .WithHttpTransport()
    .WithToolsFromAssembly()
    .WithResourcesFromAssembly()
    .WithRequestFilters(filters =>
        filters.AddCallToolFilter(next => async (context, cancellationToken) =>
        {
            var request = ToolFailureResults.ReadRequest(context.Params.Arguments);
            var correlationId = context.Services?.GetService<ITelemetryContextAccessor>()?.Current?.CorrelationId;
            if (context.Services is null || context.User is null)
            {
                return ToolFailureResults.Create(
                    context.Params.Name, request, false, ApplicationError.IdentityMismatch, correlationId);
            }

            var validator = context.Services.GetRequiredService<AttendedIdentityEnvelopeValidator>();
            if (!validator.MatchesPublic(context.User, context.Params.Arguments))
            {
                return ToolFailureResults.Create(
                    context.Params.Name, request, false, ApplicationError.IdentityMismatch, correlationId);
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
                        "public", exception.GetType().Name, correlationId);
                return ToolFailureResults.Create(
                    context.Params.Name, request, false, ToolFailureResults.Classify(exception), correlationId);
            }
        }));

var app = builder.Build();

app.UseMiddleware<TelemetryContextMiddleware>();
app.UseAuthentication();
app.UseAuthorization();
app.MapGet("/health", () => Results.Text("healthy"));
app.MapMcp("/mcp")
    .RequireAuthorization(McpServerPolicies.AttendedUser);

app.Run();

public partial class McpServerProgram;
