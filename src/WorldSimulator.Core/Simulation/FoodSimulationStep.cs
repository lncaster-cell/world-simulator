using WorldSimulator.Core.Cities;
using WorldSimulator.Core.Resources;
using WorldSimulator.Core.World;

namespace WorldSimulator.Core.Simulation;

public sealed class FoodSimulationStep : IWorldSimulationStep
{
    private readonly DailyFoodFlowCalculator _dailyFoodFlowCalculator;
    private readonly FishingProductionCalculator _fishingProductionCalculator;
    private readonly HuntingProductionCalculator _huntingProductionCalculator;
    private readonly AgricultureProductionCalculator _agricultureProductionCalculator;
    private readonly MainlandSupplyProductionCalculator _mainlandSupplyProductionCalculator;

    public FoodSimulationStep(
        DailyFoodFlowCalculator dailyFoodFlowCalculator,
        FishingProductionCalculator fishingProductionCalculator,
        HuntingProductionCalculator huntingProductionCalculator,
        AgricultureProductionCalculator agricultureProductionCalculator,
        MainlandSupplyProductionCalculator mainlandSupplyProductionCalculator)
    {
        _dailyFoodFlowCalculator = dailyFoodFlowCalculator;
        _fishingProductionCalculator = fishingProductionCalculator;
        _huntingProductionCalculator = huntingProductionCalculator;
        _agricultureProductionCalculator = agricultureProductionCalculator;
        _mainlandSupplyProductionCalculator = mainlandSupplyProductionCalculator;
    }

    public void Execute(SimulationWorld world, City city, int day, WorldSimulationContext context, WorldSimulationStepDelegate next)
    {
        var state = context.GetCityState(city);
        var profile = state.Profile;
        var agriculture = _agricultureProductionCalculator.Calculate(city, profile);
        var fishing = _fishingProductionCalculator.Calculate(city, state.ActiveEvents);
        var hunting = _huntingProductionCalculator.Calculate(city);
        var mainlandSupply = _mainlandSupplyProductionCalculator.Calculate(city, state.ActiveEvents);
        var foodInputs = new DailyFoodFlowInputs
        {
            AgricultureIncome = agriculture.FinalOutput,
            FishingIncome = fishing.FinalOutput * profile.FishingMultiplier,
            HuntingIncome = hunting.FinalOutput * profile.HuntingMultiplier,
            MainlandSupplyIncome = mainlandSupply.FinalOutput * profile.MainlandSupplyMultiplier + state.EventEffects.MainlandSupplyDelta,
            EventDelta = state.EventEffects.FoodDelta
        };
        var foodFlow = _dailyFoodFlowCalculator.Calculate(city, foodInputs);
        _dailyFoodFlowCalculator.Apply(city, foodFlow);

        state.Agriculture = agriculture;
        state.FoodFlow = foodFlow;

        next();
    }
}
