namespace WorldSimulator.Core.World;

public sealed class SettlementEconomyProfile
{
    public required string SettlementId { get; init; }

    public required decimal AgriculturePotential { get; init; }
    public required decimal FishingMultiplier { get; init; }
    public required decimal HuntingMultiplier { get; init; }
    public required decimal MainlandSupplyMultiplier { get; init; }
    public required decimal ResourceGatheringMultiplier { get; init; }
    public required decimal GoodsCraftingMultiplier { get; init; }

    public required bool IsPort { get; init; }
    public required bool IsFortress { get; init; }
    public required bool IsCapital { get; init; }
}
