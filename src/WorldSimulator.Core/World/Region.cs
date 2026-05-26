namespace WorldSimulator.Core.World;

public sealed class Region
{
    public required string Id { get; init; }
    public required string DisplayName { get; init; }
    public required string MapAssetId { get; init; }
}
