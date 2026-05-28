using System.Text.Json;
using FluentAssertions;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using WorldSimulator.Core.Trade;
using WorldSimulator.RoutePathExtractor;
using RoutePathExtractorTool = WorldSimulator.RoutePathExtractor.RoutePathExtractor;

namespace WorldSimulator.Core.Tests;

public sealed class RoutePathExtractorTests
{
    [Fact]
    public void Extractor_GeneratesRoutePaths_AndLoaderAppliesThem()
    {
        var dir = Directory.CreateTempSubdirectory();
        try
        {
            var maskPath = Path.Combine(dir.FullName, "world_map_routes_mask.png");
            var edgesPath = Path.Combine(dir.FullName, "route_edges.csv");
            var nodesPath = Path.Combine(dir.FullName, "route_nodes.csv");
            var outputPath = Path.Combine(dir.FullName, "route_paths.json");
            var reportPath = Path.Combine(dir.FullName, "route_paths_report.txt");
            var debugImagePath = Path.Combine(dir.FullName, "route_paths_debug.png");

            using (var image = new Image<Rgba32>(10, 10))
            {
                var road = new Rgba32(255, 0, 255, 255);
                for (var y = 4; y <= 7; y++) image[4, y] = road;
                image.SaveAsPng(maskPath);
            }

            File.WriteAllText(nodesPath,
                "node_id,name,name_ru,node_type,region_id,land_access,sea_access,harbor_type,is_stub,comment\n" +
                "N_BRNO,Brno,Брно,settlement,RIVIA,True,False,none,False,test\n" +
                "N_RIVENSTAL,Rivenstal,Ривенсталь,settlement,RIVIA,True,True,pier,False,test\n");

            File.WriteAllText(edgesPath,
                "route_id,from_node,to_node,route_type,travel_days,travel_hours,is_bidirectional,route_frequency,is_stub,comment\n" +
                "R_BRNO_RIVENSTAL,N_BRNO,N_RIVENSTAL,road,3.0,72,True,common,False,test\n");

            new RoutePathExtractorTool().Generate(maskPath, edgesPath, nodesPath, outputPath);

            File.Exists(outputPath).Should().BeTrue();
            using var doc = JsonDocument.Parse(File.ReadAllText(outputPath));
            var paths = doc.RootElement.GetProperty("paths");
            paths.GetArrayLength().Should().Be(1);

            var path = paths[0];
            path.GetProperty("source_route_id").GetString().Should().Be("R_BRNO_RIVENSTAL");
            path.GetProperty("trade_route_id").GetString().Should().Be("brno_rivenstal_land");
            path.GetProperty("route_type").GetString().Should().Be("road");
            path.GetProperty("generation_method").GetString().Should().Be("mask");
            path.GetProperty("warnings").ValueKind.Should().Be(JsonValueKind.Array);
            var points = path.GetProperty("points").EnumerateArray().ToList();
            points.Count.Should().BeGreaterOrEqualTo(2);
            points.Should().OnlyContain(p =>
                p.GetProperty("x").GetDouble() >= 0d && p.GetProperty("x").GetDouble() <= 1d &&
                p.GetProperty("y").GetDouble() >= 0d && p.GetProperty("y").GetDouble() <= 1d);

            File.Exists(reportPath).Should().BeTrue();
            File.ReadAllText(reportPath).Should().Contain("generation_method=mask");
            File.ReadAllText(reportPath).Should().Contain("used_forced_direct_connector=false");
            File.ReadAllText(reportPath).Should().Contain("forced_connector_length_px=0.0");
            File.Exists(debugImagePath).Should().BeTrue();

            var routes = TradeRoutePresets.CreateDefaultRoutes();
            var result = new RoutePathLoader().TryLoadAndApply(outputPath, routes);
            result.Success.Should().BeTrue();
            routes.First(r => r.Id == "brno_rivenstal_land").HasLoadedPath.Should().BeTrue();
        }
        finally
        {
            dir.Delete(recursive: true);
        }
    }


    [Fact]
    public void SplitCsv_HandlesQuotedCommasAndEscapedQuotes()
    {
        var columns = RouteDataLoader.SplitCsv("N_1,\"Mlynek, North\",\"road \"\"main\"\"\",false");

        columns.Should().Equal("N_1", "Mlynek, North", "road \"main\"", "false");
    }

    [Theory]
    [InlineData("N_HIGHROCK", "N_GAVERN", "road", "highrock_gavern_land")]
    [InlineData("N_WODENZ", "N_TOKRUS", "sea", "wodenz_thokur_rus_sea")]
    [InlineData("Custom-Node", "Other Node", "land", "custom_node_other_node_land")]
    public void BuildTradeRouteId_MapsKnownNodesAndNormalizesCustomNames(string from, string to, string type, string expected)
    {
        RouteDataLoader.BuildTradeRouteId(from, to, type).Should().Be(expected);
    }

    [Fact]
    public void FindNearestMaskCandidates_ReturnsNearestCandidatesFirst()
    {
        var mask = new bool[10 * 10];
        mask[(1 * 10) + 1] = true;
        mask[(2 * 10) + 2] = true;
        mask[(7 * 10) + 7] = true;
        var fallback = new RouteConnectorFallback(new RouteExtractionOptions(), new RoutePathFinder(new RouteExtractionOptions()));

        var candidates = fallback.FindNearestMaskCandidates(mask, 10, 10, (0, 0), maxDistance: 4);

        candidates.Should().HaveCount(2);
        candidates.Select(c => c.Point).Should().Equal((1, 1), (2, 2));
        candidates.Select(c => c.Distance).Should().BeInAscendingOrder();
    }

    [Fact]
    public void TryDirectSettlementConnectorOverride_UsesForcedGavernRouteWhenWithinLimit()
    {
        var options = new RouteExtractionOptions { ForcedDirectGavernConnectorMaxPx = 20 };
        var fallback = new RouteConnectorFallback(options, new RoutePathFinder(options));

        var handled = fallback.TryDirectSettlementConnectorOverride(
            "R_RIVENSTAL_GAVERN",
            "Rivenstal",
            "Gavern",
            (0.0, 0.0),
            (1.0, 0.0),
            w: 11,
            h: 11,
            startAnchor: (1, 1),
            endAnchor: (4, 5),
            out var result);

        handled.Should().BeTrue();
        result.Success.Should().BeTrue();
        result.UsedForcedDirectConnector.Should().BeTrue();
        result.Connectors.Should().ContainSingle().Which.Should().Be(5.0);
    }

    [Fact]
    public void Simplify_RemovesCollinearIntermediatePointsAndKeepsTurns()
    {
        var points = new List<(int X, int Y)>
        {
            (0, 0),
            (1, 0),
            (2, 0),
            (3, 2),
            (4, 4),
            (5, 4)
        };

        var simplified = RoutePathFinder.Simplify(points, eps: 0.25);

        simplified.Should().Equal((0, 0), (2, 0), (4, 4), (5, 4));
    }
}
