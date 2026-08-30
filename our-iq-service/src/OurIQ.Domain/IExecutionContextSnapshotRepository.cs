namespace OurIQ.Domain;

public interface IExecutionContextSnapshotRepository
{
    Task<ExecutionContextSnapshot> CreateAsync(
        ExecutionContextSnapshot snapshot,
        CancellationToken cancellationToken = default);

    Task<ExecutionContextSnapshot?> GetAsync(
        string executionId,
        string knowledgeSpaceId,
        CancellationToken cancellationToken = default);
}
