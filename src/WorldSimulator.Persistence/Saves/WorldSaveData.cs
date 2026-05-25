namespace WorldSimulator.Persistence.Saves;

public sealed class WorldSaveData
{
    public int Version { get; set; } = 1;

    public DateTime SavedAtUtc { get; set; }

    public ClockSaveData Clock { get; set; } = new();

    public CitySaveData City { get; set; } = new();
}

public sealed class ClockSaveData
{
    public int Day { get; set; }

    public int Hour { get; set; }

    public bool IsRunning { get; set; }

    public TimeSpan AccumulatedRealTime { get; set; }

    public TimeSpan RealTimePerGameHour { get; set; }
}

public sealed class CitySaveData
{
    public string Id { get; set; } = string.Empty;

    public string Name { get; set; } = string.Empty;

    public int Population { get; set; }

    public decimal Food { get; set; }

    public decimal Wealth { get; set; }

    public int Mood { get; set; }

    public int Security { get; set; }

    public int Crime { get; set; }

    public decimal Resources { get; set; }

    public decimal Goods { get; set; }

    public string CityState { get; set; } = string.Empty;
}
