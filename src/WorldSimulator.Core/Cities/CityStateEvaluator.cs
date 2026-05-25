namespace WorldSimulator.Core.Cities;

public sealed class CityStateEvaluator
{
    public CityState Evaluate(City city)
    {
        ArgumentNullException.ThrowIfNull(city);

        var dailyFoodConsumption = city.CalculateDailyFoodConsumption();

        if (city.Population <= 0)
        {
            return CityState.Abandoned;
        }

        if ((city.Food <= 0m && city.Mood <= 10)
            || (city.Security <= 0 && city.Crime >= 90))
        {
            return CityState.Collapse;
        }

        if (city.Food <= dailyFoodConsumption)
        {
            return CityState.Famine;
        }

        if (city.Food <= dailyFoodConsumption * 3m)
        {
            return CityState.FoodShortage;
        }

        if (city.Mood <= 20)
        {
            return CityState.Unrest;
        }

        if (city.Crime >= 70 || (city.Security <= 25 && city.Crime >= 50))
        {
            return CityState.CrimeProblem;
        }

        if (city.Wealth <= 100m || city.Goods <= 50m || city.Resources <= 50m)
        {
            return CityState.EconomicDecline;
        }

        if (city.Food > dailyFoodConsumption * 10m
            && city.Wealth >= 500m
            && city.Mood >= 70
            && city.Security >= 70
            && city.Crime <= 20
            && city.Goods >= 200m
            && city.Resources >= 200m)
        {
            return CityState.Prosperous;
        }

        if (city.Food > dailyFoodConsumption * 3m
            && city.Mood >= 40
            && city.Security >= 40
            && city.Crime <= 60)
        {
            return CityState.Stable;
        }

        return CityState.Stagnation;
    }
}
