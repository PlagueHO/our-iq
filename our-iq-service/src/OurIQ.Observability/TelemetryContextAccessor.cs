namespace OurIQ.Observability;

public interface ITelemetryContextAccessor
{
    TelemetryContext? Current { get; set; }
}

internal sealed class TelemetryContextAccessor : ITelemetryContextAccessor
{
    private static readonly AsyncLocal<TelemetryContext?> CurrentContext = new();

    public TelemetryContext? Current
    {
        get => CurrentContext.Value;
        set => CurrentContext.Value = value;
    }
}
