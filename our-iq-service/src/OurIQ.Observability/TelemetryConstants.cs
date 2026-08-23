namespace OurIQ.Observability;

public static class TelemetryConstants
{
    public const string ActivitySourceName = "OurIQ";
    public const string MeterName = "OurIQ";

    public const string ExecutionIdHeader = "X-OurIQ-Execution-Id";
    public const string CorrelationIdHeader = "X-OurIQ-Correlation-Id";
    public const string KnowledgeSpaceIdHeader = "X-OurIQ-Space-Id";
    public const string OperationIdHeader = "X-OurIQ-Operation-Id";

    public const string ExecutionIdTag = "ouriq.execution_id";
    public const string CorrelationIdTag = "ouriq.correlation_id";
    public const string KnowledgeSpaceIdTag = "ouriq.knowledge_space_id";
    public const string OperationIdTag = "ouriq.operation_id";
}
