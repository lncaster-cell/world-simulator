using System.Text.Json;

namespace WorldSimulator.Core.Trade;

public sealed class RoutePathLoadResult
{
    public bool FileFound { get; init; }
    public bool Success { get; init; }
    public int ParsedPathCount { get; init; }
    public int AppliedRouteCount { get; init; }
    public IReadOnlyList<string> AppliedRouteIds { get; init; } = [];
    public IReadOnlyList<string> Diagnostics { get; init; } = [];
    public string? ErrorMessage { get; init; }
}

public sealed class RoutePathLoader
{
    public RoutePathLoadResult TryLoadAndApply(string path, IList<TradeRoute> routes)
    {
        var diagnostics = new List<string>();
        if (!File.Exists(path))
        {
            return new RoutePathLoadResult { FileFound = false, Success = false, Diagnostics = ["route_paths.json missing"] };
        }

        try
        {
            using var stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite | FileShare.Delete);
            using var doc = JsonDocument.Parse(stream);

            if (!doc.RootElement.TryGetProperty("paths", out var pathsElement) || pathsElement.ValueKind != JsonValueKind.Array)
            {
                return new RoutePathLoadResult { FileFound = true, Success = true, Diagnostics = ["paths array missing"] };
            }

            var pointsById = new Dictionary<string, List<RoutePoint>>(StringComparer.OrdinalIgnoreCase);
            foreach (var item in pathsElement.EnumerateArray())
            {
                var routeId = item.TryGetProperty("trade_route_id", out var routeIdElement) ? routeIdElement.GetString() : null;
                if (string.IsNullOrWhiteSpace(routeId))
                {
                    continue;
                }

                if (!item.TryGetProperty("points", out var pointsElement) || pointsElement.ValueKind != JsonValueKind.Array)
                {
                    diagnostics.Add($"ignored {routeId}: points missing");
                    continue;
                }

                var points = new List<RoutePoint>();
                foreach (var point in pointsElement.EnumerateArray())
                {
                    if (!point.TryGetProperty("x", out var xElement) || !point.TryGetProperty("y", out var yElement))
                    {
                        continue;
                    }

                    points.Add(new RoutePoint { X = decimal.Clamp(xElement.GetDecimal(), 0m, 1m), Y = decimal.Clamp(yElement.GetDecimal(), 0m, 1m) });
                }

                if (points.Count >= 2)
                {
                    pointsById[routeId] = points;
                }
            }

            var appliedRouteIds = new List<string>();
            foreach (var route in routes)
            {
                route.HasLoadedPath = false;
                if (!TryResolveRoutePoints(pointsById, route, out var loadedPoints) || loadedPoints.Count < 2)
                {
                    continue;
                }

                route.Points.Clear();
                route.Points.AddRange(loadedPoints.Select(p => new RoutePoint { X = p.X, Y = p.Y }));
                route.HasLoadedPath = true;
                appliedRouteIds.Add(route.Id);
            }

            return new RoutePathLoadResult
            {
                FileFound = true,
                Success = true,
                ParsedPathCount = pointsById.Count,
                AppliedRouteCount = appliedRouteIds.Count,
                AppliedRouteIds = appliedRouteIds,
                Diagnostics = diagnostics
            };
        }
        catch (Exception ex) when (ex is IOException or JsonException or UnauthorizedAccessException)
        {
            return new RoutePathLoadResult
            {
                FileFound = true,
                Success = false,
                ErrorMessage = ex.Message,
                Diagnostics = ["route_paths.json read/parse failed"]
            };
        }
    }

    private static bool TryResolveRoutePoints(IReadOnlyDictionary<string, List<RoutePoint>> pointsByRouteId, TradeRoute route, out List<RoutePoint> points)
    {
        if (pointsByRouteId.TryGetValue(route.Id, out points!)) return true;

        var direct = $"{route.FromSettlementId}_{route.ToSettlementId}";
        if (pointsByRouteId.TryGetValue(direct, out points!)) return true;

        var reverse = $"{route.ToSettlementId}_{route.FromSettlementId}";
        if (pointsByRouteId.TryGetValue(reverse, out var matched))
        {
            points = matched.AsEnumerable().Reverse().ToList();
            return true;
        }

        points = [];
        return false;
    }
}
