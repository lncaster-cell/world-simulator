using WorldSimulator.Core.Cities;
using WorldSimulator.Core.Resources;
using WorldSimulator.Core.World;

namespace WorldSimulator.Core.Simulation;

public sealed class WealthSimulationStep : IWorldSimulationStep
{
    private readonly ResourceGatheringProductionCalculator _resourceGatheringProductionCalculator;
    private readonly GoodsCraftingProductionCalculator _goodsCraftingProductionCalculator;
    private readonly HouseholdConsumptionCalculator _householdConsumptionCalculator;
    private readonly DailyWealthFlowCalculator _dailyWealthFlowCalculator;

    public WealthSimulationStep(
        ResourceGatheringProductionCalculator resourceGatheringProductionCalculator,
        GoodsCraftingProductionCalculator goodsCraftingProductionCalculator,
        HouseholdConsumptionCalculator householdConsumptionCalculator,
        DailyWealthFlowCalculator dailyWealthFlowCalculator)
    {
        _resourceGatheringProductionCalculator = resourceGatheringProductionCalculator;
        _goodsCraftingProductionCalculator = goodsCraftingProductionCalculator;
        _householdConsumptionCalculator = householdConsumptionCalculator;
        _dailyWealthFlowCalculator = dailyWealthFlowCalculator;
    }

    public void Execute(SimulationWorld world, City city, int day, WorldSimulationContext context, WorldSimulationStepDelegate next)
    {
        var state = context.GetCityState(city);
        var profile = state.Profile;
        var foodFlow = state.FoodFlow ?? throw new InvalidOperationException("Food simulation must run before wealth simulation.");
        var agriculture = state.Agriculture ?? throw new InvalidOperationException("Food simulation must capture agriculture before wealth simulation.");

        var resourceGathering = _resourceGatheringProductionCalculator.Calculate(city);
        var gatheredResources = decimal.Round(resourceGathering.FinalOutput * profile.ResourceGatheringMultiplier, 2, MidpointRounding.AwayFromZero);
        city.Resources += gatheredResources;

        city.Mood += state.EventEffects.MoodDelta;
        city.Security += state.EventEffects.SecurityDelta;
        city.Crime += state.EventEffects.CrimeDelta;
        city.Wealth += state.EventEffects.WealthDelta;
        city.Resources += state.EventEffects.ResourcesDelta;

        var goodsCrafting = _goodsCraftingProductionCalculator.Calculate(city);
        var goodsProduced = decimal.Round(goodsCrafting.GoodsProduced * profile.GoodsCraftingMultiplier, 2, MidpointRounding.AwayFromZero);
        var resourcesConsumed = decimal.Round(goodsCrafting.ResourcesConsumed * profile.GoodsCraftingMultiplier, 2, MidpointRounding.AwayFromZero);
        resourcesConsumed = Math.Min(resourcesConsumed, city.Resources);
        city.Resources -= resourcesConsumed;
        city.Goods += goodsProduced;

        var scaledGoods = new GoodsCraftingProductionResult
        {
            NaturalPotential = goodsCrafting.NaturalPotential,
            RequiredWorkers = goodsCrafting.RequiredWorkers,
            AssignedWorkers = goodsCrafting.AssignedWorkers,
            WorkerCoverage = goodsCrafting.WorkerCoverage,
            ExtraWorkers = goodsCrafting.ExtraWorkers,
            OverstaffBonus = goodsCrafting.OverstaffBonus,
            MoodModifier = goodsCrafting.MoodModifier,
            SecurityModifier = goodsCrafting.SecurityModifier,
            StateModifier = goodsCrafting.StateModifier,
            ResourceCostPerGoods = goodsCrafting.ResourceCostPerGoods,
            PotentialGoodsOutput = goodsCrafting.PotentialGoodsOutput * profile.GoodsCraftingMultiplier,
            ResourcesNeeded = goodsCrafting.ResourcesNeeded * profile.GoodsCraftingMultiplier,
            ResourcesAvailable = goodsCrafting.ResourcesAvailable,
            ResourcesConsumed = resourcesConsumed,
            GoodsProduced = goodsProduced
        };

        var householdConsumption = _householdConsumptionCalculator.Calculate(city);
        city.Goods -= householdConsumption.GoodsConsumed;
        city.Resources -= householdConsumption.ResourcesConsumed;

        var wealthFlow = _dailyWealthFlowCalculator.Calculate(city, foodFlow, scaledGoods, householdConsumption);
        city.Wealth = wealthFlow.EndingWealth;

        var scaledResourceGathering = new ResourceGatheringProductionResult
        {
            NaturalPotential = resourceGathering.NaturalPotential,
            RequiredWorkers = resourceGathering.RequiredWorkers,
            AssignedWorkers = resourceGathering.AssignedWorkers,
            WorkerCoverage = resourceGathering.WorkerCoverage,
            ExtraWorkers = resourceGathering.ExtraWorkers,
            OverstaffBonus = resourceGathering.OverstaffBonus,
            SecurityModifier = resourceGathering.SecurityModifier,
            StateModifier = resourceGathering.StateModifier,
            FinalOutput = gatheredResources
        };

        state.ResourceGathering = scaledResourceGathering;
        state.GoodsCrafting = scaledGoods;
        state.HouseholdConsumption = householdConsumption;
        state.WealthFlow = wealthFlow;
        state.CityResult = new CityDailySimulationResult(
            city.Id,
            city.Name,
            foodFlow,
            agriculture,
            scaledResourceGathering,
            scaledGoods,
            householdConsumption,
            wealthFlow,
            state.WorkforceAllocation);

        next();
    }
}
