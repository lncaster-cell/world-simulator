namespace WorldSimulator.RoutePathExtractor;

public sealed class RouteExtractionOptions
{
    public int AnchorSearchRadiusSoftPx { get; init; } = 40;
    public int AnchorSearchRadiusHardPx { get; init; } = 160;
    public int BaseSearchMarginPx { get; init; } = 120;
    public int ExtendedSearchMarginPx { get; init; } = 220;
    public int MaskInflateRadiusPx { get; init; } = 6;
    public int ConnectorMaxDistancePx { get; init; } = 120;
    public int ConnectorSoftWarningPx { get; init; } = 40;
    public int DirectGavernConnectorMaxPx { get; init; } = 320;
    public int ForcedDirectGavernConnectorMaxPx { get; init; } = 800;
    public int MaxExpandedNodesPerRoute { get; init; } = 200_000;
    public int MaxRoutePathfindingSeconds { get; init; } = 15;
    public ISet<string> DirectConnectorSettlementOverrides { get; init; } = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "gavern" };
    public ISet<string> ForcedDirectGavernRouteIds { get; init; } = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
    {
        "R_WARDMARK_GAVERN",
        "R_RIVENSTAL_GAVERN",
        "R_BRNO_GAVERN"
    };
}
