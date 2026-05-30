using WorldSimulator.Core.World;

namespace WorldSimulator.Core.Trade;

public sealed class WorldTradeFlowService
{
    private readonly TradeShipmentProcessor _shipmentProcessor = new();
    private readonly WeeklyTradePlanner _weeklyTradePlanner = new();

    public void ProcessShipments(SimulationWorld world, int currentDay)
    {
        _shipmentProcessor.ProcessShipments(world, currentDay);
    }

    public WorldTradeFlowResult RunWeeklyTrade(SimulationWorld world, int currentDay)
    {
        _shipmentProcessor.ProcessShipments(world, currentDay);
        return _weeklyTradePlanner.Execute(world, currentDay);
    }
}
