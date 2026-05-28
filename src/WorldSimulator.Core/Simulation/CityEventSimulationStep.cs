using WorldSimulator.Core.Cities;
using WorldSimulator.Core.Events;
using WorldSimulator.Core.World;

namespace WorldSimulator.Core.Simulation;

public sealed class CityEventSimulationStep : IWorldSimulationStep
{
    private readonly CityEventEffectCalculator _eventEffectCalculator;
    private readonly CityEventGenerator _eventGenerator;

    public CityEventSimulationStep(
        CityEventEffectCalculator eventEffectCalculator,
        CityEventGenerator eventGenerator)
    {
        _eventEffectCalculator = eventEffectCalculator;
        _eventGenerator = eventGenerator;
    }

    public void Execute(SimulationWorld world, City city, int day, WorldSimulationContext context, WorldSimulationStepDelegate next)
    {
        var state = context.GetCityState(city);
        var eventManager = context.GetOrCreateCityEventManager(city.Id);
        state.EventManager = eventManager;
        state.EventEffects = _eventEffectCalculator.Calculate(city, eventManager.ActiveEvents);
        state.ActiveEvents = eventManager.ActiveEvents;

        next();

        eventManager.AdvanceDay();

        if (context.RandomEventsEnabled && city.Population > 0 && city.CityState != CityState.Abandoned)
        {
            var generationResult = _eventGenerator.TryGenerate(day, eventManager.ActiveEvents);
            if (generationResult.WasGenerated && generationResult.Event is not null)
            {
                eventManager.AddEvent(generationResult.Event);
            }
        }
    }
}
