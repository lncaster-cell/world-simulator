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
    private readonly TradeRouteAuthoringSession _session;
    private bool _isTradeRouteAuthoringModeEnabled;

    public TradeRouteAuthoringViewModel(
        Func<SimulationWorld> getWorld,
        Action<string> addTechnicalLogEntry,
        Action notifyTradeRoutesChanged,
        Action refreshTradeRouteVisuals,
        TradeRouteAuthoringService? authoringService = null,
        TradeRoutePointEditingService? pointEditingService = null,
        TradeRouteSelectionService? selectionService = null,
        TradeRoutePointsExportService? pointsExportService = null,
        TradeRouteAuthoringSession? session = null)
    {
        _getWorld = getWorld;
        _addTechnicalLogEntry = addTechnicalLogEntry;
        _notifyTradeRoutesChanged = notifyTradeRoutesChanged;
        _refreshTradeRouteVisuals = refreshTradeRouteVisuals;
        _authoringService = authoringService ?? new TradeRouteAuthoringService();
        _pointEditingService = pointEditingService ?? new TradeRoutePointEditingService();
        _selectionService = selectionService ?? new TradeRouteSelectionService();
        _pointsExportService = pointsExportService ?? new TradeRoutePointsExportService();
        _session = session ?? new TradeRouteAuthoringSession();

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
    public ObservableCollection<City> RouteAuthoringDestinationSettlements => _session.RouteAuthoringDestinationSettlements;
    public ObservableCollection<MapPointViewModel> EditedTradeRoutePoints { get; } = [];
    public int EditedTradeRoutePointCount => EditedTradeRoutePoints.Count;
    public int EditedTradeRouteIntermediatePointCount => EditedTradeRoutePoints.Count;
    public int RouteAuthoringDestinationCount => _session.RouteAuthoringDestinationCount;
    public bool CanAddMoreRouteAuthoringDestinations => _session.CanAddMoreRouteAuthoringDestinations;
    public bool HasSelectedTradeRouteForAuthoring => _session.HasSelectedTradeRouteForAuthoring;
    public List<RoutePoint> EditedTradeRoutePolylinePoints => BuildFullRoutePoints();
    public string RouteAuthoringRouteIdDisplay => _session.RouteAuthoringRouteIdDisplay;

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
        get => _session.RouteAuthoringOriginSettlement;
        set
        {
            if (!_session.SetRouteAuthoringOriginSettlement(value)) return;
            OnPropertyChanged();
            ResetRouteAuthoringOriginState();
        }
    }

    public City? RouteAuthoringDestinationCandidateSettlement
    {
        get => _session.RouteAuthoringDestinationCandidateSettlement;
        set
        {
            if (!_session.SetRouteAuthoringDestinationCandidateSettlement(value)) return;
            OnPropertyChanged();
        }
    }

    public City? ActiveRouteAuthoringDestinationSettlement
    {
        get => _session.ActiveRouteAuthoringDestinationSettlement;
        set
        {
            if (_session.ActiveRouteAuthoringDestinationSettlement == value) return;
            SaveCurrentActiveDraftPoints();
            _session.SetActiveRouteAuthoringDestinationSettlement(value);
            OnPropertyChanged();
            LoadActiveRouteDraftOrExisting();
        }
    }

    public TradeRoute? SelectedTradeRouteForAuthoring
    {
        get => _session.SelectedTradeRouteForAuthoring;
        set
        {
            if (!_session.SetSelectedTradeRouteForAuthoring(value)) return;
            ReloadEditedTradeRoutePointsFromSelectedRoute();
            OnPropertyChanged();
            OnPropertyChanged(nameof(HasSelectedTradeRouteForAuthoring));
        }
    }

    public decimal SelectedTradeRouteDistanceDays
    {
        get => _session.SelectedTradeRouteDistanceDays;
        set
        {
            if (!_session.SetSelectedTradeRouteDistanceDays(value)) return;
            OnPropertyChanged();
        }
    }

    public string SelectedTradeRouteDistanceDaysInput
    {
        get => _session.SelectedTradeRouteDistanceDaysInput;
        set
        {
            if (!_session.SetSelectedTradeRouteDistanceDaysInput(value)) return;
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
        _session.ResetForWorldReset();
        ClearTradeRoutePoints();

        OnPropertyChanged(nameof(RouteAuthoringOriginSettlement));
        OnRouteAuthoringSessionReset();
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
            _session.ApplySelectedRouteDistance(
                SelectedTradeRouteForAuthoring,
                _selectionService.FormatDistanceDaysInput(SelectedTradeRouteForAuthoring.DistanceDays));
            OnPropertyChanged(nameof(SelectedTradeRouteDistanceDays));
            OnPropertyChanged(nameof(SelectedTradeRouteDistanceDaysInput));
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
        var activeDestinationChanged = _session.AddRouteAuthoringDestination(result.Destination);
        OnPropertyChanged(nameof(RouteAuthoringDestinationCount));
        OnPropertyChanged(nameof(CanAddMoreRouteAuthoringDestinations));
        if (activeDestinationChanged)
        {
            OnPropertyChanged(nameof(ActiveRouteAuthoringDestinationSettlement));
            LoadActiveRouteDraftOrExisting();
        }
    }

    private void ResetRouteAuthoringOriginState()
    {
        _session.ResetRouteAuthoringOriginState(_pointEditingService, ClearTradeRoutePoints);
        OnRouteAuthoringSessionReset();
        _addTechnicalLogEntry("Источник маршрутов изменён, список назначений очищен.");
    }

    private void SaveCurrentActiveDraftPoints() => _pointEditingService.SaveCurrentDraft(EditedTradeRoutePoints);

    private void LoadActiveRouteDraftOrExisting()
    {
        if (!_session.LoadActiveRouteDraftOrExisting(_pointEditingService, EditedTradeRoutePoints, ClearTradeRoutePoints))
        {
            OnPropertyChanged(nameof(RouteAuthoringRouteIdDisplay));
            return;
        }

        ResolveRouteAuthoringSelection(RouteAuthoringOriginSettlement!, ActiveRouteAuthoringDestinationSettlement!);
    }

    private void ApplyRouteAuthoringSelection(TradeRouteSelectionResult selection)
    {
        var shouldClearEditedPoints = _session.ApplyRouteAuthoringSelection(selection);
        ReloadEditedTradeRoutePointsFromSelectedRoute();
        if (shouldClearEditedPoints)
        {
            ClearTradeRoutePoints();
        }

        OnPropertyChanged(nameof(SelectedTradeRouteForAuthoring));
        OnPropertyChanged(nameof(HasSelectedTradeRouteForAuthoring));
        OnPropertyChanged(nameof(SelectedTradeRouteDistanceDays));
        OnPropertyChanged(nameof(SelectedTradeRouteDistanceDaysInput));
    }

    private void OnRouteAuthoringSessionReset()
    {
        OnPropertyChanged(nameof(RouteAuthoringDestinationCandidateSettlement));
        OnPropertyChanged(nameof(ActiveRouteAuthoringDestinationSettlement));
        OnPropertyChanged(nameof(SelectedTradeRouteForAuthoring));
        OnPropertyChanged(nameof(HasSelectedTradeRouteForAuthoring));
        OnPropertyChanged(nameof(SelectedTradeRouteDistanceDays));
        OnPropertyChanged(nameof(SelectedTradeRouteDistanceDaysInput));
        OnPropertyChanged(nameof(RouteAuthoringRouteIdDisplay));
        OnPropertyChanged(nameof(RouteAuthoringDestinationCount));
        OnPropertyChanged(nameof(CanAddMoreRouteAuthoringDestinations));
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
