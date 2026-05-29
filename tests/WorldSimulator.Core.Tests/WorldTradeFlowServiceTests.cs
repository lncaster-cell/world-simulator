using FluentAssertions;
using WorldSimulator.Core.Cities;
using WorldSimulator.Core.Trade;
using WorldSimulator.Core.World;

namespace WorldSimulator.Core.Tests;

public sealed class WorldTradeFlowServiceTests
{
    [Fact]
    public void RunWeeklyTrade_CreatesFoodShipment_WhenImporterNeedsFood()
    {
        var world = BuildWorld(TradeGoodType.Food, exporterStock: 1000m, importerStock: 0m, caravanCapacity: 50m);

        var result = new WorldTradeFlowService().RunWeeklyTrade(world, currentDay: 7);

        result.Transfers.Should().ContainSingle();
        world.TradeShipments.Should().ContainSingle();
        world.TradeShipments[0].FromSettlementId.Should().Be("a");
        world.TradeShipments[0].ToSettlementId.Should().Be("b");
        world.TradeShipments[0].GoodType.Should().Be(TradeGoodType.Food);
        world.TradeShipments[0].Amount.Should().BeGreaterThan(0m);
    }

    [Fact]
    public void RunWeeklyTrade_CreatesShipmentToOppositeEndpoint_WhenRouteIsStoredReversed()
    {
        var world = BuildWorld(
            TradeGoodType.Resources,
            exporterStock: 1000m,
            importerStock: 0m,
            caravanCapacity: 50m,
            routeReversed: true);

        var result = new WorldTradeFlowService().RunWeeklyTrade(world, currentDay: 7);

        result.Transfers.Should().ContainSingle();
        world.TradeShipments.Should().ContainSingle();
        world.TradeShipments[0].FromSettlementId.Should().Be("a");
        world.TradeShipments[0].ToSettlementId.Should().Be("b");
        world.TradeShipments[0].RouteId.Should().Be("route1");
    }

    [Fact]
    public void RunWeeklyTrade_DoesNotCreateShipment_WhenNoCaravanAvailable()
    {
        var world = BuildWorld(TradeGoodType.Food, exporterStock: 1000m, importerStock: 0m, caravanCapacity: 50m, available: false);

        var result = new WorldTradeFlowService().RunWeeklyTrade(world, currentDay: 7);

        result.Transfers.Should().BeEmpty();
        world.TradeShipments.Should().BeEmpty();
    }

    [Fact]
    public void RunWeeklyTrade_UsesCaravanCapacityLimit()
    {
        var world = BuildWorld(TradeGoodType.Food, exporterStock: 1000m, importerStock: 0m, caravanCapacity: 10m);

        new WorldTradeFlowService().RunWeeklyTrade(world, currentDay: 7);

        world.TradeShipments.Should().ContainSingle();
        world.TradeShipments[0].Amount.Should().BeLessOrEqualTo(10m);
    }

    [Fact]
    public void RunWeeklyTrade_ReducesExporterStockImmediately()
    {
        var world = BuildWorld(TradeGoodType.Food, exporterStock: 1000m, importerStock: 0m, caravanCapacity: 50m);
        var exporter = world.Cities[0];

        new WorldTradeFlowService().RunWeeklyTrade(world, currentDay: 7);

        exporter.Food.Should().BeLessThan(1000m);
    }

    [Fact]
    public void ProcessShipments_AppliesArrivalAndReturn()
    {
        var world = BuildWorld(TradeGoodType.Food, exporterStock: 1000m, importerStock: 0m, caravanCapacity: 50m, travelDays: 2);
        var importer = world.Cities[1];
        new WorldTradeFlowService().RunWeeklyTrade(world, currentDay: 7);
        var shipment = world.TradeShipments[0];

        new WorldTradeFlowService().ProcessShipments(world, shipment.ArrivalDay);
        importer.Food.Should().BeGreaterThan(0m);
        world.Caravans[0].Status.Should().Be(CaravanStatus.Returning);

        new WorldTradeFlowService().ProcessShipments(world, shipment.ReturnDay);
        world.Caravans[0].Status.Should().Be(CaravanStatus.Idle);
    }

    [Fact]
    public void RunWeeklyTrade_RespectsRouteType()
    {
        var world = BuildWorld(
            TradeGoodType.Food,
            exporterStock: 1000m,
            importerStock: 0m,
            caravanCapacity: 50m,
            caravanType: CaravanType.Sea,
            routeType: CaravanType.Land);

        var result = new WorldTradeFlowService().RunWeeklyTrade(world, currentDay: 7);

        result.Transfers.Should().BeEmpty();
        world.TradeShipments.Should().BeEmpty();
    }

    [Fact]
    public void RunWeeklyTrade_DoesNotCreateShipment_WhenRouteDisabled()
    {
        var world = BuildWorld(TradeGoodType.Food, exporterStock: 1000m, importerStock: 0m, caravanCapacity: 50m, routeEnabled: false);

        var result = new WorldTradeFlowService().RunWeeklyTrade(world, currentDay: 7);

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
        CaravanType routeType = CaravanType.Land,
        bool routeReversed = false)
    {
        var cityA = City("a", 100);
        var cityB = City("b", 100, importerWealth);
        SetStock(cityA, good, exporterStock);
        SetStock(cityB, good, importerStock);

        var routeFromSettlementId = routeReversed ? "b" : "a";
        var routeToSettlementId = routeReversed ? "a" : "b";

        return new SimulationWorld
        {
            Cities = [cityA, cityB],
            Regions = [new Region { Id = "r", DisplayName = "R", MapAssetId = "map" }],
            SettlementMapLocations = [],
            SettlementEconomyProfiles = [],
            SettlementSectorCapacityProfiles = [],
            Caravans = [new Caravan { Id = "c1", OwnerSettlementId = "a", Type = caravanType, Capacity = caravanCapacity, RequiredWorkers = 1, IsAvailable = available }],
            TradeRoutes = [new TradeRoute { Id = "route1", FromSettlementId = routeFromSettlementId, ToSettlementId = routeToSettlementId, Type = routeType, Distance = 1m, TravelDays = travelDays, DistanceDays = 1m, IsEnabled = routeEnabled, DifficultyMultiplier = 1m, Points = [] }],
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
