using System.Diagnostics;

namespace WorldSimulator.RoutePathExtractor;

internal sealed class RoutePathFinder
{
    private readonly RouteExtractionOptions _options;

    public RoutePathFinder(RouteExtractionOptions options) => _options = options;

    public PathResult FindPathWithFallback(bool[] originalMask, bool[] inflatedMask, int w, int h, (int X, int Y) s, (int X, int Y) t, bool isSea)
    {
        var firstMargin = CalculateMargin(s, t, isSea, false);
        var first = FindPathInBoundingBox(originalMask, inflatedMask, w, h, s, t, firstMargin);
        if (first.Success) return first;

        var secondMargin = CalculateMargin(s, t, isSea, true);
        return FindPathInBoundingBox(originalMask, inflatedMask, w, h, s, t, secondMargin);
    }

    public PathResult FindPathInBoundingBox(bool[] originalMask, bool[] inflatedMask, int w, int h, (int X, int Y) s, (int X, int Y) t, int margin)
    {
        var bounds = RouteGeometry.BuildBounds(s, t, margin, w, h);
        var open = new PriorityQueue<(int X, int Y), double>();
        var best = new Dictionary<(int X, int Y), double>();
        var prev = new Dictionary<(int X, int Y), (int X, int Y)>();
        var startedAt = Stopwatch.StartNew();
        var expanded = 0;
        int[] d = [-1, 0, 1];

        open.Enqueue(s, Heuristic(s.X, s.Y, t.X, t.Y));
        best[s] = 0;

        while (open.Count > 0)
        {
            if (startedAt.Elapsed.TotalSeconds > _options.MaxRoutePathfindingSeconds)
                return PathResult.Failed($"Pathfinding timeout ({_options.MaxRoutePathfindingSeconds}s), margin={margin}.");

            var cur = open.Dequeue();
            expanded++;
            if (expanded > _options.MaxExpandedNodesPerRoute)
                return PathResult.Failed($"Pathfinding node limit exceeded ({_options.MaxExpandedNodesPerRoute}), margin={margin}.");

            if (cur == t)
                return ReconstructPath(cur, prev);

            var currentG = best[cur];
            foreach (var dx in d)
            foreach (var dy in d)
            {
                if (dx == 0 && dy == 0) continue;
                var nx = cur.X + dx;
                var ny = cur.Y + dy;
                if (nx < bounds.MinX || ny < bounds.MinY || nx > bounds.MaxX || ny > bounds.MaxY) continue;

                var idx = (ny * w) + nx;
                if (!inflatedMask[idx] && (nx, ny) != t) continue;
                var tentative = currentG + (originalMask[idx] ? 1.0 : 8.0);
                var next = (nx, ny);
                if (best.TryGetValue(next, out var known) && known <= tentative) continue;

                best[next] = tentative;
                prev[next] = cur;
                open.Enqueue(next, tentative + Heuristic(nx, ny, t.X, t.Y));
            }
        }

        return PathResult.Failed($"Path not found in bounded area (margin={margin}).");
    }

    public static PathResult ReconstructPath((int X, int Y) end, Dictionary<(int X, int Y), (int X, int Y)> prev)
    {
        var path = new List<(int X, int Y)>();
        var at = end;
        while (true)
        {
            path.Add(at);
            if (!prev.TryGetValue(at, out var p)) break;
            at = p;
        }
        path.Reverse();
        return PathResult.Ok(path);
    }

    public static double Heuristic(int x, int y, int tx, int ty) => Math.Sqrt(((tx - x) * (tx - x)) + ((ty - y) * (ty - y)));

    public static List<(int X, int Y)> Simplify(List<(int X, int Y)> pts, double eps)
    {
        if (pts.Count < 3) return pts;
        var keep = new bool[pts.Count];
        keep[0] = true;
        keep[^1] = true;
        SimplifyRec(pts, 0, pts.Count - 1, eps, keep);
        return pts.Where((_, i) => keep[i]).ToList();
    }

    public static void SimplifyRec(List<(int X, int Y)> pts, int s, int e, double eps, bool[] keep)
    {
        double max = 0;
        var idx = -1;
        for (var i = s + 1; i < e; i++)
        {
            var d = Perp(pts[i], pts[s], pts[e]);
            if (d > max)
            {
                max = d;
                idx = i;
            }
        }
        if (max > eps && idx > 0)
        {
            keep[idx] = true;
            SimplifyRec(pts, s, idx, eps, keep);
            SimplifyRec(pts, idx, e, eps, keep);
        }
    }

    public static double Perp((int X, int Y) p, (int X, int Y) a, (int X, int Y) b)
    {
        var dx = b.X - a.X;
        var dy = b.Y - a.Y;
        if (dx == 0 && dy == 0) return Math.Sqrt(Math.Pow(p.X - a.X, 2) + Math.Pow(p.Y - a.Y, 2));
        return Math.Abs((dy * p.X) - (dx * p.Y) + (b.X * a.Y) - (b.Y * a.X)) / Math.Sqrt((dx * dx) + (dy * dy));
    }

    private int CalculateMargin((int X, int Y) s, (int X, int Y) t, bool isSea, bool secondPass)
    {
        var distance = Heuristic(s.X, s.Y, t.X, t.Y);
        if (isSea || distance > 500) return _options.ExtendedSearchMarginPx + (secondPass ? 120 : 0);
        return _options.BaseSearchMarginPx + (secondPass ? 100 : 0);
    }
}
