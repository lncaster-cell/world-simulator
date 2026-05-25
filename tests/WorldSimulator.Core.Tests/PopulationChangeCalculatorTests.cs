using WorldSimulator.Core.Cities;
using Xunit;

namespace WorldSimulator.Core.Tests;

public sealed class PopulationChangeCalculatorTests
{
    private readonly PopulationChangeCalculator _calculator = new();

    [Fact]
    public void Famine_DecreasesPopulation()
    {
        var city = CreateCity(CityState.Famine, 420);

        var result = _calculator.Calculate(city);

        Assert.True(result.PopulationDelta < 0);
    }

    [Fact]
    public void Collapse_DecreasesPopulationStrongerThanFamine_ForSamePopulation()
    {
        var collapseCity = CreateCity(CityState.Collapse, 420);
        var famineCity = CreateCity(CityState.Famine, 420);

        var collapse = _calculator.Calculate(collapseCity);
        var famine = _calculator.Calculate(famineCity);

        Assert.True(collapse.PopulationDelta < famine.PopulationDelta);
    }

    [Fact]
    public void FoodShortage_DecreasesPopulation()
    {
        var city = CreateCity(CityState.FoodShortage, 420);

        var result = _calculator.Calculate(city);

        Assert.True(result.PopulationDelta < 0);
    }

    [Fact]
    public void Unrest_DecreasesPopulation()
    {
        var city = CreateCity(CityState.Unrest, 420);

        var result = _calculator.Calculate(city);

        Assert.True(result.PopulationDelta < 0);
    }

    [Fact]
    public void CrimeProblem_DecreasesPopulation()
    {
        var city = CreateCity(CityState.CrimeProblem, 420);

        var result = _calculator.Calculate(city);

        Assert.True(result.PopulationDelta < 0);
    }

    [Fact]
    public void Prosperous_CanIncreasePopulation()
    {
        var city = CreateCity(CityState.Prosperous, 420);
        city.Food = city.CalculateDailyFoodConsumption() * 5m;
        city.Mood = 65;
        city.Security = 60;

        var result = _calculator.Calculate(city);

        Assert.True(result.PopulationDelta > 0);
    }

    [Fact]
    public void Stable_CanIncreasePopulationByOne()
    {
        var city = CreateCity(CityState.Stable, 420);
        city.Food = city.CalculateDailyFoodConsumption() * 3m;
        city.Mood = 55;

        var result = _calculator.Calculate(city);

        Assert.Equal(1, result.PopulationDelta);
    }

    [Fact]
    public void Stagnation_DoesNotChangePopulation()
    {
        var city = CreateCity(CityState.Stagnation, 420);

        var result = _calculator.Calculate(city);

        Assert.Equal(0, result.PopulationDelta);
    }

    [Fact]
    public void Recovery_DoesNotChangePopulation()
    {
        var city = CreateCity(CityState.Recovery, 420);

        var result = _calculator.Calculate(city);

        Assert.Equal(0, result.PopulationDelta);
    }

    [Fact]
    public void EconomicDecline_DoesNotChangePopulation()
    {
        var city = CreateCity(CityState.EconomicDecline, 420);

        var result = _calculator.Calculate(city);

        Assert.Equal(0, result.PopulationDelta);
    }

    [Fact]
    public void Population_NeverGoesBelowZero()
    {
        var city = CreateCity(CityState.Collapse, 1);

        var result = _calculator.Calculate(city);

        Assert.Equal(0, result.EndingPopulation);
        Assert.Equal(-1, result.PopulationDelta);
    }

    [Fact]
    public void Rounding_IsDeterministic()
    {
        var city = CreateCity(CityState.CrimeProblem, 201);

        var result = _calculator.Calculate(city);

        Assert.Equal(-2, result.PopulationDelta);
    }

    private static City CreateCity(CityState state, int population)
    {
        var city = CityPresets.CreateGotha();
        city.CityState = state;
        city.Population = population;
        city.Food = city.CalculateDailyFoodConsumption() * 6m;
        city.Mood = 70;
        city.Security = 70;
        city.Crime = 20;
        city.Wealth = 300m;
        city.Resources = 150m;
        city.Goods = 150m;
        return city;
    }
}
