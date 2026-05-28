namespace WorldSimulator.RoutePathExtractor;

internal sealed class PathResult
{
    public bool Success { get; }
    public List<(int X, int Y)> Path { get; }
    public string? FailureReason { get; }
    public List<double> Connectors { get; }
    public double MaxConnectorLengthPx { get; }
    public bool UsedSettlementConnector { get; }
    public double DirectConnectorLengthPx { get; }
    public List<string> Warnings { get; } = [];
    public bool UsedForcedDirectConnector { get; }

    private PathResult(bool success, List<(int X, int Y)> path, string? failureReason, List<double>? connectors = null, bool usedSettlementConnector = false, double directConnectorLengthPx = 0, bool usedForcedDirectConnector = false)
    {
        Success = success;
        Path = path;
        FailureReason = failureReason;
        Connectors = connectors ?? [];
        MaxConnectorLengthPx = Connectors.Count == 0 ? 0.0 : Connectors.Max();
        UsedSettlementConnector = usedSettlementConnector;
        DirectConnectorLengthPx = directConnectorLengthPx;
        UsedForcedDirectConnector = usedForcedDirectConnector;
    }

    public static PathResult Ok(List<(int X, int Y)> path, List<double>? connectors = null, bool usedSettlementConnector = false, double directConnectorLengthPx = 0, bool usedForcedDirectConnector = false)
        => new(true, path, null, connectors, usedSettlementConnector, directConnectorLengthPx, usedForcedDirectConnector);

    public static PathResult Failed(string reason) => new(false, [], reason);
}
