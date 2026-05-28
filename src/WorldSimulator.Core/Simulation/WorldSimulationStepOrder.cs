using WorldSimulator.Core.Trade;
using WorldSimulator.Core.World;

namespace WorldSimulator.Core.Simulation;

public sealed class WorldSimulationStepOrder
{
    // The tick order is intentionally centralized here: daily shipments are
    // resolved before per-city simulation, city systems run in cadence-specific
    // chains, and longer cadence world steps run only when the cadence resolver
    // allows it.
    private readonly TradeSimulationStep _tradeSimulationStep;
    private readonly IReadOnlyList<IWorldSimulationStep> _dailyCitySteps;
    private readonly IReadOnlyList<IWorldSimulationStep> _weeklyCitySteps;
    private readonly IReadOnlyList<IWorldSimulationStep> _monthlyCitySteps;
    private readonly IReadOnlyList<IWorldSimulationStep> _halfYearlyCitySteps;
    private readonly IReadOnlyList<IWorldSimulationStep> _yearlyCitySteps;
    private readonly SimulationCadenceResolver _cadenceResolver;

    public WorldSimulationStepOrder(
        TradeSimulationStep tradeSimulationStep,
        IReadOnlyList<IWorldSimulationStep> dailyCitySteps,
        IReadOnlyList<IWorldSimulationStep> weeklyCitySteps,
        IReadOnlyList<IWorldSimulationStep> monthlyCitySteps,
        IReadOnlyList<IWorldSimulationStep> halfYearlyCitySteps,
        IReadOnlyList<IWorldSimulationStep> yearlyCitySteps,
        SimulationCadenceResolver cadenceResolver)
    {
        _tradeSimulationStep = tradeSimulationStep;
        _dailyCitySteps = dailyCitySteps;
        _weeklyCitySteps = weeklyCitySteps;
        _monthlyCitySteps = monthlyCitySteps;
        _halfYearlyCitySteps = halfYearlyCitySteps;
        _yearlyCitySteps = yearlyCitySteps;
        _cadenceResolver = cadenceResolver;
    }

    public IReadOnlyList<IWorldSimulationStep> DailyCitySteps => _dailyCitySteps;
    public IReadOnlyList<IWorldSimulationStep> WeeklyCitySteps => _weeklyCitySteps;
    public IReadOnlyList<IWorldSimulationStep> MonthlyCitySteps => _monthlyCitySteps;
    public IReadOnlyList<IWorldSimulationStep> HalfYearlyCitySteps => _halfYearlyCitySteps;
    public IReadOnlyList<IWorldSimulationStep> YearlyCitySteps => _yearlyCitySteps;

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
                populationSimulationStep
            },
            new IWorldSimulationStep[]
            {
                crimeSimulationStep
            },
            Array.Empty<IWorldSimulationStep>(),
            Array.Empty<IWorldSimulationStep>(),
            Array.Empty<IWorldSimulationStep>(),
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

    public IReadOnlyList<IWorldSimulationStep> GetCityStepsForCadence(SimulationCadence cadence) => cadence switch
    {
        SimulationCadence.Daily => _dailyCitySteps,
        SimulationCadence.Weekly => _weeklyCitySteps,
        SimulationCadence.Monthly => _monthlyCitySteps,
        SimulationCadence.HalfYearly => _halfYearlyCitySteps,
        SimulationCadence.Yearly => _yearlyCitySteps,
        _ => throw new ArgumentOutOfRangeException(nameof(cadence), cadence, "Unsupported simulation cadence.")
    };

    public IReadOnlyList<IWorldSimulationStep> GetRunnableCitySteps(int day)
    {
        var steps = new List<IWorldSimulationStep>();
        AddIfRunnable(steps, day, SimulationCadence.Daily);
        AddIfRunnable(steps, day, SimulationCadence.Weekly);
        AddIfRunnable(steps, day, SimulationCadence.Monthly);
        AddIfRunnable(steps, day, SimulationCadence.HalfYearly);
        AddIfRunnable(steps, day, SimulationCadence.Yearly);
        return steps;
    }

    private void AddIfRunnable(List<IWorldSimulationStep> steps, int day, SimulationCadence cadence)
    {
        if (!_cadenceResolver.ShouldRun(day, cadence))
        {
            return;
        }

        steps.AddRange(GetCityStepsForCadence(cadence));
    }
}
