using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Identity.Web;
using OurIQ.Contracts;
using OurIQ.McpServer;
using OurIQ.Observability;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddOurIQTelemetry(builder.Configuration);
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
    .WithRequestFilters(filters =>
        filters.AddCallToolFilter(next => async (context, cancellationToken) =>
        {
            if (context.Services is null || context.User is null)
            {
                return IdentityToolResults.IdentityMismatch();
            }

            var validator = context.Services.GetRequiredService<AttendedIdentityEnvelopeValidator>();
            return validator.MatchesPublic(context.User, context.Params.Arguments)
                ? await next(context, cancellationToken)
                : IdentityToolResults.IdentityMismatch();
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
