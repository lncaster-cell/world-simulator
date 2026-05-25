namespace WorldSimulator.Core.Resources;

/// <summary>
/// Result of one-day food flow calculation.
/// PopulationConsumption is a positive number representing consumed food amount.
/// TotalDelta is the net change applied to food stock (can be positive or negative).
/// </summary>
public sealed class DailyFoodFlowResult
{
    public required decimal StartingFood { get; init; }

    public required decimal PopulationConsumption { get; init; }

    public required decimal FishingIncome { get; init; }

    public required decimal HuntingIncome { get; init; }

    public required decimal MainlandSupplyIncome { get; init; }

    public required decimal EventDelta { get; init; }

    public required decimal TotalDelta { get; init; }

    public required decimal EndingFood { get; init; }
}
