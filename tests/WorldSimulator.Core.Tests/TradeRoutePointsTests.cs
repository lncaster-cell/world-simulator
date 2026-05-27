using FluentAssertions;
using WorldSimulator.Core.Trade;

namespace WorldSimulator.Core.Tests;

public sealed class TradeRoutePointsTests
{
    [Fact]
    public void TradeRoute_CanStorePoints()
    {
        var route = new TradeRoute
        {
            Id = "route",
            FromSettlementId = "a",
            ToSettlementId = "b",
            Type = CaravanType.Land,
            Distance = 10m,
            TravelDays = 1,
            DistanceDays = 1m,
            IsEnabled = true,
            Points = [new RoutePoint { X = 0.1m, Y = 0.2m }, new RoutePoint { X = 0.3m, Y = 0.4m }]
        };

        route.Points.Should().HaveCount(2);
    }

    [Fact]
    public void TradeRoute_DefaultPresetPoints_AreNormalizedAndNonEmpty()
    {
        var routes = TradeRoutePresets.CreateDefaultRoutes();
        routes.Should().OnlyContain(r => r.Points.Count >= 2);
        routes.Should().OnlyContain(r => r.Points.All(p => p.X >= 0m && p.X <= 1m && p.Y >= 0m && p.Y <= 1m));
    }

    [Fact]
    public void TradeRoute_DefaultPresetRoutes_HavePositiveDistanceDays()
    {
        var routes = TradeRoutePresets.CreateDefaultRoutes();
        routes.Should().OnlyContain(r => r.DistanceDays > 0m);
    }

    [Fact]
    public void RoutePathLoader_MissingFile_DoesNotMutateRoutes()
    {
        var routes = TradeRoutePresets.CreateDefaultRoutes();
        var before = routes.ToDictionary(r => r.Id, r => r.Points.Count);

        var loader = new RoutePathLoader();
        var result = loader.TryLoadAndApply(Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"), "route_paths.json"), routes);

        result.Status.Should().Be(RoutePathLoadStatus.Missing);
        routes.Should().OnlyContain(r => r.Points.Count == before[r.Id]);
    }

    [Fact]
    public void RoutePathLoader_MalformedFile_DoesNotMutateRoutes()
    {
        var routes = TradeRoutePresets.CreateDefaultRoutes();
        var routeId = "brno_rivenstal_land";
        var before = routes.First(r => r.Id == routeId).Points.Select(p => (p.X, p.Y)).ToArray();

        var path = Path.GetTempFileName();
        try
        {
            File.WriteAllText(path, "{ malformed json }");
            var loader = new RoutePathLoader();
            var result = loader.TryLoadAndApply(path, routes);

            result.Status.Should().Be(RoutePathLoadStatus.Failed);
            routes.First(r => r.Id == routeId).Points.Select(p => (p.X, p.Y)).Should().Equal(before);
        }
        finally
        {
            File.Delete(path);
        }
    }

    [Fact]
    public void RoutePathLoader_ValidFile_AppliesPoints_AndKeepsFallbackForMissingRoutes()
    {
        var routes = TradeRoutePresets.CreateDefaultRoutes();
        var untouchedRoute = routes.First(r => r.Id == "highrock_mlynek_land");
        var untouchedBefore = untouchedRoute.Points.Select(p => (p.X, p.Y)).ToArray();

        var path = Path.GetTempFileName();
        try
        {
            File.WriteAllText(path, """
            {
              "paths": [
                {
                  "trade_route_id": "brno_rivenstal_land",
                  "points": [
                    { "x": 0.11, "y": 0.22 },
                    { "x": 0.33, "y": 0.44 }
                  ]
                }
              ]
            }
            """);

            var loader = new RoutePathLoader();
            var result = loader.TryLoadAndApply(path, routes);

            result.Status.Should().Be(RoutePathLoadStatus.Found);
            result.LoadedPathCount.Should().Be(1);
            result.AppliedRouteCount.Should().Be(1);
            result.AppliedRouteIds.Should().Contain("brno_rivenstal_land");

            var route = routes.First(r => r.Id == "brno_rivenstal_land");
            route.Points.Should().HaveCount(2);
            route.Points[0].X.Should().Be(0.11m);
            route.Points[0].Y.Should().Be(0.22m);
            untouchedRoute.Points.Select(p => (p.X, p.Y)).Should().Equal(untouchedBefore);
        }
        finally
        {
            File.Delete(path);
        }
    }
}
