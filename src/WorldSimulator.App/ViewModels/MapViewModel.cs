using System.Collections.ObjectModel;
using System.IO;
using System.Windows;
using System.Windows.Input;
using System.Windows.Threading;
using WorldSimulator.App.Infrastructure;
using WorldSimulator.Core.Simulation;
using WorldSimulator.Core.Time;
using WorldSimulator.Core.Trade;
using WorldSimulator.Core.World;

namespace WorldSimulator.App.ViewModels;

public sealed class MapViewModel : ViewModelBase
{
    private const int MaxVisibleCaravanMovementMarkers = 12;

    private readonly Func<SimulationWorld> _worldProvider;
    private readonly SimulationClock _clock;
    private readonly Action<string> _log;
    private readonly List<TradeRouteVisualViewModel> _tradeRouteVisuals = [];
    private readonly HashSet<string> _loadedCaravanPathRouteIds = new(StringComparer.OrdinalIgnoreCase);
    private readonly DispatcherTimer _tradeMarkerAnimationTimer;
    private DateTimeOffset _lastTradeMarkerAnimationTickUtc;
    private bool _isMapCalibrationModeEnabled;
    private double? _lastMapCalibrationX;
    private double? _lastMapCalibrationY;
    private bool _isTradeRoutesOverlayVisible;
    private bool _isLoadedRoutePathsDebugVisible;

    public MapViewModel(Func<SimulationWorld> worldProvider, SimulationClock clock, Action<string> log)
    {
        _worldProvider = worldProvider;
        _clock = clock;
        _log = log;
        ToggleMapCalibrationModeCommand = new RelayCommand(ToggleMapCalibrationMode);

        _tradeMarkerAnimationTimer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(100) };
        _tradeMarkerAnimationTimer.Tick += OnTradeMarkerAnimationTick;
        _tradeMarkerAnimationTimer.Start();
    }

    public ICommand ToggleMapCalibrationModeCommand { get; }
    public ObservableCollection<CaravanMovementMarkerViewModel> ActiveCaravanMovementMarkers { get; } = [];
    public IReadOnlyList<TradeRouteVisualViewModel> TradeRouteVisuals => _tradeRouteVisuals;
    public IReadOnlyList<TradeRouteVisualViewModel> DebugLoadedRoutePathVisuals => _tradeRouteVisuals.Where(x => x.IsLoadedPath && x.Points.Count >= 2).ToList();
    public IReadOnlyList<SettlementMapMarkerViewModel> SettlementMapMarkers => BuildSettlementMapMarkers();

    public bool IsMapCalibrationModeEnabled
    {
        get => _isMapCalibrationModeEnabled;
        private set
        {
            if (_isMapCalibrationModeEnabled == value) return;
            _isMapCalibrationModeEnabled = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(MapCalibrationToggleButtonText));
        }
    }

    public string MapCalibrationToggleButtonText => IsMapCalibrationModeEnabled
        ? "Выключить калибровку карты"
        : "Включить калибровку карты";

    public string LastMapCalibrationPointDisplay => _lastMapCalibrationX.HasValue && _lastMapCalibrationY.HasValue
        ? $"Последняя точка карты: X={_lastMapCalibrationX.Value:0.0000}, Y={_lastMapCalibrationY.Value:0.0000}"
        : "Последняя точка карты: нет";

    public bool IsTradeRoutesOverlayVisible
    {
        get => _isTradeRoutesOverlayVisible;
        set
        {
            if (_isTradeRoutesOverlayVisible == value) return;
            _isTradeRoutesOverlayVisible = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(TradeRoutesOverlayVisibility));
        }
    }

    public Visibility TradeRoutesOverlayVisibility => IsTradeRoutesOverlayVisible ? Visibility.Visible : Visibility.Collapsed;

    public bool IsLoadedRoutePathsDebugVisible
    {
        get => _isLoadedRoutePathsDebugVisible;
        set
        {
            if (_isLoadedRoutePathsDebugVisible == value) return;
            _isLoadedRoutePathsDebugVisible = value;
            OnPropertyChanged();
        }
    }

    public void RegisterMapCalibrationPoint(double relativeX, double relativeY)
    {
        _lastMapCalibrationX = relativeX;
        _lastMapCalibrationY = relativeY;
        _log($"Калибровка карты: Гота X={relativeX:0.0000}, Y={relativeY:0.0000}");
        OnPropertyChanged(nameof(LastMapCalibrationPointDisplay));
    }

    public void RefreshSelectedCityProperties()
    {
        OnPropertyChanged(nameof(SettlementMapMarkers));
    }

    public void ResetAnimationBaseline()
    {
        _lastTradeMarkerAnimationTickUtc = DateTimeOffset.UtcNow;
    }

    public void ClearSimulationCollections()
    {
        ActiveCaravanMovementMarkers.Clear();
        OnPropertyChanged(nameof(ActiveCaravanMovementMarkers));
    }

    public void LoadRoutePathsForWorld()
    {
        _loadedCaravanPathRouteIds.Clear();
        var path = TryFindRoutePathsJsonPath();
        if (path is null)
        {
            _log("route_paths.json не загружен: движение караванов по карте отключено.");
            return;
        }

        _log($"route_paths.json найден: {path}");
        var loader = new RoutePathLoader();
        var result = loader.TryLoadAndApply(path, _worldProvider().TradeRoutes);
        if (!result.Success)
        {
            _log($"route_paths.json not applied: {result.ErrorMessage ?? "unknown error"}.");
            _log("route_paths.json not applied: caravan movement markers disabled.");
            return;
        }

        foreach (var routeId in result.AppliedRouteIds)
        {
            _loadedCaravanPathRouteIds.Add(routeId);
        }

        var unmatched = Math.Max(0, result.ParsedPathCount - result.AppliedRouteCount);
        _log($"route_paths.json parsed paths count: {result.ParsedPathCount}; applied route count: {result.AppliedRouteCount}; unmatched: {unmatched}.");
        if (result.AppliedRouteCount == 0)
        {
            _log("route_paths.json not applied: caravan movement markers disabled.");
        }
    }

    public void RefreshTradeRouteVisuals(WorldTradeFlowResult? weeklyTradeResult)
    {
        var world = _worldProvider();
        var volumeByRoute = (weeklyTradeResult?.Transfers ?? [])
            .GroupBy(x => x.RouteId, StringComparer.Ordinal)
            .ToDictionary(
                group => group.Key,
                group => new
                {
                    Food = group.Where(x => x.GoodType == TradeGoodType.Food).Sum(x => x.AmountTransferred),
                    Resources = group.Where(x => x.GoodType == TradeGoodType.Resources).Sum(x => x.AmountTransferred),
                    Goods = group.Where(x => x.GoodType == TradeGoodType.Goods).Sum(x => x.AmountTransferred)
                },
                StringComparer.Ordinal);

        _tradeRouteVisuals.Clear();
        foreach (var route in world.TradeRoutes)
        {
            var volume = volumeByRoute.GetValueOrDefault(route.Id);
            _tradeRouteVisuals.Add(new TradeRouteVisualViewModel
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
            });
        }

        ActiveCaravanMovementMarkers.Clear();
        foreach (var routeVisual in _tradeRouteVisuals
                     .Where(x => x.Points.Count >= 2 && world.TradeRoutes.Any(r => string.Equals(r.Id, x.RouteId, StringComparison.Ordinal) && r.HasLoadedPath))
                     .OrderByDescending(x => x.IsActive)
                     .ThenByDescending(x => x.TotalWeeklyVolume)
                     .Take(MaxVisibleCaravanMovementMarkers))
        {
            ActiveCaravanMovementMarkers.Add(new CaravanMovementMarkerViewModel
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

        OnPropertyChanged(nameof(TradeRouteVisuals));
        OnPropertyChanged(nameof(DebugLoadedRoutePathVisuals));
        OnPropertyChanged(nameof(ActiveCaravanMovementMarkers));
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

    private IReadOnlyList<SettlementMapMarkerViewModel> BuildSettlementMapMarkers()
    {
        var world = _worldProvider();
        var citiesById = world.Cities.ToDictionary(c => c.Id, StringComparer.Ordinal);

        return world.SettlementMapLocations
            .Where(location => location.RegionId == world.SelectedRegionId && citiesById.ContainsKey(location.SettlementId))
            .Select(location =>
            {
                var city = citiesById[location.SettlementId];
                return new SettlementMapMarkerViewModel
                {
                    SettlementId = city.Id,
                    DisplayName = city.Name,
                    X = location.X,
                    Y = location.Y,
                    IsSelected = city.Id == world.SelectedCityId
                };
            })
            .ToList();
    }

    private void ToggleMapCalibrationMode()
    {
        IsMapCalibrationModeEnabled = !IsMapCalibrationModeEnabled;
        _log(IsMapCalibrationModeEnabled ? "Режим калибровки карты включен." : "Режим калибровки карты выключен.");
    }

    private void OnTradeMarkerAnimationTick(object? sender, EventArgs e)
    {
        if (ActiveCaravanMovementMarkers.Count == 0)
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
        foreach (var marker in ActiveCaravanMovementMarkers)
        {
            marker.Progress += progressDelta;
        }
    }

    private static string? TryFindRoutePathsJsonPath()
    {
        var relativePath = Path.Combine("data", "regions", "rivia", "routes", "v1", "route_paths.json");
        var directPath = Path.Combine(AppContext.BaseDirectory, relativePath);
        if (File.Exists(directPath)) return directPath;

        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir is not null)
        {
            var candidate = Path.Combine(dir.FullName, relativePath);
            if (File.Exists(candidate)) return candidate;
            dir = dir.Parent;
        }

        return null;
    }

    private static MapPointViewModel ToMapPoint(RoutePoint point) => new() { X = (double)point.X, Y = (double)point.Y };

    private static MapPointViewModel CalculateDebugLabelPoint(IReadOnlyList<RoutePoint> points)
    {
        if (points.Count == 0) return new MapPointViewModel { X = 0d, Y = 0d };
        var point = points[points.Count / 2];
        return ToMapPoint(point);
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
