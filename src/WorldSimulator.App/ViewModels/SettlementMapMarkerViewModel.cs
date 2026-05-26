namespace WorldSimulator.App.ViewModels;

public sealed class SettlementMapMarkerViewModel
{
    public required string SettlementId { get; init; }
    public required string DisplayName { get; init; }
    public required decimal X { get; init; }
    public required decimal Y { get; init; }
    public required bool IsSelected { get; init; }
}
