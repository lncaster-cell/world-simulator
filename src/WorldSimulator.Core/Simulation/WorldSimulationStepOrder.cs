using WorldSimulator.Core.Trade;
using WorldSimulator.Core.World;

namespace WorldSimulator.Core.Simulation;

public sealed class WorldSimulationStepOrder
{
    // The daily tick order is intentionally centralized here: trade shipments are
    // resolved before per-city simulation, city systems run as an ordered chain,
    // and weekly trade is resolved only after all cities have finished the day.
    private readonly TradeSimulationStep _tradeSimulationStep;
    private readonly IReadOnlyList<IWorldSimulationStep> _citySteps;

    public WorldSimulationStepOrder(
        TradeSimulationStep tradeSimulationStep,
        IReadOnlyList<IWorldSimulationStep> citySteps)
    {
        _tradeSimulationStep = tradeSimulationStep;
        _citySteps = citySteps;
    }

    public IReadOnlyList<IWorldSimulationStep> CitySteps => _citySteps;

    public static WorldSimulationStepOrder CreateDefault(
        TradeSimulationStep tradeSimulationStep,
        CityEventSimulationStep cityEventSimulationStep,
        WorkforceSimulationStep workforceSimulationStep,
        FoodSimulationStep foodSimulationStep,
        WealthSimulationStep wealthSimulationStep,
        CityStateSimulationStep cityStateSimulationStep,
        CrimeSimulationStep crimeSimulationStep,
        PopulationSimulationStep populationSimulationStep)
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
            });
    }

    public void ExecuteBeforeCitySteps(SimulationWorld world, int day)
    {
        _tradeSimulationStep.ProcessDailyShipments(world, day);
    }

    public WorldTradeFlowResult? ExecuteAfterCitySteps(SimulationWorld world, int day)
    {
        return _tradeSimulationStep.RunWeeklyTrade(world, day);
    }

    public static bool IsWeeklyUpdateDay(int day) => day > 0 && day % 7 == 0;
}
