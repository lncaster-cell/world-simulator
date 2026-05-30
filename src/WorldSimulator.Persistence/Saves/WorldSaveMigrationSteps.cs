using WorldSimulator.Core.Cities;

namespace WorldSimulator.Persistence.Saves;

internal sealed class LegacyCityToWorldSaveMigrationStep : IWorldSaveMigrationStep
{
    public int FromVersion => 1;
    public int ToVersion => 4;

    public void Apply(WorldSaveData saveData, string filePath)
    {
        if (saveData.World is not null)
        {
            return;
        }

        if (saveData.City is null)
        {
            throw new InvalidDataException($"Save file '{filePath}' version {saveData.Version} is missing world data.");
        }

        saveData.World = WorldSaveMigrationDefaults.CreateWorldDataFromLegacyCity(saveData.City);
    }
}

internal sealed class RestoreWorldCollectionsSaveMigrationStep : IWorldSaveMigrationStep
{
    public int FromVersion => 1;
    public int ToVersion => 4;

    public void Apply(WorldSaveData saveData, string filePath)
    {
        var worldData = GetWorldData(saveData, filePath);
        EnsureCollections(worldData);
        RestoreMissingLegacyWorldParts(worldData);
    }

    private static void EnsureCollections(SimulationWorldSaveData worldData)
    {
        worldData.Cities ??= [];
        worldData.CitiesById ??= new Dictionary<string, CitySaveData>(StringComparer.Ordinal);
        worldData.Regions ??= [];
        worldData.SettlementMapLocations ??= [];
        worldData.SettlementEconomyProfiles ??= [];
        worldData.SettlementSectorCapacityProfiles ??= [];
        worldData.Caravans ??= [];
        worldData.TradeRoutes ??= [];
        worldData.TradeShipments ??= [];
    }

    private static void RestoreMissingLegacyWorldParts(SimulationWorldSaveData worldData)
    {
        var defaultWorldData = WorldSaveMigrationDefaults.CreateDefaultWorldData();

        if (worldData.Cities.Count == 0 && worldData.CitiesById.Count > 0)
        {
            worldData.Cities = worldData.CitiesById.Values.ToList();
        }

        if (worldData.CitiesById.Count == 0 && worldData.Cities.Count > 0)
        {
            worldData.CitiesById = worldData.Cities.ToDictionary(city => city.Id, city => city, StringComparer.Ordinal);
        }

        if (worldData.Regions.Count == 0) worldData.Regions = defaultWorldData.Regions;
        if (worldData.SettlementMapLocations.Count == 0) worldData.SettlementMapLocations = defaultWorldData.SettlementMapLocations;
        if (worldData.SettlementEconomyProfiles.Count == 0) worldData.SettlementEconomyProfiles = defaultWorldData.SettlementEconomyProfiles;
        if (worldData.SettlementSectorCapacityProfiles.Count == 0) worldData.SettlementSectorCapacityProfiles = defaultWorldData.SettlementSectorCapacityProfiles;
        if (worldData.Caravans.Count == 0) worldData.Caravans = defaultWorldData.Caravans;
        if (worldData.TradeRoutes.Count == 0) worldData.TradeRoutes = defaultWorldData.TradeRoutes;
    }

    private static SimulationWorldSaveData GetWorldData(WorldSaveData saveData, string filePath)
    {
        return saveData.World ?? throw new InvalidDataException($"Save file '{filePath}' version {saveData.Version} is missing world data.");
    }
}

internal sealed class RestoreCityStateSaveMigrationStep : IWorldSaveMigrationStep
{
    public int FromVersion => 1;
    public int ToVersion => 4;

    public void Apply(WorldSaveData saveData, string filePath)
    {
        var worldData = GetWorldData(saveData, filePath);
        RestoreMissingCityInfrastructure(worldData);
        RestoreMissingCityDemographics(worldData);
    }

    private static void RestoreMissingCityInfrastructure(SimulationWorldSaveData worldData)
    {
        foreach (var city in WorldSaveMigrationDefaults.GetAllCitySaveData(worldData))
        {
            city.Infrastructure ??= new CityInfrastructureSaveData();
        }
    }

    private static void RestoreMissingCityDemographics(SimulationWorldSaveData worldData)
    {
        foreach (var city in WorldSaveMigrationDefaults.GetAllCitySaveData(worldData))
        {
            if (city.Demographics is { RaceGroups.Count: > 0 })
            {
                continue;
            }

            var demographics = CityPopulationDemographics.CreateDefaultHuman(city.Population);
            city.Demographics = new CityPopulationDemographicsSaveData
            {
                RaceGroups = demographics.RaceGroups.Select(group => new RacePopulationGroupSaveData
                {
                    RaceId = group.RaceId,
                    Children = group.Children,
                    AdultMen = group.AdultMen,
                    AdultWomen = group.AdultWomen,
                    Elderly = group.Elderly
                }).ToList()
            };
        }
    }

    private static SimulationWorldSaveData GetWorldData(WorldSaveData saveData, string filePath)
    {
        return saveData.World ?? throw new InvalidDataException($"Save file '{filePath}' version {saveData.Version} is missing world data.");
    }
}

internal sealed class RestoreSectorCapacityProfilesSaveMigrationStep : IWorldSaveMigrationStep
{
    public int FromVersion => 1;
    public int ToVersion => 4;

    public void Apply(WorldSaveData saveData, string filePath)
    {
        var worldData = saveData.World ?? throw new InvalidDataException($"Save file '{filePath}' version {saveData.Version} is missing world data.");
        if (worldData.SettlementSectorCapacityProfiles.Count > 0)
        {
            return;
        }

        worldData.SettlementSectorCapacityProfiles = WorldSaveMigrationDefaults.CreateDefaultWorldData().SettlementSectorCapacityProfiles;
    }
}

internal sealed class RestoreRouteFieldsSaveMigrationStep : IWorldSaveMigrationStep
{
    public int FromVersion => 1;
    public int ToVersion => 4;

    public void Apply(WorldSaveData saveData, string filePath)
    {
        var worldData = saveData.World ?? throw new InvalidDataException($"Save file '{filePath}' version {saveData.Version} is missing world data.");
        var defaultRoutesById = WorldSaveDefaults.GetDefaultRoutes()
            .GroupBy(route => route.Id, StringComparer.Ordinal)
            .ToDictionary(group => group.Key, group => WorldSaveMapper.ToSaveData(group.First()), StringComparer.Ordinal);

        foreach (var route in worldData.TradeRoutes)
        {
            if (route.DistanceDays is null or <= 0m)
            {
                route.DistanceDays = WorldSaveDefaults.GetDefaultDistanceDays(route.Id);
            }

            if (route.DifficultyMultiplier <= 0m)
            {
                route.DifficultyMultiplier = 1m;
            }

            if (defaultRoutesById.TryGetValue(route.Id, out var defaultRoute))
            {
                if (route.Distance <= 0m) route.Distance = defaultRoute.Distance;
                if (route.TravelDays <= 0) route.TravelDays = defaultRoute.TravelDays;
                if (string.IsNullOrWhiteSpace(route.FromSettlementId)) route.FromSettlementId = defaultRoute.FromSettlementId;
                if (string.IsNullOrWhiteSpace(route.ToSettlementId)) route.ToSettlementId = defaultRoute.ToSettlementId;
                if (string.IsNullOrWhiteSpace(route.Type)) route.Type = defaultRoute.Type;
                if (route.Points is null || route.Points.Count == 0) route.Points = defaultRoute.Points;
            }

            route.Points ??= [];
        }
    }
}

internal sealed class RestoreSelectedWorldItemsSaveMigrationStep : IWorldSaveMigrationStep
{
    public int FromVersion => 1;
    public int ToVersion => 4;

    public void Apply(WorldSaveData saveData, string filePath)
    {
        var worldData = saveData.World ?? throw new InvalidDataException($"Save file '{filePath}' version {saveData.Version} is missing world data.");
        if (string.IsNullOrWhiteSpace(worldData.SelectedCityId) && worldData.Cities.Count > 0)
        {
            worldData.SelectedCityId = worldData.Cities[0].Id;
        }

        if (string.IsNullOrWhiteSpace(worldData.SelectedRegionId) && worldData.Regions.Count > 0)
        {
            worldData.SelectedRegionId = worldData.Regions[0].Id;
        }
    }
}
