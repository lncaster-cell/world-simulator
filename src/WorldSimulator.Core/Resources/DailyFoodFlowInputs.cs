namespace WorldSimulator.Core.Resources;

/// <summary>
/// Daily external/input food flow components for city food balance calculation.
/// All values are expressed in food storage units and can be positive or negative.
/// </summary>
public sealed class DailyFoodFlowInputs
{
    public static DailyFoodFlowInputs GothaPlaceholder { get; } = new()
    {
        AgricultureIncome = 0m,
        FishingIncome = 20m,
        HuntingIncome = 8m,
        MainlandSupplyIncome = 40m,
        EventDelta = 0m
    };

    public decimal AgricultureIncome { get; init; }

    public decimal FishingIncome { get; init; }

    public decimal HuntingIncome { get; init; }

    public decimal MainlandSupplyIncome { get; init; }

    public decimal EventDelta { get; init; }
}
