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
    private readonly IReadOnlyList<IWorldSimulationStep> _dailyPreCadenceCitySteps;
    private readonly IReadOnlyList<IWorldSimulationStep> _weeklyCitySteps;
    private readonly IReadOnlyList<IWorldSimulationStep> _monthlyCitySteps;
    private readonly IReadOnlyList<IWorldSimulationStep> _halfYearlyCitySteps;
    private readonly IReadOnlyList<IWorldSimulationStep> _yearlyCitySteps;
    private readonly IReadOnlyList<IWorldSimulationStep> _dailyPostCadenceCitySteps;
    private readonly SimulationCadenceResolver _cadenceResolver;

    public WorldSimulationStepOrder(
        TradeSimulationStep tradeSimulationStep,
        IReadOnlyList<IWorldSimulationStep> dailyPreCadenceCitySteps,
        IReadOnlyList<IWorldSimulationStep> weeklyCitySteps,
        IReadOnlyList<IWorldSimulationStep> monthlyCitySteps,
        IReadOnlyList<IWorldSimulationStep> halfYearlyCitySteps,
        IReadOnlyList<IWorldSimulationStep> yearlyCitySteps,
        IReadOnlyList<IWorldSimulationStep> dailyPostCadenceCitySteps,
        SimulationCadenceResolver cadenceResolver)
    {
        _tradeSimulationStep = tradeSimulationStep;
        _dailyPreCadenceCitySteps = dailyPreCadenceCitySteps;
        _weeklyCitySteps = weeklyCitySteps;
        _monthlyCitySteps = monthlyCitySteps;
        _halfYearlyCitySteps = halfYearlyCitySteps;
        _yearlyCitySteps = yearlyCitySteps;
        _dailyPostCadenceCitySteps = dailyPostCadenceCitySteps;
        _cadenceResolver = cadenceResolver;
    }

    public IReadOnlyList<IWorldSimulationStep> DailyPreCadenceCitySteps => _dailyPreCadenceCitySteps;
    public IReadOnlyList<IWorldSimulationStep> WeeklyCitySteps => _weeklyCitySteps;
    public IReadOnlyList<IWorldSimulationStep> MonthlyCitySteps => _monthlyCitySteps;
    public IReadOnlyList<IWorldSimulationStep> HalfYearlyCitySteps => _halfYearlyCitySteps;
    public IReadOnlyList<IWorldSimulationStep> YearlyCitySteps => _yearlyCitySteps;
    public IReadOnlyList<IWorldSimulationStep> DailyPostCadenceCitySteps => _dailyPostCadenceCitySteps;

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
                cityStateSimulationStep
            },
            new IWorldSimulationStep[]
            {
                crimeSimulationStep
            },
            Array.Empty<IWorldSimulationStep>(),
            Array.Empty<IWorldSimulationStep>(),
            Array.Empty<IWorldSimulationStep>(),
            new IWorldSimulationStep[]
            {
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

    public IReadOnlyList<IWorldSimulationStep> GetCityStepsForCadence(SimulationCadence cadence) => cadence switch
    {
        SimulationCadence.Daily => _dailyPreCadenceCitySteps.Concat(_dailyPostCadenceCitySteps).ToList(),
        SimulationCadence.Weekly => _weeklyCitySteps,
        SimulationCadence.Monthly => _monthlyCitySteps,
        SimulationCadence.HalfYearly => _halfYearlyCitySteps,
        SimulationCadence.Yearly => _yearlyCitySteps,
        _ => throw new ArgumentOutOfRangeException(nameof(cadence), cadence, "Unsupported simulation cadence.")
    };

    public IReadOnlyList<IWorldSimulationStep> GetRunnableCitySteps(int day)
    {
        var steps = new List<IWorldSimulationStep>();
        steps.AddRange(_dailyPreCadenceCitySteps);
        AddIfRunnable(steps, day, SimulationCadence.Weekly);
        AddIfRunnable(steps, day, SimulationCadence.Monthly);
        AddIfRunnable(steps, day, SimulationCadence.HalfYearly);
        AddIfRunnable(steps, day, SimulationCadence.Yearly);
        steps.AddRange(_dailyPostCadenceCitySteps);
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
