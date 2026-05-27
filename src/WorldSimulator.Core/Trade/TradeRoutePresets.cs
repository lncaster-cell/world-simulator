using System.Reflection;
using System.Text.Json;

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

        return routes;
    }

    private static bool TryBuildFallbackPoints(TradeRoute route, out List<RoutePoint> points)
    {
        points = [];
        if (!SettlementCoordinates.TryGetValue(route.FromSettlementId, out var start))
        {
            return false;
        }

        if (!SettlementCoordinates.TryGetValue(route.ToSettlementId, out var end))
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

    private static RoutePoint Pt(decimal x, decimal y) => new() { X = x, Y = y };
}
