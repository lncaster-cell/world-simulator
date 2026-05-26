namespace WorldSimulator.Core.World;

public static class RegionPresets
{
    public const string RiviaRegionId = "rivia_region";

    public static Region CreateRiviaRegion()
    {
        return new Region
        {
            Id = RiviaRegionId,
            DisplayName = "Ривия",
            MapAssetId = "rivia_region_map"
        };
    }
}
