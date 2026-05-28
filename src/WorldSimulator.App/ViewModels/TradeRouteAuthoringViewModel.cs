using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Globalization;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using WorldSimulator.App.Infrastructure;
using WorldSimulator.App.Services;
using WorldSimulator.Core.Cities;
using WorldSimulator.Core.Trade;
using WorldSimulator.Core.World;

namespace WorldSimulator.App.ViewModels;

public sealed class TradeRouteAuthoringViewModel : INotifyPropertyChanged
{
    private readonly Func<SimulationWorld> _getWorld;
    private readonly Action<string> _addTechnicalLogEntry;
    private readonly Action _notifyTradeRoutesChanged;
    private readonly Action _refreshTradeRouteVisuals;
    private readonly TradeRouteAuthoringService _service;
    private TradeRoute? _selectedTradeRouteForAuthoring;
    private City? _routeAuthoringOriginSettlement;
    private City? _routeAuthoringDestinationCandidateSettlement;
    private City? _activeRouteAuthoringDestinationSettlement;
    private string _routeAuthoringRouteIdDisplay = "RouteId: —";
    private string? _currentDraftDestinationId;
    private readonly Dictionary<string, List<MapPointViewModel>> _routeAuthoringDraftPointsByDestinationId = [];
    private bool _isTradeRouteAuthoringModeEnabled;
    private bool _isLoadedRoutePathsDebugVisible;
    private decimal _selectedTradeRouteDistanceDays = 1m;
    private string _selectedTradeRouteDistanceDaysInput = "1.0";

    public TradeRouteAuthoringViewModel(
        Func<SimulationWorld> getWorld,
        Action<string> addTechnicalLogEntry,
        Action notifyTradeRoutesChanged,
        Action refreshTradeRouteVisuals,
        TradeRouteAuthoringService service)
    {
        _getWorld = getWorld;
        _addTechnicalLogEntry = addTechnicalLogEntry;
        _notifyTradeRoutesChanged = notifyTradeRoutesChanged;
        _refreshTradeRouteVisuals = refreshTradeRouteVisuals;
        _service = service;

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

    public event PropertyChangedEventHandler? PropertyChanged;

    public ICommand AddTradeRoutePointCommand { get; }
    public ICommand UndoTradeRoutePointCommand { get; }
    public ICommand ClearTradeRoutePointsCommand { get; }
    public ICommand SaveTradeRoutePointsCommand { get; }
    public ICommand CopyTradeRoutePointsCommand { get; }
    public ICommand AddRouteAuthoringDestinationCommand { get; }

    public IReadOnlyList<City> RouteAuthoringSettlements => _getWorld().Cities;
    public IReadOnlyList<City> AvailableRouteAuthoringDestinations => _getWorld().Cities;
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

    public void RegisterTradeRouteAuthoringPoint(double relativeX, double relativeY)
    {
        if (!IsTradeRouteAuthoringModeEnabled)
        {
            return;
        }

        if (RouteAuthoringOriginSettlement is null || ActiveRouteAuthoringDestinationSettlement is null)
        {
            _addTechnicalLogEntry("Выберите пункт отправления и активный пункт назначения.");
            return;
        }

        AddTradeRoutePoint(new MapPointViewModel { X = Math.Clamp(relativeX, 0d, 1d), Y = Math.Clamp(relativeY, 0d, 1d) });
    }

    public void ResetForWorldReset()
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
    }

    public void RefreshWorldCollections()
    {
        OnPropertyChanged(nameof(RouteAuthoringSettlements));
        OnPropertyChanged(nameof(AvailableRouteAuthoringDestinations));
        SelectedTradeRouteForAuthoring = _getWorld().TradeRoutes.FirstOrDefault();
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
            _addTechnicalLogEntry("Маршрут не сохранён: выберите пункт отправления и активный пункт назначения.");
            return;
        }

        if (RouteAuthoringOriginSettlement.Id == ActiveRouteAuthoringDestinationSettlement.Id)
        {
            _addTechnicalLogEntry("Маршрут не сохранён: отправление и назначение совпадают.");
            return;
        }

        if (!decimal.TryParse(SelectedTradeRouteDistanceDaysInput, NumberStyles.Number, CultureInfo.InvariantCulture, out var parsedDistanceDays))
        {
            _addTechnicalLogEntry("Маршрут не сохранён: укажите корректное значение дней пути.");
            return;
        }

        if (parsedDistanceDays < 0.1m)
        {
            _addTechnicalLogEntry("Маршрут не сохранён: дней пути должно быть не меньше 0.1.");
            return;
        }

        SelectedTradeRouteDistanceDays = parsedDistanceDays;
        var fullPoints = BuildFullRoutePoints();
        if (fullPoints.Count < 2)
        {
            _addTechnicalLogEntry("Маршрут не сохранён: нужно минимум 2 точки.");
            return;
        }

        var updatedRoute = _service.SaveRoute(
            _getWorld(),
            RouteAuthoringOriginSettlement,
            ActiveRouteAuthoringDestinationSettlement,
            SelectedTradeRouteDistanceDays,
            fullPoints);
        SelectedTradeRouteForAuthoring = updatedRoute;

        _notifyTradeRoutesChanged();
        OnPropertyChanged(nameof(EditedTradeRoutePointCount));
        OnPropertyChanged(nameof(EditedTradeRoutePolylinePoints));
        _refreshTradeRouteVisuals();
        _addTechnicalLogEntry($"Маршрут {updatedRoute.Id}: сохранено {fullPoints.Count} точек, дней пути {SelectedTradeRouteDistanceDays:0.###}.");
    }

    private void CopyTradeRoutePoints()
    {
        if (RouteAuthoringOriginSettlement is null || ActiveRouteAuthoringDestinationSettlement is null)
        {
            _addTechnicalLogEntry("Копирование Points недоступно: выберите пункт отправления и активный пункт назначения.");
            return;
        }

        var fullPoints = BuildFullRoutePoints();
        var routeId = SelectedTradeRouteForAuthoring?.Id ?? $"{RouteAuthoringOriginSettlement.Id}_{ActiveRouteAuthoringDestinationSettlement.Id}";
        _service.CopyRoutePoints(
            routeId,
            RouteAuthoringOriginSettlement.Id,
            ActiveRouteAuthoringDestinationSettlement.Id,
            SelectedTradeRouteDistanceDays,
            fullPoints);
        _addTechnicalLogEntry($"Маршрут {routeId}: Points скопированы.");
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

    private void ResolveRouteAuthoringSelection(City origin, City destination)
    {
        var existingRoute = TradeRouteAuthoringService.FindRouteBetween(_getWorld(), origin, destination);
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
        var location = _getWorld().FindSettlementMapLocation(cityId);
        if (location is not null)
        {
            point = new RoutePoint { X = location.X, Y = location.Y };
            return true;
        }

        point = new RoutePoint { X = 0m, Y = 0m };
        return false;
    }

    private void AddRouteAuthoringDestination()
    {
        if (RouteAuthoringOriginSettlement is null)
        {
            _addTechnicalLogEntry("Сначала выберите пункт отправления.");
            return;
        }

        var destination = RouteAuthoringDestinationCandidateSettlement;
        if (destination is null) return;
        if (destination.Id == RouteAuthoringOriginSettlement.Id)
        {
            _addTechnicalLogEntry("Пункт назначения не может совпадать с пунктом отправления.");
            return;
        }

        if (RouteAuthoringDestinationSettlements.Any(x => x.Id == destination.Id))
        {
            _addTechnicalLogEntry("Пункт назначения уже добавлен.");
            return;
        }

        if (RouteAuthoringDestinationSettlements.Count >= 8)
        {
            _addTechnicalLogEntry("Можно выбрать максимум 8 пунктов назначения.");
            return;
        }

        RouteAuthoringDestinationSettlements.Add(destination);
        OnPropertyChanged(nameof(RouteAuthoringDestinationCount));
        OnPropertyChanged(nameof(CanAddMoreRouteAuthoringDestinations));
        if (ActiveRouteAuthoringDestinationSettlement is null)
        {
            ActiveRouteAuthoringDestinationSettlement = destination;
        }
    }

    private void ResetRouteAuthoringOriginState()
    {
        _routeAuthoringDraftPointsByDestinationId.Clear();
        _currentDraftDestinationId = null;
        RouteAuthoringDestinationSettlements.Clear();
        ActiveRouteAuthoringDestinationSettlement = null;
        RouteAuthoringDestinationCandidateSettlement = null;
        EditedTradeRoutePoints.Clear();
        SelectedTradeRouteForAuthoring = null;
        SelectedTradeRouteDistanceDays = 1m;
        SelectedTradeRouteDistanceDaysInput = "1.0";
        _routeAuthoringRouteIdDisplay = "RouteId: —";
        OnPropertyChanged(nameof(RouteAuthoringRouteIdDisplay));
        OnPropertyChanged(nameof(RouteAuthoringDestinationCount));
        OnPropertyChanged(nameof(CanAddMoreRouteAuthoringDestinations));
        _addTechnicalLogEntry("Источник маршрутов изменён, список назначений очищен.");
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

    private void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}
