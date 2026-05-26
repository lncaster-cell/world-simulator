namespace WorldSimulator.Core.Resources;

public sealed class GoodsCraftingProductionResult
{
    public required decimal NaturalPotential { get; init; }

    public required int RequiredWorkers { get; init; }
    public required int AssignedWorkers { get; init; }
    public required decimal WorkerCoverage { get; init; }

    public required int ExtraWorkers { get; init; }
    public required decimal OverstaffBonus { get; init; }

    public required decimal MoodModifier { get; init; }
    public required decimal SecurityModifier { get; init; }
    public required decimal StateModifier { get; init; }

    public required decimal ResourceCostPerGoods { get; init; }
    public required decimal PotentialGoodsOutput { get; init; }
    public required decimal ResourcesNeeded { get; init; }
    public required decimal ResourcesAvailable { get; init; }
    public required decimal ResourcesConsumed { get; init; }

    public required decimal GoodsProduced { get; init; }
}
