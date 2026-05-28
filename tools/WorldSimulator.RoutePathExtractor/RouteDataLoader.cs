namespace WorldSimulator.RoutePathExtractor;

internal static class RouteDataLoader
{
    public static readonly Dictionary<string, string> NodeMap = new(StringComparer.OrdinalIgnoreCase)
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

    public static Dictionary<string, Node> LoadNodes(string path)
        => ReadCsv(path).Skip(1).Select(c => new Node(c[0], c[1], bool.Parse(c[8]))).ToDictionary(x => x.NodeId, StringComparer.OrdinalIgnoreCase);

    public static List<Edge> LoadEdges(string path)
        => ReadCsv(path).Skip(1).Select(c => new Edge(c[0], c[1], c[2], c[3], bool.Parse(c[8]))).ToList();

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
    {
        var suffix = type.Equals("sea", StringComparison.OrdinalIgnoreCase) ? "sea" : "land";
        return $"{MapNodeOrName(from)}_{MapNodeOrName(to)}_{suffix}";
    }

    public static string NormalizeRouteType(string routeType) => routeType.Equals("sea", StringComparison.OrdinalIgnoreCase) ? "sea" : "road";

    public static Dictionary<string, (double X, double Y)> BuildSettlementCoordinates() => new(StringComparer.OrdinalIgnoreCase)
    {
        ["gotha"] = (0.6664, 0.2322),
        ["rivenstal"] = (0.4824, 0.45),
        ["gavern"] = (0.5066, 0.5963),
        ["mlynek"] = (0.2833, 0.2487),
        ["brno"] = (0.4527, 0.7448),
        ["wodenz"] = (0.8036, 0.9604),
        ["wardmark"] = (0.0380, 0.4027),
        ["highrock"] = (0.1579, 0.2179),
        ["thokur_rus"] = (0.8652, 0.4753)
    };

    private static string MapNodeOrName(string value) => NodeMap.GetValueOrDefault(value, NormalizeName(value));
}
