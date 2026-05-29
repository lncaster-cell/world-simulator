using WorldSimulator.Core.Cities;
using WorldSimulator.Core.Workforce;

namespace WorldSimulator.Core.World;

public sealed record RiviaSettlementPreset(
    string Id,
    string DisplayName,
    string RouteNodeId,
    decimal X,
    decimal Y,
    CityStartPreset CityStart,
    SettlementEconomyPreset Economy,
    SettlementSectorCapacityPreset SectorCapacity,
    bool IsPort,
    bool IsFortress,
    bool IsCapital)
{
    public SettlementMapLocation CreateMapLocation(string regionId) => new()
    {
        SettlementId = Id,
        RegionId = regionId,
        X = X,
        Y = Y
    };

    public SettlementEconomyProfile CreateEconomyProfile() => new()
    {
        SettlementId = Id,
        AgriculturePotential = Economy.AgriculturePotential,
        FishingMultiplier = Economy.FishingMultiplier,
        HuntingMultiplier = Economy.HuntingMultiplier,
        MainlandSupplyMultiplier = Economy.MainlandSupplyMultiplier,
        ResourceGatheringMultiplier = Economy.ResourceGatheringMultiplier,
        GoodsCraftingMultiplier = Economy.GoodsCraftingMultiplier,
        IsPort = IsPort,
        IsFortress = IsFortress,
        IsCapital = IsCapital
    };

    public SettlementSectorCapacityProfile CreateSectorCapacityProfile() => new()
    {
        SettlementId = Id,
        AgricultureCapacity = SectorCapacity.AgricultureCapacity,
        FishingCapacity = SectorCapacity.FishingCapacity,
        HuntingCapacity = SectorCapacity.HuntingCapacity,
        ResourceGatheringCapacity = SectorCapacity.ResourceGatheringCapacity,
        CraftingCapacity = SectorCapacity.CraftingCapacity,
        TradeCapacity = SectorCapacity.TradeCapacity,
        GuardCapacity = SectorCapacity.GuardCapacity,
        MaintenanceCapacity = SectorCapacity.MaintenanceCapacity
    };
}

public sealed record CityStartPreset(
    int Population,
    decimal Food,
    decimal Wealth,
    int Mood,
    int Security,
    int Crime,
    decimal Resources,
    decimal Goods,
    CityState CityState);

public sealed record SettlementEconomyPreset(
    decimal AgriculturePotential,
    decimal FishingMultiplier,
    decimal HuntingMultiplier,
    decimal MainlandSupplyMultiplier,
    decimal ResourceGatheringMultiplier,
    decimal GoodsCraftingMultiplier);

public sealed record SettlementSectorCapacityPreset(
    int AgricultureCapacity,
    int FishingCapacity,
    int HuntingCapacity,
    int ResourceGatheringCapacity,
    int CraftingCapacity,
    int TradeCapacity,
    int GuardCapacity,
    int MaintenanceCapacity);
