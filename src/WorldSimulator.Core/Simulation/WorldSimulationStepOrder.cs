using WorldSimulator.Core.Trade;
using WorldSimulator.Core.World;

namespace WorldSimulator.Core.Simulation;

public sealed class WorldSimulationStepOrder
{
    // The tick order is intentionally centralized here: daily shipments are
    // resolved before per-city simulation, city systems run as an ordered chain,
    // and longer cadence world steps run only when the cadence resolver allows it.
    private readonly TradeSimulationStep _tradeSimulationStep;
    private readonly IReadOnlyList<IWorldSimulationStep> _citySteps;
    private readonly SimulationCadenceResolver _cadenceResolver;

    public WorldSimulationStepOrder(
        TradeSimulationStep tradeSimulationStep,
        IReadOnlyList<IWorldSimulationStep> citySteps,
        SimulationCadenceResolver cadenceResolver)
    {
        _tradeSimulationStep = tradeSimulationStep;
        _citySteps = citySteps;
        _cadenceResolver = cadenceResolver;
    }

    public IReadOnlyList<IWorldSimulationStep> CitySteps => _citySteps;

    public SimulationCadenceResolver CadenceResolver => _cadenceResolver;

    public static WorldSimulationStepOrder CreateDefault(
        TradeSimulationStep tradeSimulationStep,
        CityEventSimulationStep cityEventSimulationStep,
        WorkforceSimulationStep workforceSimulationStep,
        FoodSimulationStep foodSimulationStep,
        WealthSimulationStep wealthSimulationStep,
        CityStateSimulationStep cityStateSimulationStep,
        CrimeSimulationStep crimeSimulationStep,
        PopulationSimulationStep populationSimulationStep,
        SimulationCadenceResolver cadenceResolver)
    {
        return new WorldSimulationStepOrder(
            tradeSimulationStep,
            new IWorldSimulationStep[]
            {
                cityEventSimulationStep,
                workforceSimulationStep,
                foodSimulationStep,
                wealthSimulationStep,
                cityStateSimulationStep,
                crimeSimulationStep,
                populationSimulationStep
            },
            cadenceResolver);
    }

    public void ExecuteBeforeCitySteps(SimulationWorld world, int day)
    {
        _tradeSimulationStep.ProcessDailyShipments(world, day);
    }

    public WorldTradeFlowResult? ExecuteAfterCitySteps(SimulationWorld world, int day)
    {
        return _tradeSimulationStep.RunWeeklyTrade(world, day, _cadenceResolver);
    }

    public bool ShouldRun(int day, SimulationCadence cadence) => _cadenceResolver.ShouldRun(day, cadence);
}
