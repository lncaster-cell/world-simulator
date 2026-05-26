using FluentAssertions;
using WorldSimulator.Core.Cities;
using WorldSimulator.Core.Trade;
using WorldSimulator.Core.World;

namespace WorldSimulator.Core.Tests;

public sealed class WorldTradeFlowServiceTests
{
    private readonly WorldTradeFlowService _service = new();

    [Fact] public void FoodSurplus_TransfersFood() => AssertTransfer(TradeGoodType.Food);
    [Fact] public void GoodsSurplus_TransfersGoods() => AssertTransfer(TradeGoodType.Goods);
    [Fact] public void ResourcesSurplus_TransfersResources() => AssertTransfer(TradeGoodType.Resources);

    [Fact]
    public void Transfer_RespectsReserveAndTargetAndCapacity()
    {
        var world = BuildWorld(TradeGoodType.Food, exporterStock: 300m, importerStock: 0m, caravanCapacity: 10m);
        var exporter = world.FindCity("a")!;
        var importer = world.FindCity("b")!;
        var reserve = 160m; // pop100 => 20/day * 8
        var target = 240m;

        _service.RunWeeklyTrade(world);

        exporter.Food.Should().BeGreaterThanOrEqualTo(reserve);
        importer.Food.Should().BeLessThanOrEqualTo(target);
        (importer.Food).Should().Be(10m);
    }

    [Fact]
    public void UnavailableCaravan_DoesNotTrade()
    {
        var world = BuildWorld(TradeGoodType.Goods, 100m, 0m, 50m, false);
        var result = _service.RunWeeklyTrade(world);
        result.Transfers.Should().BeEmpty();
    }

    [Fact]
    public void CityWithoutCaravan_DoesNotTradeAsExporter()
    {
        var world = BuildWorld(TradeGoodType.Resources, 100m, 0m, 50m);
        world.Caravans.Clear();
        var result = _service.RunWeeklyTrade(world);
        result.Transfers.Should().BeEmpty();
    }

    [Fact]
    public void Trade_ChangesWealthInSmallAmount()
    {
        var world = BuildWorld(TradeGoodType.Food, 300m, 0m, 50m);
        var result = _service.RunWeeklyTrade(world);
        result.Transfers.Should().HaveCount(1);
        result.Transfers[0].ExporterWealthDelta.Should().BePositive().And.BeLessOrEqualTo(2m);
        result.Transfers[0].ImporterWealthDelta.Should().BeNegative().And.BeGreaterOrEqualTo(-2m);
    }

    [Fact]
    public void Result_IsDeterministic()
    {
        var worldA = BuildWorld(TradeGoodType.Food, 300m, 0m, 50m);
        var worldB = BuildWorld(TradeGoodType.Food, 300m, 0m, 50m);

        var resultA = _service.RunWeeklyTrade(worldA);
        var resultB = _service.RunWeeklyTrade(worldB);

        resultA.Should().BeEquivalentTo(resultB);
    }

    private void AssertTransfer(TradeGoodType good)
    {
        var world = BuildWorld(good, 100m, 0m, 50m);
        var result = _service.RunWeeklyTrade(world);
        result.Transfers.Should().ContainSingle(t => t.GoodType == good && t.AmountTransferred > 0m);
    }

    private static SimulationWorld BuildWorld(TradeGoodType good, decimal exporterStock, decimal importerStock, decimal caravanCapacity, bool available = true)
    {
        var cityA = City("a", 100);
        var cityB = City("b", 100);
        SetStock(cityA, good, exporterStock);
        SetStock(cityB, good, importerStock);

        return new SimulationWorld
        {
            Cities = [cityA, cityB],
            Regions = [],
            SettlementMapLocations = [],
            SettlementEconomyProfiles = [],
            Caravans = [new Caravan { Id = "c1", OwnerSettlementId = "a", Type = CaravanType.Land, Capacity = caravanCapacity, RequiredWorkers = 1, IsAvailable = available }],
            SelectedCityId = "a",
            SelectedRegionId = "r"
        };
    }

    private static City City(string id, int population) => new(id, id, population, 0m, 100m, 50, 50, 10, 0m, 0m, CityState.Growing);

    private static void SetStock(City city, TradeGoodType good, decimal stock)
    {
        switch (good)
        {
            case TradeGoodType.Food: city.Food = stock; break;
            case TradeGoodType.Goods: city.Goods = stock; break;
            case TradeGoodType.Resources: city.Resources = stock; break;
        }
    }
}
