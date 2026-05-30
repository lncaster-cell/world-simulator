using WorldSimulator.Core.World;

namespace WorldSimulator.Persistence.Saves;

internal static class WorldSaveMigrationDefaults
{
    public static SimulationWorldSaveData CreateDefaultWorldData()
    {
        return WorldSaveMapper.ToSaveData(WorldPresets.CreateDefaultWorld());
    }

    public static SimulationWorldSaveData CreateWorldDataFromLegacyCity(CitySaveData legacyCity)
    {
        var worldData = CreateDefaultWorldData();
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

    public static IEnumerable<CitySaveData> GetAllCitySaveData(SimulationWorldSaveData worldData)
    {
        if (worldData.CitiesById.Count > 0)
        {
            foreach (var city in worldData.CitiesById.Values)
            {
                yield return city;
            }
        }

        foreach (var city in worldData.Cities)
        {
            yield return city;
        }
    }
}
