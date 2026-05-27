using System.IO;
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
    public void TradeRoute_DefaultPresetPoints_AreNormalized()
    {
        var routes = TradeRoutePresets.CreateDefaultRoutes();
        routes.Should().OnlyContain(r => r.Points.All(p => p.X >= 0m && p.X <= 1m && p.Y >= 0m && p.Y <= 1m));
    }

    [Fact]
    public void TradeRoute_DefaultPresetRoutes_HavePositiveDistanceDays()
    {
        var routes = TradeRoutePresets.CreateDefaultRoutes();
        routes.Should().OnlyContain(r => r.DistanceDays > 0m);
    }


    [Fact]
    public void RoutePathsLoader_DoesNotThrow_WhenFileIsMissing()
    {
        var path = Path.Combine(AppContext.BaseDirectory, "data", "regions", "rivia", "routes", "v1", "route_paths.json");
        if (File.Exists(path)) File.Delete(path);

        var act = () => TradeRoutePresets.CreateDefaultRoutes();
        act.Should().NotThrow();
    }

    [Fact]
    public void RoutePathsLoader_AppliesPoints_ByUnorderedEndpointsId()
    {
        var dir = Path.Combine(AppContext.BaseDirectory, "data", "regions", "rivia", "routes", "v1");
        Directory.CreateDirectory(dir);
        var path = Path.Combine(dir, "route_paths.json");
        File.WriteAllText(path, """
        {
          "schema_version": "rivia_route_paths_v1",
          "region_id": "RIVIA",
          "paths": [
            {
              "trade_route_id": "gavern_brno",
              "points": [
                { "x": 0.11, "y": 0.22 },
                { "x": 0.33, "y": 0.44 }
              ]
            }
          ]
        }
        """);

        var routes = TradeRoutePresets.CreateDefaultRoutes();
        var route = routes.First(r => r.Id == "brno_gavern_land");

        route.Points.Should().HaveCount(2);
        route.Points[0].X.Should().Be(0.11m);
        route.Points[0].Y.Should().Be(0.22m);
    }

    [Fact]
    public void RoutePathsLoader_ClearsPoints_WhenRoutePathNotFoundInJson()
    {
        var dir = Path.Combine(AppContext.BaseDirectory, "data", "regions", "rivia", "routes", "v1");
        Directory.CreateDirectory(dir);
        File.WriteAllText(Path.Combine(dir, "route_paths.json"), """
        {
          "paths": [
            {
              "trade_route_id": "brno_gavern",
              "points": [
                { "x": 0.1, "y": 0.1 },
                { "x": 0.2, "y": 0.2 }
              ]
            }
          ]
        }
        """);

        var routes = TradeRoutePresets.CreateDefaultRoutes();
        var missingRoute = routes.First(r => r.Id == "highrock_mlynek_land");

        missingRoute.Points.Should().BeEmpty();
    }
}

