using System.Reflection;
using System.Text.Json;
using WorldSimulator.Core.World;

namespace WorldSimulator.Core.Trade;

public static class TradeRoutePresets
{
    public static List<TradeRoute> CreateDefaultRoutes()
    {
        var routes = TradeRouteDataLoader.LoadRoutesFromEmbeddedJson();
        ApplyFallbackPoints(routes);
        return routes;
    }

    private static void ApplyFallbackPoints(IEnumerable<TradeRoute> routes)
    {
        foreach (var route in routes)
        {
            route.HasLoadedPath = false;
            if (route.Points.Count >= 2)
            {
                continue;
            }

            if (TryBuildFallbackPoints(route, out var fallbackPoints))
            {
                route.Points = fallbackPoints;
            }
        }
    }

    private static bool TryBuildFallbackPoints(TradeRoute route, out List<RoutePoint> points)
    {
        points = [];
        if (!RiviaSettlementPresets.TryGet(route.FromSettlementId, out var start))
        {
            return false;
        }

        if (!RiviaSettlementPresets.TryGet(route.ToSettlementId, out var end))
        {
            return false;
        }

        points.Add(new RoutePoint { X = start.X, Y = start.Y });
        points.Add(new RoutePoint { X = end.X, Y = end.Y });
        return true;
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

            if (RiviaSettlementPresets.TryGetSettlementIdByRouteNodeId(nodeId, out var settlementId))
            {
                nodeToSettlementId[nodeId] = settlementId;
            }
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
                Points = [],
                HasLoadedPath = false
            });
        }

        return routes;
    }

    private static string BuildRouteId(string fromSettlementId, string toSettlementId, CaravanType routeType)
        => $"{fromSettlementId}_{toSettlementId}_{(routeType == CaravanType.Sea ? "sea" : "land")}";
}
