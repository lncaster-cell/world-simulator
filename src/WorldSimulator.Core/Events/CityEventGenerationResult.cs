namespace WorldSimulator.Core.Events;

public enum CityEventGenerationReason
{
    Generated,
    ChanceMissed,
    MaxActiveEventsReached,
    NoAvailableEventTypes
}

public sealed class CityEventGenerationResult
{
    private CityEventGenerationResult(bool wasGenerated, CityEvent? cityEvent, CityEventGenerationReason reason)
    {
        WasGenerated = wasGenerated;
        Event = cityEvent;
        Reason = reason;
    }

    public bool WasGenerated { get; }

    public CityEvent? Event { get; }

    public CityEventGenerationReason Reason { get; }

    public static CityEventGenerationResult Generated(CityEvent cityEvent) =>
        new(true, cityEvent ?? throw new ArgumentNullException(nameof(cityEvent)), CityEventGenerationReason.Generated);

    public static CityEventGenerationResult NotGenerated(CityEventGenerationReason reason) =>
        new(false, null, reason);
}
