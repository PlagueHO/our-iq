namespace OurIQ.Contracts;

public sealed record ApplicationError(
    string Code,
    string Category,
    string Message,
    string Remediation)
{
    public static ApplicationError IdempotencyConflict { get; } = new(
        "idempotency_key_conflict",
        "conflict",
        "The idempotency key already identifies a different request.",
        "Use the original request or submit a new operation with a new key.");

    public static ApplicationError DependencyUnavailable { get; } = new(
        "dependency_unavailable",
        "availability",
        "A required service could not confirm the operation outcome.",
        "Retry the original request with the same idempotency key.");

    public static ApplicationError Internal { get; } = new(
        "internal_error",
        "internal",
        "The tool could not complete the request.",
        "Report the correlation ID. Retry a mutation only with its original key.");

    public static ApplicationError IdentityMismatch { get; } = new(
        "authorization_denied",
        "authorization",
        "The authenticated identity does not match the request identity.",
        "Use the authenticated initiating user and an authorized Domain Agent.");

    public static ApplicationError NotImplemented { get; } = new(
        "operation_not_implemented",
        "state",
        "This tool is not implemented by the current host.",
        "Use an implemented operation or wait for the required workflow.");
}
