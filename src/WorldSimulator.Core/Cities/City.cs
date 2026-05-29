using WorldSimulator.Core.Workforce;

namespace WorldSimulator.Core.Cities;

/// <summary>
/// Core city model for MVP 0.1. Food is measured in storage units.
/// </summary>
public sealed class City
{
    private const int MetricMin = 0;
    private const int MetricMax = 100;

    private string _id;
    private string _name;
    private int _population;
    private decimal _food;
    private decimal _wealth;
    private int _mood;
    private int _security;
    private int _crime;
    private decimal _resources;
    private decimal _goods;

    public City(
        string id,
        string name,
        int population,
        decimal food,
        decimal wealth,
        int mood,
        int security,
        int crime,
        decimal resources,
        decimal goods,
        CityState cityState,
        CityInfrastructure? infrastructure = null,
        CityPopulationDemographics? demographics = null)
    {
        Id = id;
        Name = name;
        Population = population;
        Food = food;
        Wealth = wealth;
        Mood = mood;
        Security = security;
        Crime = crime;
        Resources = resources;
        Goods = goods;
        CityState = cityState;
        Infrastructure = infrastructure ?? new CityInfrastructure();
        Demographics = demographics ?? CityPopulationDemographics.CreateDefaultHuman(Population);
    }

    public string Id
    {
        get => _id;
        set => _id = string.IsNullOrWhiteSpace(value)
            ? throw new ArgumentException("City id must not be empty.", nameof(value))
            : value;
    }

    public string Name
    {
        get => _name;
        set => _name = string.IsNullOrWhiteSpace(value)
            ? throw new ArgumentException("City name must not be empty.", nameof(value))
            : value;
    }

    public int Population
    {
        get => _population;
        set => _population = Math.Max(0, value);
    }

    public decimal Food
    {
        get => _food;
        set => _food = NormalizeEconomyValue(value);
    }

    public decimal Wealth
    {
        get => _wealth;
        set => _wealth = NormalizeEconomyValue(value);
    }

    public int Mood
    {
        get => _mood;
        set => _mood = Math.Clamp(value, MetricMin, MetricMax);
    }

    public int Security
    {
        get => _security;
        set => _security = Math.Clamp(value, MetricMin, MetricMax);
    }

    public int Crime
    {
        get => _crime;
        set => _crime = Math.Clamp(value, 1, MetricMax);
    }

    public decimal Resources
    {
        get => _resources;
        set => _resources = NormalizeEconomyValue(value);
    }

    public decimal Goods
    {
        get => _goods;
        set => _goods = NormalizeEconomyValue(value);
    }

    public CityState CityState { get; set; }

    public CityInfrastructure Infrastructure { get; }

    public CityPopulationDemographics Demographics { get; }

    public CityWorkforceAllocation? WorkforceAllocation { get; private set; }

    public void SetWorkforceAllocation(CityWorkforceAllocation allocation)
    {
        WorkforceAllocation = allocation ?? throw new ArgumentNullException(nameof(allocation));
    }

    public const decimal DailyFoodConsumptionPerPerson = 0.2m;

    public decimal CalculateDailyFoodConsumption() => Population * DailyFoodConsumptionPerPerson;

    private static decimal NormalizeEconomyValue(decimal value)
    {
        return Math.Round(Math.Max(0m, value), 1, MidpointRounding.AwayFromZero);
    }
}
