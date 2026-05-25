namespace WorldSimulator.Core.Time;

public sealed class SimulationTimeSettings
{
    private static readonly TimeSpan DefaultRealTimePerGameHour = TimeSpan.FromMinutes(5);
    private TimeSpan _realTimePerGameHour = DefaultRealTimePerGameHour;

    public TimeSpan RealTimePerGameHour
    {
        get => _realTimePerGameHour;
        set => _realTimePerGameHour = value <= TimeSpan.Zero
            ? throw new ArgumentOutOfRangeException(nameof(value), "RealTimePerGameHour must be greater than zero.")
            : value;
    }

    public static SimulationTimeSettings Default => new();
}
