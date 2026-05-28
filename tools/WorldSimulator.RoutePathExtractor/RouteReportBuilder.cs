using System.Globalization;
using System.Text;

namespace WorldSimulator.RoutePathExtractor;

internal static class RouteReportBuilder
{
    public static string BuildReport(List<RouteReportEntry> entries)
    {
        var sb = new StringBuilder();
        foreach (var e in entries)
        {
            sb.AppendLine($"[{(e.Ok ? "OK" : "FAILED")}] route_id={e.RouteId}");
            sb.AppendLine($"  trade_route_id={e.TradeRouteId}");
            sb.AppendLine($"  route_type={e.RouteType}");
            sb.AppendLine($"  generation_method={e.GenerationMethod}");
            sb.AppendLine($"  start_settlement={e.StartSettlement}");
            sb.AppendLine($"  end_settlement={e.EndSettlement}");
            sb.AppendLine($"  start_anchor_distance_px={(e.StartAnchorDistancePx.HasValue ? RouteGeometry.FormatFixedOne(e.StartAnchorDistancePx.Value) : "n/a")}");
            sb.AppendLine($"  end_anchor_distance_px={(e.EndAnchorDistancePx.HasValue ? RouteGeometry.FormatFixedOne(e.EndAnchorDistancePx.Value) : "n/a")}");
            sb.AppendLine($"  path_pixel_count={(e.PathPixelCount.HasValue ? e.PathPixelCount.Value.ToString(CultureInfo.InvariantCulture) : "n/a")}");
            sb.AppendLine($"  simplified_point_count={(e.SimplifiedPointCount.HasValue ? e.SimplifiedPointCount.Value.ToString(CultureInfo.InvariantCulture) : "n/a")}");
            sb.AppendLine($"  connector_count={e.ConnectorCount}");
            sb.AppendLine($"  max_connector_length_px={RouteGeometry.FormatFixedOne(e.MaxConnectorLengthPx)}");
            sb.AppendLine($"  connector_warning={(string.IsNullOrWhiteSpace(e.ConnectorWarning) ? "none" : e.ConnectorWarning)}");
            sb.AppendLine($"  used_settlement_connector={e.UsedSettlementConnector.ToString().ToLowerInvariant()}");
            sb.AppendLine($"  used_forced_direct_connector={e.UsedForcedDirectConnector.ToString().ToLowerInvariant()}");
            sb.AppendLine($"  forced_connector_length_px={RouteGeometry.FormatFixedOne(e.ForcedConnectorLengthPx)}");
            sb.AppendLine($"  warnings={(e.Warnings.Count == 0 ? "none" : string.Join(" | ", e.Warnings))}");
            sb.AppendLine($"  failure_reason={(string.IsNullOrWhiteSpace(e.FailureReason) ? "none" : e.FailureReason)}");
            sb.AppendLine();
        }

        return sb.ToString();
    }

    public static string DetermineGenerationMethod(PathResult result)
    {
        if (result.UsedForcedDirectConnector) return "forced_connector";
        if (result.UsedSettlementConnector || result.Connectors.Count > 0) return "connector";
        return "mask";
    }
}

internal sealed class RouteReportEntry
{
    public RouteReportEntry(string routeId, string tradeRouteId, string routeType, string startSettlement, string endSettlement)
    {
        RouteId = routeId;
        TradeRouteId = tradeRouteId;
        RouteType = routeType;
        StartSettlement = startSettlement;
        EndSettlement = endSettlement;
    }

    public string RouteId { get; }
    public string TradeRouteId { get; }
    public string RouteType { get; }
    public string StartSettlement { get; set; }
    public string EndSettlement { get; set; }
    public double? StartAnchorDistancePx { get; set; }
    public double? EndAnchorDistancePx { get; set; }
    public int? PathPixelCount { get; set; }
    public int? SimplifiedPointCount { get; set; }
    public List<string> Warnings { get; } = [];
    public int ConnectorCount { get; set; }
    public double MaxConnectorLengthPx { get; set; }
    public string? ConnectorWarning { get; set; }
    public bool UsedSettlementConnector { get; set; }
    public bool UsedForcedDirectConnector { get; set; }
    public double ForcedConnectorLengthPx { get; set; }
    public string GenerationMethod { get; set; } = "not_generated";
    public string? FailureReason { get; private set; }
    public bool Ok { get; set; }

    public void Fail(string reason)
    {
        FailureReason = reason;
        Ok = false;
    }
}
