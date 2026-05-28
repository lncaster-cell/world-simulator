namespace WorldSimulator.RoutePathExtractor;

internal sealed class RouteConnectorFallback
{
    private readonly RouteExtractionOptions _options;
    private readonly RoutePathFinder _pathFinder;

    public RouteConnectorFallback(RouteExtractionOptions options, RoutePathFinder pathFinder)
    {
        _options = options;
        _pathFinder = pathFinder;
    }

    public PathResult TryConnectorFallback(bool[] originalMask, bool[] inflatedMask, int w, int h, (double X, double Y) from, (double X, double Y) to, (int X, int Y) startAnchor, (int X, int Y) endAnchor, bool isSea)
    {
        var startSettlement = RouteGeometry.ToPixel(from, w, h);
        var endSettlement = RouteGeometry.ToPixel(to, w, h);
        var result = FindPathWithEndpointReanchor(originalMask, inflatedMask, w, h, startAnchor, endAnchor, isSea);
        if (!result.Success) return result;

        var full = new List<(int X, int Y)> { startSettlement };
        var connectors = new List<double>();
        var usedSettlementConnector = false;

        AppendConnector(full, result.Path[0], connectors);
        if (RouteGeometry.Distance(startSettlement, result.Path[0]) > 0.01) usedSettlementConnector = true;
        foreach (var p in result.Path.Skip(1))
            if (full[^1] != p) full.Add(p);
        AppendConnector(full, endSettlement, connectors);
        if (RouteGeometry.Distance(endSettlement, result.Path[^1]) > 0.01) usedSettlementConnector = true;

        var maxConnector = connectors.Count == 0 ? 0.0 : connectors.Max();
        var pathResult = PathResult.Ok(full, connectors, usedSettlementConnector);
        if (maxConnector > _options.ConnectorMaxDistancePx)
            return PathResult.Failed($"Connector distance too large ({RouteGeometry.FormatFixedOne(maxConnector)}px > {_options.ConnectorMaxDistancePx}px).");

        return pathResult;
    }

    public bool TryDirectSettlementConnectorOverride(string routeId, string fromSettlementName, string toSettlementName, (double X, double Y) from, (double X, double Y) to, int w, int h, (int X, int Y) startAnchor, (int X, int Y) endAnchor, out PathResult result)
    {
        result = PathResult.Failed("Direct settlement connector override not applicable.");
        var isForcedRoute = _options.ForcedDirectGavernRouteIds.Contains(routeId);
        var fromNormalized = RouteDataLoader.NormalizeName(fromSettlementName);
        var toNormalized = RouteDataLoader.NormalizeName(toSettlementName);
        var gavernIsEnd = _options.DirectConnectorSettlementOverrides.Contains(toNormalized);
        var gavernIsStart = _options.DirectConnectorSettlementOverrides.Contains(fromNormalized);
        if (!gavernIsEnd && !gavernIsStart) return false;
        if (!isForcedRoute) return false;

        var fromPixel = RouteGeometry.ToPixel(from, w, h);
        var toPixel = RouteGeometry.ToPixel(to, w, h);
        var connectorLength = RouteGeometry.Distance(startAnchor, endAnchor);
        if (connectorLength > _options.ForcedDirectGavernConnectorMaxPx)
        {
            result = PathResult.Failed($"Forced direct Gavern connector too long: {RouteGeometry.FormatFixedOne(connectorLength)}px > {_options.ForcedDirectGavernConnectorMaxPx}px.");
            return true;
        }

        var full = new List<(int X, int Y)> { fromPixel };
        AppendConnector(full, startAnchor, []);
        foreach (var p in RouteGeometry.RasterLine(startAnchor, endAnchor).Skip(1))
            if (full[^1] != p) full.Add(p);
        if (full[^1] != toPixel) full.Add(toPixel);

        result = PathResult.Ok(full, [connectorLength], true, connectorLength, true);
        result.Warnings.Add($"Used FORCED direct Gavern connector override, length {RouteGeometry.FormatFixedOne(connectorLength)}px.");
        return true;
    }

    public PathResult TryPathTowardTargetWithDirectConnector(bool[] originalMask, bool[] inflatedMask, int w, int h, (int X, int Y) sourceAnchor, (int X, int Y) gavernAnchor, bool isSea)
    {
        var nearestReachable = FindNearestReachablePointTowardTarget(originalMask, inflatedMask, w, h, sourceAnchor, gavernAnchor, isSea);
        if (nearestReachable is null)
            return PathResult.Failed("Direct Gavern connector failed: no reachable road-mask pixel near Gavern.");

        var connectorLength = RouteGeometry.Distance(nearestReachable.Value, gavernAnchor);
        if (connectorLength > _options.DirectGavernConnectorMaxPx)
            return PathResult.Failed($"Direct Gavern connector too long: {RouteGeometry.FormatFixedOne(connectorLength)}px > {_options.DirectGavernConnectorMaxPx}px.");

        var maskPath = _pathFinder.FindPathWithFallback(originalMask, inflatedMask, w, h, sourceAnchor, nearestReachable.Value, isSea);
        if (!maskPath.Success)
            return PathResult.Failed($"Direct Gavern connector failed: unable to path to nearest reachable mask pixel ({maskPath.FailureReason}).");

        var full = new List<(int X, int Y)>(maskPath.Path);
        if (full.Count == 0 || full[^1] != nearestReachable.Value) full.Add(nearestReachable.Value);
        AppendConnector(full, gavernAnchor, []);
        var outResult = PathResult.Ok(full, [connectorLength], true, connectorLength);
        outResult.Warnings.Add($"Used direct settlement connector override for Gavern, length {RouteGeometry.FormatFixedOne(connectorLength)}px.");
        return outResult;
    }

    public PathResult FindPathWithEndpointReanchor(bool[] originalMask, bool[] inflatedMask, int w, int h, (int X, int Y) s, (int X, int Y) t, bool isSea)
    {
        var primary = _pathFinder.FindPathWithFallback(originalMask, inflatedMask, w, h, s, t, isSea);
        if (primary.Success) return primary;

        var startCandidates = FindNearestMaskCandidates(originalMask, w, h, s, _options.ConnectorMaxDistancePx);
        var endCandidates = FindNearestMaskCandidates(originalMask, w, h, t, _options.ConnectorMaxDistancePx);
        if (startCandidates.Count == 0 || endCandidates.Count == 0)
            return PathResult.Failed("Connector fallback failed: no nearby mask pixels around endpoint.");

        var startBest = startCandidates[0];
        var endBest = endCandidates[0];
        var reanchored = _pathFinder.FindPathWithFallback(originalMask, inflatedMask, w, h, startBest.Point, endBest.Point, isSea);
        if (reanchored.Success)
        {
            if (startBest.Distance > _options.ConnectorSoftWarningPx) reanchored.Warnings.Add($"Start connector length {RouteGeometry.FormatFixedOne(startBest.Distance)}px.");
            if (endBest.Distance > _options.ConnectorSoftWarningPx) reanchored.Warnings.Add($"End connector length {RouteGeometry.FormatFixedOne(endBest.Distance)}px.");
            return reanchored;
        }

        var bridge = TryBridgeBetweenComponents(originalMask, inflatedMask, w, h, startCandidates, endCandidates, isSea);
        return bridge.Success ? bridge : PathResult.Failed($"Connector fallback failed: {bridge.FailureReason ?? reanchored.FailureReason ?? primary.FailureReason}");
    }

    public PathResult TryBridgeBetweenComponents(bool[] originalMask, bool[] inflatedMask, int w, int h, List<AnchorSearchResult> startCandidates, List<AnchorSearchResult> endCandidates, bool isSea)
    {
        var bestPair = (Start: startCandidates[0], End: endCandidates[0], Distance: double.MaxValue);
        foreach (var s in startCandidates)
        foreach (var t in endCandidates)
        {
            var d = RouteGeometry.Distance(s.Point, t.Point);
            if (d < bestPair.Distance)
                bestPair = (s, t, d);
        }

        if (bestPair.Distance > _options.ConnectorMaxDistancePx)
            return PathResult.Failed($"Bridge gap too large ({RouteGeometry.FormatFixedOne(bestPair.Distance)}px > {_options.ConnectorMaxDistancePx}px).");

        var pathS = _pathFinder.FindPathWithFallback(originalMask, inflatedMask, w, h, startCandidates[0].Point, bestPair.Start.Point, isSea);
        var pathT = _pathFinder.FindPathWithFallback(originalMask, inflatedMask, w, h, bestPair.End.Point, endCandidates[0].Point, isSea);
        if (!pathS.Success || !pathT.Success)
            return PathResult.Failed("Unable to route to bridge endpoints.");

        var bridgeLine = RouteGeometry.RasterLine(bestPair.Start.Point, bestPair.End.Point);
        var joined = new List<(int X, int Y)>();
        joined.AddRange(pathS.Path);
        foreach (var p in bridgeLine.Skip(1))
            if (joined[^1] != p) joined.Add(p);
        foreach (var p in pathT.Path.Skip(1))
            if (joined[^1] != p) joined.Add(p);

        var outResult = PathResult.Ok(joined, [bestPair.Distance], usedSettlementConnector: true);
        outResult.Warnings.Add($"Used bridge fallback, gap={RouteGeometry.FormatFixedOne(bestPair.Distance)}px.");
        return outResult;
    }

    public List<AnchorSearchResult> FindNearestMaskCandidates(bool[] mask, int w, int h, (int X, int Y) center, int maxDistance)
    {
        var candidates = new List<AnchorSearchResult>();
        var maxSq = maxDistance * maxDistance;
        for (var y = Math.Max(0, center.Y - maxDistance); y <= Math.Min(h - 1, center.Y + maxDistance); y++)
        for (var x = Math.Max(0, center.X - maxDistance); x <= Math.Min(w - 1, center.X + maxDistance); x++)
        {
            if (!mask[(y * w) + x]) continue;
            var dSq = ((x - center.X) * (x - center.X)) + ((y - center.Y) * (y - center.Y));
            if (dSq > maxSq) continue;
            candidates.Add(new AnchorSearchResult((x, y), Math.Sqrt(dSq)));
        }
        return candidates.OrderBy(c => c.Distance).Take(64).ToList();
    }

    public static void AppendConnector(List<(int X, int Y)> points, (int X, int Y) target, List<double> connectors)
    {
        if (points[^1] == target) return;
        connectors.Add(RouteGeometry.Distance(points[^1], target));
        foreach (var p in RouteGeometry.RasterLine(points[^1], target).Skip(1))
            if (points[^1] != p) points.Add(p);
    }

    private (int X, int Y)? FindNearestReachablePointTowardTarget(bool[] originalMask, bool[] inflatedMask, int w, int h, (int X, int Y) sourceAnchor, (int X, int Y) targetAnchor, bool isSea)
    {
        var candidates = FindNearestMaskCandidates(originalMask, w, h, targetAnchor, _options.DirectGavernConnectorMaxPx);
        if (candidates.Count == 0) return null;

        foreach (var candidate in candidates)
        {
            var path = _pathFinder.FindPathWithFallback(originalMask, inflatedMask, w, h, sourceAnchor, candidate.Point, isSea);
            if (path.Success) return candidate.Point;
        }
        return candidates[0].Point;
    }
}
