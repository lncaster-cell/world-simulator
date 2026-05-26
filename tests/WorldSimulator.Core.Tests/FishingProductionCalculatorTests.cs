using WorldSimulator.Core.Cities;
using WorldSimulator.Core.Events;
using WorldSimulator.Core.Resources;
using Xunit;

namespace WorldSimulator.Core.Tests;

public sealed class FishingProductionCalculatorTests
{
    private readonly FishingProductionCalculator _calculator = new();

    [Fact]
    public void StableCity_WithEnoughWorkers_ProducesExpectedOutput()
    {
        var city = CityPresets.CreateGotha();
        city.Population = 420;
        city.CityState = CityState.Stable;

        var result = _calculator.Calculate(city, Array.Empty<CityEvent>());

        Assert.Equal(2, result.InfrastructureLevel);
        Assert.Equal(18m, result.NaturalPotential);
        Assert.Equal(1.00m, result.InfrastructureModifier);
        Assert.Equal(18m, result.InfrastructureCapacity);
        Assert.Equal(30, result.RequiredWorkers);
        Assert.Equal(42, result.AssignedWorkers);
        Assert.Equal(1m, result.WorkerCoverage);
        Assert.Equal(12, result.ExtraWorkers);
        Assert.True(result.OverstaffBonus > 0m);
        Assert.True(result.FinalOutput > 18m);
        Assert.True(result.FinalOutput < 20m);
    }

    [Fact]
    public void UnderstaffedCity_ReducesOutput()
    {
        var city = CityPresets.CreateGotha();
        city.Population = 100;
        city.CityState = CityState.Stable;

        var result = _calculator.Calculate(city, Array.Empty<CityEvent>());

        Assert.Equal(10, result.AssignedWorkers);
        Assert.Equal(30, result.RequiredWorkers);
        Assert.InRange(result.WorkerCoverage, 0.33m, 0.34m);
        Assert.InRange(result.FinalOutput, 5.9m, 6.1m);
    }

    [Fact]
    public void PortStorm_ReducesOutputToFifteenPercent()
    {
        const int currentDay = 1;
        var city = CityPresets.CreateGotha();
        city.Population = 420;
        city.CityState = CityState.Stable;

        var normalOutput = _calculator.Calculate(city, Array.Empty<CityEvent>()).FinalOutput;
        var stormOutput = _calculator.Calculate(city, new[] { CityEventPresets.CreatePortStorm(currentDay) }).FinalOutput;

        Assert.InRange(stormOutput, normalOutput * 0.15m - 0.01m, normalOutput * 0.15m + 0.01m);
    }

    [Fact]
    public void AbandonedOrEmptyCity_ProducesZero()
    {
        var emptyCity = CityPresets.CreateGotha();
        emptyCity.Population = 0;

        var abandonedCity = CityPresets.CreateGotha();
        abandonedCity.Population = 420;
        abandonedCity.CityState = CityState.Abandoned;

        var emptyResult = _calculator.Calculate(emptyCity, Array.Empty<CityEvent>());
        var abandonedResult = _calculator.Calculate(abandonedCity, Array.Empty<CityEvent>());

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
        city300.CityState = CityState.Stable;

        var city900 = CityPresets.CreateGotha();
        city900.Population = 900;
        city900.CityState = CityState.Stable;

        var output300 = _calculator.Calculate(city300, Array.Empty<CityEvent>()).FinalOutput;
        var output900 = _calculator.Calculate(city900, Array.Empty<CityEvent>()).FinalOutput;

        Assert.True(output900 < output300 * 3m);
        Assert.True(output900 < (18m * 1.10m) + 0.01m);
    }

    [Fact]
    public void Collapse_StronglyReducesOutput()
    {
        var stableCity = CityPresets.CreateGotha();
        stableCity.Population = 420;
        stableCity.CityState = CityState.Stable;

        var collapseCity = CityPresets.CreateGotha();
        collapseCity.Population = 420;
        collapseCity.CityState = CityState.Collapse;

        var stableOutput = _calculator.Calculate(stableCity, Array.Empty<CityEvent>()).FinalOutput;
        var collapseOutput = _calculator.Calculate(collapseCity, Array.Empty<CityEvent>()).FinalOutput;

        Assert.True(collapseOutput < stableOutput * 0.25m);
    }
}
