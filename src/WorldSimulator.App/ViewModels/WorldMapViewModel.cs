using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Input;
using System.Windows.Threading;
using WorldSimulator.App.Infrastructure;
using WorldSimulator.Core.Trade;
using WorldSimulator.Core.World;

namespace WorldSimulator.App.ViewModels;

public sealed class WorldMapViewModel : INotifyPropertyChanged
{
    private const int MaxVisibleCaravanMovementMarkers = 12;
    private readonly Action<string> _addTechnicalLogEntry;
    private readonly List<TradeRouteVisualViewModel> _tradeRouteVisuals = [];
    private readonly DispatcherTimer _tradeMarkerAnimationTimer;
    private WorldTradeFlowResult? _lastWeeklyTradeResult;
    private DateTimeOffset _lastTradeMarkerAnimationTickUtc;
    private double? _lastMapCalibrationX;
    private double? _lastMapCalibrationY;
    private bool _isMapCalibrationModeEnabled;
    private bool _isTradeRoutesOverlayVisible;
    private bool _isLoadedRoutePathsDebugVisible;
    private bool _isCaravanAnimationRunning;

    public WorldMapViewModel(Action<string> addTechnicalLogEntry)
    {
        _addTechnicalLogEntry = addTechnicalLogEntry;
        ToggleMapCalibrationModeCommand = new RelayCommand(ToggleMapCalibrationMode);
        _tradeMarkerAnimationTimer = new DispatcherTimer
        {
            Interval = TimeSpan.FromMilliseconds(100)
        };
        _tradeMarkerAnimationTimer.Tick += OnTradeMarkerAnimationTick;
        _tradeMarkerAnimationTimer.Start();
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    public ICommand ToggleMapCalibrationModeCommand { get; }

    public ObservableCollection<SettlementMapMarkerViewModel> SettlementMapMarkers { get; } = [];
    public ObservableCollection<CaravanMovementMarkerViewModel> ActiveCaravanMovementMarkers { get; } = [];
    public IReadOnlyList<TradeRouteVisualViewModel> TradeRouteVisuals => _tradeRouteVisuals;
    public IReadOnlyList<TradeRouteVisualViewModel> DebugLoadedRoutePathVisuals => _tradeRouteVisuals.Where(x => x.IsLoadedPath && x.Points.Count >= 2).ToList();

    public bool IsMapCalibrationModeEnabled
    {
        get => _isMapCalibrationModeEnabled;
        private set
        {
            if (_isMapCalibrationModeEnabled == value)
            {
                return;
            }

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
            if (_isTradeRoutesOverlayVisible == value)
            {
                return;
            }

            _isTradeRoutesOverlayVisible = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(TradeRoutesOverlayVisibility));
        }
    }

    public Visibility TradeRoutesOverlayVisibility =>
        IsTradeRoutesOverlayVisible ? Visibility.Visible : Visibility.Collapsed;

    public bool IsLoadedRoutePathsDebugVisible
    {
        get => _isLoadedRoutePathsDebugVisible;
        set
        {
            if (_isLoadedRoutePathsDebugVisible == value)
            {
                return;
            }

            _isLoadedRoutePathsDebugVisible = value;
            OnPropertyChanged();
        }
    }

    public bool IsCaravanAnimationRunning
    {
        get => _isCaravanAnimationRunning;
        set
        {
            if (_isCaravanAnimationRunning == value)
            {
                return;
            }

            _isCaravanAnimationRunning = value;
            _lastTradeMarkerAnimationTickUtc = DateTimeOffset.UtcNow;
        }
    }

    public void RegisterMapCalibrationPoint(double relativeX, double relativeY)
    {
        _lastMapCalibrationX = relativeX;
        _lastMapCalibrationY = relativeY;

        _addTechnicalLogEntry($"Калибровка карты: Гота X={relativeX:0.0000}, Y={relativeY:0.0000}");
        OnPropertyChanged(nameof(LastMapCalibrationPointDisplay));
    }

    public void RefreshSettlementMarkers(SimulationWorld world)
    {
        var citiesById = world.Cities.ToDictionary(c => c.Id, StringComparer.Ordinal);

        SettlementMapMarkers.Clear();
        foreach (var location in world.SettlementMapLocations
                     .Where(location => location.RegionId == world.SelectedRegionId && citiesById.ContainsKey(location.SettlementId)))
        {
            var city = citiesById[location.SettlementId];
            SettlementMapMarkers.Add(new SettlementMapMarkerViewModel
            {
                SettlementId = city.Id,
                DisplayName = city.Name,
                X = location.X,
                Y = location.Y,
                IsSelected = city.Id == world.SelectedCityId
            });
        }

        OnPropertyChanged(nameof(SettlementMapMarkers));
    }

    public void RefreshTradeRouteVisuals(SimulationWorld world)
    {
        RefreshTradeRouteVisuals(world, _lastWeeklyTradeResult);
    }

    public void RefreshTradeRouteVisuals(SimulationWorld world, WorldTradeFlowResult? weeklyTradeResult)
    {
        _lastWeeklyTradeResult = weeklyTradeResult;
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

        OnPropertyChanged(nameof(TradeRouteVisuals));
        OnPropertyChanged(nameof(DebugLoadedRoutePathVisuals));
    }

    public void RefreshCaravanMovementMarkers(SimulationWorld world, DateTimeOffset now)
    {
        _lastTradeMarkerAnimationTickUtc = now;
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

        OnPropertyChanged(nameof(ActiveCaravanMovementMarkers));
    }

    private void ToggleMapCalibrationMode()
    {
        IsMapCalibrationModeEnabled = !IsMapCalibrationModeEnabled;
        _addTechnicalLogEntry(IsMapCalibrationModeEnabled
            ? "Режим калибровки карты включен."
            : "Режим калибровки карты выключен.");
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
        if (!IsCaravanAnimationRunning)
        {
            return;
        }

        var progressDelta = deltaSeconds * 0.08d;

        foreach (var marker in ActiveCaravanMovementMarkers)
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

    private static MapPointViewModel ToMapPoint(RoutePoint point) => new() { X = (double)point.X, Y = (double)point.Y };

    private static MapPointViewModel CalculateDebugLabelPoint(IReadOnlyList<RoutePoint> points)
    {
        if (points.Count == 0)
        {
            return new MapPointViewModel { X = 0d, Y = 0d };
        }

        var point = points[points.Count / 2];
        return ToMapPoint(point);
    }

    private void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}
