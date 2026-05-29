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

        result.FileFound.Should().BeFalse();
        result.Success.Should().BeFalse();
        routes.Should().OnlyContain(r => r.Points.Count == before[r.Id]);
        routes.Should().OnlyContain(r => r.HasLoadedPath == false);
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

            result.Success.Should().BeFalse();
            result.FileFound.Should().BeTrue();
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

            result.Success.Should().BeTrue();
            result.ParsedPathCount.Should().Be(1);
            result.AppliedRouteCount.Should().Be(1);
            result.AppliedRouteIds.Should().Contain("brno_rivenstal_land");

            var route = routes.First(r => r.Id == "brno_rivenstal_land");
            route.Points.Should().HaveCount(2);
            route.Points[0].X.Should().Be(0.11m);
            route.Points[0].Y.Should().Be(0.22m);
            untouchedRoute.Points.Select(p => (p.X, p.Y)).Should().Equal(untouchedBefore);
            untouchedRoute.HasLoadedPath.Should().BeFalse();
            route.HasLoadedPath.Should().BeTrue();
        }
        finally
        {
            File.Delete(path);
        }
    }

    [Fact]
    public void RoutePathLoader_ReversedSettlementIdMatch_AppliesReversedCopyWithoutMutatingSourcePath()
    {
        var reverseMatchedRoute = new TradeRoute
        {
            Id = "route_a_b",
            FromSettlementId = "a",
            ToSettlementId = "b",
            Type = CaravanType.Land,
            Distance = 10m,
            TravelDays = 1,
            DistanceDays = 1m,
            IsEnabled = true,
            Points = [new RoutePoint { X = 0m, Y = 0m }, new RoutePoint { X = 1m, Y = 1m }]
        };
        var directlyMatchedRoute = new TradeRoute
        {
            Id = "b_a",
            FromSettlementId = "b",
            ToSettlementId = "a",
            Type = CaravanType.Land,
            Distance = 10m,
            TravelDays = 1,
            DistanceDays = 1m,
            IsEnabled = true,
            Points = [new RoutePoint { X = 0m, Y = 0m }, new RoutePoint { X = 1m, Y = 1m }]
        };
        var routes = new List<TradeRoute> { reverseMatchedRoute, directlyMatchedRoute };

        var path = Path.GetTempFileName();
        try
        {
            File.WriteAllText(path, """
            {
              "paths": [
                {
                  "trade_route_id": "b_a",
                  "points": [
                    { "x": 0.11, "y": 0.22 },
                    { "x": 0.33, "y": 0.44 },
                    { "x": 0.55, "y": 0.66 }
                  ]
                }
              ]
            }
            """);

            var loader = new RoutePathLoader();
            var result = loader.TryLoadAndApply(path, routes);

            result.Success.Should().BeTrue();
            result.AppliedRouteCount.Should().Be(2);

            reverseMatchedRoute.Points.Should().HaveCount(3);
            reverseMatchedRoute.Points[0].X.Should().Be(0.55m);
            reverseMatchedRoute.Points[0].Y.Should().Be(0.66m);
            reverseMatchedRoute.Points[^1].X.Should().Be(0.11m);
            reverseMatchedRoute.Points[^1].Y.Should().Be(0.22m);

            directlyMatchedRoute.Points.Should().HaveCount(3);
            directlyMatchedRoute.Points[0].X.Should().Be(0.11m);
            directlyMatchedRoute.Points[0].Y.Should().Be(0.22m);
            directlyMatchedRoute.Points[^1].X.Should().Be(0.55m);
            directlyMatchedRoute.Points[^1].Y.Should().Be(0.66m);
        }
        finally
        {
            File.Delete(path);
        }
    }

    [Fact]
    public void TradeRoute_DefaultPresetRoutes_HaveFallbackPoints_AndNotLoadedByDefault()
    {
        var routes = TradeRoutePresets.CreateDefaultRoutes();
        routes.Should().OnlyContain(r => r.Points.Count >= 2);
        routes.Should().OnlyContain(r => r.HasLoadedPath == false);
    }

    [Fact]
    public void RoutePathLoader_UnmatchedIds_DoNotClearFallbackPoints()
    {
        var routes = TradeRoutePresets.CreateDefaultRoutes();
        var before = routes.ToDictionary(r => r.Id, r => r.Points.Select(p => (p.X, p.Y)).ToArray());

        var path = Path.GetTempFileName();
        try
        {
            File.WriteAllText(path, """
            {
              "paths": [
                {
                  "trade_route_id": "missing_route_land",
                  "points": [
                    { "x": 0.1, "y": 0.2 },
                    { "x": 0.3, "y": 0.4 }
                  ]
                }
              ]
            }
            """);

            var loader = new RoutePathLoader();
            var result = loader.TryLoadAndApply(path, routes);

            result.Success.Should().BeTrue();
            result.AppliedRouteCount.Should().Be(0);

            foreach (var route in routes)
            {
                route.HasLoadedPath.Should().BeFalse();
                route.Points
                    .Select(p => (p.X, p.Y))
                    .Should()
                    .Equal(before[route.Id]);
            }
        }
        finally
        {
            File.Delete(path);
        }
    }

}