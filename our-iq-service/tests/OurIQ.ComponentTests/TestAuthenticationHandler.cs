using System.Security.Claims;
using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using OurIQ.Contracts;

namespace OurIQ.ComponentTests;

internal static class TestWebApplicationFactoryExtensions
{
    public static WebApplicationFactory<TEntryPoint> WithTestAuthentication<TEntryPoint>(
        this WebApplicationFactory<TEntryPoint> factory,
        Action<IServiceCollection>? configureServices = null)
        where TEntryPoint : class =>
        factory.WithWebHostBuilder(builder =>
            builder.ConfigureServices(services =>
            {
                configureServices?.Invoke(services);
                services.AddAuthentication(options =>
                    {
                        options.DefaultAuthenticateScheme = TestIdentity.AuthenticationScheme;
                        options.DefaultChallengeScheme = TestIdentity.AuthenticationScheme;
                    })
                    .AddScheme<AuthenticationSchemeOptions, TestAuthenticationHandler>(
                        TestIdentity.AuthenticationScheme,
                        _ => { });
            }));
}

internal static class TestIdentity
{
    public const string AuthenticationScheme = "test-identity";
    public const string TenantId = "11111111-1111-1111-1111-111111111111";
    public const string ObjectId = "22222222-2222-2222-2222-222222222222";
    public const string AgentId = "33333333-3333-3333-3333-333333333333";
    public const string DelegatedScope = "access_as_user";
    public const string InitiatingUserId = TenantId + ":" + ObjectId;

    public static void AddTo(HttpRequestMessage request, string? agentId = AgentId)
    {
        request.Headers.Add(TestAuthenticationHandler.AuthenticatedHeader, "true");
        request.Headers.Add(TestAuthenticationHandler.TenantIdHeader, TenantId);
        request.Headers.Add(TestAuthenticationHandler.ObjectIdHeader, ObjectId);
        request.Headers.Add(TestAuthenticationHandler.ScopeHeader, DelegatedScope);

        if (agentId is not null)
        {
            request.Headers.Add(TestAuthenticationHandler.AgentIdHeader, agentId);
        }
    }
}

internal sealed class TestAuthenticationHandler(
    IOptionsMonitor<AuthenticationSchemeOptions> options,
    ILoggerFactory logger,
    UrlEncoder encoder)
    : AuthenticationHandler<AuthenticationSchemeOptions>(options, logger, encoder)
{
    public const string AuthenticatedHeader = "X-Test-Authenticated";
    public const string TenantIdHeader = "X-Test-Tenant-Id";
    public const string ObjectIdHeader = "X-Test-Object-Id";
    public const string AgentIdHeader = "X-Test-Agent-Id";
    public const string ScopeHeader = "X-Test-Scope";

    protected override Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        if (!Request.Headers.ContainsKey(AuthenticatedHeader))
        {
            return Task.FromResult(AuthenticateResult.NoResult());
        }

        var claims = new List<Claim>();
        AddClaim(claims, AttendedIdentityClaims.TenantId, TenantIdHeader);
        AddClaim(claims, AttendedIdentityClaims.ObjectId, ObjectIdHeader);
        AddClaim(claims, AttendedIdentityClaims.AuthorizedParty, AgentIdHeader);
        AddClaim(claims, AttendedIdentityClaims.Scope, ScopeHeader);

        var principal = new ClaimsPrincipal(new ClaimsIdentity(claims, TestIdentity.AuthenticationScheme));
        return Task.FromResult(AuthenticateResult.Success(
            new AuthenticationTicket(principal, TestIdentity.AuthenticationScheme)));
    }

    private void AddClaim(List<Claim> claims, string claimType, string headerName)
    {
        if (Request.Headers.TryGetValue(headerName, out var value) && value.Count == 1)
        {
            claims.Add(new Claim(claimType, value[0]!));
        }
    }
}
