using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Options;
using ModelContextProtocol.Protocol;
using OurIQ.Contracts;

namespace OurIQ.ToolServices;

public static class ToolServicesPolicies
{
    public const string PrivateTools = "private-tools";
    public const string Management = "management";
}

public sealed class PrivateIdentityOptions
{
    public const string SectionName = "PrivateIdentity";

    public string[] AuthorizedAgentClientIds { get; set; } = [];
}

public interface IPrivateExecutionContextValidator
{
    ValueTask<bool> ValidateAsync(HttpContext httpContext, CancellationToken cancellationToken);
}

public interface IManagementAccessValidator
{
    ValueTask<bool> ValidateAsync(HttpContext httpContext, CancellationToken cancellationToken);
}

public sealed class PrivateExecutionContextRequirement : IAuthorizationRequirement;

public sealed class ManagementAccessRequirement : IAuthorizationRequirement;

public sealed class PrivateExecutionContextAuthorizationHandler(
    IPrivateExecutionContextValidator validator)
    : AuthorizationHandler<PrivateExecutionContextRequirement>
{
    protected override async Task HandleRequirementAsync(
        AuthorizationHandlerContext authorizationContext,
        PrivateExecutionContextRequirement requirement)
    {
        if (authorizationContext.Resource is not HttpContext httpContext)
        {
            return;
        }

        if (await validator.ValidateAsync(httpContext, httpContext.RequestAborted))
        {
            authorizationContext.Succeed(requirement);
        }
    }
}

public sealed class ManagementAccessAuthorizationHandler(
    IManagementAccessValidator validator)
    : AuthorizationHandler<ManagementAccessRequirement>
{
    protected override async Task HandleRequirementAsync(
        AuthorizationHandlerContext authorizationContext,
        ManagementAccessRequirement requirement)
    {
        if (authorizationContext.Resource is not HttpContext httpContext)
        {
            return;
        }

        if (await validator.ValidateAsync(httpContext, httpContext.RequestAborted))
        {
            authorizationContext.Succeed(requirement);
        }
    }
}

public sealed class DenyPrivateExecutionContextValidator : IPrivateExecutionContextValidator
{
    public ValueTask<bool> ValidateAsync(
        HttpContext httpContext,
        CancellationToken cancellationToken) =>
        ValueTask.FromResult(false);
}

public sealed class EntraPrivateExecutionContextValidator(
    IOptions<PrivateIdentityOptions> options)
    : IPrivateExecutionContextValidator
{
    public ValueTask<bool> ValidateAsync(
        HttpContext httpContext,
        CancellationToken cancellationToken)
    {
        if (!AttendedIdentityClaims.TryCreatePrivate(httpContext.User, out var identity))
        {
            return ValueTask.FromResult(false);
        }

        var isAuthorized = options.Value.AuthorizedAgentClientIds.Any(
            agentId => Guid.TryParse(agentId, out var parsed)
                && string.Equals(
                    parsed.ToString("D"),
                    identity.ActingAgentId,
                    StringComparison.OrdinalIgnoreCase));
        return ValueTask.FromResult(isAuthorized);
    }
}

public sealed class DenyManagementAccessValidator : IManagementAccessValidator
{
    public ValueTask<bool> ValidateAsync(
        HttpContext httpContext,
        CancellationToken cancellationToken) =>
        ValueTask.FromResult(false);
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
