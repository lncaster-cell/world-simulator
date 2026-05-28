using WorldSimulator.Core.World;

namespace WorldSimulator.Persistence.Saves;

internal sealed class WorldSaveMigrationService
{
    public const int CurrentSaveVersion = 3;

    public WorldSaveData Migrate(WorldSaveData saveData, string filePath)
    {
        ArgumentNullException.ThrowIfNull(saveData);

        if (saveData.Version <= 0 || saveData.Version > CurrentSaveVersion)
            throw new InvalidDataException($"Save file '{filePath}' has unsupported version '{saveData.Version}'. Expected '{CurrentSaveVersion}' or earlier.");

        var originalVersion = saveData.Version;

        saveData.Clock ??= new ClockSaveData();
        saveData.Events ??= new EventSaveData();

        if (saveData.World is null)
        {
            if (saveData.City is null)
                throw new InvalidDataException($"Save file '{filePath}' version {saveData.Version} is missing world data.");

            saveData.World = CreateWorldDataFromLegacyCity(saveData.City);
        }

        EnsureCollections(saveData.World);
        if (originalVersion < CurrentSaveVersion)
        {
            RestoreMissingLegacyWorldParts(saveData.World);
        }

        RestoreMissingRouteFields(saveData.World);
        RestoreMissingSelections(saveData.World);

        saveData.Version = CurrentSaveVersion;
        return saveData;
    }

    private static SimulationWorldSaveData CreateWorldDataFromLegacyCity(CitySaveData legacyCity)
    {
        var worldData = WorldSaveMapper.ToSaveData(WorldPresets.CreateDefaultWorld());
        var existingIndex = worldData.Cities.FindIndex(city => string.Equals(city.Id, legacyCity.Id, StringComparison.Ordinal));
        if (existingIndex >= 0)
        {
            worldData.Cities[existingIndex] = legacyCity;
        }
        else
        {
            worldData.Cities.Insert(0, legacyCity);
        }

        worldData.CitiesById = worldData.Cities.ToDictionary(city => city.Id, city => city, StringComparer.Ordinal);
        worldData.SelectedCityId = legacyCity.Id;
        return worldData;
    }

    private static void EnsureCollections(SimulationWorldSaveData worldData)
    {
        worldData.Cities ??= [];
        worldData.CitiesById ??= new Dictionary<string, CitySaveData>(StringComparer.Ordinal);
        worldData.Regions ??= [];
        worldData.SettlementMapLocations ??= [];
        worldData.SettlementEconomyProfiles ??= [];
        worldData.Caravans ??= [];
        worldData.TradeRoutes ??= [];
        worldData.TradeShipments ??= [];
    }

    private static void RestoreMissingLegacyWorldParts(SimulationWorldSaveData worldData)
    {
        var defaultWorldData = WorldSaveMapper.ToSaveData(WorldPresets.CreateDefaultWorld());

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
        if (worldData.Caravans.Count == 0) worldData.Caravans = defaultWorldData.Caravans;
        if (worldData.TradeRoutes.Count == 0) worldData.TradeRoutes = defaultWorldData.TradeRoutes;
    }

    private static void RestoreMissingRouteFields(SimulationWorldSaveData worldData)
    {
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

    private static void RestoreMissingSelections(SimulationWorldSaveData worldData)
    {
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
