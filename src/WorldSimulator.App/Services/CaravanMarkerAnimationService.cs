using System.Collections.ObjectModel;
using System.Windows.Threading;
using WorldSimulator.App.ViewModels;
using WorldSimulator.Core.Time;

namespace WorldSimulator.App.Services;

public sealed class CaravanMarkerAnimationService
{
    private const int MaxVisibleCaravanMovementMarkers = 12;
    private readonly SimulationClock _clock;
    private readonly ObservableCollection<CaravanMovementMarkerViewModel> _markers;
    private readonly DispatcherTimer _tradeMarkerAnimationTimer;
    private DateTimeOffset _lastTradeMarkerAnimationTickUtc;

    public CaravanMarkerAnimationService(
        SimulationClock clock,
        ObservableCollection<CaravanMovementMarkerViewModel> markers)
    {
        _clock = clock;
        _markers = markers;

        _tradeMarkerAnimationTimer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(100) };
        _tradeMarkerAnimationTimer.Tick += OnTradeMarkerAnimationTick;
        _tradeMarkerAnimationTimer.Start();
    }

    public void ResetBaseline()
    {
        _lastTradeMarkerAnimationTickUtc = DateTimeOffset.UtcNow;
    }

    public void ClearMarkers()
    {
        _markers.Clear();
    }

    public void RefreshMarkers(IReadOnlyList<TradeRouteVisualViewModel> tradeRouteVisuals)
    {
        _markers.Clear();
        foreach (var routeVisual in tradeRouteVisuals
                     .Where(x => x.Points.Count >= 2 && x.IsLoadedPath)
                     .OrderByDescending(x => x.IsActive)
                     .ThenByDescending(x => x.TotalWeeklyVolume)
                     .Take(MaxVisibleCaravanMovementMarkers))
        {
            _markers.Add(new CaravanMovementMarkerViewModel
            {
                RouteId = routeVisual.RouteId,
                DisplayName = routeVisual.DisplayName,
                Points = routeVisual.Points,
                Progress = CalculateInitialCaravanProgress(routeVisual.RouteId),
                FoodMoved = routeVisual.WeeklyFoodMoved,
                ResourcesMoved = routeVisual.WeeklyResourcesMoved,
                GoodsMoved = routeVisual.WeeklyGoodsMoved,
                HasActiveFlow = routeVisual.IsActive && routeVisual.TotalWeeklyVolume > 0m
            });
        }
    }

    public static MapPointViewModel CalculatePointOnPolyline(IReadOnlyList<MapPointViewModel> points, double progress)
    {
        if (points.Count == 0) return new MapPointViewModel { X = 0d, Y = 0d };
        if (points.Count == 1) return points[0];
        var clamped = double.Clamp(progress, 0d, 1d);

        var segmentLengths = new double[points.Count - 1];
        var totalLength = 0d;
        for (var i = 0; i < points.Count - 1; i++)
        {
            var dx = points[i + 1].X - points[i].X;
            var dy = points[i + 1].Y - points[i].Y;
            var length = Math.Sqrt(dx * dx + dy * dy);
            segmentLengths[i] = length;
            totalLength += length;
        }

        if (totalLength <= 0d) return points[0];
        var targetLength = clamped * totalLength;
        var walked = 0d;
        for (var i = 0; i < segmentLengths.Length; i++)
        {
            var segmentLength = segmentLengths[i];
            if (segmentLength <= 0d) continue;
            if (walked + segmentLength >= targetLength)
            {
                var t = (targetLength - walked) / segmentLength;
                return new MapPointViewModel
                {
                    X = points[i].X + ((points[i + 1].X - points[i].X) * t),
                    Y = points[i].Y + ((points[i + 1].Y - points[i].Y) * t)
                };
            }

            walked += segmentLength;
        }

        return points[^1];
    }

    private void OnTradeMarkerAnimationTick(object? sender, EventArgs e)
    {
        if (_markers.Count == 0)
        {
            _lastTradeMarkerAnimationTickUtc = DateTimeOffset.UtcNow;
            return;
        }

        var now = DateTimeOffset.UtcNow;
        if (_lastTradeMarkerAnimationTickUtc == default)
        {
            _lastTradeMarkerAnimationTickUtc = now;
        }

        var deltaSeconds = (now - _lastTradeMarkerAnimationTickUtc).TotalSeconds;
        _lastTradeMarkerAnimationTickUtc = now;
        if (!_clock.IsRunning)
        {
            return;
        }

        var progressDelta = deltaSeconds * 0.08d;
        foreach (var marker in _markers)
        {
            marker.Progress += progressDelta;
        }
    }

    private static double CalculateInitialCaravanProgress(string routeId)
    {
        unchecked
        {
            var hash = 17;
            foreach (var ch in routeId)
            {
                hash = (hash * 31) + ch;
            }

            var normalized = Math.Abs(hash % 1000);
            return normalized / 1000d;
        }
    }
}
