using WorldSimulator.Core.Trade;

namespace WorldSimulator.Persistence.Saves;

internal static class WorldSaveDefaults
{
    private static readonly Lazy<Dictionary<string, decimal>> DefaultRouteDistanceDaysCache = new(() =>
        GetDefaultRoutes()
            .GroupBy(route => route.Id, StringComparer.Ordinal)
            .ToDictionary(group => group.Key, group => group.First().DistanceDays, StringComparer.Ordinal));

    public static IReadOnlyDictionary<string, decimal> DefaultRouteDistanceDaysByRouteId => DefaultRouteDistanceDaysCache.Value;

    public static List<TradeRoute> GetDefaultRoutes() => TradeRoutePresets.CreateDefaultRoutes();

    public static decimal GetDefaultDistanceDays(string routeId)
    {
        return DefaultRouteDistanceDaysByRouteId.TryGetValue(routeId, out var distanceDays)
            ? distanceDays
            : 1m;
    }
}
