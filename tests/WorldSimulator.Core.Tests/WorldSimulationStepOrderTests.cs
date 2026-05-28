using WorldSimulator.Core.Cities;
using WorldSimulator.Core.Events;
using WorldSimulator.Core.Resources;
using WorldSimulator.Core.Simulation;
using WorldSimulator.Core.Trade;
using WorldSimulator.Core.World;
using Xunit;

namespace WorldSimulator.Core.Tests;

public sealed class WorldSimulationStepOrderTests
{
    [Fact]
    public void CreateDefault_AssignsExpectedDefaultCadenceGroups()
    {
        var order = CreateOrder();

        Assert.Equal(5, order.DailyPreCadenceCitySteps.Count);
        Assert.Single(order.WeeklyCitySteps);
        Assert.Empty(order.MonthlyCitySteps);
        Assert.Empty(order.HalfYearlyCitySteps);
        Assert.Empty(order.YearlyCitySteps);
        Assert.Single(order.DailyPostCadenceCitySteps);
    }

    [Fact]
    public void GetRunnableCitySteps_OnNormalDay_ReturnsOnlyDailySteps()
    {
        var order = CreateOrder();

        var steps = order.GetRunnableCitySteps(1);

        Assert.Equal(order.DailyPreCadenceCitySteps.Count + order.DailyPostCadenceCitySteps.Count, steps.Count);
        Assert.DoesNotContain(steps, step => step is CrimeSimulationStep);
        Assert.IsType<PopulationSimulationStep>(steps[^1]);
    }

    [Fact]
    public void GetRunnableCitySteps_OnWeeklyDay_IncludesWeeklyStepsBeforePostDailySteps()
    {
        var order = CreateOrder();

        var steps = order.GetRunnableCitySteps(7);

        Assert.Equal(order.DailyPreCadenceCitySteps.Count + order.WeeklyCitySteps.Count + order.DailyPostCadenceCitySteps.Count, steps.Count);
        Assert.IsType<CrimeSimulationStep>(steps[^2]);
        Assert.IsType<PopulationSimulationStep>(steps[^1]);
    }

    [Fact]
    public void GetCityStepsForCadence_ReturnsCadenceSpecificLists()
    {
        var order = CreateOrder();

        Assert.Equal(
            order.DailyPreCadenceCitySteps.Concat(order.DailyPostCadenceCitySteps),
            order.GetCityStepsForCadence(SimulationCadence.Daily));
        Assert.Same(order.WeeklyCitySteps, order.GetCityStepsForCadence(SimulationCadence.Weekly));
        Assert.Same(order.MonthlyCitySteps, order.GetCityStepsForCadence(SimulationCadence.Monthly));
        Assert.Same(order.HalfYearlyCitySteps, order.GetCityStepsForCadence(SimulationCadence.HalfYearly));
        Assert.Same(order.YearlyCitySteps, order.GetCityStepsForCadence(SimulationCadence.Yearly));
    }

    private static WorldSimulationStepOrder CreateOrder()
    {
        return WorldSimulationStepOrder.CreateDefault(
            new TradeSimulationStep(new WorldTradeFlowService(), new CaravanHiringService()),
            new CityEventSimulationStep(new CityEventEffectCalculator(), new CityEventGenerator(new FakeRandomProvider())),
            new WorkforceSimulationStep(new WorldSimulator.Core.Workforce.CityWorkforceAllocator(), new WorldSimulator.Core.Workforce.WorkforceLawProfile()),
            new FoodSimulationStep(
                new DailyFoodFlowCalculator(),
                new FishingProductionCalculator(),
                new HuntingProductionCalculator(),
                new AgricultureProductionCalculator(),
                new MainlandSupplyProductionCalculator()),
            new WealthSimulationStep(
                new ResourceGatheringProductionCalculator(),
                new GoodsCraftingProductionCalculator(),
                new HouseholdConsumptionCalculator(),
                new DailyWealthFlowCalculator()),
            new CityStateSimulationStep(new CityStateEvaluator()),
            new CrimeSimulationStep(new HouseholdConsumptionCalculator(), new WeeklyCrimeFlowCalculator()),
            new PopulationSimulationStep(new PopulationChangeCalculator()),
            new SimulationCadenceResolver());
    }

    private sealed class FakeRandomProvider : IRandomProvider
    {
        public double NextDouble() => 1.0;
        public int NextInt(int maxExclusive) => 0;
    }
}
