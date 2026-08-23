using Azure.Monitor.OpenTelemetry.AspNetCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using OpenTelemetry.Logs;

namespace OurIQ.Observability;

public static class TelemetryServiceCollectionExtensions
{
    public static IServiceCollection AddOurIQTelemetry(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddSingleton<ITelemetryContextAccessor, TelemetryContextAccessor>();
        var telemetry = services.AddOpenTelemetry()
            .WithTracing(tracing => tracing.AddSource(TelemetryConstants.ActivitySourceName))
            .WithMetrics(metrics => metrics.AddMeter(TelemetryConstants.MeterName));
        services.Configure<OpenTelemetryLoggerOptions>(options => options.IncludeScopes = true);

        if (!string.IsNullOrWhiteSpace(configuration["APPLICATIONINSIGHTS_CONNECTION_STRING"]))
        {
            telemetry.UseAzureMonitor();
        }

        return services;
    }
}
