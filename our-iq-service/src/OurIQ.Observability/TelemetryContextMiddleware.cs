using System.Diagnostics;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

namespace OurIQ.Observability;

public sealed class TelemetryContextMiddleware(
    RequestDelegate next,
    ITelemetryContextAccessor contextAccessor,
    ILogger<TelemetryContextMiddleware> logger)
{
    public async Task InvokeAsync(HttpContext httpContext)
    {
        var activity = TelemetryInstrumentation.ActivitySource.StartActivity(
            $"{httpContext.Request.Method} {httpContext.Request.Path}",
            ActivityKind.Internal)
            ?? new Activity($"{httpContext.Request.Method} {httpContext.Request.Path}").Start()!;

        var telemetryContext = TelemetryContext.Create(activity, httpContext.Request.Headers);
        telemetryContext.Enrich(activity);
        contextAccessor.Current = telemetryContext;
        telemetryContext.ApplyTo(httpContext.Response);

        var startTimestamp = Stopwatch.GetTimestamp();
        var failed = false;
        using var scope = logger.BeginScope(telemetryContext.ToLogState());

        try
        {
            await next(httpContext);
            activity.SetTag("http.response.status_code", httpContext.Response.StatusCode);
        }
        catch (Exception exception)
        {
            failed = true;
            activity.SetStatus(ActivityStatusCode.Error, exception.GetType().Name);
            activity.SetTag("error.type", exception.GetType().FullName);
            logger.LogError(
                exception,
                "Request failed at the {Stage} stage.",
                "host");
            throw;
        }
        finally
        {
            var elapsedMilliseconds = Stopwatch.GetElapsedTime(startTimestamp).TotalMilliseconds;
            var outcome = httpContext.Response.StatusCode >= 500 ? "failure" : "completed";
            TelemetryInstrumentation.Requests.Add(1, new KeyValuePair<string, object?>("outcome", outcome));
            TelemetryInstrumentation.RequestDuration.Record(
                elapsedMilliseconds,
                new KeyValuePair<string, object?>("outcome", outcome));
            activity.SetTag("ouriq.outcome", outcome);

            if (!failed)
            {
                logger.LogInformation(
                    "Request completed at the {Stage} stage with {Outcome} outcome in {DurationMilliseconds} ms.",
                    "host",
                    outcome,
                    elapsedMilliseconds);
            }

            activity.Stop();
            contextAccessor.Current = null;
        }
    }
}
