using System.Globalization;
using System.Windows;
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
        var existingRoute = FindRouteBetween(world, origin, destination);
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

    public void CopyRoutePoints(
        string routeId,
        string originId,
        string destinationId,
        decimal distanceDays,
        IReadOnlyList<RoutePoint> points)
    {
        Clipboard.SetText(FormatRoutePoints(routeId, originId, destinationId, distanceDays, points));
    }

    public static TradeRoute? FindRouteBetween(SimulationWorld world, City from, City to) => world.TradeRoutes.FirstOrDefault(route =>
        (route.FromSettlementId == from.Id && route.ToSettlementId == to.Id)
        || (route.FromSettlementId == to.Id && route.ToSettlementId == from.Id));

    private static string FormatRoutePoints(
        string routeId,
        string originId,
        string destinationId,
        decimal distanceDays,
        IReadOnlyList<RoutePoint> points)
    {
        var lines = points.Select(x =>
            $"    new RoutePoint {{ X = {Math.Clamp(x.X, 0m, 1m).ToString("0.0000", CultureInfo.InvariantCulture)}m, Y = {Math.Clamp(x.Y, 0m, 1m).ToString("0.0000", CultureInfo.InvariantCulture)}m }}");

        return $"RouteId: {routeId}{Environment.NewLine}" +
               $"From: {originId}{Environment.NewLine}" +
               $"To: {destinationId}{Environment.NewLine}" +
               $"DistanceDays: {distanceDays.ToString("0.0###", CultureInfo.InvariantCulture)}{Environment.NewLine}{Environment.NewLine}" +
               $"Points ={Environment.NewLine}" +
               $"[{Environment.NewLine}{string.Join($",{Environment.NewLine}", lines)}{Environment.NewLine}]";
    }

    private static CaravanType InferRouteType(string fromId, string toId)
        => fromId == "thokur_rus" || toId == "thokur_rus" ? CaravanType.Sea : CaravanType.Land;
}
