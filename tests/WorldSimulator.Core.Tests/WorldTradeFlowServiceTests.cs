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

        var result = _service.RunWeeklyTrade(world, currentDay: 7);

        world.TradeShipments.Should().ContainSingle();
        world.TradeShipments[0].Status.Should().Be(TradeShipmentStatus.InTransitToDestination);
        world.FindCity("b")!.Food.Should().Be(0m);
        result.Transfers.Should().ContainSingle(t =>
            t.RouteId == "route1"
            && t.ExporterCityId == "a"
            && t.ImporterCityId == "b"
            && t.CaravanId == "c1"
            && t.GoodType == TradeGoodType.Food
            && t.AmountTransferred == 10m);
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
        world.TradeShipments[0].Status.Should().Be(TradeShipmentStatus.InTransitToDestination);

        _service.ProcessShipments(world, currentDay: 10);
        world.FindCity("b")!.Food.Should().Be(10m);
        world.TradeShipments[0].Status.Should().Be(TradeShipmentStatus.DeliveredReturning);
        world.Caravans[0].Status.Should().Be(CaravanStatus.Returning);
    }

    [Fact]
    public void ActiveShipment_BlocksCaravanReuse_UntilReturnDay()
    {
        var world = BuildWorld(TradeGoodType.Food, 500m, 0m, 10m, travelDays: 2);

        _service.RunWeeklyTrade(world, currentDay: 7);
        _service.RunWeeklyTrade(world, currentDay: 14);
        world.TradeShipments.Should().HaveCount(1);

        _service.ProcessShipments(world, currentDay: 11);
        world.TradeShipments[0].Status.Should().Be(TradeShipmentStatus.Completed);
        world.Caravans[0].Status.Should().Be(CaravanStatus.Idle);

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
    public void WeeklyTradeResult_AccumulatesSettlementTotalsAndWealthDeltas()
    {
        var world = BuildWorld(TradeGoodType.Food, 300m, 0m, 10m);

        var result = _service.RunWeeklyTrade(world, currentDay: 7);

        result.TotalFoodMoved.Should().Be(10m);
        result.TotalGoodsMoved.Should().Be(0m);
        result.TotalResourcesMoved.Should().Be(0m);
        result.TotalExporterWealthGain.Should().Be(0.2m);
        result.TotalImporterWealthCost.Should().Be(0.2m);
        result.SettlementResults["a"].Should().Be(new SettlementTradeFlowResult("a", 10m, 0m, 0m, 0m, 0m, 0m, 0.2m));
        result.SettlementResults["b"].Should().Be(new SettlementTradeFlowResult("b", 0m, 10m, 0m, 0m, 0m, 0m, -0.2m));
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
            TradeRoutes = [new TradeRoute { Id = "route1", FromSettlementId = "a", ToSettlementId = "b", Type = routeType, Distance = 1m, TravelDays = travelDays, DistanceDays = 1m, IsEnabled = routeEnabled, DifficultyMultiplier = 1m, Points = [] }],
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
