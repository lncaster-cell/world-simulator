using System.Text.Json;
using WorldSimulator.Core.Trade;
using WorldSimulator.Core.World;

namespace WorldSimulator.Core.Tests;

public sealed class TradeRoutePresetTests
{
    [Fact]
    public void DefaultRoutes_AllEndpointsExistAmongRiviaSettlementsAndHaveCoordinates()
    {
        var settlementIds = RiviaSettlementPresets.All
            .Select(x => x.Id)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);
        var routes = TradeRoutePresets.CreateDefaultRoutes();

        routes.Should().OnlyContain(route =>
            EndpointExistsAndHasCoordinates(route.FromSettlementId, settlementIds) &&
            EndpointExistsAndHasCoordinates(route.ToSettlementId, settlementIds));
    }

    [Fact]
    public void DefaultRoutes_LoadOnlyRouteGraphResourceAndIgnoreAuthoringMetaFile()
    {
        var routesDirectory = Path.Combine(FindRepositoryRoot(), "data", "regions", "rivia", "routes", "v1");
        var routeGraphPath = Path.Combine(routesDirectory, "rivia_routes.json");
        var metaPath = Path.Combine(routesDirectory, "rivia_routes_meta.json");

        using var routeGraph = JsonDocument.Parse(File.ReadAllText(routeGraphPath));
        var routeGraphFields = routeGraph.RootElement.EnumerateObject().Select(property => property.Name).ToArray();
        routeGraphFields.Should().Equal("schema_version", "region_id", "units", "nodes", "edges");

        File.Exists(metaPath).Should().BeTrue("authoring TODOs and notes should stay outside the runtime route graph");
        using var meta = JsonDocument.Parse(File.ReadAllText(metaPath));
        meta.RootElement.TryGetProperty("todo", out _).Should().BeTrue();
        meta.RootElement.TryGetProperty("notes", out _).Should().BeTrue();

        var embeddedResources = typeof(TradeRoutePresets).Assembly.GetManifestResourceNames();
        embeddedResources.Should().Contain("WorldSimulator.Core.Data.rivia_routes.json");
        embeddedResources.Should().NotContain(resource => resource.Contains("rivia_routes_meta", StringComparison.OrdinalIgnoreCase));

        var routes = TradeRoutePresets.CreateDefaultRoutes();
        var expectedRuntimeRouteCount = routeGraph.RootElement.GetProperty("edges")
            .EnumerateArray()
            .Count(edge => !edge.GetProperty("is_stub").GetBoolean());

        routes.Should().HaveCount(expectedRuntimeRouteCount);
        routes.Should().OnlyContain(route =>
            !string.IsNullOrWhiteSpace(route.FromSettlementId) &&
            !string.IsNullOrWhiteSpace(route.ToSettlementId) &&
            route.DistanceDays > 0m);
    }

    private static bool EndpointExistsAndHasCoordinates(string settlementId, IReadOnlySet<string> settlementIds)
    {
        if (!settlementIds.Contains(settlementId))
        {
            return false;
        }

        if (!RiviaSettlementPresets.TryGet(settlementId, out var preset))
        {
            return false;
        }

        return preset.X is >= 0m and <= 1m && preset.Y is >= 0m and <= 1m;
    }

    private static string FindRepositoryRoot()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null)
        {
            if (Directory.Exists(Path.Combine(directory.FullName, "data", "regions", "rivia", "routes", "v1")))
            {
                return directory.FullName;
            }

            directory = directory.Parent;
        }

        throw new DirectoryNotFoundException("Could not locate repository root from test output directory.");
    }
}
