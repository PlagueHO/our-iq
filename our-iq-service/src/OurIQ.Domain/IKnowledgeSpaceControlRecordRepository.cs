namespace OurIQ.Domain;

public interface IKnowledgeSpaceControlRecordRepository
{
    Task<KnowledgeSpaceControlRecord> CreateAsync(
        KnowledgeSpaceCreation creation,
        CancellationToken cancellationToken = default);

    Task<KnowledgeSpaceControlRecord?> GetAsync(
        string knowledgeSpaceId,
        CancellationToken cancellationToken = default);

    Task<KnowledgeSpaceControlRecord> UpdateAsync(
        KnowledgeSpaceControlRecord record,
        string expectedETag,
        CancellationToken cancellationToken = default);
}
