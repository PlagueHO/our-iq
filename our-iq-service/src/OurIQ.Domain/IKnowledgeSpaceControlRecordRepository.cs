namespace OurIQ.Domain;

public interface IKnowledgeSpaceControlRecordRepository
{
    Task<KnowledgeSpaceControlRecord> CreateAsync(
        KnowledgeSpaceCreation creation,
        CancellationToken cancellationToken = default);

    Task<KnowledgeSpaceControlRecord?> GetAsync(
        string knowledgeSpaceId,
        CancellationToken cancellationToken = default);

    Task<KnowledgeSpaceControlRecordPage> ListAsync(
        KnowledgeSpaceControlRecordQuery query,
        CancellationToken cancellationToken = default);

    Task<KnowledgeSpaceControlRecord> UpdateAsync(
        KnowledgeSpaceControlRecord record,
        string expectedETag,
        CancellationToken cancellationToken = default);
}

public sealed record KnowledgeSpaceControlRecordQuery(
    string UserId,
    int PageSize,
    string? Cursor = null,
    string? LifecycleState = null)
{
    public const int MaximumPageSize = 100;

    public void Validate()
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(UserId);

        if (PageSize is < 1 or > MaximumPageSize)
        {
            throw new ArgumentOutOfRangeException(
                nameof(PageSize),
                $"The {nameof(PageSize)} value must be between 1 and {MaximumPageSize}.");
        }

        if (Cursor is not null && string.IsNullOrWhiteSpace(Cursor))
        {
            throw new ArgumentException("The cursor must not be empty.", nameof(Cursor));
        }

        if (LifecycleState is not null && !KnowledgeSpaceLifecycleStates.IsDefined(LifecycleState))
        {
            throw new ArgumentException(
                $"The lifecycle state '{LifecycleState}' is not supported.",
                nameof(LifecycleState));
        }
    }
}

public sealed record KnowledgeSpaceControlRecordPage(
    IReadOnlyList<KnowledgeSpaceControlRecord> Records,
    string? NextCursor);
