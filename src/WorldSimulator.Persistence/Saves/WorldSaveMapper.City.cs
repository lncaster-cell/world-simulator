using WorldSimulator.Core.Cities;
using WorldSimulator.Core.Workforce;

namespace WorldSimulator.Persistence.Saves;

internal static partial class WorldSaveMapper
{
    public static CitySaveData ToSaveData(City city) => new()
    {
        Id = city.Id,
        Name = city.Name,
        Population = city.Population,
        Food = city.Food,
        Wealth = city.Wealth,
        Mood = city.Mood,
        Security = city.Security,
        Crime = city.Crime,
        Resources = city.Resources,
        Goods = city.Goods,
        CityState = city.CityState.ToString(),
        Infrastructure = ToSaveData(city.Infrastructure),
        Demographics = ToSaveData(city.Demographics)
    };

    private static CityInfrastructureSaveData ToSaveData(CityInfrastructure infrastructure) => new()
    {
        HousingLevel = infrastructure.HousingLevel,
        UrbanLevel = infrastructure.UrbanLevel,
        ProductionLevel = infrastructure.ProductionLevel,
        MilitaryLevel = infrastructure.MilitaryLevel
    };

    private static CityPopulationDemographicsSaveData ToSaveData(CityPopulationDemographics demographics) => new()
    {
        RaceGroups = demographics.RaceGroups.Select(ToSaveData).ToList()
    };

    private static RacePopulationGroupSaveData ToSaveData(RacePopulationGroup group) => new()
    {
        RaceId = group.RaceId,
        Children = group.Children,
        AdultMen = group.AdultMen,
        AdultWomen = group.AdultWomen,
        Elderly = group.Elderly
    };

    private static City ToCoreCity(CitySaveData cityData, string filePath)
    {
        if (!Enum.TryParse<CityState>(cityData.CityState, true, out var parsedCityState))
            throw new InvalidDataException($"Save file '{filePath}' contains unknown city_state '{cityData.CityState}'.");
        if (cityData.Infrastructure is null)
            throw new InvalidDataException($"Save file '{filePath}' city '{cityData.Id}' is missing infrastructure data.");
        if (cityData.Demographics is null)
            throw new InvalidDataException($"Save file '{filePath}' city '{cityData.Id}' is missing demographics data.");

        return new City(
            cityData.Id,
            cityData.Name,
            cityData.Population,
            cityData.Food,
            cityData.Wealth,
            cityData.Mood,
            cityData.Security,
            cityData.Crime,
            cityData.Resources,
            cityData.Goods,
            parsedCityState,
            ToCoreInfrastructure(cityData.Infrastructure),
            ToCoreDemographics(cityData.Demographics));
    }

    private static CityInfrastructure ToCoreInfrastructure(CityInfrastructureSaveData infrastructureData) => new()
    {
        HousingLevel = infrastructureData.HousingLevel,
        UrbanLevel = infrastructureData.UrbanLevel,
        ProductionLevel = infrastructureData.ProductionLevel,
        MilitaryLevel = infrastructureData.MilitaryLevel
    };

    private static CityPopulationDemographics ToCoreDemographics(CityPopulationDemographicsSaveData demographicsData)
    {
        var demographics = new CityPopulationDemographics();
        demographics.ReplaceWith((demographicsData.RaceGroups ?? []).Select(ToCoreRacePopulationGroup));
        return demographics;
    }

    private static RacePopulationGroup ToCoreRacePopulationGroup(RacePopulationGroupSaveData groupData) => new()
    {
        RaceId = groupData.RaceId,
        Children = groupData.Children,
        AdultMen = groupData.AdultMen,
        AdultWomen = groupData.AdultWomen,
        Elderly = groupData.Elderly
    };
}
