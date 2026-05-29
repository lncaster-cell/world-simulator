using FluentAssertions;
using WorldSimulator.Core.Cities;
using WorldSimulator.Core.Events;
using WorldSimulator.Core.Resources;
using WorldSimulator.Core.Simulation;
using WorldSimulator.Core.World;
using WorldSimulator.Core.Trade;

namespace WorldSimulator.Core.Tests;

public sealed class WorldSimulationServiceTests
{
    [Fact]
    public void AdvanceDay_UpdatesSelectedAndNonSelectedCities()
    {
        var world = WorldPresets.CreateDefaultWorld();
        var selected = world.Cities[0];
        var nonSelected = world.Cities[1];
        var selectedFoodBefore = selected.Food;
        var nonSelectedFoodBefore = nonSelected.Food;

        CreateService().AdvanceDay(world, selected.Id, day: 1, randomEventsEnabled: false);

        selected.Food.Should().NotBe(selectedFoodBefore);
        nonSelected.Food.Should().NotBe(nonSelectedFoodBefore);
    }

    [Fact]
    public void AdvanceDay_AppliesWeeklyCrimeFlowToNonSelectedCity()
    {
        var world = WorldPresets.CreateDefaultWorld();
        var selected = world.Cities[0];
        var nonSelected = world.Cities[1];
        var crimeBefore = nonSelected.Crime;

        CreateService().AdvanceDay(world, selected.Id, day: 7, randomEventsEnabled: false);

        nonSelected.Crime.Should().NotBe(crimeBefore);
    }

    [Fact]
    public void AdvanceDay_DoesNotApplyWeeklyCrimeFlowOnNonWeeklyDay()
    {
        var world = WorldPresets.CreateDefaultWorld();
        var selected = world.Cities[0];
        var nonSelected = world.Cities[1];
        var crimeBefore = nonSelected.Crime;

        CreateService().AdvanceDay(world, selected.Id, day: 1, randomEventsEnabled: false);

        nonSelected.Crime.Should().Be(crimeBefore);
    }

    [Fact]
    public void AdvanceDay_DoesNotApplyPopulationChangeOnNonMonthlyDay()
    {
        var world = WorldPresets.CreateDefaultWorld();
        var selected = world.Cities[0];
        var nonSelected = world.Cities[1];
        nonSelected.Food = 0m;
        var populationBefore = nonSelected.Population;

        var result = CreateService().AdvanceDay(world, selected.Id, day: 1, randomEventsEnabled: false);

        nonSelected.Population.Should().Be(populationBefore);
        result.SelectedCityPopulationChange.Should().BeNull();
    }

    [Fact]
    public void AdvanceDay_AppliesPopulationChangeOnMonthlyDay()
    {
        var world = WorldPresets.CreateDefaultWorld();
        var selected = world.Cities[0];
        var nonSelected = world.Cities[1];
        nonSelected.Food = 0m;
        var populationBefore = nonSelected.Population;

        CreateService().AdvanceDay(world, selected.Id, day: 30, randomEventsEnabled: false);

        nonSelected.Population.Should().BeLessThan(populationBefore);
    }

    [Fact]
    public void AdvanceDay_SynchronizesDemographicsAfterMonthlyPopulationChange()
    {
        var world = WorldPresets.CreateDefaultWorld();
        var selected = world.Cities[0];
        var nonSelected = world.Cities[1];
        nonSelected.Food = 0m;

        CreateService().AdvanceDay(world, selected.Id, day: 30, randomEventsEnabled: false);

        nonSelected.Demographics.TotalPopulation.Should().Be(nonSelected.Population);
    }

    [Fact]
    public void AdvanceDay_UsesPerCityEventsWithoutLeakBetweenCities()
    {
        var world = WorldPresets.CreateDefaultWorld();
        var selected = world.Cities[0];
        var nonSelected = world.Cities[1];

        var selectedManager = new CityEventManager();
        selectedManager.AddEvent(CityEventPresets.CreateFire(1));
        var service = CreateService(selectedManager);

        var selectedWealthBefore = selected.Wealth;
        var nonSelectedWealthBefore = nonSelected.Wealth;

        service.AdvanceDay(world, selected.Id, day: 1, randomEventsEnabled: false);

        (selectedWealthBefore - selected.Wealth).Should().BeGreaterThan(nonSelectedWealthBefore - nonSelected.Wealth);
    }

    [Fact]
    public void AdvanceDay_ReturnsCompletedEventsOnlyOnCompletionDay_AndKeepsCompletedHistory()
    {
        var world = WorldPresets.CreateDefaultWorld();
        var selected = world.Cities[0];
        var eventManager = new CityEventManager();
        eventManager.AddEvent(new CityEvent("one-day", "One Day Event", "Completes after one day.", startedDay: 1, durationDays: 1));
        var service = CreateService(eventManager);

        var completionDayResult = service.AdvanceDay(world, selected.Id, day: 1, randomEventsEnabled: false);
        var nextDayResult = service.AdvanceDay(world, selected.Id, day: 2, randomEventsEnabled: false);
        var selectedCityEventHistory = service.ExportEventState().GetManagerOrEmpty(selected.Id).CompletedEvents;

        completionDayResult.CompletedEvents.Should().ContainSingle(e => e.Id == "one-day");
        nextDayResult.CompletedEvents.Should().BeEmpty();
        selectedCityEventHistory.Should().ContainSingle(e => e.Id == "one-day");
    }

    [Fact]
    public void AdvanceDay_SelectedCityIdChangesOnlyReturnedSelection()
    {
        var worldA = WorldPresets.CreateDefaultWorld();
        var worldB = WorldPresets.CreateDefaultWorld();

        var serviceA = CreateService();
        var serviceB = CreateService();

        var firstResult = serviceA.AdvanceDay(worldA, worldA.Cities[0].Id, day: 1, randomEventsEnabled: false);
        var secondResult = serviceB.AdvanceDay(worldB, worldB.Cities[1].Id, day: 1, randomEventsEnabled: false);

        worldA.Cities.Select(c => (c.Id, c.Population, c.Food, c.Wealth, c.Crime, c.Resources, c.Goods))
            .Should().BeEquivalentTo(worldB.Cities.Select(c => (c.Id, c.Population, c.Food, c.Wealth, c.Crime, c.Resources, c.Goods)));

        firstResult.SelectedCityResult!.CityId.Should().Be(worldA.Cities[0].Id);
        secondResult.SelectedCityResult!.CityId.Should().Be(worldB.Cities[1].Id);
    }

    [Fact]
    public void AdvanceDay_ReturnsSelectedCityWorkforceAllocation()
    {
        var world = WorldPresets.CreateDefaultWorld();
        var selected = world.Cities[0];

        var result = CreateService().AdvanceDay(world, selected.Id, day: 1, randomEventsEnabled: false);

        result.SelectedCityResult.Should().NotBeNull();
        result.SelectedCityResult!.WorkforceAllocation.Should().NotBeNull();
        result.SelectedCityResult.WorkforceAllocation!.Workforce.TotalWorkers.Should().BeGreaterThan(0);
        result.SelectedCityResult.Agriculture.AssignedWorkers.Should().Be(result.SelectedCityResult.WorkforceAllocation.AgricultureWorkers);
    }

    [Fact]
    public void AdvanceDay_UsesWorkforceAllocationForResourceAndGoodsProduction()
    {
        var world = WorldPresets.CreateDefaultWorld();
        var selected = world.Cities[0];

        var result = CreateService().AdvanceDay(world, selected.Id, day: 1, randomEventsEnabled: false);

        result.SelectedCityResult.Should().NotBeNull();
        var cityResult = result.SelectedCityResult!;
        cityResult.WorkforceAllocation.Should().NotBeNull();
        cityResult.ResourceGathering.AssignedWorkers.Should().Be(cityResult.WorkforceAllocation!.ResourceGatheringWorkers);
        cityResult.GoodsCrafting.AssignedWorkers.Should().Be(cityResult.WorkforceAllocation.CraftingWorkers);
    }

    [Fact]
    public void AdvanceDay_AgriculturalSettlementsOutproduceFortressAndPortAgriculture()
    {
        var outputsByCityId = new[]
        {
            RiviaSettlementPresets.MlynekId,
            RiviaSettlementPresets.BrnoId,
            RiviaSettlementPresets.WodenzId,
            RiviaSettlementPresets.GothaId,
            RiviaSettlementPresets.HighrockId
        }.ToDictionary(
            cityId => cityId,
            cityId =>
            {
                var world = WorldPresets.CreateDefaultWorld();
                var result = CreateService().AdvanceDay(world, cityId, day: 1, randomEventsEnabled: false);
                return result.SelectedCityResult!.Agriculture.FinalOutput;
            });

        outputsByCityId[RiviaSettlementPresets.MlynekId].Should().BeGreaterThan(0m);
        outputsByCityId[RiviaSettlementPresets.BrnoId].Should().BeGreaterThan(0m);
        outputsByCityId[RiviaSettlementPresets.WodenzId].Should().BeGreaterThan(0m);

        var agriculturalMinimum = new[]
        {
            outputsByCityId[RiviaSettlementPresets.MlynekId],
            outputsByCityId[RiviaSettlementPresets.BrnoId],
            outputsByCityId[RiviaSettlementPresets.WodenzId]
        }.Min();
        var nonAgriculturalMaximum = new[]
        {
            outputsByCityId[RiviaSettlementPresets.GothaId],
            outputsByCityId[RiviaSettlementPresets.HighrockId]
        }.Max();

        agriculturalMinimum.Should().BeGreaterThan(nonAgriculturalMaximum);
    }

    [Fact]
    public void AdvanceDay_ReturnsDeterministicExternalDailyTickResult()
    {
        var worldA = WorldPresets.CreateDefaultWorld();
        var worldB = WorldPresets.CreateDefaultWorld();

        var resultA = CreateService().AdvanceDay(worldA, worldA.Cities[0].Id, day: 7, randomEventsEnabled: false);
        var resultB = CreateService().AdvanceDay(worldB, worldB.Cities[0].Id, day: 7, randomEventsEnabled: false);

        ProjectDailyTickResult(resultA).Should().BeEquivalentTo(ProjectDailyTickResult(resultB));
    }

    [Fact]
    public void ExportEventState_ReturnsSnapshotThatDoesNotMutateInternalState()
    {
        var service = CreateService();
        var world = WorldPresets.CreateDefaultWorld();
        var selectedCityId = world.Cities[0].Id;

        service.AdvanceDay(world, selectedCityId, day: 1, randomEventsEnabled: false);

        var snapshot = service.ExportEventState();
        snapshot.GetOrCreateManager(selectedCityId).AddEvent(CityEventPresets.CreateFire(10));

        var internalState = service.ExportEventState();
        internalState.GetManagerOrEmpty(selectedCityId).ActiveEvents.Should().BeEmpty();
    }

    [Fact]
    public void AdvanceDay_SelectedCityIdDoesNotAffectWeeklyTradeExecution()
    {
        var worldA = WorldPresets.CreateDefaultWorld();
        var worldB = WorldPresets.CreateDefaultWorld();

        var serviceA = CreateService();
        var serviceB = CreateService();

        serviceA.AdvanceDay(worldA, worldA.Cities[0].Id, day: 7, randomEventsEnabled: false);
        serviceB.AdvanceDay(worldB, worldB.Cities[1].Id, day: 7, randomEventsEnabled: false);

        worldA.Cities.Select(c => (c.Id, c.Food, c.Goods, c.Resources, c.Wealth))
            .Should().BeEquivalentTo(worldB.Cities.Select(c => (c.Id, c.Food, c.Goods, c.Resources, c.Wealth)));
        worldA.TradeShipments.Select(s => (s.CaravanId, s.RouteId, s.GoodType, s.Amount, s.DepartureDay, s.ArrivalDay, s.ReturnDay, s.Status))
            .Should().BeEquivalentTo(worldB.TradeShipments.Select(s => (s.CaravanId, s.RouteId, s.GoodType, s.Amount, s.DepartureDay, s.ArrivalDay, s.ReturnDay, s.Status)));
    }

    private static object ProjectDailyTickResult(WorldDayAdvanceResult result)
    {
        var selected = result.SelectedCityResult;
        return new
        {
            SelectedCityId = selected?.CityId,
            SelectedCityName = selected?.CityName,
            FoodFlow = selected?.FoodFlow,
            Agriculture = selected?.Agriculture,
            ResourceGathering = selected?.ResourceGathering,
            GoodsCrafting = selected?.GoodsCrafting,
            HouseholdConsumption = selected?.HouseholdConsumption,
            WealthFlow = selected?.WealthFlow,
            WorkforceAllocation = selected?.WorkforceAllocation,
            EventEffects = result.SelectedCityEventEffects,
            PopulationChange = result.SelectedCityPopulationChange,
            CrimeFlow = result.SelectedCityCrimeFlow,
            WeeklyTradeFlow = result.WeeklyTradeFlowResult,
            CompletedEvents = result.CompletedEvents.Select(e => (e.Name, e.StartedDay, e.DurationDays)),
            GeneratedEvent = result.GeneratedEvent is null
                ? null
                : new
                {
                    result.GeneratedEvent.Name,
                    result.GeneratedEvent.StartedDay,
                    result.GeneratedEvent.DurationDays
                },
            result.ActiveEventNamesBeforeAdvance
        };
    }

    private static WorldSimulationService CreateService(CityEventManager? eventManager = null)
    {
        return new WorldSimulationService(
            new DailyFoodFlowCalculator(),
            new FishingProductionCalculator(),
            new HuntingProductionCalculator(),
            new AgricultureProductionCalculator(),
            new MainlandSupplyProductionCalculator(),
            new GoodsCraftingProductionCalculator(),
            new ResourceGatheringProductionCalculator(),
            new HouseholdConsumptionCalculator(),
            new DailyWealthFlowCalculator(),
            new WeeklyCrimeFlowCalculator(),
            new WorldTradeFlowService(),
            new CaravanHiringService(),
            new CityStateEvaluator(),
            new PopulationChangeCalculator(),
            eventManager ?? new CityEventManager(),
            new CityEventEffectCalculator(),
            new CityEventGenerator(new FakeRandomProvider()));
    }

    private sealed class FakeRandomProvider : IRandomProvider
    {
        public double NextDouble() => 1.0;
        public int NextInt(int maxExclusive) => 0;
    }
}
