namespace WorldSimulator.Core.Resources;

public sealed class DailyWealthFlowResult
{
    public required decimal StartingWealth { get; init; }
    public required decimal PortTradeBonus { get; init; }
    public required decimal GoodsProductionBonus { get; init; }
    public required decimal ConsumptionCoverageBonus { get; init; }
    public required decimal FoodShortagePenalty { get; init; }
    public required decimal GoodsShortagePenalty { get; init; }
    public required decimal ResourcesShortagePenalty { get; init; }
    public required decimal SecurityModifierDelta { get; init; }
    public required decimal CrimePenalty { get; init; }
    public required decimal CityStateDelta { get; init; }
    public required decimal TotalDelta { get; init; }
    public required decimal EndingWealth { get; init; }
}
