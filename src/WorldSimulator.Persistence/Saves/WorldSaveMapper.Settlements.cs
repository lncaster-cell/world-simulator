using WorldSimulator.Core.Workforce;
using WorldSimulator.Core.World;

namespace WorldSimulator.Persistence.Saves;

internal static partial class WorldSaveMapper
{
    public static RegionSaveData ToSaveData(Region region) => new()
    {
        Id = region.Id,
        DisplayName = region.DisplayName,
        MapAssetId = region.MapAssetId
    };

    public static SettlementMapLocationSaveData ToSaveData(SettlementMapLocation location) => new()
    {
        SettlementId = location.SettlementId,
        RegionId = location.RegionId,
        X = location.X,
        Y = location.Y
    };

    public static SettlementEconomyProfileSaveData ToSaveData(SettlementEconomyProfile profile) => new()
    {
        SettlementId = profile.SettlementId,
        AgriculturePotential = profile.AgriculturePotential,
        FishingMultiplier = profile.FishingMultiplier,
        HuntingMultiplier = profile.HuntingMultiplier,
        MainlandSupplyMultiplier = profile.MainlandSupplyMultiplier,
        ResourceGatheringMultiplier = profile.ResourceGatheringMultiplier,
        GoodsCraftingMultiplier = profile.GoodsCraftingMultiplier,
        IsPort = profile.IsPort,
        IsFortress = profile.IsFortress,
        IsCapital = profile.IsCapital
    };

    public static SettlementSectorCapacityProfileSaveData ToSaveData(SettlementSectorCapacityProfile profile) => new()
    {
        SettlementId = profile.SettlementId,
        AgricultureCapacity = profile.AgricultureCapacity,
        FishingCapacity = profile.FishingCapacity,
        HuntingCapacity = profile.HuntingCapacity,
        ResourceGatheringCapacity = profile.ResourceGatheringCapacity,
        CraftingCapacity = profile.CraftingCapacity,
        TradeCapacity = profile.TradeCapacity,
        GuardCapacity = profile.GuardCapacity,
        MaintenanceCapacity = profile.MaintenanceCapacity
    };

    public static Region ToCoreRegion(RegionSaveData regionData) => new()
    {
        Id = regionData.Id,
        DisplayName = regionData.DisplayName,
        MapAssetId = regionData.MapAssetId
    };

    public static SettlementMapLocation ToCoreSettlementMapLocation(SettlementMapLocationSaveData locationData) => new()
    {
        SettlementId = locationData.SettlementId,
        RegionId = locationData.RegionId,
        X = locationData.X,
        Y = locationData.Y
    };

    public static SettlementEconomyProfile ToCoreSettlementEconomyProfile(SettlementEconomyProfileSaveData profileData) => new()
    {
        SettlementId = profileData.SettlementId,
        AgriculturePotential = profileData.AgriculturePotential,
        FishingMultiplier = profileData.FishingMultiplier,
        HuntingMultiplier = profileData.HuntingMultiplier,
        MainlandSupplyMultiplier = profileData.MainlandSupplyMultiplier,
        ResourceGatheringMultiplier = profileData.ResourceGatheringMultiplier,
        GoodsCraftingMultiplier = profileData.GoodsCraftingMultiplier,
        IsPort = profileData.IsPort,
        IsFortress = profileData.IsFortress,
        IsCapital = profileData.IsCapital
    };

    public static SettlementSectorCapacityProfile ToCoreSettlementSectorCapacityProfile(SettlementSectorCapacityProfileSaveData profileData) => new()
    {
        SettlementId = profileData.SettlementId,
        AgricultureCapacity = profileData.AgricultureCapacity,
        FishingCapacity = profileData.FishingCapacity,
        HuntingCapacity = profileData.HuntingCapacity,
        ResourceGatheringCapacity = profileData.ResourceGatheringCapacity,
        CraftingCapacity = profileData.CraftingCapacity,
        TradeCapacity = profileData.TradeCapacity,
        GuardCapacity = profileData.GuardCapacity,
        MaintenanceCapacity = profileData.MaintenanceCapacity
    };
}