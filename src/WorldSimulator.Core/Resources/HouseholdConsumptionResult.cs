namespace WorldSimulator.Core.Resources;

public sealed class HouseholdConsumptionResult
{
    public required decimal GoodsShortage { get; init; }
    public required decimal ResourcesShortage { get; init; }
    public required bool HasAnyShortage { get; init; }
}
