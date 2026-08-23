using System.Diagnostics;
using OurIQ.Observability;

namespace OurIQ.UnitTests;

[TestClass]
public sealed class TelemetryContextTests
{
    [TestMethod]
    public void CreatePreservesSafeIdentifiersAndTraceId()
    {
        using var activity = new Activity("test").SetIdFormat(ActivityIdFormat.W3C).Start();
        var headers = new Dictionary<string, string?>
        {
            [TelemetryConstants.ExecutionIdHeader] = "execution-1",
            [TelemetryConstants.CorrelationIdHeader] = "correlation-1",
            [TelemetryConstants.KnowledgeSpaceIdHeader] = "space-1",
            [TelemetryConstants.OperationIdHeader] = "operation-1"
        };

        var context = TelemetryContext.Create(activity!, headers);

        Assert.AreEqual("execution-1", context.ExecutionId);
        Assert.AreEqual("correlation-1", context.CorrelationId);
        Assert.AreEqual("space-1", context.KnowledgeSpaceId);
        Assert.AreEqual("operation-1", context.OperationId);
        Assert.AreEqual(activity!.TraceId.ToHexString(), context.TraceId);
    }

    [TestMethod]
    public void CreateGeneratesMissingIdentifiersAndDropsUnsafeValues()
    {
        using var activity = new Activity("test").SetIdFormat(ActivityIdFormat.W3C).Start();
        var headers = new Dictionary<string, string?>
        {
            [TelemetryConstants.ExecutionIdHeader] = "execution with spaces",
            [TelemetryConstants.CorrelationIdHeader] = "correlation-1",
            [TelemetryConstants.KnowledgeSpaceIdHeader] = "space\nwith-control-character"
        };

        var context = TelemetryContext.Create(activity!, headers);

        Assert.AreNotEqual("execution with spaces", context.ExecutionId);
        Assert.AreEqual("correlation-1", context.CorrelationId);
        Assert.IsNull(context.KnowledgeSpaceId);
        Assert.IsNull(context.OperationId);
        Assert.AreEqual(32, context.ExecutionId.Length);
    }

    [TestMethod]
    public void LogStateContainsOnlyAllowListedCorrelationFields()
    {
        var context = new TelemetryContext("execution-1", "correlation-1", "space-1", "operation-1", "trace-1");

        var logState = context.ToLogState();

        CollectionAssert.AreEquivalent(
            new[]
            {
                TelemetryConstants.ExecutionIdTag,
                TelemetryConstants.CorrelationIdTag,
                TelemetryConstants.KnowledgeSpaceIdTag,
                TelemetryConstants.OperationIdTag,
                "trace_id"
            },
            logState.Keys.ToArray());
        Assert.IsFalse(logState.Keys.Any(key => key.Contains("prompt", StringComparison.OrdinalIgnoreCase)));
        Assert.IsFalse(logState.Keys.Any(key => key.Contains("secret", StringComparison.OrdinalIgnoreCase)));
        Assert.IsFalse(logState.Keys.Any(key => key.Contains("content", StringComparison.OrdinalIgnoreCase)));
    }

    [TestMethod]
    public void ApplyToHttpRequestPropagatesOnlyCorrelationIdentifiers()
    {
        var context = new TelemetryContext("execution-1", "correlation-1", "space-1", "operation-1", "trace-1");
        using var request = new HttpRequestMessage();

        context.ApplyTo(request);

        Assert.AreEqual("execution-1", request.Headers.GetValues(TelemetryConstants.ExecutionIdHeader).Single());
        Assert.AreEqual("correlation-1", request.Headers.GetValues(TelemetryConstants.CorrelationIdHeader).Single());
        Assert.AreEqual("space-1", request.Headers.GetValues(TelemetryConstants.KnowledgeSpaceIdHeader).Single());
        Assert.AreEqual("operation-1", request.Headers.GetValues(TelemetryConstants.OperationIdHeader).Single());
        Assert.IsFalse(request.Headers.Any(header => header.Key.Contains("prompt", StringComparison.OrdinalIgnoreCase)));
    }
}
