using System.Text.Json;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;

namespace WorldSimulator.RoutePathExtractor;

public sealed class RoutePathExtractor
{
    private readonly RouteExtractionOptions _options;
    private readonly RouteMaskBuilder _maskBuilder;
    private readonly RoutePathFinder _pathFinder;
    private readonly RouteConnectorFallback _connectorFallback;

    public RoutePathExtractor()
        : this(new RouteExtractionOptions())
    {
    }

    public RoutePathExtractor(RouteExtractionOptions options)
    {
        _options = options;
        _maskBuilder = new RouteMaskBuilder(options);
        _pathFinder = new RoutePathFinder(options);
        _connectorFallback = new RouteConnectorFallback(options, _pathFinder);
    }

    public void Generate(string maskPath, string edgesPath, string nodesPath, string outputPath)
    {
        var nodes = RouteDataLoader.LoadNodes(nodesPath);
        var edges = RouteDataLoader.LoadEdges(edgesPath);
        var settlements = RouteDataLoader.BuildSettlementCoordinates();
        using var image = Image.Load<Rgba32>(maskPath);
        var landMask = _maskBuilder.BuildMask(image, RouteMaskBuilder.IsRoad);
        var seaMask = _maskBuilder.BuildMask(image, RouteMaskBuilder.IsSea);
        var inflatedLandMask = _maskBuilder.DilateMask(landMask, image.Width, image.Height, _options.MaskInflateRadiusPx);
        var inflatedSeaMask = _maskBuilder.DilateMask(seaMask, image.Width, image.Height, _options.MaskInflateRadiusPx);

        var paths = new List<GeneratedRoutePath>();
        var reportEntries = new List<RouteReportEntry>();
        var nonStubEdges = edges.Where(e => !e.IsStub).ToList();

        for (var i = 0; i < nonStubEdges.Count; i++)
        {
            var edge = nonStubEdges[i];
            var routeType = RouteDataLoader.NormalizeRouteType(edge.RouteType);
            var logPrefix = $"[{i + 1}/{nonStubEdges.Count}] {edge.RouteId} {routeType}:";
            Console.WriteLine($"{logPrefix} starting...");

            var entry = new RouteReportEntry(edge.RouteId, RouteDataLoader.BuildTradeRouteId(edge.FromNode, edge.ToNode, edge.RouteType), edge.RouteType, edge.FromNode, edge.ToNode);
            reportEntries.Add(entry);

            if (!nodes.TryGetValue(edge.FromNode, out var fromNode) || !nodes.TryGetValue(edge.ToNode, out var toNode))
            {
                entry.Fail("Node metadata missing.");
                Console.WriteLine($"{logPrefix} FAILED reason={entry.FailureReason}");
                Console.Out.Flush();
                continue;
            }

            entry.StartSettlement = fromNode.Name;
            entry.EndSettlement = toNode.Name;

            if (!settlements.TryGetValue(RouteDataLoader.NormalizeName(fromNode.Name), out var from) || !settlements.TryGetValue(RouteDataLoader.NormalizeName(toNode.Name), out var to))
            {
                entry.Fail("Settlement coordinate missing.");
                Console.WriteLine($"{logPrefix} FAILED reason={entry.FailureReason}");
                Console.Out.Flush();
                continue;
            }

            var isSea = edge.RouteType.Equals("sea", StringComparison.OrdinalIgnoreCase);
            var originalMask = isSea ? seaMask : landMask;
            var inflatedMask = isSea ? inflatedSeaMask : inflatedLandMask;
            var start = _maskBuilder.FindNearestMaskPixel(originalMask, image.Width, image.Height, from);
            var end = _maskBuilder.FindNearestMaskPixel(originalMask, image.Width, image.Height, to);

            if (start is null || end is null)
            {
                entry.Fail("Mask anchor not found within hard radius.");
                Console.WriteLine($"{logPrefix} FAILED reason={entry.FailureReason}");
                Console.Out.Flush();
                continue;
            }

            Console.WriteLine($"{logPrefix} anchors: start={start.Value.Point.X},{start.Value.Point.Y} end={end.Value.Point.X},{end.Value.Point.Y}");
            entry.StartAnchorDistancePx = start.Value.Distance;
            entry.EndAnchorDistancePx = end.Value.Distance;
            if (start.Value.Distance > _options.AnchorSearchRadiusSoftPx) entry.Warnings.Add($"Start anchor snapped at {RouteGeometry.FormatFixedOne(start.Value.Distance)}px (> {_options.AnchorSearchRadiusSoftPx}px).");
            if (end.Value.Distance > _options.AnchorSearchRadiusSoftPx) entry.Warnings.Add($"End anchor snapped at {RouteGeometry.FormatFixedOne(end.Value.Distance)}px (> {_options.AnchorSearchRadiusSoftPx}px).");

            Console.WriteLine($"{logPrefix} pathfinding...");
            var pathResult = _pathFinder.FindPathWithFallback(originalMask, inflatedMask, image.Width, image.Height, start.Value.Point, end.Value.Point, isSea);
            if (!pathResult.Success)
            {
                if (_connectorFallback.TryDirectSettlementConnectorOverride(edge.RouteId, fromNode.Name, toNode.Name, from, to, image.Width, image.Height, start.Value.Point, end.Value.Point, out var directConnectorResult))
                {
                    pathResult = directConnectorResult;
                    if (pathResult.UsedForcedDirectConnector)
                    {
                        Console.WriteLine($"{logPrefix} primary path failed, using FORCED direct Gavern connector length={RouteGeometry.FormatFixedOne(pathResult.DirectConnectorLengthPx)}px");
                        Console.WriteLine($"{logPrefix} OK with forced direct Gavern connector");
                    }
                    else
                    {
                        Console.WriteLine($"{logPrefix} OK with direct Gavern connector length={RouteGeometry.FormatFixedOne(pathResult.DirectConnectorLengthPx)}px");
                    }
                }
                else
                {
                    Console.WriteLine($"{logPrefix} primary path failed, trying connector fallback...");
                    pathResult = _connectorFallback.TryConnectorFallback(originalMask, inflatedMask, image.Width, image.Height, from, to, start.Value.Point, end.Value.Point, isSea);
                    if (pathResult.Success)
                    {
                        Console.WriteLine($"{logPrefix} connector fallback OK, connectors={pathResult.Connectors.Count}, max={RouteGeometry.FormatFixedOne(pathResult.MaxConnectorLengthPx)}px");
                    }
                }
            }
            if (!pathResult.Success)
            {
                entry.Fail(pathResult.FailureReason ?? "Path not found.");
                Console.WriteLine($"{logPrefix} FAILED reason={entry.FailureReason}");
                Console.Out.Flush();
                continue;
            }

            entry.ConnectorCount = pathResult.Connectors.Count;
            entry.MaxConnectorLengthPx = pathResult.MaxConnectorLengthPx;
            entry.UsedSettlementConnector = pathResult.UsedSettlementConnector;
            entry.UsedForcedDirectConnector = pathResult.UsedForcedDirectConnector;
            entry.ForcedConnectorLengthPx = pathResult.UsedForcedDirectConnector ? pathResult.DirectConnectorLengthPx : 0.0;
            entry.GenerationMethod = RouteReportBuilder.DetermineGenerationMethod(pathResult);
            if (pathResult.MaxConnectorLengthPx > _options.ConnectorSoftWarningPx)
            {
                entry.ConnectorWarning = $"Longest connector {RouteGeometry.FormatFixedOne(pathResult.MaxConnectorLengthPx)}px (> {_options.ConnectorSoftWarningPx}px).";
                entry.Warnings.Add(entry.ConnectorWarning);
            }
            entry.Warnings.AddRange(pathResult.Warnings);

            var fullPath = BuildFullPath(from, to, image.Width, image.Height, pathResult);
            entry.PathPixelCount = fullPath.Count;

            var simplified = RoutePathFinder.Simplify(fullPath, 2.0);
            entry.SimplifiedPointCount = simplified.Count;
            entry.Ok = true;

            paths.Add(new GeneratedRoutePath
            {
                SourceRouteId = edge.RouteId,
                TradeRouteId = entry.TradeRouteId,
                RouteType = routeType,
                GenerationMethod = entry.GenerationMethod,
                Warnings = entry.Warnings.ToList(),
                Points = simplified.Select(p => new GeneratedRoutePoint(
                    Math.Clamp((double)p.X / (image.Width - 1), 0d, 1d),
                    Math.Clamp((double)p.Y / (image.Height - 1), 0d, 1d))).ToList(),
                PixelPoints = simplified,
                UsedForcedDirectConnector = entry.UsedForcedDirectConnector
            });

            Console.WriteLine($"{logPrefix} OK points={simplified.Count}");
            Console.Out.Flush();
        }

        var payload = new
        {
            schema_version = "rivia_route_paths_v1",
            region_id = "RIVIA",
            paths = paths.Select(path => new
            {
                source_route_id = path.SourceRouteId,
                trade_route_id = path.TradeRouteId,
                route_type = path.RouteType,
                generation_method = path.GenerationMethod,
                warnings = path.Warnings,
                points = path.Points.Select(point => new { x = point.X, y = point.Y })
            })
        };
        var tempOutputPath = outputPath + ".tmp";
        File.WriteAllText(tempOutputPath, JsonSerializer.Serialize(payload, new JsonSerializerOptions { WriteIndented = true }));
        File.Move(tempOutputPath, outputPath, overwrite: true);

        var outputDirectory = Path.GetDirectoryName(outputPath) ?? string.Empty;
        var reportPath = Path.Combine(outputDirectory, "route_paths_report.txt");
        File.WriteAllText(reportPath, RouteReportBuilder.BuildReport(reportEntries));

        var debugImagePath = Path.Combine(outputDirectory, "route_paths_debug.png");
        RouteDebugImageWriter.WriteDebugImage(debugImagePath, image.Width, image.Height, paths);

        var success = reportEntries.Count(x => x.Ok);
        var failed = reportEntries.Count - success;
        Console.WriteLine("Route extraction complete.");
        Console.WriteLine($"Total routes: {reportEntries.Count}");
        Console.WriteLine($"Successful routes: {success}");
        Console.WriteLine($"Failed routes: {failed}");
        Console.WriteLine($"Output: {outputPath}");
        Console.WriteLine($"Report: {reportPath}");
        Console.WriteLine($"Debug image: {debugImagePath}");
        if (failed > 0) Console.WriteLine("Open route_paths_report.txt for details.");
    }

    private static List<(int X, int Y)> BuildFullPath((double X, double Y) from, (double X, double Y) to, int width, int height, PathResult result)
    {
        var startSettlement = RouteGeometry.ToPixel(from, width, height);
        var endSettlement = RouteGeometry.ToPixel(to, width, height);
        var points = new List<(int X, int Y)> { startSettlement };
        foreach (var p in result.Path)
        {
            if (points[^1] != p) points.Add(p);
        }
        if (points[^1] != endSettlement) points.Add(endSettlement);
        return points;
    }
}
