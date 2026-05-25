namespace WorldSimulator.Core.Cities;

public sealed class PopulationChangeCalculator
{
    public PopulationChangeResult Calculate(City city)
    {
        ArgumentNullException.ThrowIfNull(city);

        var startingPopulation = city.Population;

        if (startingPopulation <= 0)
        {
            return new PopulationChangeResult(
                startingPopulation,
                0,
                0,
                "город опустел");
        }

        var delta = city.CityState switch
        {
            CityState.Collapse => -Math.Max(5, CeilPercent(startingPopulation, 0.05m)),
            CityState.Famine => -Math.Max(3, CeilPercent(startingPopulation, 0.03m)),
            CityState.FoodShortage => -Math.Max(1, CeilPercent(startingPopulation, 0.01m)),
            CityState.Unrest => -Math.Max(1, CeilPercent(startingPopulation, 0.01m)),
            CityState.CrimeProblem => -Math.Max(1, CeilPercent(startingPopulation, 0.005m)),
            CityState.Prosperous => CalculateProsperousDelta(city, startingPopulation),
            CityState.Stable => CalculateStableDelta(city),
            CityState.Stagnation => 0,
            CityState.Recovery => 0,
            CityState.EconomicDecline => 0,
            _ => 0
        };

        var endingPopulation = Math.Max(0, startingPopulation + delta);

        return new PopulationChangeResult(
            startingPopulation,
            endingPopulation - startingPopulation,
            endingPopulation,
            GetReason(city.CityState, endingPopulation - startingPopulation));
    }

    private static int CalculateProsperousDelta(City city, int population)
    {
        var dailyConsumption = city.CalculateDailyFoodConsumption();
        if (city.Food >= dailyConsumption * 5m && city.Mood >= 65 && city.Security >= 60)
        {
            return Math.Max(1, CeilPercent(population, 0.005m));
        }

        return 0;
    }

    private static int CalculateStableDelta(City city)
    {
        var dailyConsumption = city.CalculateDailyFoodConsumption();
        if (city.Food >= dailyConsumption * 3m && city.Mood >= 55)
        {
            return 1;
        }

        return 0;
    }

    private static int CeilPercent(int population, decimal percent)
    {
        return (int)Math.Ceiling(population * percent);
    }

    private static string GetReason(CityState state, int delta)
    {
        return state switch
        {
            CityState.Collapse => "коллапс",
            CityState.Abandoned => "город опустел",
            CityState.Famine => "голод",
            CityState.FoodShortage => "нехватка пищи",
            CityState.Unrest => "беспорядки",
            CityState.CrimeProblem => "преступность",
            CityState.Prosperous => delta > 0 ? "рост при процветании" : "процветание",
            CityState.Stable => delta > 0 ? "естественный рост" : "стабильность",
            CityState.Stagnation => "стагнация",
            CityState.Recovery => "восстановление",
            CityState.EconomicDecline => "экономический спад",
            _ => state.ToString()
        };
    }
}

public sealed record PopulationChangeResult(
    int StartingPopulation,
    int PopulationDelta,
    int EndingPopulation,
    string Reason);
