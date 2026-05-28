namespace WorldSimulator.RoutePathExtractor;

internal sealed class GeneratedRoutePath
{
    public required string SourceRouteId { get; init; }
    public required string TradeRouteId { get; init; }
    public required string RouteType { get; init; }
    public required string GenerationMethod { get; init; }
    public required List<string> Warnings { get; init; }
    public required List<GeneratedRoutePoint> Points { get; init; }
    public required List<(int X, int Y)> PixelPoints { get; init; }
    public bool UsedForcedDirectConnector { get; init; }
}

internal readonly record struct GeneratedRoutePoint(double X, double Y);
