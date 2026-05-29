using WorldSimulator.Core.Cities;
using WorldSimulator.Core.World;

namespace WorldSimulator.Persistence.Saves;

internal static partial class WorldSaveMapper
{
    public static SimulationWorldSaveData ToSaveData(SimulationWorld world) => new()
    {
        Cities = world.Cities.Select(ToSaveData).ToList(),
        CitiesById = world.Cities.ToDictionary(city => city.Id, ToSaveData, StringComparer.Ordinal),
        Regions = world.Regions.Select(ToSaveData).ToList(),
        SettlementMapLocations = world.SettlementMapLocations.Select(ToSaveData).ToList(),
        SettlementEconomyProfiles = world.SettlementEconomyProfiles.Select(ToSaveData).ToList(),
        SettlementSectorCapacityProfiles = world.SettlementSectorCapacityProfiles.Select(ToSaveData).ToList(),
        Caravans = world.Caravans.Select(ToSaveData).ToList(),
        TradeRoutes = world.TradeRoutes.Select(ToSaveData).ToList(),
        TradeShipments = world.TradeShipments.Select(ToSaveData).ToList(),
        SelectedCityId = world.SelectedCityId,
        SelectedRegionId = world.SelectedRegionId
    };

    public static SimulationWorld ToCoreWorld(SimulationWorldSaveData worldData, string filePath)
    {
        var loadedCities = ResolveCities(worldData, filePath);
        var regions = (worldData.Regions ?? []).Select(ToCoreRegion).ToList();
        var settlementMapLocations = (worldData.SettlementMapLocations ?? []).Select(ToCoreSettlementMapLocation).ToList();
        var settlementEconomyProfiles = (worldData.SettlementEconomyProfiles ?? []).Select(ToCoreSettlementEconomyProfile).ToList();
        var settlementSectorCapacityProfiles = (worldData.SettlementSectorCapacityProfiles ?? []).Select(ToCoreSettlementSectorCapacityProfile).ToList();
        var caravans = (worldData.Caravans ?? []).Select(ToCoreCaravan).ToList();
        var tradeRoutes = (worldData.TradeRoutes ?? []).Select(ToCoreTradeRoute).ToList();

        if (loadedCities.Count == 0) throw new InvalidDataException($"Save file '{filePath}' has no cities.");
        if (regions.Count == 0) throw new InvalidDataException($"Save file '{filePath}' has no regions.");

        var selectedCityId = loadedCities.Any(c => c.Id == worldData.SelectedCityId)
            ? worldData.SelectedCityId
            : loadedCities.First().Id;
        var selectedRegionId = regions.Any(r => r.Id == worldData.SelectedRegionId)
            ? worldData.SelectedRegionId
            : regions.First().Id;

        return new SimulationWorld
        {
            Cities = loadedCities,
            Regions = regions,
            SettlementMapLocations = settlementMapLocations,
            SettlementEconomyProfiles = settlementEconomyProfiles,
            SettlementSectorCapacityProfiles = settlementSectorCapacityProfiles,
            Caravans = caravans,
            TradeRoutes = tradeRoutes,
            TradeShipments = (worldData.TradeShipments ?? []).Select(ToCoreTradeShipment).ToList(),
            SelectedCityId = selectedCityId,
            SelectedRegionId = selectedRegionId
        };
    }

    private static List<City> ResolveCities(SimulationWorldSaveData worldData, string filePath)
    {
        if (worldData.CitiesById is { Count: > 0 })
        {
            return worldData.CitiesById.Values.Select(x => ToCoreCity(x, filePath)).ToList();
        }

        return (worldData.Cities ?? []).Select(x => ToCoreCity(x, filePath)).ToList();
    }
}
