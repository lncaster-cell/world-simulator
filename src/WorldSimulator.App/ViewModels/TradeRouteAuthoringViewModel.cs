using System.Collections.ObjectModel;
using System.Globalization;
using System.Windows;
using System.Windows.Input;
using WorldSimulator.App.Infrastructure;
using WorldSimulator.Core.Cities;
using WorldSimulator.Core.Trade;
using WorldSimulator.Core.World;

namespace WorldSimulator.App.ViewModels;

public sealed class TradeRouteAuthoringViewModel : ViewModelBase
{
    private readonly Func<SimulationWorld> _worldProvider;
    private readonly Action<string> _log;
    private readonly Action _routesChanged;
    private readonly Action _refreshTradeRouteVisuals;
    private TradeRoute? _selectedTradeRouteForAuthoring;
    private City? _routeAuthoringOriginSettlement;
    private City? _routeAuthoringDestinationCandidateSettlement;
    private City? _activeRouteAuthoringDestinationSettlement;
    private string _routeAuthoringRouteIdDisplay = "RouteId: —";
    private string? _currentDraftDestinationId;
    private readonly Dictionary<string, List<MapPointViewModel>> _routeAuthoringDraftPointsByDestinationId = [];
    private bool _isTradeRouteAuthoringModeEnabled;
    private decimal _selectedTradeRouteDistanceDays = 1m;
    private string _selectedTradeRouteDistanceDaysInput = "1.0";

    public TradeRouteAuthoringViewModel(Func<SimulationWorld> worldProvider, Action<string> log, Action routesChanged, Action refreshTradeRouteVisuals)
    {
        _worldProvider = worldProvider;
        _log = log;
        _routesChanged = routesChanged;
        _refreshTradeRouteVisuals = refreshTradeRouteVisuals;

        AddTradeRoutePointCommand = new RelayCommand<MapPointViewModel>(AddTradeRoutePoint);
        UndoTradeRoutePointCommand = new RelayCommand(UndoTradeRoutePoint);
        ClearTradeRoutePointsCommand = new RelayCommand(ClearTradeRoutePoints);
        SaveTradeRoutePointsCommand = new RelayCommand(SaveTradeRoutePoints);
        CopyTradeRoutePointsCommand = new RelayCommand(CopyTradeRoutePoints);
        AddRouteAuthoringDestinationCommand = new RelayCommand(AddRouteAuthoringDestination);

        EditedTradeRoutePoints.CollectionChanged += (_, _) =>
        {
            OnPropertyChanged(nameof(EditedTradeRoutePointCount));
            OnPropertyChanged(nameof(EditedTradeRouteIntermediatePointCount));
            OnPropertyChanged(nameof(EditedTradeRoutePolylinePoints));
        };
    }

    public ICommand AddTradeRoutePointCommand { get; }
    public ICommand UndoTradeRoutePointCommand { get; }
    public ICommand ClearTradeRoutePointsCommand { get; }
    public ICommand SaveTradeRoutePointsCommand { get; }
    public ICommand CopyTradeRoutePointsCommand { get; }
    public ICommand AddRouteAuthoringDestinationCommand { get; }

    public IReadOnlyList<City> RouteAuthoringSettlements => _worldProvider().Cities;
    public IReadOnlyList<City> AvailableRouteAuthoringDestinations => _worldProvider().Cities;
    public ObservableCollection<City> RouteAuthoringDestinationSettlements { get; } = [];
    public ObservableCollection<MapPointViewModel> EditedTradeRoutePoints { get; } = [];
    public int EditedTradeRoutePointCount => EditedTradeRoutePoints.Count;
    public int EditedTradeRouteIntermediatePointCount => EditedTradeRoutePoints.Count;
    public int RouteAuthoringDestinationCount => RouteAuthoringDestinationSettlements.Count;
    public bool CanAddMoreRouteAuthoringDestinations => RouteAuthoringDestinationSettlements.Count < 8;
    public bool HasSelectedTradeRouteForAuthoring => SelectedTradeRouteForAuthoring is not null;
    public List<RoutePoint> EditedTradeRoutePolylinePoints => BuildFullRoutePoints();
    public string RouteAuthoringRouteIdDisplay => _routeAuthoringRouteIdDisplay;

    public bool IsTradeRouteAuthoringModeEnabled
    {
        get => _isTradeRouteAuthoringModeEnabled;
        set
        {
            if (_isTradeRouteAuthoringModeEnabled == value) return;
            _isTradeRouteAuthoringModeEnabled = value;
            OnPropertyChanged();
        }
    }

    public City? RouteAuthoringOriginSettlement
    {
        get => _routeAuthoringOriginSettlement;
        set
        {
            if (_routeAuthoringOriginSettlement == value) return;
            _routeAuthoringOriginSettlement = value;
            OnPropertyChanged();
            ResetRouteAuthoringOriginState();
        }
    }

    public City? RouteAuthoringDestinationCandidateSettlement
    {
        get => _routeAuthoringDestinationCandidateSettlement;
        set
        {
            if (_routeAuthoringDestinationCandidateSettlement == value) return;
            _routeAuthoringDestinationCandidateSettlement = value;
            OnPropertyChanged();
        }
    }

    public City? ActiveRouteAuthoringDestinationSettlement
    {
        get => _activeRouteAuthoringDestinationSettlement;
        set
        {
            if (_activeRouteAuthoringDestinationSettlement == value) return;
            SaveCurrentActiveDraftPoints();
            _activeRouteAuthoringDestinationSettlement = value;
            OnPropertyChanged();
            LoadActiveRouteDraftOrExisting();
        }
    }

    public TradeRoute? SelectedTradeRouteForAuthoring
    {
        get => _selectedTradeRouteForAuthoring;
        set
        {
            if (_selectedTradeRouteForAuthoring == value) return;
            _selectedTradeRouteForAuthoring = value;
            ReloadEditedTradeRoutePointsFromSelectedRoute();
            OnPropertyChanged();
            OnPropertyChanged(nameof(HasSelectedTradeRouteForAuthoring));
        }
    }

    public decimal SelectedTradeRouteDistanceDays
    {
        get => _selectedTradeRouteDistanceDays;
        set
        {
            if (_selectedTradeRouteDistanceDays == value) return;
            _selectedTradeRouteDistanceDays = value;
            OnPropertyChanged();
        }
    }

    public string SelectedTradeRouteDistanceDaysInput
    {
        get => _selectedTradeRouteDistanceDaysInput;
        set
        {
            if (_selectedTradeRouteDistanceDaysInput == value) return;
            _selectedTradeRouteDistanceDaysInput = value;
            OnPropertyChanged();
        }
    }

    public void InitializeForWorld()
    {
        SelectedTradeRouteForAuthoring = _worldProvider().TradeRoutes.FirstOrDefault();
        OnPropertyChanged(nameof(RouteAuthoringSettlements));
        OnPropertyChanged(nameof(AvailableRouteAuthoringDestinations));
    }

    public void RegisterTradeRouteAuthoringPoint(double relativeX, double relativeY)
    {
        if (!IsTradeRouteAuthoringModeEnabled) return;
        if (RouteAuthoringOriginSettlement is null || ActiveRouteAuthoringDestinationSettlement is null)
        {
            _log("Выберите пункт отправления и активный пункт назначения.");
            return;
        }

        AddTradeRoutePoint(new MapPointViewModel { X = Math.Clamp(relativeX, 0d, 1d), Y = Math.Clamp(relativeY, 0d, 1d) });
    }

    public void ResetRouteAuthoringStateForWorldReset()
    {
        _routeAuthoringDraftPointsByDestinationId.Clear();
        _currentDraftDestinationId = null;
        _routeAuthoringOriginSettlement = null;
        _routeAuthoringDestinationCandidateSettlement = null;
        _activeRouteAuthoringDestinationSettlement = null;
        RouteAuthoringDestinationSettlements.Clear();
        EditedTradeRoutePoints.Clear();
        SelectedTradeRouteDistanceDays = 1m;
        SelectedTradeRouteDistanceDaysInput = "1.0";
        _routeAuthoringRouteIdDisplay = "RouteId: —";
        _selectedTradeRouteForAuthoring = null;

        OnPropertyChanged(nameof(RouteAuthoringOriginSettlement));
        OnPropertyChanged(nameof(RouteAuthoringDestinationCandidateSettlement));
        OnPropertyChanged(nameof(ActiveRouteAuthoringDestinationSettlement));
        OnPropertyChanged(nameof(SelectedTradeRouteForAuthoring));
        OnPropertyChanged(nameof(HasSelectedTradeRouteForAuthoring));
        OnPropertyChanged(nameof(RouteAuthoringDestinationCount));
        OnPropertyChanged(nameof(CanAddMoreRouteAuthoringDestinations));
        OnPropertyChanged(nameof(RouteAuthoringRouteIdDisplay));
        OnPropertyChanged(nameof(RouteAuthoringSettlements));
        OnPropertyChanged(nameof(AvailableRouteAuthoringDestinations));
    }

    private void AddTradeRoutePoint(MapPointViewModel? point)
    {
        if (point is null) return;
        EditedTradeRoutePoints.Add(new MapPointViewModel { X = Math.Clamp(point.X, 0d, 1d), Y = Math.Clamp(point.Y, 0d, 1d) });
    }

    private void UndoTradeRoutePoint()
    {
        if (EditedTradeRoutePoints.Count > 0) EditedTradeRoutePoints.RemoveAt(EditedTradeRoutePoints.Count - 1);
    }

    private void ClearTradeRoutePoints() => EditedTradeRoutePoints.Clear();

    private void SaveTradeRoutePoints()
    {
        if (RouteAuthoringOriginSettlement is null || ActiveRouteAuthoringDestinationSettlement is null)
        {
            _log("Маршрут не сохранён: выберите пункт отправления и активный пункт назначения.");
            return;
        }
        if (RouteAuthoringOriginSettlement.Id == ActiveRouteAuthoringDestinationSettlement.Id)
        {
            _log("Маршрут не сохранён: отправление и назначение совпадают.");
            return;
        }
        if (!decimal.TryParse(SelectedTradeRouteDistanceDaysInput, NumberStyles.Number, CultureInfo.InvariantCulture, out var parsedDistanceDays))
        {
            _log("Маршрут не сохранён: укажите корректное значение дней пути.");
            return;
        }
        if (parsedDistanceDays < 0.1m)
        {
            _log("Маршрут не сохранён: дней пути должно быть не меньше 0.1.");
            return;
        }

        SelectedTradeRouteDistanceDays = parsedDistanceDays;
        var fullPoints = BuildFullRoutePoints();
        if (fullPoints.Count < 2)
        {
            _log("Маршрут не сохранён: не удалось определить начальную и конечную точки.");
            return;
        }

        var world = _worldProvider();
        var existingRoute = FindRouteBetween(RouteAuthoringOriginSettlement, ActiveRouteAuthoringDestinationSettlement);
        var updatedRoute = new TradeRoute
        {
            Id = existingRoute?.Id ?? $"{RouteAuthoringOriginSettlement.Id}_{ActiveRouteAuthoringDestinationSettlement.Id}",
            FromSettlementId = existingRoute?.FromSettlementId ?? RouteAuthoringOriginSettlement.Id,
            ToSettlementId = existingRoute?.ToSettlementId ?? ActiveRouteAuthoringDestinationSettlement.Id,
            Type = existingRoute?.Type ?? InferRouteType(RouteAuthoringOriginSettlement.Id, ActiveRouteAuthoringDestinationSettlement.Id),
            Distance = existingRoute?.Distance ?? 100m,
            TravelDays = existingRoute?.TravelDays ?? 3,
            DistanceDays = SelectedTradeRouteDistanceDays,
            IsEnabled = existingRoute?.IsEnabled ?? true,
            DifficultyMultiplier = existingRoute?.DifficultyMultiplier ?? 1m,
            Points = fullPoints
        };

        var routeIndex = existingRoute is null ? -1 : world.TradeRoutes.FindIndex(x => x.Id == existingRoute.Id);
        if (routeIndex >= 0) world.TradeRoutes[routeIndex] = updatedRoute;
        else world.TradeRoutes.Add(updatedRoute);

        SelectedTradeRouteForAuthoring = updatedRoute;
        _routesChanged();
        OnPropertyChanged(nameof(SelectedTradeRouteForAuthoring));
        OnPropertyChanged(nameof(EditedTradeRoutePointCount));
        OnPropertyChanged(nameof(EditedTradeRoutePolylinePoints));
        _refreshTradeRouteVisuals();
        _log($"Маршрут {updatedRoute.Id}: сохранено {fullPoints.Count} точек, дней пути {SelectedTradeRouteDistanceDays:0.###}.");
    }

    private void CopyTradeRoutePoints()
    {
        if (RouteAuthoringOriginSettlement is null || ActiveRouteAuthoringDestinationSettlement is null)
        {
            _log("Копирование Points недоступно: выберите пункт отправления и активный пункт назначения.");
            return;
        }

        var fullPoints = BuildFullRoutePoints();
        var routeId = SelectedTradeRouteForAuthoring?.Id ?? $"{RouteAuthoringOriginSettlement.Id}_{ActiveRouteAuthoringDestinationSettlement.Id}";
        var lines = fullPoints.Select(x =>
            $"    new RoutePoint {{ X = {Math.Clamp(x.X, 0m, 1m).ToString("0.0000", CultureInfo.InvariantCulture)}m, Y = {Math.Clamp(x.Y, 0m, 1m).ToString("0.0000", CultureInfo.InvariantCulture)}m }}");
        var text = $"RouteId: {routeId}{Environment.NewLine}From: {RouteAuthoringOriginSettlement.Id}{Environment.NewLine}To: {ActiveRouteAuthoringDestinationSettlement.Id}{Environment.NewLine}DistanceDays: {SelectedTradeRouteDistanceDays:0.0###}{Environment.NewLine}{Environment.NewLine}Points ={Environment.NewLine}[{Environment.NewLine}{string.Join($",{Environment.NewLine}", lines)}{Environment.NewLine}]";
        Clipboard.SetText(text);
        _log($"Маршрут {routeId}: Points скопированы.");
    }

    private void ReloadEditedTradeRoutePointsFromSelectedRoute()
    {
        EditedTradeRoutePoints.Clear();
        if (SelectedTradeRouteForAuthoring is not null)
        {
            SelectedTradeRouteDistanceDays = SelectedTradeRouteForAuthoring.DistanceDays;
            SelectedTradeRouteDistanceDaysInput = SelectedTradeRouteDistanceDays.ToString("0.0###", CultureInfo.InvariantCulture);
            foreach (var point in SelectedTradeRouteForAuthoring.Points)
            {
                EditedTradeRoutePoints.Add(new MapPointViewModel { X = (double)point.X, Y = (double)point.Y });
            }
        }

        OnPropertyChanged(nameof(EditedTradeRoutePoints));
        OnPropertyChanged(nameof(EditedTradeRoutePointCount));
        OnPropertyChanged(nameof(EditedTradeRouteIntermediatePointCount));
        OnPropertyChanged(nameof(EditedTradeRoutePolylinePoints));
    }

    private TradeRoute? FindRouteBetween(City from, City to) => _worldProvider().TradeRoutes.FirstOrDefault(route =>
        (route.FromSettlementId == from.Id && route.ToSettlementId == to.Id)
        || (route.FromSettlementId == to.Id && route.ToSettlementId == from.Id));

    private void ResolveRouteAuthoringSelection(City origin, City destination)
    {
        var existingRoute = FindRouteBetween(origin, destination);
        if (existingRoute is not null)
        {
            SelectedTradeRouteForAuthoring = existingRoute;
            _routeAuthoringRouteIdDisplay = $"RouteId: {existingRoute.Id}";
        }
        else
        {
            SelectedTradeRouteForAuthoring = null;
            EditedTradeRoutePoints.Clear();
            SelectedTradeRouteDistanceDays = 1m;
            SelectedTradeRouteDistanceDaysInput = "1.0";
            _routeAuthoringRouteIdDisplay = $"Новый маршрут: {origin.Id}_{destination.Id}";
        }
        OnPropertyChanged(nameof(RouteAuthoringRouteIdDisplay));
    }

    private List<RoutePoint> BuildFullRoutePoints()
    {
        var fullPoints = new List<RoutePoint>();
        if (RouteAuthoringOriginSettlement is not null && TryGetSettlementPoint(RouteAuthoringOriginSettlement.Id, out var start))
        {
            fullPoints.Add(start);
        }
        fullPoints.AddRange(EditedTradeRoutePoints.Select(x => new RoutePoint { X = (decimal)Math.Clamp(x.X, 0d, 1d), Y = (decimal)Math.Clamp(x.Y, 0d, 1d) }));
        if (ActiveRouteAuthoringDestinationSettlement is not null && TryGetSettlementPoint(ActiveRouteAuthoringDestinationSettlement.Id, out var end))
        {
            fullPoints.Add(end);
        }
        return fullPoints;
    }

    private bool TryGetSettlementPoint(string cityId, out RoutePoint point)
    {
        var location = _worldProvider().SettlementMapLocations.FirstOrDefault(x => x.SettlementId == cityId);
        if (location is null)
        {
            point = new RoutePoint();
            return false;
        }
        point = new RoutePoint { X = (decimal)location.X, Y = (decimal)location.Y };
        return true;
    }

    private static CaravanType InferRouteType(string fromId, string toId)
    {
        return fromId.Contains("harbor", StringComparison.OrdinalIgnoreCase)
               || toId.Contains("harbor", StringComparison.OrdinalIgnoreCase)
               || fromId.Contains("port", StringComparison.OrdinalIgnoreCase)
               || toId.Contains("port", StringComparison.OrdinalIgnoreCase)
            ? CaravanType.Sea
            : CaravanType.Land;
    }

    private void AddRouteAuthoringDestination()
    {
        if (RouteAuthoringOriginSettlement is null)
        {
            _log("Сначала выберите пункт отправления.");
            return;
        }
        if (RouteAuthoringDestinationCandidateSettlement is null)
        {
            _log("Выберите пункт назначения для добавления.");
            return;
        }
        if (RouteAuthoringDestinationCandidateSettlement.Id == RouteAuthoringOriginSettlement.Id)
        {
            _log("Назначение совпадает с пунктом отправления.");
            return;
        }
        if (RouteAuthoringDestinationSettlements.Any(x => x.Id == RouteAuthoringDestinationCandidateSettlement.Id))
        {
            _log("Это назначение уже добавлено.");
            return;
        }
        if (!CanAddMoreRouteAuthoringDestinations)
        {
            _log("Достигнут лимит назначений для одного источника.");
            return;
        }

        RouteAuthoringDestinationSettlements.Add(RouteAuthoringDestinationCandidateSettlement);
        ActiveRouteAuthoringDestinationSettlement = RouteAuthoringDestinationCandidateSettlement;
        OnPropertyChanged(nameof(RouteAuthoringDestinationCount));
        OnPropertyChanged(nameof(CanAddMoreRouteAuthoringDestinations));
    }

    private void ResetRouteAuthoringOriginState()
    {
        SaveCurrentActiveDraftPoints();
        _routeAuthoringDraftPointsByDestinationId.Clear();
        _currentDraftDestinationId = null;
        _activeRouteAuthoringDestinationSettlement = null;
        RouteAuthoringDestinationSettlements.Clear();
        EditedTradeRoutePoints.Clear();
        _selectedTradeRouteForAuthoring = null;
        _routeAuthoringRouteIdDisplay = "RouteId: —";

        OnPropertyChanged(nameof(ActiveRouteAuthoringDestinationSettlement));
        OnPropertyChanged(nameof(SelectedTradeRouteForAuthoring));
        OnPropertyChanged(nameof(HasSelectedTradeRouteForAuthoring));
        OnPropertyChanged(nameof(RouteAuthoringRouteIdDisplay));
        OnPropertyChanged(nameof(RouteAuthoringDestinationCount));
        OnPropertyChanged(nameof(CanAddMoreRouteAuthoringDestinations));
        _log("Источник маршрутов изменён, список назначений очищен.");
    }

    private void SaveCurrentActiveDraftPoints()
    {
        if (string.IsNullOrWhiteSpace(_currentDraftDestinationId)) return;
        _routeAuthoringDraftPointsByDestinationId[_currentDraftDestinationId] = EditedTradeRoutePoints
            .Select(x => new MapPointViewModel { X = x.X, Y = x.Y })
            .ToList();
    }

    private void LoadActiveRouteDraftOrExisting()
    {
        EditedTradeRoutePoints.Clear();
        if (RouteAuthoringOriginSettlement is null || ActiveRouteAuthoringDestinationSettlement is null)
        {
            _currentDraftDestinationId = null;
            _routeAuthoringRouteIdDisplay = "RouteId: —";
            OnPropertyChanged(nameof(RouteAuthoringRouteIdDisplay));
            return;
        }

        _currentDraftDestinationId = ActiveRouteAuthoringDestinationSettlement.Id;
        if (_routeAuthoringDraftPointsByDestinationId.TryGetValue(_currentDraftDestinationId, out var draftPoints))
        {
            foreach (var point in draftPoints)
            {
                EditedTradeRoutePoints.Add(new MapPointViewModel { X = point.X, Y = point.Y });
            }
        }

        ResolveRouteAuthoringSelection(RouteAuthoringOriginSettlement, ActiveRouteAuthoringDestinationSettlement);
    }
}
