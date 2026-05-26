namespace WorldSimulator.App.ViewModels;

public sealed class CaravanMovementMarkerViewModel
{
    public required string RouteId { get; init; }
    public required string DisplayName { get; init; }
    public required IReadOnlyList<MapPointViewModel> Points { get; init; }
    public required double Progress { get; init; }
    public required decimal FoodMoved { get; init; }
    public required decimal ResourcesMoved { get; init; }
    public required decimal GoodsMoved { get; init; }

    public decimal TotalVolume => FoodMoved + ResourcesMoved + GoodsMoved;
}
