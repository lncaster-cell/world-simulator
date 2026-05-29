using System.Reflection;
using System.Text.Json;
using WorldSimulator.Core.World;

namespace WorldSimulator.Core.Trade;

internal static class TradeRouteDataLoader
{
    private const string RiviaRoutesResourceName = "WorldSimulator.Core.Data.rivia_routes.json";

    public static List<TradeRoute> LoadRoutesFromEmbeddedJson()
    {
        using var stream = Assembly.GetExecutingAssembly()
            .GetManifestResourceStream(RiviaRoutesResourceName)
            ?? throw new InvalidOperationException($"Embedded route data '{RiviaRoutesResourceName}' was not found.");

        using var doc = JsonDocument.Parse(stream);
        var root = doc.RootElement;
        var nodeToSettlementId = TradeRouteIdBuilder.BuildNodeToSettlementIdMap(root.GetProperty("nodes"));

        var routes = new List<TradeRoute>();
        foreach (var edge in root.GetProperty("edges").EnumerateArray())
        {
            if (edge.GetProperty("is_stub").GetBoolean())
            {
                continue;
            }

            var fromNode = edge.GetProperty("from_node").GetString();
            var toNode = edge.GetProperty("to_node").GetString();
            if (string.IsNullOrWhiteSpace(fromNode) || string.IsNullOrWhiteSpace(toNode))
            {
                continue;
            }

            if (!nodeToSettlementId.TryGetValue(fromNode, out var fromSettlementId))
            {
                continue;
            }

            if (!nodeToSettlementId.TryGetValue(toNode, out var toSettlementId))
            {
                continue;
            }

            var routeTypeRaw = edge.GetProperty("route_type").GetString() ?? "road";
            var routeType = routeTypeRaw.Equals("sea", StringComparison.OrdinalIgnoreCase) ? CaravanType.Sea : CaravanType.Land;
            var distanceDays = edge.GetProperty("travel_days").GetDecimal();

            routes.Add(new TradeRoute
            {
                Id = TradeRouteIdBuilder.BuildRouteId(fromSettlementId, toSettlementId, routeType),
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
}

internal static class TradeRouteIdBuilder
{
    public static Dictionary<string, string> BuildNodeToSettlementIdMap(JsonElement nodes)
    {
        var nodeToSettlementId = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        foreach (var node in nodes.EnumerateArray())
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
            else
            {
                nodeToSettlementId[nodeId] = ToSettlementId(name);
            }
        }

        return nodeToSettlementId;
    }

    public static string BuildRouteId(string fromSettlementId, string toSettlementId, CaravanType routeType)
        => $"{fromSettlementId}_{toSettlementId}_{(routeType == CaravanType.Sea ? "sea" : "land")}";

    private static string ToSettlementId(string name)
        => name.ToLowerInvariant().Replace('ö', 'o').Replace('-', '_').Replace(' ', '_') switch
        {
            "tokrus" => "thokur_rus",
            var normalized => normalized
        };
}
