namespace WorldSimulator.Core.Events;

public sealed class CityEventEffectsResult
{
    public decimal FoodDelta { get; init; }

    public int MoodDelta { get; init; }

    public int SecurityDelta { get; init; }

    public int CrimeDelta { get; init; }

    public decimal WealthDelta { get; init; }

    public decimal ResourcesDelta { get; init; }

    public decimal MainlandSupplyDelta { get; init; }

    public bool HasAnyEffect =>
        FoodDelta != 0m ||
        MoodDelta != 0 ||
        SecurityDelta != 0 ||
        CrimeDelta != 0 ||
        WealthDelta != 0m ||
        ResourcesDelta != 0m ||
        MainlandSupplyDelta != 0m;
}
