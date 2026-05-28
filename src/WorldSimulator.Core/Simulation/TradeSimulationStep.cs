using WorldSimulator.Core.Trade;
using WorldSimulator.Core.World;

namespace WorldSimulator.Core.Simulation;

public sealed class TradeSimulationStep
{
    private readonly WorldTradeFlowService _worldTradeFlowService;
    private readonly CaravanHiringService _caravanHiringService;

    public TradeSimulationStep(
        WorldTradeFlowService worldTradeFlowService,
        CaravanHiringService caravanHiringService)
    {
        _worldTradeFlowService = worldTradeFlowService;
        _caravanHiringService = caravanHiringService;
    }

    public void ProcessDailyShipments(SimulationWorld world, int day)
    {
        _worldTradeFlowService.ProcessShipments(world, day);
    }

    public WorldTradeFlowResult? RunWeeklyTrade(SimulationWorld world, int day, SimulationCadenceResolver cadenceResolver)
    {
        if (!cadenceResolver.ShouldRun(day, SimulationCadence.Weekly))
        {
            return null;
        }

        _caravanHiringService.EvaluateAndHire(world);
        return _worldTradeFlowService.RunWeeklyTrade(world, day);
    }
}
