namespace WorldSimulator.Core.Workforce;

public sealed class SettlementSectorCapacityProfile
{
    public required string SettlementId { get; init; }
    public int AgricultureCapacity { get; init; }
    public int FishingCapacity { get; init; }
    public int HuntingCapacity { get; init; }
    public int ResourceGatheringCapacity { get; init; }
    public int CraftingCapacity { get; init; }
    public int TradeCapacity { get; init; }
    public int GuardCapacity { get; init; }
    public int MaintenanceCapacity { get; init; }

    public int GetCapacity(WorkforceSector sector) => sector switch
    {
        WorkforceSector.Agriculture => AgricultureCapacity,
        WorkforceSector.Fishing => FishingCapacity,
        WorkforceSector.Hunting => HuntingCapacity,
        WorkforceSector.ResourceGathering => ResourceGatheringCapacity,
        WorkforceSector.Crafting => CraftingCapacity,
        WorkforceSector.Trade => TradeCapacity,
        WorkforceSector.Guards => GuardCapacity,
        WorkforceSector.Maintenance => MaintenanceCapacity,
        WorkforceSector.Idle => int.MaxValue,
        _ => throw new ArgumentOutOfRangeException(nameof(sector), sector, "Unsupported workforce sector.")
    };
}
