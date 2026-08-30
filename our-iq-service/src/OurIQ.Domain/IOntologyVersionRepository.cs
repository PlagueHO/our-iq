namespace OurIQ.Domain;

public interface IOntologyVersionRepository
{
    Task<OntologyVersionEnvelope?> GetVersionAsync(
        string ontologyVersionId,
        string knowledgeSpaceId,
        CancellationToken cancellationToken = default);

    Task<OntologyVersionEnvelope> CreateVersionAsync(
        OntologyVersionEnvelope version,
        CancellationToken cancellationToken = default);

    Task<OntologyProposal> CreateProposalAsync(
        OntologyProposal proposal,
        CancellationToken cancellationToken = default);

    Task<OntologyCompatibilityAssessment> CreateCompatibilityAssessmentAsync(
        OntologyCompatibilityAssessment assessment,
        CancellationToken cancellationToken = default);

    Task<OntologyCompatibilityAssessment?> GetCompatibilityAssessmentAsync(
        string assessmentId,
        string knowledgeSpaceId,
        CancellationToken cancellationToken = default);

    Task<OntologyApproval> CreateApprovalAsync(
        OntologyApproval approval,
        CancellationToken cancellationToken = default);

    Task<OntologyApproval?> GetApprovalAsync(
        string approvalId,
        string knowledgeSpaceId,
        CancellationToken cancellationToken = default);

    Task<KnowledgeSpaceControlRecord> ActivateAsync(
        OntologyActivationRequest request,
        CancellationToken cancellationToken = default);
}
