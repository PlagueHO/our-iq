using Newtonsoft.Json;
using OurIQ.Domain;

namespace OurIQ.ToolServices;

internal sealed class KnowledgeSpaceControlRecordDocument
{
    [JsonProperty("id")]
    public string KnowledgeSpaceId { get; init; } = string.Empty;

    [JsonProperty("knowledgeSpaceId")]
    public string PartitionKnowledgeSpaceId { get; init; } = string.Empty;

    [JsonProperty("recordType")]
    public string RecordType { get; init; } = string.Empty;

    [JsonProperty("displayName")]
    public string DisplayName { get; init; } = string.Empty;

    [JsonProperty("lifecycleState")]
    public string LifecycleState { get; init; } = string.Empty;

    [JsonProperty("mutationPolicy")]
    public string MutationPolicy { get; init; } = string.Empty;

    [JsonProperty("mutationPolicyVersion")]
    public string MutationPolicyVersion { get; init; } = string.Empty;

    [JsonProperty("activeOntologyVersionId")]
    public string? ActiveOntologyVersionId { get; init; }

    [JsonProperty("activeOntologyDigest")]
    public string? ActiveOntologyDigest { get; init; }

    [JsonProperty("canonicalHeadVersion")]
    public string? CanonicalHeadVersion { get; init; }

    [JsonProperty("activeChangeSetId")]
    public string? ActiveChangeSetId { get; init; }

    [JsonProperty("createdAt")]
    public DateTimeOffset CreatedAt { get; init; }

    [JsonProperty("updatedAt")]
    public DateTimeOffset UpdatedAt { get; init; }

    [JsonProperty("createdBy")]
    public string? CreatedBy { get; init; }

    public static KnowledgeSpaceControlRecordDocument FromDomain(
        KnowledgeSpaceControlRecord record) =>
        new()
        {
            KnowledgeSpaceId = record.KnowledgeSpaceId,
            PartitionKnowledgeSpaceId = record.KnowledgeSpaceId,
            RecordType = record.RecordType,
            DisplayName = record.DisplayName,
            LifecycleState = record.LifecycleState,
            MutationPolicy = record.MutationPolicy,
            MutationPolicyVersion = record.MutationPolicyVersion,
            ActiveOntologyVersionId = record.ActiveOntologyVersionId,
            ActiveOntologyDigest = record.ActiveOntologyDigest,
            CanonicalHeadVersion = record.CanonicalHeadVersion,
            ActiveChangeSetId = record.ActiveChangeSetId,
            CreatedAt = record.CreatedAt,
            UpdatedAt = record.UpdatedAt,
            CreatedBy = record.CreatedBy
        };

    public KnowledgeSpaceControlRecord ToDomain(string? etag) =>
        new()
        {
            KnowledgeSpaceId = KnowledgeSpaceId,
            RecordType = RecordType,
            DisplayName = DisplayName,
            LifecycleState = LifecycleState,
            MutationPolicy = MutationPolicy,
            MutationPolicyVersion = MutationPolicyVersion,
            ActiveOntologyVersionId = ActiveOntologyVersionId,
            ActiveOntologyDigest = ActiveOntologyDigest,
            CanonicalHeadVersion = CanonicalHeadVersion,
            ActiveChangeSetId = ActiveChangeSetId,
            CreatedAt = CreatedAt,
            UpdatedAt = UpdatedAt,
            CreatedBy = CreatedBy,
            ETag = etag
        };
}
