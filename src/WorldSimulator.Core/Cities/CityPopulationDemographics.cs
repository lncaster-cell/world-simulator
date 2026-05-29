namespace WorldSimulator.Core.Cities;

public sealed class CityPopulationDemographics
{
    public List<RacePopulationGroup> RaceGroups { get; } = [];

    public int Children => RaceGroups.Sum(x => x.Children);
    public int AdultMen => RaceGroups.Sum(x => x.AdultMen);
    public int AdultWomen => RaceGroups.Sum(x => x.AdultWomen);
    public int Elderly => RaceGroups.Sum(x => x.Elderly);
    public int TotalPopulation => RaceGroups.Sum(x => x.TotalPopulation);

    public static CityPopulationDemographics CreateDefaultHuman(int totalPopulation)
    {
        var population = Math.Max(0, totalPopulation);
        var children = (int)Math.Round(population * 0.22m, MidpointRounding.AwayFromZero);
        var elderly = (int)Math.Round(population * 0.09m, MidpointRounding.AwayFromZero);
        var remainingAdults = Math.Max(0, population - children - elderly);
        var adultMen = remainingAdults / 2;
        var adultWomen = remainingAdults - adultMen;

        var demographics = new CityPopulationDemographics();
        demographics.RaceGroups.Add(new RacePopulationGroup
        {
            RaceId = "human",
            Children = children,
            AdultMen = adultMen,
            AdultWomen = adultWomen,
            Elderly = elderly
        });
        return demographics;
    }

    public void ReplaceWith(IEnumerable<RacePopulationGroup> raceGroups)
    {
        ArgumentNullException.ThrowIfNull(raceGroups);

        var snapshot = raceGroups.Select(Clone).ToList();
        RaceGroups.Clear();
        RaceGroups.AddRange(snapshot);
    }

    private static RacePopulationGroup Clone(RacePopulationGroup source) => new()
    {
        RaceId = source.RaceId,
        Children = source.Children,
        AdultMen = source.AdultMen,
        AdultWomen = source.AdultWomen,
        Elderly = source.Elderly
    };
}
