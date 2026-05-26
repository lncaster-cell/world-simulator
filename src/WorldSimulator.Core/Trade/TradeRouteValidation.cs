using WorldSimulator.Core.World;

namespace WorldSimulator.Core.Trade;

public static class TradeRouteValidation
{
    public static bool IsValidPoint(RoutePoint point) => point.X >= 0m && point.X <= 1m && point.Y >= 0m && point.Y <= 1m;

    public static bool IsValidRoute(TradeRoute route, IReadOnlyCollection<string> settlementIds)
    {
        return settlementIds.Contains(route.FromSettlementId)
               && settlementIds.Contains(route.ToSettlementId)
               && route.Points.Count >= 2
               && route.Points.All(IsValidPoint)
               && route.Distance > 0m
               && route.TravelDays > 0;
    }

    public static TradeRoute? FindEnabledRoute(SimulationWorld world, string fromSettlementId, string toSettlementId, CaravanType caravanType)
    {
        return world.TradeRoutes
            .Where(r => r.IsEnabled && r.Type == caravanType && r.Distance > 0m && r.TravelDays > 0)
            .Where(r => (r.FromSettlementId == fromSettlementId && r.ToSettlementId == toSettlementId) || (r.FromSettlementId == toSettlementId && r.ToSettlementId == fromSettlementId))
            .OrderBy(r => r.Id, StringComparer.Ordinal)
            .FirstOrDefault();
    }
}
