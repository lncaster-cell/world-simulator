using WorldSimulator.Core.World;

namespace WorldSimulator.Core.Trade;

public static class TradeRoutePresets
{
    public static List<TradeRoute> CreateDefaultRoutes()
    {
        var routes = TradeRouteDataLoader.LoadRoutesFromEmbeddedJson();
        ApplyFallbackPoints(routes);
        return routes;
    }

    private static void ApplyFallbackPoints(IEnumerable<TradeRoute> routes)
    {
        foreach (var route in routes)
        {
            route.HasLoadedPath = false;
            if (route.Points.Count >= 2)
            {
                continue;
            }

            if (TryBuildFallbackPoints(route, out var fallbackPoints))
            {
                route.Points = fallbackPoints;
            }
        }
    }

    private static bool TryBuildFallbackPoints(TradeRoute route, out List<RoutePoint> points)
    {
        points = [];
        if (!RiviaSettlementPresets.TryGet(route.FromSettlementId, out var start))
        {
            return false;
        }

        if (!RiviaSettlementPresets.TryGet(route.ToSettlementId, out var end))
        {
            return false;
        }

        points.Add(new RoutePoint { X = start.X, Y = start.Y });
        points.Add(new RoutePoint { X = end.X, Y = end.Y });
        return true;
    }
}
