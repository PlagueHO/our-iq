using System.Collections.Concurrent;
using System.Diagnostics;
using System.Net;
using Microsoft.AspNetCore.Mvc.Testing;
using OurIQ.Observability;

namespace OurIQ.ComponentTests;

[TestClass]
[DoNotParallelize]
public sealed class TelemetryComponentTests
{
    private static readonly ConcurrentBag<Activity> Activities = [];
    private static ActivityListener _activityListener = null!;

    [ClassInitialize]
    public static void ClassInitialize(TestContext _)
    {
        _activityListener = new ActivityListener
        {
            ShouldListenTo = source => source.Name == TelemetryConstants.ActivitySourceName,
            Sample = (ref ActivityCreationOptions<ActivityContext> _) => ActivitySamplingResult.AllData,
            ActivityStarted = activity => Activities.Add(activity)
        };
        ActivitySource.AddActivityListener(_activityListener);
    }

    [ClassCleanup]
    public static void ClassCleanup()
    {
        _activityListener.Dispose();
    }

    [TestMethod]
    public async Task PublicHostEmitsTraceAndPropagatesCorrelationIdentifiers()
    {
        using var factory = new WebApplicationFactory<McpServerProgram>();
        using var client = factory.CreateClient();
        using var request = new HttpRequestMessage(HttpMethod.Get, "/health");
        request.Headers.Add(TelemetryConstants.ExecutionIdHeader, "execution-public");
        request.Headers.Add(TelemetryConstants.CorrelationIdHeader, "correlation-public");
        request.Headers.Add(TelemetryConstants.KnowledgeSpaceIdHeader, "space-public");
        request.Headers.Add(TelemetryConstants.OperationIdHeader, "operation-public");

        using var response = await client.SendAsync(request);

        Assert.AreEqual(HttpStatusCode.OK, response.StatusCode);
        Assert.AreEqual("execution-public", response.Headers.GetValues(TelemetryConstants.ExecutionIdHeader).Single());
        Assert.AreEqual("correlation-public", response.Headers.GetValues(TelemetryConstants.CorrelationIdHeader).Single());
        Assert.AreEqual("space-public", response.Headers.GetValues(TelemetryConstants.KnowledgeSpaceIdHeader).Single());
        Assert.AreEqual("operation-public", response.Headers.GetValues(TelemetryConstants.OperationIdHeader).Single());

        var activity = Activities
            .Where(candidate => candidate.DisplayName == "GET /health")
            .OrderByDescending(candidate => candidate.StartTimeUtc)
            .FirstOrDefault();

        Assert.IsNotNull(activity);
        Assert.AreEqual("execution-public", activity.GetTagItem(TelemetryConstants.ExecutionIdTag));
        Assert.AreEqual("correlation-public", activity.GetTagItem(TelemetryConstants.CorrelationIdTag));
        Assert.AreEqual("space-public", activity.GetTagItem(TelemetryConstants.KnowledgeSpaceIdTag));
        Assert.AreEqual("operation-public", activity.GetTagItem(TelemetryConstants.OperationIdTag));
        Assert.AreEqual("completed", activity.GetTagItem("ouriq.outcome"));
    }

    [TestMethod]
    public async Task PrivateHostGeneratesMissingCorrelationIdentifiers()
    {
        using var factory = new WebApplicationFactory<ToolServicesProgram>();
        using var client = factory.CreateClient();

        using var response = await client.GetAsync("/health");

        Assert.AreEqual(HttpStatusCode.OK, response.StatusCode);
        Assert.IsTrue(response.Headers.Contains(TelemetryConstants.ExecutionIdHeader));
        Assert.IsTrue(response.Headers.Contains(TelemetryConstants.CorrelationIdHeader));
        Assert.AreEqual(
            32,
            response.Headers.GetValues(TelemetryConstants.ExecutionIdHeader).Single().Length);
        Assert.AreEqual(
            32,
            response.Headers.GetValues(TelemetryConstants.CorrelationIdHeader).Single().Length);
    }
}
