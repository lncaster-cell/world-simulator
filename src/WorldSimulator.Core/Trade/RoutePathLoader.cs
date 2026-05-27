using System.Text.Json;

namespace WorldSimulator.Core.Trade;

public enum RoutePathLoadStatus
{
    Found,
    Missing,
    Failed
}

public sealed class RoutePathLoadResult
{
    public RoutePathLoadStatus Status { get; init; }
    public int LoadedPathCount { get; init; }
    public int AppliedRouteCount { get; init; }
    public IReadOnlyList<string> AppliedRouteIds { get; init; } = [];
    public string? ErrorMessage { get; init; }
}

public sealed class RoutePathLoader
{
    public RoutePathLoadResult TryLoadAndApply(string path, IList<TradeRoute> routes)
    {
        if (!File.Exists(path))
        {
            return new RoutePathLoadResult { Status = RoutePathLoadStatus.Missing };
        }

        try
        {
            using var stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite | FileShare.Delete);
            using var doc = JsonDocument.Parse(stream);
            if (!doc.RootElement.TryGetProperty("paths", out var pathsElement) || pathsElement.ValueKind != JsonValueKind.Array)
            {
                return new RoutePathLoadResult { Status = RoutePathLoadStatus.Found };
            }

            var pointsById = new Dictionary<string, List<RoutePoint>>(StringComparer.OrdinalIgnoreCase);
            foreach (var item in pathsElement.EnumerateArray())
            {
                var routeId = item.TryGetProperty("trade_route_id", out var routeIdElement) ? routeIdElement.GetString() : null;
                if (string.IsNullOrWhiteSpace(routeId)) continue;
                if (!item.TryGetProperty("points", out var pointsElement) || pointsElement.ValueKind != JsonValueKind.Array) continue;

                var points = new List<RoutePoint>();
                foreach (var point in pointsElement.EnumerateArray())
                {
                    if (!point.TryGetProperty("x", out var xElement) || !point.TryGetProperty("y", out var yElement)) continue;
                    points.Add(new RoutePoint
                    {
                        X = decimal.Clamp(xElement.GetDecimal(), 0m, 1m),
                        Y = decimal.Clamp(yElement.GetDecimal(), 0m, 1m)
                    });
                }

                if (points.Count >= 2)
                {
                    pointsById[routeId] = points;
                }
            }

            var appliedRouteIds = new List<string>();
            foreach (var route in routes)
            {
                if (!TryResolveRoutePoints(pointsById, route, out var points))
                {
                    continue;
                }

                route.Points = points.Select(p => new RoutePoint { X = p.X, Y = p.Y }).ToList();
                appliedRouteIds.Add(route.Id);
            }

            return new RoutePathLoadResult
            {
                Status = RoutePathLoadStatus.Found,
                LoadedPathCount = pointsById.Count,
                AppliedRouteCount = appliedRouteIds.Count,
                AppliedRouteIds = appliedRouteIds
            };
        }
        catch (IOException ex)
        {
            return new RoutePathLoadResult { Status = RoutePathLoadStatus.Failed, ErrorMessage = ex.Message };
        }
        catch (JsonException ex)
        {
            return new RoutePathLoadResult { Status = RoutePathLoadStatus.Failed, ErrorMessage = ex.Message };
        }
    }

    private static bool TryResolveRoutePoints(IReadOnlyDictionary<string, List<RoutePoint>> pointsByRouteId, TradeRoute route, out List<RoutePoint> points)
    {
        if (pointsByRouteId.TryGetValue(route.Id, out points!)) return true;

        var direct = $"{route.FromSettlementId}_{route.ToSettlementId}";
        if (pointsByRouteId.TryGetValue(direct, out points!)) return true;

        var reverse = $"{route.ToSettlementId}_{route.FromSettlementId}";
        return pointsByRouteId.TryGetValue(reverse, out points!);
    }
}
