using WorldSimulator.Core.Cities;
using WorldSimulator.Core.Events;
using WorldSimulator.Core.Resources;
using Xunit;

namespace WorldSimulator.Core.Tests;

public sealed class MainlandSupplyProductionCalculatorTests
{
    private readonly MainlandSupplyProductionCalculator _calculator = new();

    [Fact]
    public void StableCity_Produces_Expected_MainlandSupply()
    {
        var city = CreateCity(security: 60, wealth: 320m, cityState: CityState.Stagnation);

        var result = _calculator.Calculate(city, []);

        Assert.Equal(40m, result.NaturalSupplyPotential);
        Assert.Equal(2, result.InfrastructureLevel);
        Assert.Equal(1.00m, result.InfrastructureModifier);
        Assert.Equal(40m, result.InfrastructureCapacity);
        Assert.Equal(1.00m, result.SecurityModifier);
        Assert.Equal(1.00m, result.WealthModifier);
        Assert.Equal(0.90m, result.StateModifier);
        Assert.Equal(36m, result.FinalOutput);
    }

    [Fact]
    public void Prosperous_HighWealthCity_Increases_Output()
    {
        var city = CreateCity(security: 75, wealth: 420m, cityState: CityState.Prosperous);

        var result = _calculator.Calculate(city, []);

        Assert.True(result.FinalOutput > 40m);
    }

    [Fact]
    public void LowSecurity_Reduces_Output()
    {
        var normal = _calculator.Calculate(CreateCity(security: 60, wealth: 320m, cityState: CityState.Stable), []);
        var lowSecurity = _calculator.Calculate(CreateCity(security: 10, wealth: 320m, cityState: CityState.Stable), []);

        Assert.True(lowSecurity.FinalOutput < normal.FinalOutput);
    }

    [Fact]
    public void LowWealth_Reduces_Output()
    {
        var normal = _calculator.Calculate(CreateCity(security: 60, wealth: 320m, cityState: CityState.Stable), []);
        var lowWealth = _calculator.Calculate(CreateCity(security: 60, wealth: 80m, cityState: CityState.Stable), []);

        Assert.True(lowWealth.FinalOutput < normal.FinalOutput);
    }

    [Fact]
    public void Storm_Reduces_Output_To_25Percent()
    {
        const int currentDay = 1;
        var city = CreateCity(security: 60, wealth: 320m, cityState: CityState.Stable);

        var normal = _calculator.Calculate(city, []);
        var storm = _calculator.Calculate(city, [CityEventPresets.CreatePortStorm(currentDay)]);

        Assert.Equal(decimal.Round(normal.FinalOutput * 0.25m, 2), storm.FinalOutput);
    }

    [Fact]
    public void AbandonedOrEmptyCity_Produces_Zero()
    {
        var abandoned = CreateCity(security: 60, wealth: 320m, cityState: CityState.Abandoned);
        var emptyPopulation = CreateCity(security: 60, wealth: 320m, cityState: CityState.Stable, population: 0);

        var abandonedResult = _calculator.Calculate(abandoned, []);
        var emptyResult = _calculator.Calculate(emptyPopulation, []);

        Assert.Equal(0m, abandonedResult.FinalOutput);
        Assert.Equal(0m, abandonedResult.StateModifier);
        Assert.Equal(0m, emptyResult.FinalOutput);
        Assert.Equal(0m, emptyResult.StateModifier);
    }

    [Fact]
    public void Collapse_Strongly_Reduces_Output()
    {
        var stable = _calculator.Calculate(CreateCity(security: 60, wealth: 320m, cityState: CityState.Stable), []);
        var collapse = _calculator.Calculate(CreateCity(security: 60, wealth: 320m, cityState: CityState.Collapse), []);

        Assert.True(collapse.FinalOutput < stable.FinalOutput);
        Assert.Equal(8m, collapse.FinalOutput);
    }

    private static City CreateCity(int security, decimal wealth, CityState cityState, int population = 420)
    {
        return new City(
            id: "gotha_test",
            name: "GothaTest",
            population: population,
            food: 1000m,
            wealth: wealth,
            mood: 50,
            security: security,
            crime: 30,
            resources: 100m,
            goods: 100m,
            cityState: cityState);
    }
}
