using WorldSimulator.Core.Cities;
using WorldSimulator.Core.World;

namespace WorldSimulator.Core.Simulation;

public sealed class PopulationSimulationStep : IWorldSimulationStep
{
    private readonly PopulationChangeCalculator _populationChangeCalculator;

    public PopulationSimulationStep(PopulationChangeCalculator populationChangeCalculator)
    {
        _populationChangeCalculator = populationChangeCalculator;
    }

    public void Execute(SimulationWorld world, City city, int day, WorldSimulationContext context, WorldSimulationStepDelegate next)
    {
        var state = context.GetCityState(city);
        var populationChange = _populationChangeCalculator.Calculate(city);
        if (populationChange.PopulationDelta != 0)
        {
            city.Population = populationChange.EndingPopulation;
        }

        state.PopulationChange = populationChange;
        context.CaptureCityResult(city, state);

        next();
    }
}
