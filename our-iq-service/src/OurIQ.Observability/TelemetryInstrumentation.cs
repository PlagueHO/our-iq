using System.Diagnostics;
using System.Diagnostics.Metrics;

namespace OurIQ.Observability;

public static class TelemetryInstrumentation
{
    public static readonly ActivitySource ActivitySource = new(TelemetryConstants.ActivitySourceName);
    public static readonly Meter Meter = new(TelemetryConstants.MeterName);
    public static readonly Counter<long> Requests = Meter.CreateCounter<long>("ouriq.requests");
    public static readonly Histogram<double> RequestDuration = Meter.CreateHistogram<double>(
        "ouriq.request.duration",
        unit: "ms");
}
