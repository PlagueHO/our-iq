using System.ComponentModel;
using System.Text.Json;
using Microsoft.AspNetCore.Http;
using ModelContextProtocol.Server;
using OurIQ.Contracts;
using OurIQ.Domain;

namespace OurIQ.McpServer;

[McpServerResourceType]
public static class PublicSpaceResources
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    [McpServerResource(
        UriTemplate = "ouriq://spaces{?cursor,pageSize,lifecycleState}",
        Name = "Authorized knowledge spaces",
        MimeType = "application/json")]
    [Description("List the knowledge spaces visible to the authenticated user.")]
    public static async Task<string> ListAsync(
        IKnowledgeSpaceControlRecordRepository repository,
        IHttpContextAccessor httpContextAccessor,
        string? cursor = null,
        int? pageSize = null,
        string? lifecycleState = null,
        CancellationToken cancellationToken = default)
    {
        var identity = GetAuthenticatedIdentity(httpContextAccessor);
        var requestedPageSize = pageSize ?? 20;
        var page = await repository.ListAsync(
            new KnowledgeSpaceControlRecordQuery(
                identity.InitiatingUserId,
                requestedPageSize,
                cursor,
                lifecycleState),
            cancellationToken);

        return JsonSerializer.Serialize(new
        {
            spaces = page.Records.Select(PublicKnowledgeSpace.From),
            pagination = new
            {
                pageSize = requestedPageSize,
                nextCursor = page.NextCursor
            }
        }, JsonOptions);
    }

    [McpServerResource(
        UriTemplate = "ouriq://spaces/{knowledgeSpaceId}",
        Name = "Authorized knowledge space",
        MimeType = "application/json")]
    [Description("Inspect public state for an accessible knowledge space.")]
    public static async Task<string> GetAsync(
        IKnowledgeSpaceControlRecordRepository repository,
        IHttpContextAccessor httpContextAccessor,
        string knowledgeSpaceId,
        CancellationToken cancellationToken)
    {
        var identity = GetAuthenticatedIdentity(httpContextAccessor);
        var record = await repository.GetAsync(knowledgeSpaceId, cancellationToken);

        if (record is null || !record.RoleGrants.Any(grant =>
                string.Equals(grant.UserId, identity.InitiatingUserId, StringComparison.Ordinal)))
        {
            throw new KeyNotFoundException($"The knowledge space '{knowledgeSpaceId}' was not found.");
        }

        return JsonSerializer.Serialize(PublicKnowledgeSpace.From(record), JsonOptions);
    }

    private static AttendedIdentity GetAuthenticatedIdentity(IHttpContextAccessor httpContextAccessor)
    {
        var user = httpContextAccessor.HttpContext?.User;
        if (user is null || !AttendedIdentityClaims.TryCreateUser(user, out var identity))
        {
            throw new UnauthorizedAccessException("An attended user identity is required.");
        }

        return identity;
    }

    private sealed record PublicKnowledgeSpace(
        string KnowledgeSpaceId,
        string DisplayName,
        string LifecycleState,
        DateTimeOffset CreatedAt,
        DateTimeOffset UpdatedAt)
    {
        public static PublicKnowledgeSpace From(KnowledgeSpaceControlRecord record) =>
            new(
                record.KnowledgeSpaceId,
                record.DisplayName,
                record.LifecycleState,
                record.CreatedAt,
                record.UpdatedAt);
    }
}
