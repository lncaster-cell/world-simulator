using System.Collections.ObjectModel;
using System.ComponentModel;
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
    private readonly TradeRouteAuthoringService _authoringService;
    private readonly TradeRoutePointEditingService _pointEditingService;
    private readonly TradeRouteSelectionService _selectionService;
    private readonly TradeRoutePointsExportService _pointsExportService;
    private TradeRoute? _selectedTradeRouteForAuthoring;
    private City? _routeAuthoringOriginSettlement;
    private City? _routeAuthoringDestinationCandidateSettlement;
    private City? _activeRouteAuthoringDestinationSettlement;
    private string _routeAuthoringRouteIdDisplay = "RouteId: —";
    private bool _isTradeRouteAuthoringModeEnabled;
    private bool _isLoadedRoutePathsDebugVisible;
    private decimal _selectedTradeRouteDistanceDays = 1m;
    private string _selectedTradeRouteDistanceDaysInput = "1.0";

    public TradeRouteAuthoringViewModel(
        Func<SimulationWorld> getWorld,
        Action<string> addTechnicalLogEntry,
        Action notifyTradeRoutesChanged,
        Action refreshTradeRouteVisuals,
        TradeRouteAuthoringService? authoringService = null,
        TradeRoutePointEditingService? pointEditingService = null,
        TradeRouteSelectionService? selectionService = null,
        TradeRoutePointsExportService? pointsExportService = null)
    {
        _getWorld = getWorld;
        _addTechnicalLogEntry = addTechnicalLogEntry;
        _notifyTradeRoutesChanged = notifyTradeRoutesChanged;
        _refreshTradeRouteVisuals = refreshTradeRouteVisuals;
        _authoringService = authoringService ?? new TradeRouteAuthoringService();
        _pointEditingService = pointEditingService ?? new TradeRoutePointEditingService();
        _selectionService = selectionService ?? new TradeRouteSelectionService();
        _pointsExportService = pointsExportService ?? new TradeRoutePointsExportService();

        AddTradeRoutePointCommand = new RelayCommand<MapPointViewModel>(AddTradeRoutePoint);
        UndoTradeRoutePointCommand = new RelayCommand(UndoTradeRoutePoint);
        ClearTradeRoutePointsCommand = new RelayCommand(ClearTradeRoutePoints);
        SaveTradeRoutePointsCommand = new RelayCommand(SaveTradeRoutePoints);
        CopyTradeRoutePointsCommand = new RelayCommand(CopyTradeRoutePoints);
        AddRouteAuthoringDestinationCommand = new RelayCommand(AddRouteAuthoringDestination);

        EditedTradeRoutePoints.CollectionChanged += (_, _) => NotifyEditedRoutePointsChanged();
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
    public bool CanAddMoreRouteAuthoringDestinations => RouteAuthoringDestinationSettlements.Count < TradeRouteSelectionService.MaxDestinationCount;
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

        AddTradeRoutePoint(new MapPointViewModel { X = relativeX, Y = relativeY });
    }

    public void ResetForWorldReset()
    {
        _pointEditingService.ClearDrafts();
        _routeAuthoringOriginSettlement = null;
        _routeAuthoringDestinationCandidateSettlement = null;
        _activeRouteAuthoringDestinationSettlement = null;
        RouteAuthoringDestinationSettlements.Clear();
        ClearTradeRoutePoints();
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

    private void AddTradeRoutePoint(MapPointViewModel? point) => _pointEditingService.AddPoint(EditedTradeRoutePoints, point);

    private void UndoTradeRoutePoint() => _pointEditingService.UndoLastPoint(EditedTradeRoutePoints);

    private void ClearTradeRoutePoints() => _pointEditingService.ClearPoints(EditedTradeRoutePoints);

    private void SaveTradeRoutePoints()
    {
        var saveInput = _selectionService.ResolveSaveInput(
            RouteAuthoringOriginSettlement,
            ActiveRouteAuthoringDestinationSettlement,
            SelectedTradeRouteDistanceDaysInput);
        if (!saveInput.IsValid || saveInput.Origin is null || saveInput.Destination is null)
        {
            _addTechnicalLogEntry(saveInput.ErrorMessage ?? "Маршрут не сохранён: неверные параметры маршрута.");
            return;
        }

        SelectedTradeRouteDistanceDays = saveInput.DistanceDays;
        var fullPoints = BuildFullRoutePoints();
        if (fullPoints.Count < 2)
        {
            _addTechnicalLogEntry("Маршрут не сохранён: нужно минимум 2 точки.");
            return;
        }

        var updatedRoute = _authoringService.SaveRoute(
            _getWorld(),
            saveInput.Origin,
            saveInput.Destination,
            SelectedTradeRouteDistanceDays,
            fullPoints);
        SelectedTradeRouteForAuthoring = updatedRoute;

        _notifyTradeRoutesChanged();
        NotifyEditedRoutePointsChanged();
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
        _pointsExportService.CopyRoutePoints(
            routeId,
            RouteAuthoringOriginSettlement.Id,
            ActiveRouteAuthoringDestinationSettlement.Id,
            SelectedTradeRouteDistanceDays,
            fullPoints);
        _addTechnicalLogEntry($"Маршрут {routeId}: Points скопированы.");
    }

    private void ReloadEditedTradeRoutePointsFromSelectedRoute()
    {
        _pointEditingService.ReplacePoints(
            EditedTradeRoutePoints,
            _pointEditingService.BuildIntermediateRoutePoints(SelectedTradeRouteForAuthoring));
        if (SelectedTradeRouteForAuthoring is not null)
        {
            SelectedTradeRouteDistanceDays = SelectedTradeRouteForAuthoring.DistanceDays;
            SelectedTradeRouteDistanceDaysInput = _selectionService.FormatDistanceDaysInput(SelectedTradeRouteDistanceDays);
        }

        NotifyEditedRoutePointsChanged();
    }

    private void ResolveRouteAuthoringSelection(City origin, City destination)
    {
        var selection = _selectionService.ResolveSelection(_getWorld(), origin, destination);
        ApplyRouteAuthoringSelection(selection);
        OnPropertyChanged(nameof(RouteAuthoringRouteIdDisplay));
    }

    private List<RoutePoint> BuildFullRoutePoints()
    {
        return _pointEditingService.BuildFullRoutePoints(
            _getWorld(),
            RouteAuthoringOriginSettlement,
            ActiveRouteAuthoringDestinationSettlement,
            EditedTradeRoutePoints);
    }

    private bool TryGetSettlementPoint(string cityId, out RoutePoint point)
        => _pointEditingService.TryGetSettlementPoint(_getWorld(), cityId, out point);

    private void AddRouteAuthoringDestination()
    {
        var result = _selectionService.TryAddDestination(
            RouteAuthoringOriginSettlement,
            RouteAuthoringDestinationCandidateSettlement,
            RouteAuthoringDestinationSettlements);

        if (!string.IsNullOrWhiteSpace(result.ErrorMessage))
        {
            _addTechnicalLogEntry(result.ErrorMessage);
            return;
        }

        if (!result.ShouldAdd || result.Destination is null) return;
        RouteAuthoringDestinationSettlements.Add(result.Destination);
        OnPropertyChanged(nameof(RouteAuthoringDestinationCount));
        OnPropertyChanged(nameof(CanAddMoreRouteAuthoringDestinations));
        if (ActiveRouteAuthoringDestinationSettlement is null)
        {
            ActiveRouteAuthoringDestinationSettlement = result.Destination;
        }
    }

    private void ResetRouteAuthoringOriginState()
    {
        _pointEditingService.ClearDrafts();
        RouteAuthoringDestinationSettlements.Clear();
        ActiveRouteAuthoringDestinationSettlement = null;
        RouteAuthoringDestinationCandidateSettlement = null;
        ClearTradeRoutePoints();
        SelectedTradeRouteForAuthoring = null;
        SelectedTradeRouteDistanceDays = 1m;
        SelectedTradeRouteDistanceDaysInput = "1.0";
        _routeAuthoringRouteIdDisplay = "RouteId: —";
        OnPropertyChanged(nameof(RouteAuthoringRouteIdDisplay));
        OnPropertyChanged(nameof(RouteAuthoringDestinationCount));
        OnPropertyChanged(nameof(CanAddMoreRouteAuthoringDestinations));
        _addTechnicalLogEntry("Источник маршрутов изменён, список назначений очищен.");
    }

    private void SaveCurrentActiveDraftPoints() => _pointEditingService.SaveCurrentDraft(EditedTradeRoutePoints);

    private void LoadActiveRouteDraftOrExisting()
    {
        ClearTradeRoutePoints();
        if (RouteAuthoringOriginSettlement is null || ActiveRouteAuthoringDestinationSettlement is null)
        {
            _pointEditingService.ResetCurrentDraftDestination();
            _routeAuthoringRouteIdDisplay = "RouteId: —";
            OnPropertyChanged(nameof(RouteAuthoringRouteIdDisplay));
            return;
        }

        _pointEditingService.SetCurrentDraftDestination(ActiveRouteAuthoringDestinationSettlement.Id);
        _pointEditingService.TryLoadCurrentDraft(EditedTradeRoutePoints);

        ResolveRouteAuthoringSelection(RouteAuthoringOriginSettlement, ActiveRouteAuthoringDestinationSettlement);
    }

    private void ApplyRouteAuthoringSelection(TradeRouteSelectionResult selection)
    {
        SelectedTradeRouteForAuthoring = selection.SelectedRoute;
        if (selection.SelectedRoute is null && selection.ShouldClearEditedPoints)
        {
            ClearTradeRoutePoints();
        }

        SelectedTradeRouteDistanceDays = selection.DistanceDays;
        SelectedTradeRouteDistanceDaysInput = selection.DistanceDaysInput;
        _routeAuthoringRouteIdDisplay = selection.RouteIdDisplay;
    }

    private void NotifyEditedRoutePointsChanged()
    {
        OnPropertyChanged(nameof(EditedTradeRoutePoints));
        OnPropertyChanged(nameof(EditedTradeRoutePointCount));
        OnPropertyChanged(nameof(EditedTradeRouteIntermediatePointCount));
        OnPropertyChanged(nameof(EditedTradeRoutePolylinePoints));
    }

    private void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}
