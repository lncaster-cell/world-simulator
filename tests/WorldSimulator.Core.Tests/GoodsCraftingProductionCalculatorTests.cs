using WorldSimulator.Core.Cities;
using WorldSimulator.Core.Resources;
using Xunit;

namespace WorldSimulator.Core.Tests;

public sealed class GoodsCraftingProductionCalculatorTests
{
    private readonly GoodsCraftingProductionCalculator _calculator = new();

    [Fact]
    public void StableCity_WithEnoughResources_ProducesGoodsAndConsumesResources()
    {
        var city = CityPresets.CreateGotha();
        city.Population = 420;
        city.Resources = 260m;
        city.Mood = 55;
        city.Security = 60;
        city.CityState = CityState.Stable;

        var result = _calculator.Calculate(city);

        Assert.Equal(23, result.AssignedWorkers);
        Assert.Equal(30, result.RequiredWorkers);
        Assert.InRange(result.WorkerCoverage, 0.76m, 0.77m);
        Assert.True(result.GoodsProduced > 0m);
        Assert.True(result.ResourcesConsumed > 0m);
        Assert.InRange(result.ResourcesConsumed, result.GoodsProduced * 1.2m - 0.01m, result.GoodsProduced * 1.2m + 0.01m);
    }

    [Fact]
    public void NoResources_ProducesNoGoods()
    {
        var city = CityPresets.CreateGotha();
        city.Population = 420;
        city.Resources = 0m;

        var result = _calculator.Calculate(city);

        Assert.Equal(0m, result.GoodsProduced);
        Assert.Equal(0m, result.ResourcesConsumed);
    }

    [Fact]
    public void LimitedResources_CapsProduction()
    {
        var city = CityPresets.CreateGotha();
        city.Population = 420;
        city.Resources = 3m;
        city.Mood = 60;
        city.Security = 60;
        city.CityState = CityState.Stable;

        var result = _calculator.Calculate(city);

        Assert.InRange(result.GoodsProduced, 2.49m, 2.5m);
        Assert.True(result.ResourcesConsumed <= 3m);
    }

    [Fact]
    public void LowMood_ReducesOutput()
    {
        var normalMoodCity = CityPresets.CreateGotha();
        normalMoodCity.Population = 420;
        normalMoodCity.Resources = 260m;
        normalMoodCity.Mood = 60;
        normalMoodCity.Security = 60;

        var lowMoodCity = CityPresets.CreateGotha();
        lowMoodCity.Population = 420;
        lowMoodCity.Resources = 260m;
        lowMoodCity.Mood = 10;
        lowMoodCity.Security = 60;

        var normalOutput = _calculator.Calculate(normalMoodCity).GoodsProduced;
        var lowOutput = _calculator.Calculate(lowMoodCity).GoodsProduced;

        Assert.True(lowOutput < normalOutput);
    }

    [Fact]
    public void LowSecurity_ReducesOutput()
    {
        var normalSecurityCity = CityPresets.CreateGotha();
        normalSecurityCity.Population = 420;
        normalSecurityCity.Resources = 260m;
        normalSecurityCity.Mood = 60;
        normalSecurityCity.Security = 60;

        var lowSecurityCity = CityPresets.CreateGotha();
        lowSecurityCity.Population = 420;
        lowSecurityCity.Resources = 260m;
        lowSecurityCity.Mood = 60;
        lowSecurityCity.Security = 10;

        var normalOutput = _calculator.Calculate(normalSecurityCity).GoodsProduced;
        var lowOutput = _calculator.Calculate(lowSecurityCity).GoodsProduced;

        Assert.True(lowOutput < normalOutput);
    }

    [Fact]
    public void AbandonedOrEmptyCity_ProducesZero()
    {
        var emptyCity = CityPresets.CreateGotha();
        emptyCity.Population = 0;
        emptyCity.Resources = 260m;

        var abandonedCity = CityPresets.CreateGotha();
        abandonedCity.Population = 420;
        abandonedCity.Resources = 260m;
        abandonedCity.CityState = CityState.Abandoned;

        var emptyResult = _calculator.Calculate(emptyCity);
        var abandonedResult = _calculator.Calculate(abandonedCity);

        Assert.Equal(0m, emptyResult.GoodsProduced);
        Assert.Equal(0m, emptyResult.ResourcesConsumed);
        Assert.Equal(0m, abandonedResult.GoodsProduced);
        Assert.Equal(0m, abandonedResult.ResourcesConsumed);
    }

    [Fact]
    public void Collapse_StronglyReducesOutput()
    {
        var stableCity = CityPresets.CreateGotha();
        stableCity.Population = 420;
        stableCity.Resources = 260m;
        stableCity.Mood = 60;
        stableCity.Security = 60;
        stableCity.CityState = CityState.Stable;

        var collapseCity = CityPresets.CreateGotha();
        collapseCity.Population = 420;
        collapseCity.Resources = 260m;
        collapseCity.Mood = 60;
        collapseCity.Security = 60;
        collapseCity.CityState = CityState.Collapse;

        var stableOutput = _calculator.Calculate(stableCity).GoodsProduced;
        var collapseOutput = _calculator.Calculate(collapseCity).GoodsProduced;

        Assert.True(collapseOutput < stableOutput * 0.3m);
    }

    [Fact]
    public void OverstaffBonus_RemainsWeak()
    {
        var city420 = CityPresets.CreateGotha();
        city420.Population = 420;
        city420.Resources = 260m;
        city420.Mood = 100;
        city420.Security = 100;
        city420.CityState = CityState.Prosperous;

        var city1200 = CityPresets.CreateGotha();
        city1200.Population = 1200;
        city1200.Resources = 260m;
        city1200.Mood = 100;
        city1200.Security = 100;
        city1200.CityState = CityState.Prosperous;

        var output420 = _calculator.Calculate(city420).GoodsProduced;
        var result1200 = _calculator.Calculate(city1200);

        Assert.True(result1200.GoodsProduced < output420 * 3m);
        Assert.True(result1200.OverstaffBonus < result1200.NaturalPotential * 0.05m);
    }
}
