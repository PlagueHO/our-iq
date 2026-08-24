namespace OurIQ.Domain;

public sealed record OntologyValidationFinding(
    string Code,
    OntologyRuleLevel Level,
    string Message,
    bool BlocksActivation);

public static class OntologyRuleEvaluation
{
    public static OntologyValidationFinding? Evaluate(OntologyRule rule, bool violated)
    {
        ArgumentNullException.ThrowIfNull(rule);

        return violated
            ? new OntologyValidationFinding(
                rule.Code,
                rule.Level,
                rule.Rationale,
                rule.Level is OntologyRuleLevel.Required)
            : null;
    }
}
