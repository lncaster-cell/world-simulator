using System.Reflection;
using System.Text.Json;
using System.IO;

namespace WorldSimulator.Core.Trade;

public static class TradeRoutePresets
{
    private static readonly Dictionary<string, string> NodeToSettlementIdMap = new(StringComparer.OrdinalIgnoreCase)
    {
        ["N_HIGHROCK"] = "highrock",
        ["N_MLYNEK"] = "mlynek",
        ["N_WARDMARK"] = "wardmark",
        ["N_RIVENSTAL"] = "rivenstal",
        ["N_GAVERN"] = "gavern",
        ["N_BRNO"] = "brno",
        ["N_WODENZ"] = "wodenz",
        ["N_GOTHA"] = "gotha",
        ["N_TOKRUS"] = "thokur_rus"
    };
    public static List<TradeRoute> CreateDefaultRoutes()
    {
        var routes = LoadRoutesFromEmbeddedJson();
        ApplyRoutePathsFromJsonIfExists(routes);
        return routes;
    }



    private static void ApplyRoutePathsFromJsonIfExists(List<TradeRoute> routes)
    {
        var path = Path.Combine(AppContext.BaseDirectory, "data", "regions", "rivia", "routes", "v1", "route_paths.json");
        if (!File.Exists(path)) return;

        using var doc = JsonDocument.Parse(File.ReadAllText(path));
        if (!doc.RootElement.TryGetProperty("paths", out var pathsElement) || pathsElement.ValueKind != JsonValueKind.Array) return;

        var pointsByRouteId = new Dictionary<string, List<RoutePoint>>(StringComparer.OrdinalIgnoreCase);
        foreach (var item in pathsElement.EnumerateArray())
        {
            var tradeRouteId = item.TryGetProperty("trade_route_id", out var tradeRouteIdElement) ? tradeRouteIdElement.GetString() : null;
            if (string.IsNullOrWhiteSpace(tradeRouteId)) continue;
            if (!item.TryGetProperty("points", out var pointsElement) || pointsElement.ValueKind != JsonValueKind.Array) continue;

            var points = new List<RoutePoint>();
            foreach (var point in pointsElement.EnumerateArray())
            {
                var x = point.GetProperty("x").GetDecimal();
                var y = point.GetProperty("y").GetDecimal();
                points.Add(new RoutePoint { X = decimal.Clamp(x, 0m, 1m), Y = decimal.Clamp(y, 0m, 1m) });
            }

            if (points.Count >= 2) pointsByRouteId[tradeRouteId] = points;
        }

        foreach (var route in routes)
        {
            route.Points.Clear();
            if (TryResolveRoutePoints(pointsByRouteId, route, out var points))
            {
                route.Points.AddRange(points);
            }
        }
    }


    private static bool TryResolveRoutePoints(IReadOnlyDictionary<string, List<RoutePoint>> pointsByRouteId, TradeRoute route, out List<RoutePoint> points)
    {
        if (pointsByRouteId.TryGetValue(route.Id, out points!))
        {
            return true;
        }

        var direct = $"{route.FromSettlementId}_{route.ToSettlementId}";
        if (pointsByRouteId.TryGetValue(direct, out points!))
        {
            return true;
        }

        var reverse = $"{route.ToSettlementId}_{route.FromSettlementId}";
        return pointsByRouteId.TryGetValue(reverse, out points!);
    }

    private static List<TradeRoute> LoadRoutesFromEmbeddedJson()
    {
        using var stream = Assembly.GetExecutingAssembly()
            .GetManifestResourceStream("WorldSimulator.Core.Data.rivia_routes.json")
            ?? throw new InvalidOperationException("Embedded route data 'WorldSimulator.Core.Data.rivia_routes.json' was not found.");

        using var doc = JsonDocument.Parse(stream);
        var root = doc.RootElement;

        var nodeToSettlementId = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        foreach (var node in root.GetProperty("nodes").EnumerateArray())
        {
            if (node.GetProperty("is_stub").GetBoolean())
            {
                continue;
            }

            var nodeId = node.GetProperty("node_id").GetString();
            var name = node.GetProperty("name").GetString();
            if (string.IsNullOrWhiteSpace(nodeId) || string.IsNullOrWhiteSpace(name))
            {
                continue;
            }

            if (NodeToSettlementIdMap.TryGetValue(nodeId, out var mapped))
            {
                nodeToSettlementId[nodeId] = mapped;
                continue;
            }

            nodeToSettlementId[nodeId] = ToSettlementId(name);
        }

        var routes = new List<TradeRoute>();
        foreach (var edge in root.GetProperty("edges").EnumerateArray())
        {
            if (edge.GetProperty("is_stub").GetBoolean()) continue;

            var fromNode = edge.GetProperty("from_node").GetString();
            var toNode = edge.GetProperty("to_node").GetString();
            if (string.IsNullOrWhiteSpace(fromNode) || string.IsNullOrWhiteSpace(toNode)) continue;
            if (!nodeToSettlementId.TryGetValue(fromNode, out var fromSettlementId)) continue;
            if (!nodeToSettlementId.TryGetValue(toNode, out var toSettlementId)) continue;

            var routeTypeRaw = edge.GetProperty("route_type").GetString() ?? "road";
            var routeType = routeTypeRaw.Equals("sea", StringComparison.OrdinalIgnoreCase) ? CaravanType.Sea : CaravanType.Land;
            var distanceDays = edge.GetProperty("travel_days").GetDecimal();

            var points = BuildDefaultPoints(fromSettlementId, toSettlementId, routeType);
            routes.Add(new TradeRoute
            {
                Id = BuildRouteId(fromSettlementId, toSettlementId, routeType),
                FromSettlementId = fromSettlementId,
                ToSettlementId = toSettlementId,
                Type = routeType,
                Distance = distanceDays * 30m,
                TravelDays = Math.Max(1, (int)Math.Ceiling(distanceDays)),
                DistanceDays = distanceDays,
                IsEnabled = true,
                Points = points
            });
        }

        return routes;
    }

    private static List<RoutePoint> BuildDefaultPoints(string fromSettlementId, string toSettlementId, CaravanType routeType)
    {
        var key = BuildPairTypeKey(fromSettlementId, toSettlementId, routeType);
        if (CuratedPolylineByPairType.TryGetValue(key, out var curated))
        {
            return curated.Select(p => new RoutePoint { X = p.X, Y = p.Y }).ToList();
        }

        var start = SettlementCoordinates[fromSettlementId];
        var end = SettlementCoordinates[toSettlementId];
        return [new RoutePoint { X = start.X, Y = start.Y }, new RoutePoint { X = end.X, Y = end.Y }];
    }

    private static string BuildRouteId(string fromSettlementId, string toSettlementId, CaravanType routeType)
        => $"{fromSettlementId}_{toSettlementId}_{(routeType == CaravanType.Sea ? "sea" : "land")}";

    private static string BuildPairTypeKey(string a, string b, CaravanType type)
    {
        var pair = string.CompareOrdinal(a, b) <= 0 ? $"{a}|{b}" : $"{b}|{a}";
        return $"{pair}|{(type == CaravanType.Sea ? "sea" : "land")}";
    }

    private static string ToSettlementId(string name)
        => name.ToLowerInvariant().Replace('ö', 'o').Replace('-', '_').Replace(' ', '_') switch
        {
            "tokrus" => "thokur_rus",
            var normalized => normalized
        };

    private static readonly Dictionary<string, RoutePoint> SettlementCoordinates = new(StringComparer.OrdinalIgnoreCase)
    {
        ["gotha"] = Pt(0.6664m, 0.2322m),
        ["rivenstal"] = Pt(0.4824m, 0.4500m),
        ["gavern"] = Pt(0.5066m, 0.5963m),
        ["mlynek"] = Pt(0.2833m, 0.2487m),
        ["brno"] = Pt(0.4527m, 0.7448m),
        ["wodenz"] = Pt(0.8036m, 0.9604m),
        ["wardmark"] = Pt(0.0380m, 0.4027m),
        ["highrock"] = Pt(0.1579m, 0.2179m),
        ["thokur_rus"] = Pt(0.8652m, 0.4753m)
    };

    private static readonly Dictionary<string, List<RoutePoint>> CuratedPolylineByPairType = new(StringComparer.OrdinalIgnoreCase)
    {
        [BuildPairTypeKey("highrock", "mlynek", CaravanType.Land)] = [Pt(0.1579m, 0.2179m), Pt(0.2200m, 0.2300m), Pt(0.2833m, 0.2487m)],
        [BuildPairTypeKey("mlynek", "wardmark", CaravanType.Land)] = [Pt(0.2833m, 0.2487m), Pt(0.1900m, 0.3300m), Pt(0.0380m, 0.4027m)],
        [BuildPairTypeKey("highrock", "wardmark", CaravanType.Land)] = [Pt(0.1579m, 0.2179m), Pt(0.1150m, 0.3100m), Pt(0.0380m, 0.4027m)],
        [BuildPairTypeKey("wardmark", "rivenstal", CaravanType.Land)] = [Pt(0.0380m, 0.4027m), Pt(0.2200m, 0.4300m), Pt(0.4824m, 0.4500m)],
        [BuildPairTypeKey("wardmark", "gavern", CaravanType.Land)] = [Pt(0.0380m, 0.4027m), Pt(0.2800m, 0.5000m), Pt(0.5066m, 0.5963m)],
        [BuildPairTypeKey("wardmark", "brno", CaravanType.Land)] = [Pt(0.0380m, 0.4027m), Pt(0.2300m, 0.6100m), Pt(0.4527m, 0.7448m)],
        [BuildPairTypeKey("rivenstal", "gavern", CaravanType.Land)] = [Pt(0.4824m, 0.4500m), Pt(0.4980m, 0.5250m), Pt(0.5066m, 0.5963m)],
        [BuildPairTypeKey("gavern", "brno", CaravanType.Land)] = [Pt(0.4527m, 0.7448m), Pt(0.4800m, 0.6700m), Pt(0.5066m, 0.5963m)],
        [BuildPairTypeKey("brno", "rivenstal", CaravanType.Land)] = [Pt(0.4527m, 0.7448m), Pt(0.4680m, 0.6100m), Pt(0.4824m, 0.4500m)],
        [BuildPairTypeKey("gavern", "wodenz", CaravanType.Land)] = [Pt(0.5066m, 0.5963m), Pt(0.6200m, 0.8600m), Pt(0.8036m, 0.9604m)],
        [BuildPairTypeKey("rivenstal", "gotha", CaravanType.Sea)] = [Pt(0.4824m, 0.4500m), Pt(0.5600m, 0.3900m), Pt(0.6200m, 0.3200m), Pt(0.6664m, 0.2322m)],
        [BuildPairTypeKey("gavern", "gotha", CaravanType.Sea)] = [Pt(0.5066m, 0.5963m), Pt(0.5600m, 0.5200m), Pt(0.6200m, 0.4100m), Pt(0.6664m, 0.2322m)],
        [BuildPairTypeKey("gotha", "thokur_rus", CaravanType.Sea)] = [Pt(0.6664m, 0.2322m), Pt(0.7900m, 0.3200m), Pt(0.8800m, 0.4100m), Pt(0.8652m, 0.4753m)],
        [BuildPairTypeKey("rivenstal", "gavern", CaravanType.Sea)] = [Pt(0.4824m, 0.4500m), Pt(0.4700m, 0.5150m), Pt(0.4900m, 0.5650m), Pt(0.5066m, 0.5963m)]
    };

    private static RoutePoint Pt(decimal x, decimal y) => new() { X = x, Y = y };
}
