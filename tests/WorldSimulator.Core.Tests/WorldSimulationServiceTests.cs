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
    public void AdvanceDay_AppliesPopulationChangeToNonSelectedCity()
    {
        var world = WorldPresets.CreateDefaultWorld();
        var selected = world.Cities[0];
        var nonSelected = world.Cities[1];
        nonSelected.Food = 0m;
        var populationBefore = nonSelected.Population;

        CreateService().AdvanceDay(world, selected.Id, day: 1, randomEventsEnabled: false);

        nonSelected.Population.Should().BeLessThan(populationBefore);
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
