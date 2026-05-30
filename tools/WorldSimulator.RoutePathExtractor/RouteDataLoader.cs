using System.Text.Json;
using System.Text.Json.Serialization;

namespace WorldSimulator.RoutePathExtractor;

internal static class RouteDataLoader
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        PropertyNameCaseInsensitive = true
    };

    public static Dictionary<string, Node> LoadNodes(string path)
        => ReadCsv(path).Skip(1).Select(c => new Node(c[0], c[1], bool.Parse(c[8]))).ToDictionary(x => x.NodeId, StringComparer.OrdinalIgnoreCase);

    public static List<Edge> LoadEdges(string path)
        => ReadCsv(path).Skip(1).Select(c => new Edge(c[0], c[1], c[2], c[3], bool.Parse(c[8]))).ToList();

    public static List<RouteSettlement> LoadSettlements(string path)
    {
        var json = File.ReadAllText(path);
        var payload = JsonSerializer.Deserialize<RouteSettlementsDocument>(json, JsonOptions)
            ?? throw new InvalidDataException($"Unable to deserialize settlements from '{path}'.");

        return payload.Settlements;
    }

    public static Dictionary<string, string> BuildNodeMap(IEnumerable<RouteSettlement> settlements)
        => settlements
            .Where(s => !string.IsNullOrWhiteSpace(s.RouteNodeId) && !string.IsNullOrWhiteSpace(s.Id))
            .ToDictionary(s => s.RouteNodeId, s => s.Id, StringComparer.OrdinalIgnoreCase);

    public static Dictionary<string, (double X, double Y)> BuildSettlementCoordinates(IEnumerable<RouteSettlement> settlements)
        => settlements
            .Where(s => !string.IsNullOrWhiteSpace(s.Id))
            .ToDictionary(s => s.Id, s => (s.X, s.Y), StringComparer.OrdinalIgnoreCase);

    public static IEnumerable<string[]> ReadCsv(string path)
    {
        foreach (var line in File.ReadLines(path))
            yield return SplitCsv(line).ToArray();
    }

    public static List<string> SplitCsv(string line)
    {
        var result = new List<string>();
        var cur = string.Empty;
        var quoted = false;
        for (var i = 0; i < line.Length; i++)
        {
            var ch = line[i];
            if (ch == '"')
            {
                if (quoted && i + 1 < line.Length && line[i + 1] == '"')
                {
                    cur += '"';
                    i++;
                    continue;
                }

                quoted = !quoted;
                continue;
            }

            if (ch == ',' && !quoted)
            {
                result.Add(cur);
                cur = string.Empty;
            }
            else
            {
                cur += ch;
            }
        }

        result.Add(cur);
        return result;
    }

    public static string NormalizeName(string name)
        => name.ToLowerInvariant().Replace('ö', 'o').Replace('-', '_').Replace(' ', '_') switch
        {
            "tokrus" => "thokur_rus",
            var n => n
        };

    public static string BuildTradeRouteId(string from, string to, string type)
        => BuildTradeRouteId(from, to, type, new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase));

    public static string BuildTradeRouteId(string from, string to, string type, IReadOnlyDictionary<string, string> nodeMap)
    {
        var suffix = type.Equals("sea", StringComparison.OrdinalIgnoreCase) ? "sea" : "land";
        return $"{MapNodeOrName(from, nodeMap)}_{MapNodeOrName(to, nodeMap)}_{suffix}";
    }

    public static string NormalizeRouteType(string routeType) => routeType.Equals("sea", StringComparison.OrdinalIgnoreCase) ? "sea" : "road";

    private static string MapNodeOrName(string value, IReadOnlyDictionary<string, string> nodeMap) => nodeMap.GetValueOrDefault(value, NormalizeName(value));
}

internal sealed class RouteSettlementsDocument
{
    [JsonPropertyName("settlements")]
    public List<RouteSettlement> Settlements { get; init; } = [];
}

internal sealed class RouteSettlement
{
    [JsonPropertyName("id")]
    public required string Id { get; init; }

    [JsonPropertyName("route_node_id")]
    public required string RouteNodeId { get; init; }

    [JsonPropertyName("x")]
    public double X { get; init; }

    [JsonPropertyName("y")]
    public double Y { get; init; }
}
