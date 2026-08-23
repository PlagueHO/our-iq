using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Options;

namespace OurIQ.ToolServices;

public static class ToolServicesPolicies
{
    public const string Authentication = "tool-services";
    public const string PrivateTools = "private-tools";
    public const string Management = "management";
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

public sealed class DenyManagementAccessValidator : IManagementAccessValidator
{
    public ValueTask<bool> ValidateAsync(
        HttpContext httpContext,
        CancellationToken cancellationToken) =>
        ValueTask.FromResult(false);
}

public sealed class DenyAuthenticationHandler(
    IOptionsMonitor<AuthenticationSchemeOptions> options,
    ILoggerFactory logger,
    UrlEncoder encoder)
    : AuthenticationHandler<AuthenticationSchemeOptions>(options, logger, encoder)
{
    protected override Task<AuthenticateResult> HandleAuthenticateAsync() =>
        Task.FromResult(AuthenticateResult.NoResult());

    protected override Task HandleChallengeAsync(AuthenticationProperties? properties)
    {
        Response.StatusCode = StatusCodes.Status403Forbidden;
        return Task.CompletedTask;
    }

    protected override Task HandleForbiddenAsync(AuthenticationProperties? properties)
    {
        Response.StatusCode = StatusCodes.Status403Forbidden;
        return Task.CompletedTask;
    }
}
