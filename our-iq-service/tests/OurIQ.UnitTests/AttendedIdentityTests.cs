using System.Security.Claims;
using System.Text.Json;
using OurIQ.Contracts;
using OurIQ.Domain;

namespace OurIQ.UnitTests;

[TestClass]
public sealed class AttendedIdentityTests
{
    private const string TenantId = "11111111-1111-1111-1111-111111111111";
    private const string ObjectId = "22222222-2222-2222-2222-222222222222";
    private const string AgentId = "33333333-3333-3333-3333-333333333333";
    private const string DelegatedScope = "access_as_user";

    [TestMethod]
    public void UserIdentityIsTenantQualified()
    {
        var principal = CreateDelegatedPrincipal(
            (AttendedIdentityClaims.TenantId, TenantId.ToUpperInvariant()),
            (AttendedIdentityClaims.ObjectId, ObjectId.ToUpperInvariant()));

        var created = AttendedIdentityClaims.TryCreateUser(principal, out var identity);

        Assert.IsTrue(created);
        Assert.AreEqual($"{TenantId}:{ObjectId}", identity.InitiatingUserId);
        Assert.IsNull(identity.ActingAgentId);
    }

    [TestMethod]
    public void PrivateIdentityUsesAuthorizedPartyClaim()
    {
        var principal = CreateDelegatedPrincipal(
            (AttendedIdentityClaims.TenantId, TenantId),
            (AttendedIdentityClaims.ObjectId, ObjectId),
            (AttendedIdentityClaims.AuthorizedParty, AgentId));

        var created = AttendedIdentityClaims.TryCreatePrivate(principal, out var identity);

        Assert.IsTrue(created);
        Assert.AreEqual(AgentId, identity.ActingAgentId);
    }

    [TestMethod]
    public void PrivateIdentitySupportsV1ApplicationIdClaim()
    {
        var principal = CreateDelegatedPrincipal(
            (AttendedIdentityClaims.TenantId, TenantId),
            (AttendedIdentityClaims.ObjectId, ObjectId),
            (AttendedIdentityClaims.ApplicationId, AgentId));

        var created = AttendedIdentityClaims.TryCreatePrivate(principal, out var identity);

        Assert.IsTrue(created);
        Assert.AreEqual(AgentId, identity.ActingAgentId);
    }

    [TestMethod]
    public void ConflictingAgentClaimsFailClosed()
    {
        var principal = CreateDelegatedPrincipal(
            (AttendedIdentityClaims.TenantId, TenantId),
            (AttendedIdentityClaims.ObjectId, ObjectId),
            (AttendedIdentityClaims.AuthorizedParty, AgentId),
            (AttendedIdentityClaims.ApplicationId, "44444444-4444-4444-4444-444444444444"));

        Assert.IsFalse(AttendedIdentityClaims.TryCreatePrivate(principal, out _));
    }

    [TestMethod]
    public void MissingOrDuplicateUserClaimsFailClosed()
    {
        var missingObjectId = CreateDelegatedPrincipal((AttendedIdentityClaims.TenantId, TenantId));
        var duplicateTenant = CreateDelegatedPrincipal(
            (AttendedIdentityClaims.TenantId, TenantId),
            (AttendedIdentityClaims.TenantId, "55555555-5555-5555-5555-555555555555"),
            (AttendedIdentityClaims.ObjectId, ObjectId));

        Assert.IsFalse(AttendedIdentityClaims.TryCreateUser(missingObjectId, out _));
        Assert.IsFalse(AttendedIdentityClaims.TryCreateUser(duplicateTenant, out _));
    }

    [TestMethod]
    public void AppOnlyIdentityFailsClosed()
    {
        var principal = CreatePrincipal(
            (AttendedIdentityClaims.TenantId, TenantId),
            (AttendedIdentityClaims.ObjectId, ObjectId),
            (AttendedIdentityClaims.AuthorizedParty, AgentId),
            ("roles", "access_as_application"));

        Assert.IsFalse(AttendedIdentityClaims.TryCreateUser(principal, out _));
        Assert.IsFalse(AttendedIdentityClaims.TryCreatePrivate(principal, out _));
    }

    [TestMethod]
    public void EnvelopeIdentityMustMatchValidatedClaims()
    {
        var principal = CreateDelegatedPrincipal(
            (AttendedIdentityClaims.TenantId, TenantId),
            (AttendedIdentityClaims.ObjectId, ObjectId),
            (AttendedIdentityClaims.AuthorizedParty, AgentId));
        var validator = new AttendedIdentityEnvelopeValidator();

        Assert.IsTrue(validator.MatchesPublic(
            principal,
            CreateArguments($"{TenantId}:{ObjectId}", null)));
        Assert.IsTrue(validator.MatchesPrivate(
            principal,
            CreateArguments($"{TenantId}:{ObjectId}", AgentId)));
        Assert.IsTrue(validator.MatchesPrivate(
            principal,
            CreateArguments($"{TenantId}:{ObjectId}", DomainAgentIdentities.Ontology)));
    }

    private static ClaimsPrincipal CreatePrincipal(params (string Type, string Value)[] claims) =>
        new(new ClaimsIdentity(
            claims.Select(claim => new Claim(claim.Type, claim.Value)),
            "test"));

    private static ClaimsPrincipal CreateDelegatedPrincipal(
        params (string Type, string Value)[] claims) =>
        CreatePrincipal(
            claims.Append((AttendedIdentityClaims.Scope, DelegatedScope)).ToArray());

    private static Dictionary<string, JsonElement> CreateArguments(
        string initiatingUserId,
        string? actingAgentId)
    {
        var identity = actingAgentId is null
            ? new { initiatingUserId }
            : (object)new { initiatingUserId, actingAgentId };
        return new Dictionary<string, JsonElement>
        {
            ["request"] = JsonSerializer.SerializeToElement(new { identity })
        };
    }
}
