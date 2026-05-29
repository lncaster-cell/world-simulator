using WorldSimulator.Core.Cities;
using Xunit;

namespace WorldSimulator.Core.Tests;

public sealed class CityEconomyValueTests
{
    [Fact]
    public void Constructor_RoundsStoredEconomyValuesToOneDecimal()
    {
        var city = new City(
            "test",
            "Test",
            population: 100,
            food: 707.8565m,
            wealth: 123.456m,
            mood: 50,
            security: 50,
            crime: 10,
            resources: 45.678m,
            goods: 9.991m,
            CityState.Stable);

        Assert.Equal(707.9m, city.Food);
        Assert.Equal(123.5m, city.Wealth);
        Assert.Equal(45.7m, city.Resources);
        Assert.Equal(10.0m, city.Goods);
    }

    [Fact]
    public void Setters_RoundStoredEconomyValuesToOneDecimalAndClampBelowZero()
    {
        var city = new City("test", "Test", 100, 0m, 0m, 50, 50, 10, 0m, 0m, CityState.Stable);

        city.Food = 1.24m;
        city.Wealth = 1.25m;
        city.Resources = -99m;
        city.Goods = 2.26m;

        Assert.Equal(1.2m, city.Food);
        Assert.Equal(1.3m, city.Wealth);
        Assert.Equal(0m, city.Resources);
        Assert.Equal(2.3m, city.Goods);
    }
}
