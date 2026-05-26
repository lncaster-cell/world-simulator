using FluentAssertions;
using WorldSimulator.Core.Cities;
using WorldSimulator.Core.Trade;
using WorldSimulator.Core.World;

namespace WorldSimulator.Core.Tests;

public sealed class WorldTradeFlowServiceTests
{
    private readonly WorldTradeFlowService _service = new();

    [Fact]
    public void WeeklyTrade_CreatesShipmentInsteadOfInstantDelivery()
    {
        var world = BuildWorld(TradeGoodType.Food, 300m, 0m, 10m);

        _service.RunWeeklyTrade(world, currentDay: 7);

        world.TradeShipments.Should().ContainSingle();
        world.FindCity("b")!.Food.Should().Be(0m);
    }

    [Fact]
    public void ExporterStock_DecreasesOnDeparture()
    {
        var world = BuildWorld(TradeGoodType.Food, 300m, 0m, 10m);

        _service.RunWeeklyTrade(world, currentDay: 7);

        world.FindCity("a")!.Food.Should().Be(290m);
    }

    [Fact]
    public void ImporterStock_IncreasesOnArrivalDayOnly()
    {
        var world = BuildWorld(TradeGoodType.Food, 300m, 0m, 10m, travelDays: 3);

        _service.RunWeeklyTrade(world, currentDay: 7);
        _service.ProcessShipments(world, currentDay: 9);
        world.FindCity("b")!.Food.Should().Be(0m);

        _service.ProcessShipments(world, currentDay: 10);
        world.FindCity("b")!.Food.Should().Be(10m);
    }

    [Fact]
    public void ActiveShipment_BlocksCaravanReuse_UntilReturnDay()
    {
        var world = BuildWorld(TradeGoodType.Food, 500m, 0m, 10m, travelDays: 2);

        _service.RunWeeklyTrade(world, currentDay: 7);
        _service.RunWeeklyTrade(world, currentDay: 14);
        world.TradeShipments.Should().HaveCount(1);

        _service.ProcessShipments(world, currentDay: 11);
        _service.RunWeeklyTrade(world, currentDay: 14);
        world.TradeShipments.Should().HaveCount(2);
    }

    [Fact]
    public void ZeroWealthImporter_CreatesNoShipment()
    {
        var world = BuildWorld(TradeGoodType.Food, 300m, 0m, 10m, importerWealth: 0m);

        var result = _service.RunWeeklyTrade(world, currentDay: 7);

        result.Transfers.Should().BeEmpty();
        world.TradeShipments.Should().BeEmpty();
    }

    [Fact]
    public void PartialWealth_LimitsShipmentAmount()
    {
        var world = BuildWorld(TradeGoodType.Food, 300m, 0m, 10m, importerWealth: 0.1m);

        _service.RunWeeklyTrade(world, currentDay: 7);

        world.TradeShipments.Should().ContainSingle();
        world.TradeShipments[0].Amount.Should().Be(5m);
    }

    [Fact]
    public void DisabledRoute_PreventsShipment()
    {
        var world = BuildWorld(TradeGoodType.Food, 300m, 0m, 10m, routeEnabled: false);

        var result = _service.RunWeeklyTrade(world, currentDay: 7);

        result.Transfers.Should().BeEmpty();
        world.TradeShipments.Should().BeEmpty();
    }

    [Fact]
    public void WrongCaravanType_PreventsShipment()
    {
        var world = BuildWorld(TradeGoodType.Food, 300m, 0m, 10m, caravanType: CaravanType.Sea, routeType: CaravanType.Land);

        var result = _service.RunWeeklyTrade(world, currentDay: 7);

        result.Transfers.Should().BeEmpty();
        world.TradeShipments.Should().BeEmpty();
    }

    private static SimulationWorld BuildWorld(
        TradeGoodType good,
        decimal exporterStock,
        decimal importerStock,
        decimal caravanCapacity,
        bool available = true,
        decimal importerWealth = 100m,
        int travelDays = 2,
        bool routeEnabled = true,
        CaravanType caravanType = CaravanType.Land,
        CaravanType routeType = CaravanType.Land)
    {
        var cityA = City("a", 100);
        var cityB = City("b", 100, importerWealth);
        SetStock(cityA, good, exporterStock);
        SetStock(cityB, good, importerStock);

        return new SimulationWorld
        {
            Cities = [cityA, cityB],
            Regions = [new Region { Id = "r", DisplayName = "R", MapAssetId = "map" }],
            SettlementMapLocations = [],
            SettlementEconomyProfiles = [],
            Caravans = [new Caravan { Id = "c1", OwnerSettlementId = "a", Type = caravanType, Capacity = caravanCapacity, RequiredWorkers = 1, IsAvailable = available }],
            TradeRoutes = [new TradeRoute { Id = "route1", FromSettlementId = "a", ToSettlementId = "b", Type = routeType, Distance = 1m, TravelDays = travelDays, IsEnabled = routeEnabled, DifficultyMultiplier = 1m, Points = [] }],
            TradeShipments = [],
            SelectedCityId = "a",
            SelectedRegionId = "r"
        };
    }

    private static City City(string id, int population, decimal wealth = 100m) => new(id, id, population, 0m, wealth, 50, 50, 10, 0m, 0m, CityState.Prosperous);

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
