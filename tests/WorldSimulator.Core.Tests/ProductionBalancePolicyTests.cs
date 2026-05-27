using WorldSimulator.Core.Cities;
using WorldSimulator.Core.Resources;
using Xunit;

namespace WorldSimulator.Core.Tests;

public sealed class ProductionBalancePolicyTests
{
    [Theory]
    [InlineData(70, 1.05)]
    [InlineData(69, 1.00)]
    [InlineData(40, 1.00)]
    [InlineData(39, 0.80)]
    [InlineData(20, 0.80)]
    [InlineData(19, 0.55)]
    public void MoodModifier_BaselineThresholds_AgricultureAndGoods(int mood, decimal expected)
    {
        Assert.Equal(expected, ProductionBalancePolicy.GetMoodModifier(mood, ProductionDomain.Agriculture));
        Assert.Equal(expected, ProductionBalancePolicy.GetMoodModifier(mood, ProductionDomain.Goods));
    }

    [Theory]
    [InlineData(70, 1.05)]
    [InlineData(69, 1.00)]
    [InlineData(40, 1.00)]
    [InlineData(39, 0.80)]
    [InlineData(20, 0.80)]
    [InlineData(19, 0.55)]
    public void SecurityModifier_BaselineThresholds_AgricultureAndGoods(int security, decimal expected)
    {
        Assert.Equal(expected, ProductionBalancePolicy.GetSecurityModifier(security, ProductionDomain.Agriculture));
        Assert.Equal(expected, ProductionBalancePolicy.GetSecurityModifier(security, ProductionDomain.Goods));
    }

    [Theory]
    [InlineData(70, 1.05)]
    [InlineData(69, 1.00)]
    [InlineData(40, 1.00)]
    [InlineData(39, 0.75)]
    [InlineData(20, 0.75)]
    [InlineData(19, 0.50)]
    public void SecurityModifier_BaselineThresholds_Resource(int security, decimal expected)
    {
        Assert.Equal(expected, ProductionBalancePolicy.GetSecurityModifier(security, ProductionDomain.Resource));
    }

    [Theory]
    [InlineData(CityState.Abandoned, 0.00, 0.00, 0.00)]
    [InlineData(CityState.Collapse, 0.20, 0.15, 0.20)]
    [InlineData(CityState.Famine, 0.70, 0.45, 0.65)]
    [InlineData(CityState.FoodShortage, 0.85, 0.65, 0.80)]
    [InlineData(CityState.Unrest, 0.65, 0.45, 0.55)]
    [InlineData(CityState.CrimeProblem, 0.80, 0.70, 0.75)]
    [InlineData(CityState.EconomicDecline, 0.85, 0.75, 0.80)]
    [InlineData(CityState.Stagnation, 0.90, 0.85, 0.90)]
    [InlineData(CityState.Stable, 1.00, 1.00, 1.00)]
    [InlineData(CityState.Prosperous, 1.10, 1.10, 1.05)]
    [InlineData(CityState.Recovery, 0.90, 0.90, 0.90)]
    public void StateModifier_BaselineValues_AllCityStates(
        CityState cityState,
        decimal agricultureExpected,
        decimal goodsExpected,
        decimal resourceExpected)
    {
        Assert.Equal(agricultureExpected, ProductionBalancePolicy.GetStateModifier(cityState, ProductionDomain.Agriculture));
        Assert.Equal(goodsExpected, ProductionBalancePolicy.GetStateModifier(cityState, ProductionDomain.Goods));
        Assert.Equal(resourceExpected, ProductionBalancePolicy.GetStateModifier(cityState, ProductionDomain.Resource));
    }
}
