using WorldSimulator.Core.Cities;
using WorldSimulator.Core.World;

namespace WorldSimulator.Core.Simulation;

public sealed class CityStateSimulationStep : IWorldSimulationStep
{
    private readonly CityStateEvaluator _cityStateEvaluator;

    public CityStateSimulationStep(CityStateEvaluator cityStateEvaluator)
    {
        _cityStateEvaluator = cityStateEvaluator;
    }

    public void Execute(SimulationWorld world, City city, int day, WorldSimulationContext context, WorldSimulationStepDelegate next)
    {
        city.CityState = _cityStateEvaluator.Evaluate(city);
        next();
    }
}
