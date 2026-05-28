using WorldSimulator.Core.Cities;
using WorldSimulator.Core.Resources;
using WorldSimulator.Core.World;

namespace WorldSimulator.Core.Simulation;

public sealed class CrimeSimulationStep : IWorldSimulationStep
{
    private readonly HouseholdConsumptionCalculator _householdConsumptionCalculator;
    private readonly WeeklyCrimeFlowCalculator _weeklyCrimeFlowCalculator;

    public CrimeSimulationStep(
        HouseholdConsumptionCalculator householdConsumptionCalculator,
        WeeklyCrimeFlowCalculator weeklyCrimeFlowCalculator)
    {
        _householdConsumptionCalculator = householdConsumptionCalculator;
        _weeklyCrimeFlowCalculator = weeklyCrimeFlowCalculator;
    }

    public void Execute(SimulationWorld world, City city, int day, WorldSimulationContext context, WorldSimulationStepDelegate next)
    {
        if (WorldSimulationStepOrder.IsWeeklyUpdateDay(day))
        {
            var state = context.GetCityState(city);
            var foodFlow = state.FoodFlow ?? throw new InvalidOperationException("Food simulation must run before crime simulation.");
            var householdConsumption = _householdConsumptionCalculator.Calculate(city);
            var crimeFlow = _weeklyCrimeFlowCalculator.Calculate(city, foodFlow, householdConsumption);
            city.Crime = crimeFlow.EndingCrime;
            context.CaptureCrimeFlow(city, crimeFlow);
        }

        next();
    }
}
