namespace WorldSimulator.Core.Trade;

public enum TradeShipmentStatus
{
    InTransitToDestination,
    DeliveredReturning,
    Completed
}

public sealed class RoutePoint
{
    public required decimal X { get; init; }
    public required decimal Y { get; init; }
}

public sealed class TradeRoute
{
    public required string Id { get; init; }
    public required string FromSettlementId { get; init; }
    public required string ToSettlementId { get; init; }
    public required CaravanType Type { get; init; }
    public required decimal Distance { get; init; }
    public required int TravelDays { get; init; }
    public required decimal DistanceDays { get; set; }
    public required bool IsEnabled { get; init; }
    public decimal DifficultyMultiplier { get; init; } = 1m;
    public required List<RoutePoint> Points { get; init; }
}

public sealed class TradeShipment
{
    public required string Id { get; init; }
    public required string CaravanId { get; init; }
    public required string RouteId { get; init; }
    public required string FromSettlementId { get; init; }
    public required string ToSettlementId { get; init; }
    public required TradeGoodType GoodType { get; init; }
    public required decimal Amount { get; init; }
    public required int DepartureDay { get; init; }
    public required int ArrivalDay { get; init; }
    public required int ReturnDay { get; init; }
    public required decimal ExporterWealthDelta { get; init; }
    public required decimal ImporterWealthDelta { get; init; }
    public required TradeShipmentStatus Status { get; set; }
}
