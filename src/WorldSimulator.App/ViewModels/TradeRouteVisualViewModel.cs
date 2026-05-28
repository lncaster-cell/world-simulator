namespace WorldSimulator.App.ViewModels;

public sealed class TradeRouteVisualViewModel
{
    public required string RouteId { get; init; }
    public required string FromSettlementId { get; init; }
    public required string ToSettlementId { get; init; }
    public required string DisplayName { get; init; }
    public required IReadOnlyList<MapPointViewModel> Points { get; init; }
    public required bool IsActive { get; init; }
    public required bool IsLoadedPath { get; init; }
    public required bool IsSeaRoute { get; init; }
    public required MapPointViewModel DebugLabelPoint { get; init; }
    public required decimal WeeklyFoodMoved { get; init; }
    public required decimal WeeklyResourcesMoved { get; init; }
    public required decimal WeeklyGoodsMoved { get; init; }

    public decimal TotalWeeklyVolume => WeeklyFoodMoved + WeeklyResourcesMoved + WeeklyGoodsMoved;
}
