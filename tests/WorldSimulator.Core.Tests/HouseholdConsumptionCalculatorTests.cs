using WorldSimulator.Core.Cities;
using WorldSimulator.Core.Resources;
using Xunit;

namespace WorldSimulator.Core.Tests;

public sealed class HouseholdConsumptionCalculatorTests
{
    private readonly HouseholdConsumptionCalculator _calculator = new();

    [Fact]
    public void PopulationConsumesGoodsAndResources()
    {
        var city = CityPresets.CreateGotha();
        city.Population = 420;
        city.Goods = 100m;
        city.Resources = 100m;

        var result = _calculator.Calculate(city);

        Assert.Equal(21m, result.RequiredGoods);
        Assert.Equal(21m, result.GoodsConsumed);
        Assert.Equal(0m, result.GoodsShortage);
        Assert.Equal(4.2m, result.RequiredResources);
        Assert.Equal(4.2m, result.ResourcesConsumed);
        Assert.Equal(0m, result.ResourcesShortage);
    }

    [Fact]
    public void GoodsShortageIsCalculated()
    {
        var city = CityPresets.CreateGotha();
        city.Population = 420;
        city.Goods = 5m;

        var result = _calculator.Calculate(city);

        Assert.Equal(21m, result.RequiredGoods);
        Assert.Equal(5m, result.GoodsConsumed);
        Assert.Equal(16m, result.GoodsShortage);
    }

    [Fact]
    public void ResourcesShortageIsCalculated()
    {
        var city = CityPresets.CreateGotha();
        city.Population = 420;
        city.Resources = 1m;

        var result = _calculator.Calculate(city);

        Assert.Equal(4.2m, result.RequiredResources);
        Assert.Equal(1m, result.ResourcesConsumed);
        Assert.Equal(3.2m, result.ResourcesShortage);
    }

    [Fact]
    public void NoPopulationConsumesNothing()
    {
        var city = CityPresets.CreateGotha();
        city.Population = 0;
        city.Goods = 100m;
        city.Resources = 100m;

        var result = _calculator.Calculate(city);

        Assert.Equal(0m, result.RequiredGoods);
        Assert.Equal(0m, result.GoodsConsumed);
        Assert.Equal(0m, result.GoodsShortage);
        Assert.Equal(0m, result.RequiredResources);
        Assert.Equal(0m, result.ResourcesConsumed);
        Assert.Equal(0m, result.ResourcesShortage);
    }

    [Fact]
    public void Calculate_DoesNotMutateCity()
    {
        var city = CityPresets.CreateGotha();
        city.Population = 420;
        city.Goods = 12.5m;
        city.Resources = 7.75m;
        var goodsBefore = city.Goods;
        var resourcesBefore = city.Resources;

        _calculator.Calculate(city);

        Assert.Equal(goodsBefore, city.Goods);
        Assert.Equal(resourcesBefore, city.Resources);
    }
}
