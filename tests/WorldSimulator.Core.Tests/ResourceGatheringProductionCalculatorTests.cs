using WorldSimulator.Core.Cities;
using WorldSimulator.Core.Resources;
using Xunit;

namespace WorldSimulator.Core.Tests;

public sealed class ResourceGatheringProductionCalculatorTests
{
    private readonly ResourceGatheringProductionCalculator _calculator = new();

    [Fact]
    public void StableCity_WithEnoughWorkers_ProducesExpectedResources()
    {
        var city = CityPresets.CreateGotha(); city.Population = 420; city.Security = 60; city.CityState = CityState.Stable;
        var result = _calculator.Calculate(city);
        Assert.Equal(14m, result.NaturalPotential); Assert.Equal(25, result.RequiredWorkers); Assert.Equal(28, result.AssignedWorkers); Assert.Equal(1m, result.WorkerCoverage); Assert.Equal(3, result.ExtraWorkers);
        Assert.True(result.FinalOutput > 14m); Assert.True(result.FinalOutput < 15m);
    }

    [Fact]
    public void UnderstaffedCity_ReducesOutput()
    {
        var city = CityPresets.CreateGotha(); city.Population = 150; city.Security = 60; city.CityState = CityState.Stable;
        var result = _calculator.Calculate(city);
        Assert.Equal(10, result.AssignedWorkers); Assert.Equal(25, result.RequiredWorkers); Assert.Equal(0.4m, result.WorkerCoverage); Assert.InRange(result.FinalOutput, 5.59m, 5.61m);
    }

    [Fact]
    public void LowSecurity_StronglyReducesOutput()
    {
        var stable = CityPresets.CreateGotha(); stable.Population = 420; stable.Security = 60; stable.CityState = CityState.Stable;
        var low = CityPresets.CreateGotha(); low.Population = 420; low.Security = 10; low.CityState = CityState.Stable;
        Assert.True(_calculator.Calculate(low).FinalOutput < _calculator.Calculate(stable).FinalOutput * 0.6m);
    }

    [Fact]
    public void AbandonedOrEmptyCity_ProducesZero()
    {
        var emptyCity = CityPresets.CreateGotha(); emptyCity.Population = 0;
        var abandonedCity = CityPresets.CreateGotha(); abandonedCity.Population = 420; abandonedCity.CityState = CityState.Abandoned;
        var emptyResult = _calculator.Calculate(emptyCity); var abandonedResult = _calculator.Calculate(abandonedCity);
        Assert.Equal(0m, emptyResult.FinalOutput); Assert.Equal(0m, abandonedResult.FinalOutput);
    }

    [Fact]
    public void OverstaffBonus_RemainsWeak()
    {
        var city420 = CityPresets.CreateGotha(); city420.Population = 420; city420.Security = 100; city420.CityState = CityState.Stable;
        var city1200 = CityPresets.CreateGotha(); city1200.Population = 1200; city1200.Security = 100; city1200.CityState = CityState.Stable;
        var output420 = _calculator.Calculate(city420).FinalOutput; var result1200 = _calculator.Calculate(city1200);
        Assert.True(result1200.FinalOutput < output420 * 2m); Assert.True(result1200.OverstaffBonus < 14m * 0.08m);
    }

    [Fact]
    public void Collapse_StronglyReducesOutput()
    {
        var stable = CityPresets.CreateGotha(); stable.Population = 420; stable.Security = 60; stable.CityState = CityState.Stable;
        var collapse = CityPresets.CreateGotha(); collapse.Population = 420; collapse.Security = 60; collapse.CityState = CityState.Collapse;
        Assert.True(_calculator.Calculate(collapse).FinalOutput < _calculator.Calculate(stable).FinalOutput * 0.25m);
    }
}
