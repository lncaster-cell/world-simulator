using WorldSimulator.Core.Cities;
using WorldSimulator.Core.Resources;
using WorldSimulator.Core.World;
using Xunit;

namespace WorldSimulator.Core.Tests;

public sealed class AgricultureProductionCalculatorTests
{
    private readonly AgricultureProductionCalculator _calculator = new();

    [Fact]
    public void AgriculturalVillage_ProducesFood()
    {
        var city = CityPresets.CreateMlynek();
        var profile = new SettlementEconomyProfile{ SettlementId = city.Id, AgriculturePotential = 26m, FishingMultiplier=0m, HuntingMultiplier=0m, MainlandSupplyMultiplier=0m, ResourceGatheringMultiplier=0m, GoodsCraftingMultiplier=0m, IsPort=false, IsFortress=false, IsCapital=false};

        var result = _calculator.Calculate(city, profile);

        Assert.True(result.FinalOutput > 0m);
    }

    [Fact]
    public void AssignedWorkers_ControlProductionAndDoNotGrowWithoutBound()
    {
        var city = CityPresets.CreateMlynek();
        city.Mood = 60;
        city.Security = 60;
        city.CityState = CityState.Stable;
        var profile = new SettlementEconomyProfile{ SettlementId = city.Id, AgriculturePotential = 26m, FishingMultiplier=0m, HuntingMultiplier=0m, MainlandSupplyMultiplier=0m, ResourceGatheringMultiplier=0m, GoodsCraftingMultiplier=0m, IsPort=false, IsFortress=false, IsCapital=false};

        var noWorkers = _calculator.Calculate(city, profile, assignedWorkers: 0);
        var staffed = _calculator.Calculate(city, profile, noWorkers.RequiredWorkers);
        var overstaffed = _calculator.Calculate(city, profile, noWorkers.RequiredWorkers * 10);

        Assert.Equal(0, noWorkers.AssignedWorkers);
        Assert.Equal(0m, noWorkers.FinalOutput);
        Assert.Equal(1m, staffed.WorkerCoverage);
        Assert.True(staffed.FinalOutput > 0m);
        Assert.Equal(1m, overstaffed.WorkerCoverage);
        Assert.Equal(staffed.FinalOutput, overstaffed.FinalOutput);
    }

    [Fact]
    public void ZeroPotential_ProducesZero()
    {
        var city = CityPresets.CreateMlynek();
        var profile = new SettlementEconomyProfile{ SettlementId = city.Id, AgriculturePotential = 0m, FishingMultiplier=0m, HuntingMultiplier=0m, MainlandSupplyMultiplier=0m, ResourceGatheringMultiplier=0m, GoodsCraftingMultiplier=0m, IsPort=false, IsFortress=false, IsCapital=false};
        Assert.Equal(0m, _calculator.Calculate(city, profile).FinalOutput);
    }

    [Fact]
    public void LowSecurity_ReducesOutput()
    {
        var city = CityPresets.CreateMlynek();
        var profile = new SettlementEconomyProfile{ SettlementId = city.Id, AgriculturePotential = 26m, FishingMultiplier=0m, HuntingMultiplier=0m, MainlandSupplyMultiplier=0m, ResourceGatheringMultiplier=0m, GoodsCraftingMultiplier=0m, IsPort=false, IsFortress=false, IsCapital=false};
        var normal = _calculator.Calculate(city, profile).FinalOutput;
        city.Security = 10;
        var low = _calculator.Calculate(city, profile).FinalOutput;
        Assert.True(low < normal);
    }

    [Fact]
    public void LowMood_ReducesOutput()
    {
        var city = CityPresets.CreateMlynek();
        var profile = new SettlementEconomyProfile{ SettlementId = city.Id, AgriculturePotential = 26m, FishingMultiplier=0m, HuntingMultiplier=0m, MainlandSupplyMultiplier=0m, ResourceGatheringMultiplier=0m, GoodsCraftingMultiplier=0m, IsPort=false, IsFortress=false, IsCapital=false};
        var normal = _calculator.Calculate(city, profile).FinalOutput;
        city.Mood = 10;
        var low = _calculator.Calculate(city, profile).FinalOutput;
        Assert.True(low < normal);
    }

    [Fact]
    public void AbandonedCity_ProducesZero()
    {
        var city = CityPresets.CreateMlynek();
        city.CityState = CityState.Abandoned;
        var profile = new SettlementEconomyProfile{ SettlementId = city.Id, AgriculturePotential = 26m, FishingMultiplier=0m, HuntingMultiplier=0m, MainlandSupplyMultiplier=0m, ResourceGatheringMultiplier=0m, GoodsCraftingMultiplier=0m, IsPort=false, IsFortress=false, IsCapital=false};
        Assert.Equal(0m, _calculator.Calculate(city, profile).FinalOutput);
    }
}
