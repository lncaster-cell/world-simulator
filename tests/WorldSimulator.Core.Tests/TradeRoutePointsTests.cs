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
}
