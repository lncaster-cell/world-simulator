namespace WorldSimulator.Core.World;

public sealed class SettlementMapLocation
{
    public required string SettlementId { get; init; }
    public required string RegionId { get; init; }
    public required decimal X { get; init; }
    public required decimal Y { get; init; }
}
