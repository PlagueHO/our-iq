using System.Security.Claims;
using System.Text.Json;

namespace OurIQ.Contracts;

public sealed record AttendedIdentity(string InitiatingUserId, string? ActingAgentId);

public static class AttendedIdentityClaims
{
    public const string TenantId = "tid";
    public const string ObjectId = "oid";
    public const string AuthorizedParty = "azp";
    public const string ApplicationId = "appid";
    public const string Scope = "scp";

    public static bool TryCreateUser(ClaimsPrincipal principal, out AttendedIdentity identity)
    {
        identity = default!;

        if (!HasDelegatedScope(principal)
            || !TryReadGuidClaim(principal, TenantId, out var tenantId)
            || !TryReadGuidClaim(principal, ObjectId, out var objectId))
        {
            return false;
        }

        identity = new AttendedIdentity($"{tenantId}:{objectId}", null);
        return true;
    }

    public static bool TryCreatePrivate(ClaimsPrincipal principal, out AttendedIdentity identity)
    {
        identity = default!;

        if (!TryCreateUser(principal, out var user)
            || !TryReadActingAgent(principal, out var actingAgentId))
        {
            return false;
        }

        identity = user with { ActingAgentId = actingAgentId };
        return true;
    }

    private static bool TryReadActingAgent(ClaimsPrincipal principal, out string actingAgentId)
    {
        actingAgentId = string.Empty;
        var hasAuthorizedParty = TryReadGuidClaim(principal, AuthorizedParty, out var authorizedParty);
        var hasApplicationId = TryReadGuidClaim(principal, ApplicationId, out var applicationId);

        if (hasAuthorizedParty && hasApplicationId && authorizedParty != applicationId)
        {
            return false;
        }

        if (!hasAuthorizedParty && !hasApplicationId)
        {
            return false;
        }

        actingAgentId = hasAuthorizedParty ? authorizedParty : applicationId;
        return true;
    }

    private static bool TryReadGuidClaim(
        ClaimsPrincipal principal,
        string claimType,
        out string canonicalValue)
    {
        canonicalValue = string.Empty;
        var values = principal.FindAll(claimType)
            .Select(claim => claim.Value)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();

        if (values.Length != 1 || !Guid.TryParse(values[0], out var value))
        {
            return false;
        }

        canonicalValue = value.ToString("D");
        return true;
    }

    private static bool HasDelegatedScope(ClaimsPrincipal principal)
    {
        var values = principal.FindAll(Scope)
            .Select(claim => claim.Value)
            .ToArray();
        return values.Length == 1 && !string.IsNullOrWhiteSpace(values[0]);
    }
}

public sealed class AttendedIdentityEnvelopeValidator
{
    public bool MatchesPublic(
        ClaimsPrincipal principal,
        IDictionary<string, JsonElement>? arguments) =>
        AttendedIdentityClaims.TryCreateUser(principal, out var identity)
        && TryReadEnvelopeIdentity(arguments, out var initiatingUserId, out _)
        && string.Equals(
            identity.InitiatingUserId,
            initiatingUserId,
            StringComparison.OrdinalIgnoreCase);

    public bool MatchesPrivate(
        ClaimsPrincipal principal,
        IDictionary<string, JsonElement>? arguments) =>
        AttendedIdentityClaims.TryCreatePrivate(principal, out var identity)
        && TryReadEnvelopeIdentity(arguments, out var initiatingUserId, out var actingAgentId)
        && string.Equals(
            identity.InitiatingUserId,
            initiatingUserId,
            StringComparison.OrdinalIgnoreCase)
        && string.Equals(
            identity.ActingAgentId,
            actingAgentId,
            StringComparison.OrdinalIgnoreCase);

    private static bool TryReadEnvelopeIdentity(
        IDictionary<string, JsonElement>? arguments,
        out string initiatingUserId,
        out string? actingAgentId)
    {
        initiatingUserId = string.Empty;
        actingAgentId = null;

        if (arguments is null
            || !arguments.TryGetValue("request", out var request)
            || request.ValueKind != JsonValueKind.Object
            || !request.TryGetProperty("identity", out var identity)
            || identity.ValueKind != JsonValueKind.Object
            || !identity.TryGetProperty("initiatingUserId", out var initiatingUser)
            || initiatingUser.ValueKind != JsonValueKind.String)
        {
            return false;
        }

        initiatingUserId = initiatingUser.GetString()!;
        if (identity.TryGetProperty("actingAgentId", out var actingAgent)
            && actingAgent.ValueKind == JsonValueKind.String)
        {
            actingAgentId = actingAgent.GetString();
        }

        return !string.IsNullOrWhiteSpace(initiatingUserId);
    }
}
