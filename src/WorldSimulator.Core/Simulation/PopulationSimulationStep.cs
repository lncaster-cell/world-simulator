using WorldSimulator.Core.Cities;
using WorldSimulator.Core.World;

namespace WorldSimulator.Core.Simulation;

public sealed class PopulationSimulationStep : IWorldSimulationStep
{
    private readonly PopulationChangeCalculator _populationChangeCalculator;
    private readonly CityPopulationDemographicsSynchronizer _demographicsSynchronizer;

    public PopulationSimulationStep(PopulationChangeCalculator populationChangeCalculator)
        : this(populationChangeCalculator, new CityPopulationDemographicsSynchronizer())
    {
    }

    public PopulationSimulationStep(
        PopulationChangeCalculator populationChangeCalculator,
        CityPopulationDemographicsSynchronizer demographicsSynchronizer)
    {
        _populationChangeCalculator = populationChangeCalculator;
        _demographicsSynchronizer = demographicsSynchronizer;
    }

    public void Execute(SimulationWorld world, City city, int day, WorldSimulationContext context, WorldSimulationStepDelegate next)
    {
        var state = context.GetCityState(city);
        var populationChange = _populationChangeCalculator.Calculate(city);
        if (populationChange.PopulationDelta != 0)
        {
            city.Population = populationChange.EndingPopulation;
            _demographicsSynchronizer.SynchronizeToPopulation(city.Demographics, city.Population);
        }

        state.PopulationChange = populationChange;
        context.CaptureCityResult(city, state);

        next();
    }
}
