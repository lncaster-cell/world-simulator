using System.Collections.ObjectModel;
using System.IO;
using System.Windows;
using System.Windows.Input;
using WorldSimulator.App.Infrastructure;
using WorldSimulator.App.Services;
using WorldSimulator.Core.Simulation;
using WorldSimulator.Core.Time;
using WorldSimulator.Core.Trade;
using WorldSimulator.Core.World;

namespace WorldSimulator.App.ViewModels;

public sealed class MapViewModel : ViewModelBase
{
    private readonly Func<SimulationWorld> _worldProvider;
    private readonly Action<string> _log;
    private readonly List<TradeRouteVisualViewModel> _tradeRouteVisuals = [];
    private readonly MapRouteVisualBuilder _mapRouteVisualBuilder = new();
    private readonly CaravanMarkerAnimationService _caravanMarkerAnimationService;
    private bool _isMapCalibrationModeEnabled;
    private double? _lastMapCalibrationX;
    private double? _lastMapCalibrationY;
    private bool _isTradeRoutesOverlayVisible;
    private bool _isLoadedRoutePathsDebugVisible;

    public MapViewModel(Func<SimulationWorld> worldProvider, SimulationClock clock, Action<string> log)
    {
        _worldProvider = worldProvider;
        _log = log;
        ToggleMapCalibrationModeCommand = new RelayCommand(ToggleMapCalibrationMode);
        _caravanMarkerAnimationService = new CaravanMarkerAnimationService(clock, ActiveCaravanMovementMarkers);
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
        _caravanMarkerAnimationService.ResetBaseline();
    }

    public void ClearSimulationCollections()
    {
        _caravanMarkerAnimationService.ClearMarkers();
        OnPropertyChanged(nameof(ActiveCaravanMovementMarkers));
    }

    public void LoadRoutePathsForWorld()
    {
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
        _tradeRouteVisuals.Clear();
        _tradeRouteVisuals.AddRange(_mapRouteVisualBuilder.BuildTradeRouteVisuals(world.TradeRoutes, weeklyTradeResult));
        _caravanMarkerAnimationService.RefreshMarkers(_tradeRouteVisuals);

        OnPropertyChanged(nameof(TradeRouteVisuals));
        OnPropertyChanged(nameof(DebugLoadedRoutePathVisuals));
        OnPropertyChanged(nameof(ActiveCaravanMovementMarkers));
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
}
