using WorldSimulator.Core.Cities;
using Xunit;

namespace WorldSimulator.Core.Tests;

public sealed class CityStateEvaluatorTests
{
    private readonly CityStateEvaluator _evaluator = new();

    [Fact]
    public void Food_AtOneDayConsumption_ReturnsFamine()
    {
        var city = CreateBaselineCity();
        city.Food = city.CalculateDailyFoodConsumption();

        var state = _evaluator.Evaluate(city);

        Assert.Equal(CityState.Famine, state);
    }

    [Fact]
    public void Food_AtThreeDayConsumption_ReturnsFoodShortage()
    {
        var city = CreateBaselineCity();
        city.Food = city.CalculateDailyFoodConsumption() * 3m;

        var state = _evaluator.Evaluate(city);

        Assert.Equal(CityState.FoodShortage, state);
    }

    [Fact]
    public void LowMood_ReturnsUnrest()
    {
        var city = CreateBaselineCity();
        city.Mood = 20;

        var state = _evaluator.Evaluate(city);

        Assert.Equal(CityState.Unrest, state);
    }

    [Fact]
    public void HighCrime_ReturnsCrimeProblem()
    {
        var city = CreateBaselineCity();
        city.Crime = 70;

        var state = _evaluator.Evaluate(city);

        Assert.Equal(CityState.CrimeProblem, state);
    }

    [Fact]
    public void LowEconomyMetrics_ReturnsEconomicDecline()
    {
        var city = CreateBaselineCity();
        city.Wealth = 100m;

        var state = _evaluator.Evaluate(city);

        Assert.Equal(CityState.EconomicDecline, state);
    }

    [Fact]
    public void StrongCity_ReturnsProsperous()
    {
        var city = CreateBaselineCity();
        city.Food = city.CalculateDailyFoodConsumption() * 11m;
        city.Wealth = 600m;
        city.Mood = 80;
        city.Security = 80;
        city.Crime = 10;
        city.Goods = 250m;
        city.Resources = 250m;

        var state = _evaluator.Evaluate(city);

        Assert.Equal(CityState.Prosperous, state);
    }

    [Fact]
    public void GoodButNotProsperousCity_ReturnsStable()
    {
        var city = CreateBaselineCity();
        city.Food = city.CalculateDailyFoodConsumption() * 4m;
        city.Wealth = 300m;
        city.Mood = 50;
        city.Security = 50;
        city.Crime = 35;
        city.Goods = 150m;
        city.Resources = 150m;

        var state = _evaluator.Evaluate(city);

        Assert.Equal(CityState.Stable, state);
    }

    [Fact]
    public void WeakMixedCity_FallsBackToStagnation()
    {
        var city = CreateBaselineCity();

        var state = _evaluator.Evaluate(city);

        Assert.Equal(CityState.Stagnation, state);
    }

    [Fact]
    public void ZeroPopulation_ReturnsAbandoned()
    {
        var city = CreateBaselineCity();
        city.Population = 0;

        var state = _evaluator.Evaluate(city);

        Assert.Equal(CityState.Abandoned, state);
    }

    [Fact]
    public void Famine_HasPriorityOverUnrest()
    {
        var city = CreateBaselineCity();
        city.Food = city.CalculateDailyFoodConsumption();
        city.Mood = 10;

        var state = _evaluator.Evaluate(city);

        Assert.Equal(CityState.Famine, state);
    }

    [Fact]
    public void CatastrophicPopulatedCity_ReturnsCollapse()
    {
        var city = CreateBaselineCity();
        city.Population = 420;
        city.Food = 0m;
        city.Mood = 10;

        var state = _evaluator.Evaluate(city);

        Assert.Equal(CityState.Collapse, state);
    }

    private static City CreateBaselineCity()
    {
        var city = CityPresets.CreateGotha();
        city.Food = city.CalculateDailyFoodConsumption() * 4m;
        city.Wealth = 200m;
        city.Mood = 35;
        city.Security = 35;
        city.Crime = 45;
        city.Resources = 120m;
        city.Goods = 120m;
        city.Population = 420;
        return city;
    }
}
