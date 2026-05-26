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
