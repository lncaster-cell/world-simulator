using System.Text;
using System.Text.Json;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;

namespace WorldSimulator.RoutePathExtractor;

public sealed class RoutePathExtractor
{
    private const int AnchorSearchRadiusSoftPx = 40;
    private const int AnchorSearchRadiusHardPx = 160;
    private const int MaxBridgeGapPx = 60;
    private const double OffMaskPenalty = 50.0;

    public void Generate(string maskPath, string edgesPath, string nodesPath, string outputPath)
    {
        var nodes = LoadNodes(nodesPath);
        var edges = LoadEdges(edgesPath);
        var settlements = BuildSettlementCoordinates();
        using var image = Image.Load<Rgba32>(maskPath);
        var landMask = BuildMask(image, IsRoad);
        var seaMask = BuildMask(image, IsSea);

        var paths = new List<object>();
        var reportEntries = new List<RouteReportEntry>();

        foreach (var edge in edges.Where(e => !e.IsStub))
        {
            var entry = new RouteReportEntry(edge.RouteId, BuildTradeRouteId(edge.FromNode, edge.ToNode, edge.RouteType), edge.RouteType, edge.FromNode, edge.ToNode);
            reportEntries.Add(entry);

            if (!nodes.TryGetValue(edge.FromNode, out var fromNode) || !nodes.TryGetValue(edge.ToNode, out var toNode))
            {
                entry.Fail("Node metadata missing.");
                continue;
            }

            entry.StartSettlement = fromNode.Name;
            entry.EndSettlement = toNode.Name;

            if (!settlements.TryGetValue(NormalizeName(fromNode.Name), out var from) || !settlements.TryGetValue(NormalizeName(toNode.Name), out var to))
            {
                entry.Fail("Settlement coordinate missing.");
                continue;
            }

            var mask = edge.RouteType.Equals("sea", StringComparison.OrdinalIgnoreCase) ? seaMask : landMask;
            var start = FindNearestMaskPixel(mask, image.Width, image.Height, from);
            var end = FindNearestMaskPixel(mask, image.Width, image.Height, to);

            if (start is null || end is null)
            {
                entry.Fail("Mask anchor not found within hard radius.");
                continue;
            }

            entry.StartAnchorDistancePx = start.Value.Distance;
            entry.EndAnchorDistancePx = end.Value.Distance;
            if (start.Value.Distance > AnchorSearchRadiusSoftPx) entry.Warnings.Add($"Start anchor snapped at {start.Value.Distance:F1}px (> {AnchorSearchRadiusSoftPx}px).");
            if (end.Value.Distance > AnchorSearchRadiusSoftPx) entry.Warnings.Add($"End anchor snapped at {end.Value.Distance:F1}px (> {AnchorSearchRadiusSoftPx}px).");

            var pathResult = FindPath(mask, image.Width, image.Height, start.Value.Point, end.Value.Point);
            if (!pathResult.Success)
            {
                entry.OffMaskBridgeCount = pathResult.OffMaskBridgeCount;
                entry.MaxOffMaskBridgeLengthPx = pathResult.MaxOffMaskBridgeLengthPx;
                entry.Fail(pathResult.FailureReason ?? "Path not found.");
                continue;
            }

            entry.OffMaskBridgeCount = pathResult.OffMaskBridgeCount;
            entry.MaxOffMaskBridgeLengthPx = pathResult.MaxOffMaskBridgeLengthPx;
            if (pathResult.MaxOffMaskBridgeLengthPx > 0)
            {
                entry.Warnings.Add($"Used off-mask bridging: segments={pathResult.OffMaskBridgeCount}, max={pathResult.MaxOffMaskBridgeLengthPx}px.");
            }

            var fullPath = BuildFullPath(from, to, image.Width, image.Height, start.Value.Point, end.Value.Point, pathResult.Path);
            entry.PathPixelCount = fullPath.Count;

            var simplified = Simplify(fullPath, 2.0);
            entry.SimplifiedPointCount = simplified.Count;
            entry.Ok = true;

            paths.Add(new
            {
                source_route_id = edge.RouteId,
                trade_route_id = entry.TradeRouteId,
                route_type = edge.RouteType.Equals("sea", StringComparison.OrdinalIgnoreCase) ? "sea" : "road",
                points = simplified.Select(p => new
                {
                    x = Math.Clamp((double)p.X / (image.Width - 1), 0d, 1d),
                    y = Math.Clamp((double)p.Y / (image.Height - 1), 0d, 1d)
                })
            });
        }

        var payload = new { schema_version = "rivia_route_paths_v1", region_id = "RIVIA", paths };
        var tempOutputPath = outputPath + ".tmp";
        File.WriteAllText(tempOutputPath, JsonSerializer.Serialize(payload, new JsonSerializerOptions { WriteIndented = true }));
        File.Move(tempOutputPath, outputPath, overwrite: true);

        var reportPath = Path.Combine(Path.GetDirectoryName(outputPath) ?? string.Empty, "route_paths_report.txt");
        File.WriteAllText(reportPath, BuildReport(reportEntries));
    }

    static List<(int X, int Y)> BuildFullPath((double X, double Y) from, (double X, double Y) to, int width, int height, (int X, int Y) startAnchor, (int X, int Y) endAnchor, List<(int X, int Y)> pixelPath)
    {
        var startSettlement = ToPixel(from, width, height);
        var endSettlement = ToPixel(to, width, height);
        var result = new List<(int X, int Y)> { startSettlement };
        if (result[^1] != startAnchor) result.Add(startAnchor);
        foreach (var p in pixelPath.Skip(1))
        {
            if (result[^1] != p) result.Add(p);
        }
        if (result[^1] != endSettlement) result.Add(endSettlement);
        return result;
    }

    static (int X, int Y) ToPixel((double X, double Y) pt, int width, int height)
        => ((int)Math.Round(pt.X * (width - 1)), (int)Math.Round(pt.Y * (height - 1)));

    static string BuildReport(List<RouteReportEntry> entries)
    {
        var sb = new StringBuilder();
        foreach (var e in entries)
        {
            sb.AppendLine($"[{(e.Ok ? "OK" : "FAILED")}] route_id={e.RouteId}");
            sb.AppendLine($"  trade_route_id={e.TradeRouteId}");
            sb.AppendLine($"  route_type={e.RouteType}");
            sb.AppendLine($"  start_settlement={e.StartSettlement}");
            sb.AppendLine($"  end_settlement={e.EndSettlement}");
            sb.AppendLine($"  start_anchor_distance_px={(e.StartAnchorDistancePx.HasValue ? e.StartAnchorDistancePx.Value.ToString("F1") : "n/a")}");
            sb.AppendLine($"  end_anchor_distance_px={(e.EndAnchorDistancePx.HasValue ? e.EndAnchorDistancePx.Value.ToString("F1") : "n/a")}");
            sb.AppendLine($"  path_pixel_count={(e.PathPixelCount.HasValue ? e.PathPixelCount.Value.ToString() : "n/a")}");
            sb.AppendLine($"  simplified_point_count={(e.SimplifiedPointCount.HasValue ? e.SimplifiedPointCount.Value.ToString() : "n/a")}");
            sb.AppendLine($"  off_mask_bridge_count={e.OffMaskBridgeCount}");
            sb.AppendLine($"  max_bridge_length_px={e.MaxOffMaskBridgeLengthPx}");
            sb.AppendLine($"  warnings={(e.Warnings.Count == 0 ? "none" : string.Join(" | ", e.Warnings))}");
            sb.AppendLine($"  failure_reason={(string.IsNullOrWhiteSpace(e.FailureReason) ? "none" : e.FailureReason)}");
            sb.AppendLine();
        }

        return sb.ToString();
    }

    static bool[] BuildMask(Image<Rgba32> image, Func<Rgba32, bool> pred)
    {
        var mask = new bool[image.Width * image.Height];
        for (var y = 0; y < image.Height; y++)
        for (var x = 0; x < image.Width; x++)
            mask[(y * image.Width) + x] = pred(image[x, y]);
        return mask;
    }
    static bool IsRoad(Rgba32 p) => p.A >= 128 && p.R >= 180 && p.B >= 180 && p.G <= 120;
    static bool IsSea(Rgba32 p) => p.A >= 128 && p.G >= 180 && p.B >= 180 && p.R <= 120;

    static AnchorSearchResult? FindNearestMaskPixel(bool[] mask, int width, int height, (double X, double Y) pt)
    {
        var center = ToPixel(pt, width, height);
        var bestDistSq = double.MaxValue;
        (int, int)? best = null;
        foreach (var radius in new[] { AnchorSearchRadiusSoftPx, 80, AnchorSearchRadiusHardPx })
        {
            var foundAny = false;
            for (var y = Math.Max(0, center.Y - radius); y <= Math.Min(height - 1, center.Y + radius); y++)
            for (var x = Math.Max(0, center.X - radius); x <= Math.Min(width - 1, center.X + radius); x++)
            {
                if (!mask[(y * width) + x]) continue;
                foundAny = true;
                var d = ((x - center.X) * (x - center.X)) + ((y - center.Y) * (y - center.Y));
                if (d < bestDistSq)
                {
                    bestDistSq = d;
                    best = (x, y);
                }
            }

            if (foundAny && best.HasValue)
            {
                return new AnchorSearchResult(best.Value, Math.Sqrt(bestDistSq));
            }
        }

        return null;
    }

    static PathResult FindPath(bool[] mask, int w, int h, (int X, int Y) s, (int X, int Y) t)
    {
        var open = new PriorityQueue<State, double>();
        var best = new Dictionary<State, double>();
        var prev = new Dictionary<State, State>();
        var startMask = mask[(s.Y * w) + s.X];
        var start = new State(s.X, s.Y, startMask ? 0 : 1, 0, 0);
        open.Enqueue(start, Heuristic(s.X, s.Y, t.X, t.Y));
        best[start] = 0;
        int[] d = [-1, 0, 1];

        while (open.Count > 0)
        {
            var cur = open.Dequeue();
            if (cur.X == t.X && cur.Y == t.Y)
            {
                return ReconstructPath(cur, prev);
            }

            var currentG = best[cur];
            foreach (var dx in d)
            foreach (var dy in d)
            {
                if (dx == 0 && dy == 0) continue;
                var nx = cur.X + dx;
                var ny = cur.Y + dy;
                if (nx < 0 || ny < 0 || nx >= w || ny >= h) continue;

                var nextOnMask = mask[(ny * w) + nx];
                var segmentLen = nextOnMask ? 0 : cur.CurrentOffMaskSegment + 1;
                if (segmentLen > MaxBridgeGapPx) continue;

                var nextState = new State(nx, ny, segmentLen, nextOnMask ? cur.OffMaskBridgeCount : (cur.CurrentOffMaskSegment == 0 ? cur.OffMaskBridgeCount + 1 : cur.OffMaskBridgeCount), nextOnMask ? cur.MaxOffMaskBridgeLength : Math.Max(cur.MaxOffMaskBridgeLength, segmentLen));
                var stepCost = 1.0 + (nextOnMask ? 0.0 : OffMaskPenalty);
                var tentative = currentG + stepCost;
                if (best.TryGetValue(nextState, out var known) && known <= tentative) continue;

                best[nextState] = tentative;
                prev[nextState] = cur;
                open.Enqueue(nextState, tentative + Heuristic(nx, ny, t.X, t.Y));
            }
        }

        return PathResult.Failed("Path not found within bridge constraints.");
    }

    static PathResult ReconstructPath(State end, Dictionary<State, State> prev)
    {
        var path = new List<(int X, int Y)>();
        var at = end;
        while (true)
        {
            path.Add((at.X, at.Y));
            if (!prev.TryGetValue(at, out var p)) break;
            at = p;
        }
        path.Reverse();
        return PathResult.Ok(path, end.OffMaskBridgeCount, end.MaxOffMaskBridgeLength);
    }

    static double Heuristic(int x, int y, int tx, int ty) => Math.Sqrt(((tx - x) * (tx - x)) + ((ty - y) * (ty - y)));

    static List<(int X, int Y)> Simplify(List<(int X, int Y)> pts, double eps)
    {
        if (pts.Count < 3) return pts;
        var keep = new bool[pts.Count];
        keep[0] = true;
        keep[^1] = true;
        SimplifyRec(pts, 0, pts.Count - 1, eps, keep);
        return pts.Where((_, i) => keep[i]).ToList();
    }
    static void SimplifyRec(List<(int X, int Y)> pts, int s, int e, double eps, bool[] keep)
    {
        double max = 0;
        var idx = -1;
        for (var i = s + 1; i < e; i++)
        {
            var d = Perp(pts[i], pts[s], pts[e]);
            if (d > max) { max = d; idx = i; }
        }
        if (max > eps && idx > 0) { keep[idx] = true; SimplifyRec(pts, s, idx, eps, keep); SimplifyRec(pts, idx, e, eps, keep); }
    }
    static double Perp((int X, int Y) p, (int X, int Y) a, (int X, int Y) b)
    {
        var dx = b.X - a.X;
        var dy = b.Y - a.Y;
        if (dx == 0 && dy == 0) return Math.Sqrt(Math.Pow(p.X - a.X, 2) + Math.Pow(p.Y - a.Y, 2));
        return Math.Abs((dy * p.X) - (dx * p.Y) + (b.X * a.Y) - (b.Y * a.X)) / Math.Sqrt((dx * dx) + (dy * dy));
    }

    record Node(string NodeId, string Name, bool IsStub);
    record Edge(string RouteId, string FromNode, string ToNode, string RouteType, bool IsStub);
    readonly record struct AnchorSearchResult((int X, int Y) Point, double Distance);
    readonly record struct State(int X, int Y, int CurrentOffMaskSegment, int OffMaskBridgeCount, int MaxOffMaskBridgeLength);

    sealed class PathResult
    {
        public bool Success { get; }
        public List<(int X, int Y)> Path { get; }
        public int OffMaskBridgeCount { get; }
        public int MaxOffMaskBridgeLengthPx { get; }
        public string? FailureReason { get; }
        PathResult(bool success, List<(int X, int Y)> path, int count, int max, string? failureReason)
        { Success = success; Path = path; OffMaskBridgeCount = count; MaxOffMaskBridgeLengthPx = max; FailureReason = failureReason; }
        public static PathResult Ok(List<(int X, int Y)> path, int count, int max) => new(true, path, count, max, null);
        public static PathResult Failed(string reason) => new(false, [], 0, 0, reason);
    }

    sealed class RouteReportEntry
    {
        public RouteReportEntry(string routeId, string tradeRouteId, string routeType, string startSettlement, string endSettlement)
        { RouteId = routeId; TradeRouteId = tradeRouteId; RouteType = routeType; StartSettlement = startSettlement; EndSettlement = endSettlement; }
        public string RouteId { get; }
        public string TradeRouteId { get; }
        public string RouteType { get; }
        public string StartSettlement { get; set; }
        public string EndSettlement { get; set; }
        public double? StartAnchorDistancePx { get; set; }
        public double? EndAnchorDistancePx { get; set; }
        public int? PathPixelCount { get; set; }
        public int? SimplifiedPointCount { get; set; }
        public int OffMaskBridgeCount { get; set; }
        public int MaxOffMaskBridgeLengthPx { get; set; }
        public List<string> Warnings { get; } = [];
        public string? FailureReason { get; private set; }
        public bool Ok { get; set; }
        public void Fail(string reason) { FailureReason = reason; Ok = false; }
    }

    static Dictionary<string, Node> LoadNodes(string path) => ReadCsv(path).Skip(1).Select(c => new Node(c[0], c[1], bool.Parse(c[8]))).ToDictionary(x => x.NodeId, StringComparer.OrdinalIgnoreCase);
    static List<Edge> LoadEdges(string path) => ReadCsv(path).Skip(1).Select(c => new Edge(c[0], c[1], c[2], c[3], bool.Parse(c[8]))).ToList();
    static IEnumerable<string[]> ReadCsv(string path) { foreach (var line in File.ReadLines(path)) yield return SplitCsv(line).ToArray(); }
    static List<string> SplitCsv(string line) { var result = new List<string>(); var cur = string.Empty; var quoted = false; foreach (var ch in line) { if (ch == '"') { quoted = !quoted; continue; } if (ch == ',' && !quoted) { result.Add(cur); cur = string.Empty; } else cur += ch; } result.Add(cur); return result; }
    static string NormalizeName(string name) => name.ToLowerInvariant().Replace('ö', 'o').Replace('-', '_').Replace(' ', '_') switch { "tokrus" => "thokur_rus", var n => n };
    static readonly Dictionary<string, string> NodeMap = new(StringComparer.OrdinalIgnoreCase) { ["N_HIGHROCK"] = "highrock", ["N_MLYNEK"] = "mlynek", ["N_WARDMARK"] = "wardmark", ["N_RIVENSTAL"] = "rivenstal", ["N_GAVERN"] = "gavern", ["N_BRNO"] = "brno", ["N_WODENZ"] = "wodenz", ["N_GOTHA"] = "gotha", ["N_TOKRUS"] = "thokur_rus" };
    static string BuildTradeRouteId(string from, string to, string type) { var suffix = type.Equals("sea", StringComparison.OrdinalIgnoreCase) ? "sea" : "land"; return $"{MapNodeOrName(from)}_{MapNodeOrName(to)}_{suffix}"; }
    static string MapNodeOrName(string value) => NodeMap.GetValueOrDefault(value, NormalizeName(value));
    static Dictionary<string, (double X, double Y)> BuildSettlementCoordinates() => new(StringComparer.OrdinalIgnoreCase)
    {
        ["gotha"] = (0.6664, 0.2322), ["rivenstal"] = (0.4824, 0.45), ["gavern"] = (0.5066, 0.5963), ["mlynek"] = (0.2833, 0.2487), ["brno"] = (0.4527, 0.7448), ["wodenz"] = (0.8036, 0.9604), ["wardmark"] = (0.0380, 0.4027), ["highrock"] = (0.1579, 0.2179), ["thokur_rus"] = (0.8652, 0.4753)
    };
}
