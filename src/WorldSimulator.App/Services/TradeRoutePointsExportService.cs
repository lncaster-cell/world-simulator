using System.Globalization;
using System.Windows;
using WorldSimulator.Core.Trade;

namespace WorldSimulator.App.Services;

public sealed class TradeRoutePointsExportService
{
    public void CopyRoutePoints(
        string routeId,
        string originId,
        string destinationId,
        decimal distanceDays,
        IReadOnlyList<RoutePoint> points)
    {
        Clipboard.SetText(FormatRoutePoints(routeId, originId, destinationId, distanceDays, points));
    }

    public string FormatRoutePoints(
        string routeId,
        string originId,
        string destinationId,
        decimal distanceDays,
        IReadOnlyList<RoutePoint> points)
    {
        var lines = points.Select(point =>
            $"    new RoutePoint {{ X = {Math.Clamp(point.X, 0m, 1m).ToString("0.0000", CultureInfo.InvariantCulture)}m, Y = {Math.Clamp(point.Y, 0m, 1m).ToString("0.0000", CultureInfo.InvariantCulture)}m }}");

        return $"RouteId: {routeId}{Environment.NewLine}" +
               $"From: {originId}{Environment.NewLine}" +
               $"To: {destinationId}{Environment.NewLine}" +
               $"DistanceDays: {distanceDays.ToString("0.0###", CultureInfo.InvariantCulture)}{Environment.NewLine}{Environment.NewLine}" +
               $"Points ={Environment.NewLine}" +
               $"[{Environment.NewLine}{string.Join($",{Environment.NewLine}", lines)}{Environment.NewLine}]";
    }
}
