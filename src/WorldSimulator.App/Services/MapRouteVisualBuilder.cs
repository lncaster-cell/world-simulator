using WorldSimulator.App.ViewModels;
using WorldSimulator.Core.Trade;

namespace WorldSimulator.App.Services;

public sealed class MapRouteVisualBuilder
{
    public IReadOnlyList<TradeRouteVisualViewModel> BuildTradeRouteVisuals(
        IEnumerable<TradeRoute> tradeRoutes,
        WorldTradeFlowResult? weeklyTradeResult)
    {
        var volumeByRoute = (weeklyTradeResult?.Transfers ?? [])
            .GroupBy(x => x.RouteId, StringComparer.Ordinal)
            .ToDictionary(
                group => group.Key,
                CalculateWeeklyVolume,
                StringComparer.Ordinal);

        return tradeRoutes
            .Select(route =>
            {
                var volume = volumeByRoute.GetValueOrDefault(route.Id);
                return new TradeRouteVisualViewModel
                {
                    RouteId = route.Id,
                    FromSettlementId = route.FromSettlementId,
                    ToSettlementId = route.ToSettlementId,
                    DisplayName = $"{route.FromSettlementId} → {route.ToSettlementId}",
                    Points = route.Points.Select(ToMapPoint).ToList(),
                    IsActive = volume is not null,
                    IsLoadedPath = route.HasLoadedPath,
                    IsSeaRoute = route.Type == CaravanType.Sea,
                    DebugLabelPoint = CalculateDebugLabelPoint(route.Points),
                    WeeklyFoodMoved = volume?.Food ?? 0m,
                    WeeklyResourcesMoved = volume?.Resources ?? 0m,
                    WeeklyGoodsMoved = volume?.Goods ?? 0m
                };
            })
            .ToList();
    }

    private static RouteWeeklyVolume CalculateWeeklyVolume(IEnumerable<TradeTransferResult> transfers)
    {
        var food = 0m;
        var resources = 0m;
        var goods = 0m;
        foreach (var transfer in transfers)
        {
            switch (transfer.GoodType)
            {
                case TradeGoodType.Food:
                    food += transfer.AmountTransferred;
                    break;
                case TradeGoodType.Resources:
                    resources += transfer.AmountTransferred;
                    break;
                case TradeGoodType.Goods:
                    goods += transfer.AmountTransferred;
                    break;
            }
        }

        return new RouteWeeklyVolume(food, resources, goods);
    }

    private static MapPointViewModel ToMapPoint(RoutePoint point) => new() { X = (double)point.X, Y = (double)point.Y };

    private static MapPointViewModel CalculateDebugLabelPoint(IReadOnlyList<RoutePoint> points)
    {
        if (points.Count == 0) return new MapPointViewModel { X = 0d, Y = 0d };
        var point = points[points.Count / 2];
        return ToMapPoint(point);
    }

    private sealed record RouteWeeklyVolume(decimal Food, decimal Resources, decimal Goods);
}
