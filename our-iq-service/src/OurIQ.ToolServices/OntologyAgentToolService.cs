using System.Diagnostics;
using System.Text.Json;
using System.Text.Json.Serialization;
using ModelContextProtocol.Protocol;
using OurIQ.Contracts;
using OurIQ.Domain;

namespace OurIQ.ToolServices;

public sealed class OntologyAgentToolService(
    IKnowledgeSpaceControlRecordRepository spaces,
    IOntologyVersionRepository ontologies,
    IExecutionContextSnapshotRepository snapshots,
    ILogger<OntologyAgentToolService>? logger = null)
{
    public const string ContractVersion = "1.0";

    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        PropertyNameCaseInsensitive = false,
        UnmappedMemberHandling = JsonUnmappedMemberHandling.Disallow,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        Converters = { new JsonStringEnumConverter(JsonNamingPolicy.CamelCase) }
    };

    private static readonly IReadOnlyDictionary<string, OperationPolicy> Policies =
        new Dictionary<string, OperationPolicy>(StringComparer.Ordinal)
        {
            ["get_space"] = new(
                KnowledgeSpaceUserCapabilities.InspectOntology,
                [
                    KnowledgeSpaceLifecycleStates.Draft,
                    KnowledgeSpaceLifecycleStates.Pending,
                    KnowledgeSpaceLifecycleStates.Active,
                    KnowledgeSpaceLifecycleStates.Readonly,
                    KnowledgeSpaceLifecycleStates.Maintenance,
                    KnowledgeSpaceLifecycleStates.Retired
                ]),
            ["get_ontology"] = ReadOntologyPolicy(),
            ["list_all_templates"] = ReadOntologyPolicy(),
            ["get_template"] = ReadOntologyPolicy(),
            ["stage_ontology_version"] = ChangeOntologyPolicy(
                KnowledgeSpaceUserCapabilities.StageOntologyVersion),
            ["validate_ontology_compatibility"] = ChangeOntologyPolicy(
                KnowledgeSpaceUserCapabilities.StageOntologyVersion),
            ["record_approval"] = new(
                KnowledgeSpaceUserCapabilities.ApproveOntology,
                [KnowledgeSpaceLifecycleStates.Pending]),
            ["activate_ontology_version"] = new(
                KnowledgeSpaceUserCapabilities.ApproveOntology,
                [KnowledgeSpaceLifecycleStates.Pending])
        };

    public async Task<CallToolResult> ExecuteAsync(
        string expectedOperation,
        JsonElement requestJson,
        CancellationToken cancellationToken = default)
    {
        OntologyToolRequest? request = null;
        AuthorizedOperation? authorized = null;

        try
        {
            _ = JsonCanonicalizer.Canonicalize(requestJson);
            ValidateRequestShape(requestJson);
            request = requestJson.Deserialize<OntologyToolRequest>(JsonOptions)
                ?? throw new JsonException("The request envelope is required.");
            for (var attempt = 0; attempt < 3; attempt++)
            {
                authorized = null;
                var context = await AuthorizeAsync(
                    expectedOperation, request, requestJson, cancellationToken);
                authorized = context;
                if (context.ReplayIdentity is { } identity)
                {
                    var stored = await ontologies.GetReplayAsync(identity, cancellationToken);
                    if (stored is not null)
                    {
                        return ToolResult(stored.Match(identity));
                    }
                }

                await ValidateStateAsync(context, cancellationToken);
                try
                {
                    var result = await ExecuteAuthorizedAsync(context, cancellationToken);
                    return result is IdempotencyRecord outcome
                        ? ToolResult(outcome)
                        : ToolResult(Success(request, result), false);
                }
                catch (MutationConcurrencyException)
                {
                    cancellationToken.ThrowIfCancellationRequested();
                }
            }

            throw new MutationDependencyException();
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (IdempotencyKeyConflictException)
        {
            return ToolFailureResults.Create(
                expectedOperation, requestJson, true, ApplicationError.IdempotencyConflict);
        }
        catch (Exception exception) when (IsBusinessFailure(exception))
        {
            var response = Error(request, expectedOperation, ToToolException(exception));
            if (authorized?.ReplayIdentity is not { } identity
                || exception is OntologyToolException { Code: "authorization_denied" })
            {
                return ToolResult(response, true);
            }

            try
            {
                var outcome = await ontologies.SaveFailureAsync(
                    new(identity, JsonSerializer.Serialize(response, JsonOptions), true, DateTimeOffset.UtcNow),
                    cancellationToken);
                return ToolResult(outcome.Match(identity));
            }
            catch (IdempotencyKeyConflictException)
            {
                return ToolFailureResults.Create(
                    expectedOperation, requestJson, true, ApplicationError.IdempotencyConflict);
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                throw;
            }
            catch (Exception persistenceException)
            {
                return Failure(persistenceException);
            }
        }
        catch (Exception exception)
        {
            return Failure(exception);
        }

        CallToolResult Failure(Exception exception)
        {
            var correlationId = Activity.Current?.TraceId.ToHexString() ?? Guid.NewGuid().ToString("N");
            logger?.LogError(
                "Tool failed at {Stage} with {ErrorType}. Correlation {CorrelationId}.",
                "ontology", exception.GetType().Name, correlationId);
            return ToolFailureResults.Create(
                expectedOperation, requestJson, true, ToolFailureResults.Classify(exception), correlationId);
        }
    }

    private async Task<AuthorizedOperation> AuthorizeAsync(
        string expectedOperation,
        OntologyToolRequest request,
        JsonElement requestJson,
        CancellationToken cancellationToken)
    {
        ValidateEnvelope(expectedOperation, request);
        var policy = Policies[expectedOperation];
        var current = await spaces.GetAsync(request.KnowledgeSpaceId, cancellationToken)
            ?? throw new OntologyToolException(
                "validation_failed",
                "validation",
                $"Knowledge space '{request.KnowledgeSpaceId}' does not exist.",
                "Supply an existing knowledge-space identifier.");

        var authorization = KnowledgeSpaceCapabilityAuthorizer.Authorize(
            current,
            new KnowledgeSpaceCapabilityAuthorizationRequest(
                request.Identity.InitiatingUserId,
                policy.UserCapability,
                request.Identity.ActingAgentId,
                request.Identity.AgentDefinitionVersion,
                expectedOperation));
        if (!authorization.IsAuthorized)
        {
            throw new OntologyToolException(
                "authorization_denied",
                "authorization",
                "The initiating user or Ontology Agent lacks the required capability.",
                "Use the accepted Ontology Agent definition and an authorized Ontology Manager.");
        }

        var identity = IsMutating(expectedOperation)
            ? IdempotencyIdentity.Create(
                expectedOperation, request.Identity.InitiatingUserId,
                request.Identity.ActingAgentId, requestJson)
            : null;
        return new AuthorizedOperation(request, current, identity);
    }

    private async Task ValidateStateAsync(
        AuthorizedOperation operation,
        CancellationToken cancellationToken)
    {
        var request = operation.Request;
        var current = operation.Space;
        if (!Policies[request.Operation].LegalStates.Contains(current.LifecycleState))
        {
            throw new OntologyToolException(
                "space_state_conflict",
                "state",
                $"Operation '{request.Operation}' is not legal while the knowledge space is '{current.LifecycleState}'.",
                "Move the knowledge space to a legal lifecycle state before retrying.");
        }

        var persistedSnapshot = await snapshots.GetAsync(
            request.ExecutionContext.ExecutionId,
            request.KnowledgeSpaceId,
            cancellationToken)
            ?? throw new OntologyToolException(
                "replan_required",
                "conflict",
                "The execution-context snapshot does not exist.",
                "Create a new immutable execution-context snapshot.");
        ValidateSnapshotEnvelope(request, persistedSnapshot);

        var freshness = persistedSnapshot.CheckFreshness(current);
        if (!freshness.IsFresh)
        {
            throw new OntologyToolException(
                ExecutionContextFreshnessResult.ReplanRequiredCode,
                "conflict",
                $"The execution-context snapshot is stale: {string.Join(", ", freshness.ChangedFields)}.",
                "Create a fresh execution-context snapshot and retry.");
        }
    }

    public static bool IsMutating(string operation) =>
        operation is "stage_ontology_version" or "validate_ontology_compatibility"
            or "record_approval" or "activate_ontology_version";

    private static bool IsBusinessFailure(Exception exception) =>
        exception is OntologyToolException or OntologyActivationConflictException
            or KnowledgeSpaceStateConflictException or JsonException or ArgumentException
            or OntologyPayloadValidationException or ExecutionContextSnapshotValidationException
            or OntologyControlRecordConflictException;

    private static OntologyToolException ToToolException(Exception exception) =>
        exception switch
        {
            OntologyToolException known => known,
            OntologyActivationConflictException => new(
                "replan_required", "conflict", "The ontology activation context changed.",
                "Create a fresh execution context and retry with a new key."),
            KnowledgeSpaceStateConflictException => new(
                "space_state_conflict", "state", "The lifecycle state does not permit this operation.",
                "Inspect the current knowledge-space lifecycle state."),
            _ => new(
                "validation_failed", "validation", "The request or its domain records are invalid.",
                "Correct the request and use a new key for changed input.")
        };

    private async Task<object> ExecuteAuthorizedAsync(
        AuthorizedOperation operation,
        CancellationToken cancellationToken) =>
        operation.Request.Operation switch
        {
            "get_space" => GetSpace(operation),
            "get_ontology" => await GetOntologyAsync(operation, cancellationToken),
            "list_all_templates" => await ListAllTemplatesAsync(operation, cancellationToken),
            "get_template" => await GetTemplateAsync(operation, cancellationToken),
            "stage_ontology_version" => await StageOntologyVersionAsync(operation, cancellationToken),
            "validate_ontology_compatibility" =>
                await ValidateOntologyCompatibilityAsync(operation, cancellationToken),
            "record_approval" => await RecordApprovalAsync(operation, cancellationToken),
            "activate_ontology_version" =>
                await ActivateOntologyVersionAsync(operation, cancellationToken),
            _ => throw new InvalidOperationException("The operation policy is incomplete.")
        };

    private static object GetSpace(AuthorizedOperation operation)
    {
        _ = DeserializeArguments<EmptyArguments>(operation.Request);
        return new
        {
            operation.Space.KnowledgeSpaceId,
            operation.Space.DisplayName,
            operation.Space.LifecycleState,
            operation.Space.MutationPolicy,
            operation.Space.MutationPolicyVersion,
            operation.Space.ActiveOntologyVersionId,
            operation.Space.ActiveOntologyDigest,
            operation.Space.CanonicalHeadVersion,
            operation.Space.CreatedAt,
            operation.Space.UpdatedAt
        };
    }

    private async Task<object> GetOntologyAsync(
        AuthorizedOperation operation,
        CancellationToken cancellationToken)
    {
        var arguments = DeserializeArguments<OntologyVersionArguments>(operation.Request);
        var version = await GetRequiredVersionAsync(
            operation,
            arguments.OntologyVersionId,
            cancellationToken);
        return new { ontology = version };
    }

    private async Task<object> ListAllTemplatesAsync(
        AuthorizedOperation operation,
        CancellationToken cancellationToken)
    {
        var arguments = DeserializeArguments<OntologyVersionArguments>(operation.Request);
        var version = await GetRequiredVersionAsync(
            operation,
            arguments.OntologyVersionId,
            cancellationToken);
        return new
        {
            version.OntologyVersionId,
            templates = version.Payload.TemplateReferences
                .OrderBy(template => template.TemplateId, StringComparer.Ordinal)
                .ThenBy(template => template.RevisionId, StringComparer.Ordinal)
                .ToArray()
        };
    }

    private async Task<object> GetTemplateAsync(
        AuthorizedOperation operation,
        CancellationToken cancellationToken)
    {
        var arguments = DeserializeArguments<GetTemplateArguments>(operation.Request);
        Require(arguments.TemplateId, "templateId");
        Require(arguments.RevisionId, "revisionId");
        var version = await GetRequiredVersionAsync(
            operation,
            arguments.OntologyVersionId,
            cancellationToken);
        var template = version.Payload.TemplateReferences.SingleOrDefault(candidate =>
            string.Equals(candidate.TemplateId, arguments.TemplateId, StringComparison.Ordinal)
            && string.Equals(candidate.RevisionId, arguments.RevisionId, StringComparison.Ordinal))
            ?? throw new OntologyToolException(
                "validation_failed",
                "validation",
                $"Template '{arguments.TemplateId}' revision '{arguments.RevisionId}' does not exist.",
                "Use list_all_templates to select an immutable template revision.");
        return new { version.OntologyVersionId, template };
    }

    private async Task<object> StageOntologyVersionAsync(
        AuthorizedOperation operation,
        CancellationToken cancellationToken)
    {
        EnsureArgumentProperties(
            operation.Request,
            "proposalId",
            "payload");
        EnsureOntologyPayloadProperties(operation.Request);
        var arguments = DeserializeArguments<StageOntologyVersionArguments>(operation.Request);
        Require(arguments.ProposalId, "proposalId");
        ArgumentNullException.ThrowIfNull(arguments.Payload);
        var timestamp = DateTimeOffset.UtcNow;
        var createdBy = ActorReference(operation.Request);
        var version = new OntologyVersionEnvelope
        {
            Id = arguments.Payload.OntologyVersionId,
            RecordType = "ontologyVersion",
            KnowledgeSpaceId = operation.Request.KnowledgeSpaceId,
            OntologyId = arguments.Payload.OntologyId,
            OntologyVersionId = arguments.Payload.OntologyVersionId,
            SchemaVersion = ContractVersion,
            Payload = arguments.Payload,
            PayloadDigest = OntologyPayloadDigest.Compute(arguments.Payload),
            CreatedAt = timestamp,
            CreatedBy = createdBy,
            SourceReferences = arguments.SourceReferences ?? []
        };
        version.Validate();
        var proposal = new OntologyProposal
        {
            Id = arguments.ProposalId,
            KnowledgeSpaceId = operation.Request.KnowledgeSpaceId,
            OntologyVersionId = version.OntologyVersionId,
            CreatedAt = timestamp,
            CreatedBy = createdBy,
            SourceReferences = arguments.SourceReferences ?? []
        };
        proposal.Validate();

        var result = new
        {
            version.OntologyVersionId,
            version.PayloadDigest,
            proposalId = proposal.Id
        };
        return await ontologies.CommitStagedVersionAsync(
            version, proposal, operation.Space, Outcome(operation, result), cancellationToken);
    }

    private async Task<object> ValidateOntologyCompatibilityAsync(
        AuthorizedOperation operation,
        CancellationToken cancellationToken)
    {
        EnsureArgumentProperties(
            operation.Request,
            "assessmentId",
            "ontologyVersionId");
        var arguments = DeserializeArguments<ValidateCompatibilityArguments>(operation.Request);
        Require(arguments.AssessmentId, "assessmentId");
        Require(arguments.OntologyVersionId, "ontologyVersionId");
        _ = await GetRequiredVersionAsync(
            operation,
            arguments.OntologyVersionId,
            cancellationToken);

        var hasDifferentActiveVersion =
            operation.Space.ActiveOntologyVersionId is not null
            && !string.Equals(
                operation.Space.ActiveOntologyVersionId,
                arguments.OntologyVersionId,
                StringComparison.Ordinal);
        string[] findings = hasDifferentActiveVersion
            ? new[] { "active_ontology_requires_migration" }
            : [];
        var assessment = new OntologyCompatibilityAssessment
        {
            Id = arguments.AssessmentId,
            KnowledgeSpaceId = operation.Request.KnowledgeSpaceId,
            OntologyVersionId = arguments.OntologyVersionId,
            IsApproved = !hasDifferentActiveVersion,
            RequiresMigration = hasDifferentActiveVersion,
            CreatedAt = DateTimeOffset.UtcNow,
            CreatedBy = ActorReference(operation.Request),
            Findings = findings
        };
        var result = new
        {
            assessmentId = assessment.Id,
            compatible = assessment.IsApproved,
            assessment.RequiresMigration,
            assessment.Findings
        };
        return await ontologies.CommitAssessmentAsync(
            assessment, operation.Space, Outcome(operation, result), cancellationToken);
    }

    private async Task<object> RecordApprovalAsync(
        AuthorizedOperation operation,
        CancellationToken cancellationToken)
    {
        EnsureArgumentProperties(
            operation.Request,
            "approvalId",
            "ontologyVersionId",
            "compatibilityAssessmentId",
            "decision");
        var arguments = DeserializeArguments<RecordApprovalArguments>(operation.Request);
        Require(arguments.ApprovalId, "approvalId");
        Require(arguments.OntologyVersionId, "ontologyVersionId");
        Require(arguments.CompatibilityAssessmentId, "compatibilityAssessmentId");
        if (arguments.Decision is not ("approve" or "reject"))
        {
            throw new OntologyPayloadValidationException(
                "The approval decision must be 'approve' or 'reject'.");
        }

        var assessment = await ontologies.GetCompatibilityAssessmentAsync(
            arguments.CompatibilityAssessmentId,
            operation.Request.KnowledgeSpaceId,
            cancellationToken)
            ?? throw new OntologyToolException(
                "validation_failed",
                "validation",
                $"Compatibility assessment '{arguments.CompatibilityAssessmentId}' does not exist.",
                "Validate ontology compatibility before recording approval.");
        if (!string.Equals(
                assessment.OntologyVersionId,
                arguments.OntologyVersionId,
                StringComparison.Ordinal))
        {
            throw new OntologyPayloadValidationException(
                "The approval and compatibility assessment must identify the same ontology version.");
        }

        var approved = arguments.Decision == "approve";
        if (approved && !assessment.IsApproved)
        {
            throw new OntologyToolException(
                "validation_failed",
                "validation",
                "An ontology that requires migration cannot be approved in the thin slice.",
                "Reject the version or use a future approved migration workflow.");
        }

        var approval = new OntologyApproval
        {
            Id = arguments.ApprovalId,
            KnowledgeSpaceId = operation.Request.KnowledgeSpaceId,
            OntologyVersionId = arguments.OntologyVersionId,
            CompatibilityAssessmentId = assessment.Id,
            ActorId = operation.Request.Identity.InitiatingUserId,
            Authority = KnowledgeSpaceRoles.OntologyManager,
            IsApproved = approved,
            CreatedAt = DateTimeOffset.UtcNow
        };
        return await ontologies.CommitApprovalAsync(
            approval, operation.Space,
            Outcome(operation, new { approvalId = approval.Id, approved = approval.IsApproved }),
            cancellationToken);
    }

    private async Task<object> ActivateOntologyVersionAsync(
        AuthorizedOperation operation,
        CancellationToken cancellationToken)
    {
        EnsureArgumentProperties(
            operation.Request,
            "ontologyVersionId",
            "payloadDigest",
            "approvalId",
            "activationEvidenceId",
            "expectedActiveOntologyVersionId",
            "expectedActiveOntologyDigest");
        var arguments = DeserializeArguments<ActivateOntologyVersionArguments>(operation.Request);
        Require(arguments.OntologyVersionId, "ontologyVersionId");
        Require(arguments.PayloadDigest, "payloadDigest");
        Require(arguments.ApprovalId, "approvalId");
        Require(arguments.ActivationEvidenceId, "activationEvidenceId");
        var activation = new OntologyActivationRequest(
                operation.Request.KnowledgeSpaceId,
                arguments.OntologyVersionId,
                arguments.PayloadDigest,
                arguments.ApprovalId,
                arguments.ExpectedActiveOntologyVersionId,
                arguments.ExpectedActiveOntologyDigest,
                arguments.ActivationEvidenceId);
        var result = new
        {
            ontologyVersionId = arguments.OntologyVersionId,
            payloadDigest = arguments.PayloadDigest,
            lifecycleState = KnowledgeSpaceLifecycleStates.Active,
            activationEvidenceId = arguments.ActivationEvidenceId
        };
        return await ontologies.CommitActivationAsync(
            activation, operation.Space, Outcome(operation, result), cancellationToken);
    }

    private async Task<OntologyVersionEnvelope> GetRequiredVersionAsync(
        AuthorizedOperation operation,
        string? requestedVersionId,
        CancellationToken cancellationToken)
    {
        var versionId = string.IsNullOrWhiteSpace(requestedVersionId)
            ? operation.Space.ActiveOntologyVersionId
            : requestedVersionId;
        if (string.IsNullOrWhiteSpace(versionId))
        {
            throw new OntologyToolException(
                "ontology_not_active",
                "state",
                "No active ontology exists and no ontology version was requested.",
                "Supply a staged ontology version identifier or activate an ontology.");
        }

        return await ontologies.GetVersionAsync(
            versionId,
            operation.Request.KnowledgeSpaceId,
            cancellationToken)
            ?? throw new OntologyToolException(
                "validation_failed",
                "validation",
                $"Ontology version '{versionId}' does not exist.",
                "Supply an immutable ontology version in this knowledge space.");
    }

    private static T DeserializeArguments<T>(OntologyToolRequest request)
        where T : class, new()
    {
        if (request.Arguments.ValueKind is JsonValueKind.Undefined or JsonValueKind.Null)
        {
            return new T();
        }

        if (request.Arguments.ValueKind != JsonValueKind.Object)
        {
            throw new JsonException("The arguments value must be an object.");
        }

        return request.Arguments.Deserialize<T>(JsonOptions)
            ?? throw new JsonException("The arguments value is invalid.");
    }

    private static void EnsureArgumentProperties(
        OntologyToolRequest request,
        params string[] propertyNames)
    {
        foreach (var propertyName in propertyNames)
        {
            if (!request.Arguments.TryGetProperty(propertyName, out _))
            {
                throw new JsonException($"The arguments.{propertyName} property is required.");
            }
        }
    }

    private static void EnsureOntologyPayloadProperties(OntologyToolRequest request)
    {
        var payload = request.Arguments.GetProperty("payload");
        if (payload.ValueKind != JsonValueKind.Object)
        {
            throw new JsonException("The arguments.payload property must be an object.");
        }

        foreach (var propertyName in new[]
                 {
                     "ontologyId",
                     "ontologyVersionId",
                     "title",
                     "description",
                     "documentTypes",
                     "hierarchy",
                     "relationshipTypes",
                     "rules",
                     "filterableFields",
                     "templateReferences"
                 })
        {
            if (!payload.TryGetProperty(propertyName, out _))
            {
                throw new JsonException($"The arguments.payload.{propertyName} property is required.");
            }
        }
    }

    private static void ValidateRequestShape(JsonElement request)
    {
        if (request.ValueKind != JsonValueKind.Object)
        {
            throw new JsonException("The request envelope must be an object.");
        }

        foreach (var propertyName in new[]
                 {
                     "contractVersion",
                     "operation",
                     "knowledgeSpaceId",
                     "identity",
                     "executionContext",
                     "arguments"
                 })
        {
            if (!request.TryGetProperty(propertyName, out _))
            {
                throw new JsonException($"The {propertyName} property is required.");
            }
        }

        var context = request.GetProperty("executionContext");
        if (context.ValueKind != JsonValueKind.Object)
        {
            throw new JsonException("The executionContext property must be an object.");
        }

        foreach (var propertyName in new[]
                 {
                     "executionId",
                     "traceId",
                     "correlationId",
                     "knowledgeSpaceId",
                     "lifecycleState",
                     "ontologyVersion",
                     "ontologyDigest",
                     "mutationPolicy",
                     "mutationPolicyVersion",
                     "canonicalHeadVersion"
                 })
        {
            if (!context.TryGetProperty(propertyName, out _))
            {
                throw new JsonException(
                    $"The executionContext.{propertyName} property is required.");
            }
        }

        if (request.GetProperty("arguments").ValueKind != JsonValueKind.Object)
        {
            throw new JsonException("The arguments property must be an object.");
        }
    }

    private static void ValidateEnvelope(string expectedOperation, OntologyToolRequest request)
    {
        if (!string.Equals(request.ContractVersion, ContractVersion, StringComparison.Ordinal))
        {
            throw new OntologyToolException(
                "contract_version_unsupported",
                "validation",
                $"Contract version '{request.ContractVersion}' is not supported.",
                $"Use private contract version '{ContractVersion}'.");
        }

        Require(request.KnowledgeSpaceId, "knowledgeSpaceId");
        Require(request.Operation, "operation");
        if (IsMutating(expectedOperation))
        {
            Require(request.IdempotencyKey, "idempotencyKey");
        }

        ArgumentNullException.ThrowIfNull(request.Identity);
        ArgumentNullException.ThrowIfNull(request.ExecutionContext);
        if (!string.Equals(request.Operation, expectedOperation, StringComparison.Ordinal)
            || !string.Equals(
                request.Identity.RequiredCapability,
                expectedOperation,
                StringComparison.Ordinal))
        {
            throw new OntologyToolException(
                "authorization_denied",
                "authorization",
                "The invoked tool, operation, and required capability must match.",
                "Use the fixed Ontology Agent manifest entry for the invoked operation.");
        }

        if (!string.Equals(
                request.Identity.ActingAgentId,
                DomainAgentIdentities.Ontology,
                StringComparison.Ordinal)
            || !string.Equals(
                request.Identity.AgentDefinitionVersion,
                DomainAgentIdentities.InitialDefinitionVersion,
                StringComparison.Ordinal)
            || !DomainAgentCapabilities.HasCapability(
                DomainAgentIdentities.Ontology,
                request.Identity.AgentDefinitionVersion,
                expectedOperation))
        {
            throw new OntologyToolException(
                "authorization_denied",
                "authorization",
                "The call does not use the accepted Ontology Agent identity and fixed manifest.",
                "Use Ontology Agent definition version 1.0.");
        }

        Require(request.Identity.InitiatingUserId, "initiatingUserId");
        Require(request.ExecutionContext.ExecutionId, "executionId");
        Require(request.ExecutionContext.TraceId, "traceId");
        Require(request.ExecutionContext.CorrelationId, "correlationId");
        if (!string.Equals(
                request.KnowledgeSpaceId,
                request.ExecutionContext.KnowledgeSpaceId,
                StringComparison.Ordinal))
        {
            throw new ExecutionContextSnapshotValidationException(
                "The request and execution-context knowledge-space identifiers must match.");
        }
    }

    private static void ValidateSnapshotEnvelope(
        OntologyToolRequest request,
        ExecutionContextSnapshot snapshot)
    {
        snapshot.Validate();
        var context = request.ExecutionContext;
        var mismatches = new List<string>();
        AddMismatch(mismatches, "executionId", context.ExecutionId, snapshot.ExecutionId);
        AddMismatch(mismatches, "traceId", context.TraceId, snapshot.TraceId);
        AddMismatch(
            mismatches,
            "knowledgeSpaceId",
            context.KnowledgeSpaceId,
            snapshot.KnowledgeSpaceId);
        AddMismatch(mismatches, "lifecycleState", context.LifecycleState, snapshot.LifecycleState);
        AddMismatch(
            mismatches,
            "ontologyVersion",
            context.OntologyVersion,
            snapshot.ActiveOntologyVersionId);
        AddMismatch(
            mismatches,
            "ontologyDigest",
            context.OntologyDigest,
            snapshot.ActiveOntologyDigest);
        AddMismatch(
            mismatches,
            "mutationPolicy",
            context.MutationPolicy,
            snapshot.MutationPolicy);
        AddMismatch(
            mismatches,
            "mutationPolicyVersion",
            context.MutationPolicyVersion,
            snapshot.MutationPolicyVersion);
        AddMismatch(
            mismatches,
            "canonicalHeadVersion",
            context.CanonicalHeadVersion,
            snapshot.CanonicalHeadVersion);
        AddMismatch(
            mismatches,
            "initiatingUserId",
            request.Identity.InitiatingUserId,
            snapshot.InitiatingUserId);
        AddMismatch(
            mismatches,
            "actingAgentId",
            request.Identity.ActingAgentId,
            snapshot.ActingAgentId);
        AddMismatch(
            mismatches,
            "agentDefinitionVersion",
            request.Identity.AgentDefinitionVersion,
            snapshot.AgentDefinitionVersion);

        if (mismatches.Count > 0)
        {
            throw new OntologyToolException(
                "replan_required",
                "conflict",
                $"The request does not match its immutable execution-context snapshot: {string.Join(", ", mismatches)}.",
                "Use the original snapshot values or create a fresh execution context.");
        }
    }

    private static void AddMismatch(
        ICollection<string> mismatches,
        string field,
        string? requested,
        string? persisted)
    {
        if (!string.Equals(requested, persisted, StringComparison.Ordinal))
        {
            mismatches.Add(field);
        }
    }

    private static void Require(string? value, string name)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new OntologyPayloadValidationException($"The {name} value is required.");
        }
    }

    private static string ActorReference(OntologyToolRequest request) =>
        $"{request.Identity.InitiatingUserId}|{request.Identity.ActingAgentId}|{request.Identity.AgentDefinitionVersion}";

    private static CallToolResult ToolResult(OntologyToolResponse response, bool isError) =>
        new()
        {
            IsError = isError,
            Content =
            [
                new TextContentBlock { Text = JsonSerializer.Serialize(response, JsonOptions) }
            ]
        };

    private static CallToolResult ToolResult(IdempotencyRecord outcome) =>
        new()
        {
            IsError = outcome.IsError,
            Content = [new TextContentBlock { Text = outcome.ResponseJson }]
        };

    private static IdempotencyRecord Outcome(AuthorizedOperation operation, object result) =>
        new(
            operation.ReplayIdentity
                ?? throw new InvalidOperationException("The mutation requires a replay identity."),
            JsonSerializer.Serialize(Success(operation.Request, result), JsonOptions),
            false,
            DateTimeOffset.UtcNow);

    private static OntologyToolResponse Success(OntologyToolRequest request, object result) =>
        new(
            ContractVersion,
            request.Operation,
            request.KnowledgeSpaceId,
            "completed",
            request.ExecutionContext.CorrelationId,
            "notApplicable",
            result,
            null);

    private static OntologyToolResponse Error(
        OntologyToolRequest? request,
        string operation,
        OntologyToolException exception) =>
        new(
            ContractVersion,
            operation,
            string.IsNullOrWhiteSpace(request?.KnowledgeSpaceId)
                ? null
                : request.KnowledgeSpaceId,
            "error",
            string.IsNullOrWhiteSpace(request?.ExecutionContext?.CorrelationId)
                ? Activity.Current?.TraceId.ToHexString() ?? Guid.NewGuid().ToString("N")
                : request.ExecutionContext.CorrelationId,
            "notApplicable",
            null,
            new ApplicationError(
                exception.Code,
                exception.Category,
                exception.Message,
                exception.Remediation));

    private static OperationPolicy ReadOntologyPolicy() =>
        new(
            KnowledgeSpaceUserCapabilities.InspectOntology,
            [
                KnowledgeSpaceLifecycleStates.Draft,
                KnowledgeSpaceLifecycleStates.Pending,
                KnowledgeSpaceLifecycleStates.Active,
                KnowledgeSpaceLifecycleStates.Readonly,
                KnowledgeSpaceLifecycleStates.Maintenance,
                KnowledgeSpaceLifecycleStates.Retired
            ]);

    private static OperationPolicy ChangeOntologyPolicy(string userCapability) =>
        new(
            userCapability,
            [
                KnowledgeSpaceLifecycleStates.Draft,
                KnowledgeSpaceLifecycleStates.Pending,
                KnowledgeSpaceLifecycleStates.Maintenance
            ]);

    private sealed record AuthorizedOperation(
        OntologyToolRequest Request,
        KnowledgeSpaceControlRecord Space,
        IdempotencyIdentity? ReplayIdentity);

    private sealed record OperationPolicy(
        string UserCapability,
        IReadOnlySet<string> LegalStates)
    {
        public OperationPolicy(string userCapability, string[] legalStates)
            : this(userCapability, new HashSet<string>(legalStates, StringComparer.Ordinal))
        {
        }
    }
}

public sealed record OntologyToolRequest
{
    public string? IdempotencyKey { get; init; }

    public string ContractVersion { get; init; } = string.Empty;

    public string Operation { get; init; } = string.Empty;

    public string KnowledgeSpaceId { get; init; } = string.Empty;

    public OntologyToolIdentity Identity { get; init; } = new();

    public OntologyToolExecutionContext ExecutionContext { get; init; } = new();

    public JsonElement Arguments { get; init; }
}

public sealed record OntologyToolIdentity
{
    public string InitiatingUserId { get; init; } = string.Empty;

    public string ActingAgentId { get; init; } = string.Empty;

    public string AgentDefinitionVersion { get; init; } = string.Empty;

    public string RequiredCapability { get; init; } = string.Empty;
}

public sealed record OntologyToolExecutionContext
{
    public string ExecutionId { get; init; } = string.Empty;

    public string TraceId { get; init; } = string.Empty;

    public string CorrelationId { get; init; } = string.Empty;

    public string KnowledgeSpaceId { get; init; } = string.Empty;

    public string LifecycleState { get; init; } = string.Empty;

    public string? OntologyVersion { get; init; }

    public string? OntologyDigest { get; init; }

    public string MutationPolicy { get; init; } = string.Empty;

    public string MutationPolicyVersion { get; init; } = string.Empty;

    public string? CanonicalHeadVersion { get; init; }
}

public sealed record OntologyToolResponse(
    string ContractVersion,
    string Operation,
    [property: JsonIgnore(Condition = JsonIgnoreCondition.Never)] string? KnowledgeSpaceId,
    string Outcome,
    string CorrelationId,
    string Pagination,
    object? Result,
    ApplicationError? Error);

public sealed class OntologyToolException(
    string code,
    string category,
    string message,
    string remediation)
    : InvalidOperationException(message)
{
    public string Code { get; } = code;

    public string Category { get; } = category;

    public string Remediation { get; } = remediation;
}

internal sealed record OntologyVersionArguments
{
    public string? OntologyVersionId { get; init; }
}

internal sealed record EmptyArguments;

internal sealed record GetTemplateArguments
{
    public string? OntologyVersionId { get; init; }

    public string TemplateId { get; init; } = string.Empty;

    public string RevisionId { get; init; } = string.Empty;
}

internal sealed record StageOntologyVersionArguments
{
    public string ProposalId { get; init; } = string.Empty;

    public OntologyPayload Payload { get; init; } = new();

    public IReadOnlyList<string>? SourceReferences { get; init; }
}

internal sealed record ValidateCompatibilityArguments
{
    public string AssessmentId { get; init; } = string.Empty;

    public string OntologyVersionId { get; init; } = string.Empty;
}

internal sealed record RecordApprovalArguments
{
    public string ApprovalId { get; init; } = string.Empty;

    public string OntologyVersionId { get; init; } = string.Empty;

    public string CompatibilityAssessmentId { get; init; } = string.Empty;

    public string Decision { get; init; } = string.Empty;
}

internal sealed record ActivateOntologyVersionArguments
{
    public string OntologyVersionId { get; init; } = string.Empty;

    public string PayloadDigest { get; init; } = string.Empty;

    public string ApprovalId { get; init; } = string.Empty;

    public string ActivationEvidenceId { get; init; } = string.Empty;

    public string? ExpectedActiveOntologyVersionId { get; init; }

    public string? ExpectedActiveOntologyDigest { get; init; }
}
