using Microsoft.AspNetCore.Authorization;
using Microsoft.Identity.Web;
using Microsoft.Extensions.Options;
using ModelContextProtocol.Protocol;
using OurIQ.Contracts;

namespace OurIQ.McpServer;

public static class McpServerPolicies
{
    public const string AttendedUser = "attended-user";
}

public sealed class AttendedUserRequirement : IAuthorizationRequirement;

public sealed class AttendedUserAuthorizationHandler
    : AuthorizationHandler<AttendedUserRequirement>
{
    protected override Task HandleRequirementAsync(
        AuthorizationHandlerContext context,
        AttendedUserRequirement requirement)
    {
        if (AttendedIdentityClaims.TryCreateUser(context.User, out _))
        {
            context.Succeed(requirement);
        }

        return Task.CompletedTask;
    }
}

public sealed class ToolServicesDelegationOptions
{
    public const string SectionName = "ToolServicesDelegation";

    public string Scope { get; init; } = string.Empty;
}

public interface IToolServicesTokenAcquirer
{
    Task<string> AcquireForUserAsync(
        System.Security.Claims.ClaimsPrincipal user,
        CancellationToken cancellationToken = default);
}

public sealed class ToolServicesTokenAcquirer(
    ITokenAcquisition tokenAcquisition,
    IOptions<ToolServicesDelegationOptions> options)
    : IToolServicesTokenAcquirer
{
    public Task<string> AcquireForUserAsync(
        System.Security.Claims.ClaimsPrincipal user,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return tokenAcquisition.GetAccessTokenForUserAsync(
            [options.Value.Scope],
            user: user);
    }
}

public static class IdentityToolResults
{
    public static CallToolResult IdentityMismatch() =>
        new()
        {
            IsError = true,
            Content =
            [
                new TextContentBlock
                {
                    Text = "The authenticated identity does not match the request identity."
                }
            ]
        };
}
