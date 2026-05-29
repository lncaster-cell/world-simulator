using WorldSimulator.Core.Cities;
using WorldSimulator.Core.Trade;
using WorldSimulator.Core.World;

namespace WorldSimulator.App.Services;

public sealed class TradeRouteAuthoringService
{
    public TradeRoute SaveRoute(
        SimulationWorld world,
        City origin,
        City destination,
        decimal distanceDays,
        IReadOnlyList<RoutePoint> points)
    {
        var existingRoute = TradeRouteSelectionService.FindRouteBetween(world, origin, destination);
        var updatedRoute = new TradeRoute
        {
            Id = existingRoute?.Id ?? $"{origin.Id}_{destination.Id}",
            FromSettlementId = existingRoute?.FromSettlementId ?? origin.Id,
            ToSettlementId = existingRoute?.ToSettlementId ?? destination.Id,
            Type = existingRoute?.Type ?? InferRouteType(origin.Id, destination.Id),
            Distance = existingRoute?.Distance ?? 100m,
            TravelDays = existingRoute?.TravelDays ?? 3,
            DistanceDays = distanceDays,
            IsEnabled = existingRoute?.IsEnabled ?? true,
            DifficultyMultiplier = existingRoute?.DifficultyMultiplier ?? 1m,
            Points = points.ToList()
        };

        var routeIndex = existingRoute is null ? -1 : world.TradeRoutes.FindIndex(x => x.Id == existingRoute.Id);
        if (routeIndex >= 0)
        {
            world.TradeRoutes[routeIndex] = updatedRoute;
        }
        else
        {
            world.TradeRoutes.Add(updatedRoute);
        }

        return updatedRoute;
    }

    private static CaravanType InferRouteType(string fromId, string toId)
        => fromId == "thokur_rus" || toId == "thokur_rus" ? CaravanType.Sea : CaravanType.Land;
}
