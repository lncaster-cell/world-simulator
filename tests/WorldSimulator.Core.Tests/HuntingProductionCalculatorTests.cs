using WorldSimulator.Core.Cities;
using WorldSimulator.Core.Resources;
using Xunit;

namespace WorldSimulator.Core.Tests;

public sealed class HuntingProductionCalculatorTests
{
    private readonly HuntingProductionCalculator _calculator = new();

    [Fact]
    public void StableCity_WithEnoughWorkers_ProducesExpectedOutput()
    {
        var city = CityPresets.CreateGotha();
        city.Population = 420;
        city.Security = 60;
        city.CityState = CityState.Stable;

        var result = _calculator.Calculate(city);

        Assert.Equal(8m, result.NaturalPotential);
        Assert.Equal(15, result.RequiredWorkers);
        Assert.Equal(21, result.AssignedWorkers);
        Assert.Equal(1m, result.WorkerCoverage);
        Assert.Equal(6, result.ExtraWorkers);
        Assert.True(result.FinalOutput > 8m);
        Assert.True(result.FinalOutput < 8.5m);
    }

    [Fact]
    public void UnderstaffedCity_ReducesOutput()
    {
        var city = CityPresets.CreateGotha();
        city.Population = 100;
        city.Security = 60;
        city.CityState = CityState.Stable;

        var result = _calculator.Calculate(city);

        Assert.Equal(5, result.AssignedWorkers);
        Assert.Equal(15, result.RequiredWorkers);
        Assert.InRange(result.WorkerCoverage, 0.33m, 0.34m);
        Assert.InRange(result.FinalOutput, 2.66m, 2.68m);
    }

    [Fact]
    public void LowSecurity_StronglyReducesOutput()
    {
        var stableSecurityCity = CityPresets.CreateGotha();
        stableSecurityCity.Population = 420;
        stableSecurityCity.Security = 60;
        stableSecurityCity.CityState = CityState.Stable;

        var lowSecurityCity = CityPresets.CreateGotha();
        lowSecurityCity.Population = 420;
        lowSecurityCity.Security = 10;
        lowSecurityCity.CityState = CityState.Stable;

        var stableSecurityOutput = _calculator.Calculate(stableSecurityCity).FinalOutput;
        var lowSecurityOutput = _calculator.Calculate(lowSecurityCity).FinalOutput;

        Assert.True(lowSecurityOutput < stableSecurityOutput * 0.5m);
    }

    [Fact]
    public void AbandonedOrEmptyCity_ProducesZero()
    {
        var emptyCity = CityPresets.CreateGotha();
        emptyCity.Population = 0;

        var abandonedCity = CityPresets.CreateGotha();
        abandonedCity.Population = 420;
        abandonedCity.CityState = CityState.Abandoned;

        var emptyResult = _calculator.Calculate(emptyCity);
        var abandonedResult = _calculator.Calculate(abandonedCity);

        Assert.Equal(0, emptyResult.AssignedWorkers);
        Assert.Equal(0m, emptyResult.WorkerCoverage);
        Assert.Equal(0, emptyResult.ExtraWorkers);
        Assert.Equal(0m, emptyResult.OverstaffBonus);
        Assert.Equal(0m, emptyResult.FinalOutput);

        Assert.Equal(0, abandonedResult.AssignedWorkers);
        Assert.Equal(0m, abandonedResult.WorkerCoverage);
        Assert.Equal(0, abandonedResult.ExtraWorkers);
        Assert.Equal(0m, abandonedResult.OverstaffBonus);
        Assert.Equal(0m, abandonedResult.FinalOutput);
    }

    [Fact]
    public void OverstaffBonus_RemainsWeakAndCapped()
    {
        var city300 = CityPresets.CreateGotha();
        city300.Population = 300;
        city300.Security = 100;
        city300.CityState = CityState.Prosperous;

        var city900 = CityPresets.CreateGotha();
        city900.Population = 900;
        city900.Security = 100;
        city900.CityState = CityState.Prosperous;

        var output300 = _calculator.Calculate(city300).FinalOutput;
        var output900 = _calculator.Calculate(city900).FinalOutput;

        Assert.True(output900 < output300 * 3m);
        Assert.True(output900 < (8m * 1.05m * 1.05m) + 0.05m);
    }

    [Fact]
    public void Collapse_StronglyReducesOutput()
    {
        var stableCity = CityPresets.CreateGotha();
        stableCity.Population = 420;
        stableCity.Security = 60;
        stableCity.CityState = CityState.Stable;

        var collapseCity = CityPresets.CreateGotha();
        collapseCity.Population = 420;
        collapseCity.Security = 60;
        collapseCity.CityState = CityState.Collapse;

        var stableOutput = _calculator.Calculate(stableCity).FinalOutput;
        var collapseOutput = _calculator.Calculate(collapseCity).FinalOutput;

        Assert.True(collapseOutput < stableOutput * 0.25m);
    }
}
