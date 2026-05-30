using System.Collections.ObjectModel;
using System.IO;
using System.Windows.Input;
using WorldSimulator.App.Infrastructure;
using WorldSimulator.App.Services;
using WorldSimulator.Core.Cities;
using WorldSimulator.Core.Events;
using WorldSimulator.Core.Resources;
using WorldSimulator.Core.Simulation;
using WorldSimulator.Core.Time;
using WorldSimulator.Core.Trade;
using WorldSimulator.Core.World;
using WorldSimulator.Persistence.Saves;

namespace WorldSimulator.App.ViewModels;

public sealed class MainWindowViewModel : ViewModelBase
{
    private const string SaveFilePath = "data/save/world_save.json";
    private const int MaxTechnicalLogEntries = 500;

    private SimulationWorld _world;
    private City _city;
    private readonly SimulationClock _clock = new();
    private readonly DailyFoodFlowCalculator _dailyFoodFlowCalculator = new();
    private readonly FishingProductionCalculator _fishingProductionCalculator = new();
    private readonly HuntingProductionCalculator _huntingProductionCalculator = new();
    private readonly AgricultureProductionCalculator _agricultureProductionCalculator = new();
    private readonly MainlandSupplyProductionCalculator _mainlandSupplyProductionCalculator = new();
    private readonly GoodsCraftingProductionCalculator _goodsCraftingProductionCalculator = new();
    private readonly ResourceGatheringProductionCalculator _resourceGatheringProductionCalculator = new();
    private readonly HouseholdConsumptionCalculator _householdConsumptionCalculator = new();
    private readonly DailyWealthFlowCalculator _dailyWealthFlowCalculator = new();
    private readonly WeeklyCrimeFlowCalculator _weeklyCrimeFlowCalculator = new();
    private readonly CityStateEvaluator _cityStateEvaluator = new();
    private readonly CityEventManager _eventManager = new();
    private readonly CityEventEffectCalculator _eventEffectCalculator = new();
    private readonly CityEventGenerator _eventGenerator = new(new SystemRandomProvider());
    private readonly CityEventEffectTextFormatter _eventEffectTextFormatter = new();
    private readonly DailySimulationPresentationService _dailySimulationPresentationService;
    private readonly WorldSimulationService _worldSimulationService;
    private readonly JsonWorldSaveService _saveService = new();
    private readonly SimulationJournalService _journalService;

    private DailyFoodFlowResult _dailyFoodFlowResult;
    private DailyWealthFlowResult? _dailyWealthFlowResult;
    private bool _isRandomEventGenerationEnabled = true;
    private string _lastImportantChange = "none";
    private bool _isCityPanelVisible;
    private int _selectedCityTabIndex;

    public MainWindowViewModel()
    {
        _world = WorldPresets.CreateDefaultWorld();
        _city = _world.SelectedCity;
        _dailySimulationPresentationService = new DailySimulationPresentationService(_eventEffectTextFormatter);
        _journalService = new SimulationJournalService(_eventEffectTextFormatter);
        _worldSimulationService = new WorldSimulationService(
            _dailyFoodFlowCalculator,
            _fishingProductionCalculator,
            _huntingProductionCalculator,
            _agricultureProductionCalculator,
            _mainlandSupplyProductionCalculator,
            _goodsCraftingProductionCalculator,
            _resourceGatheringProductionCalculator,
            _householdConsumptionCalculator,
            _dailyWealthFlowCalculator,
            _weeklyCrimeFlowCalculator,
            new WorldTradeFlowService(),
            new CaravanHiringService(),
            _cityStateEvaluator,
            new PopulationChangeCalculator(),
            _eventManager,
            _eventEffectCalculator,
            _eventGenerator);

        _dailyFoodFlowResult = _dailyFoodFlowCalculator.Calculate(_city, BuildDailyFoodFlowInputs());
        Journal = new SimulationJournalViewModel(_journalService, _city.Id, _city.Name);
        Control = new SimulationControlViewModel(_clock, AddTechnicalLogEntry);
        Log = new SimulationLogViewModel();
        SelectedCity = new SelectedCityViewModel(() => _world, () => _city, () => _dailyFoodFlowResult, () => _dailyWealthFlowResult);
        Summary = new SimulationSummaryViewModel(Control, SelectedCity, Log, () => IsRandomEventGenerationEnabled, () => _lastImportantChange);
        Map = new MapViewModel(() => _world, _clock, AddTechnicalLogEntry);
        TradeRouteAuthoring = new TradeRouteAuthoringViewModel(() => _world, AddTechnicalLogEntry, NotifyTradeRoutesChanged, () => Map.RefreshTradeRouteVisuals(null), new TradeRouteAuthoringService());

        StartCommand = Control.StartCommand;
        PauseCommand = Control.PauseCommand;
        ResetCommand = Control.ResetCommand;
        SetNormalSpeedCommand = Control.SetNormalSpeedCommand;
        SetFastSpeedCommand = Control.SetFastSpeedCommand;
        SetVeryFastSpeedCommand = Control.SetVeryFastSpeedCommand;
        SetTurboSpeedCommand = Control.SetTurboSpeedCommand;
        SelectSettlementCommand = new RelayCommand<string>(SelectSettlement);
        OpenSelectedCityCommand = new RelayCommand(OpenSelectedCity, () => !string.IsNullOrWhiteSpace(_world.SelectedCityId));
        TriggerFireEventCommand = new RelayCommand(() => TryStartEvent(CityEventPresets.CreateFire));
        TriggerDiseaseEventCommand = new RelayCommand(() => TryStartEvent(CityEventPresets.CreateDisease));
        TriggerRatInfestationEventCommand = new RelayCommand(() => TryStartEvent(CityEventPresets.CreateRatInfestation));
        TriggerArtistsPerformanceEventCommand = new RelayCommand(() => TryStartEvent(CityEventPresets.CreateArtistsPerformance));
        TriggerPortStormEventCommand = new RelayCommand(() => TryStartEvent(CityEventPresets.CreatePortStorm));
        ToggleRandomEventGenerationCommand = new RelayCommand(ToggleRandomEventGeneration);
        SaveCommand = new AsyncRelayCommand(SaveStateAsync);
        LoadCommand = new AsyncRelayCommand(LoadStateAsync);

        Control.ResetRequested += ResetSimulation;
        Control.PropertyChanged += (_, e) =>
        {
            if (e.PropertyName is nameof(SimulationControlViewModel.Day) or nameof(SimulationControlViewModel.Hour))
            {
                OnPropertyChanged(nameof(Day));
                OnPropertyChanged(nameof(Hour));
                Summary.RefreshClock();
            }
            else if (e.PropertyName is nameof(SimulationControlViewModel.IsRunning))
            {
                OnPropertyChanged(nameof(IsRunning));
            }
            else if (e.PropertyName is nameof(SimulationControlViewModel.SimulationState))
            {
                OnPropertyChanged(nameof(SimulationState));
            }
            else if (e.PropertyName is nameof(SimulationControlViewModel.CurrentSimulationSpeedDisplay))
            {
                OnPropertyChanged(nameof(CurrentSimulationSpeedDisplay));
            }
        };
        _clock.DayAdvanced += OnDayAdvanced;

        TradeRouteAuthoring.SelectedTradeRouteForAuthoring = _world.TradeRoutes.FirstOrDefault();
        RefreshCityState();
        RefreshDailyFoodFlowPreview();
        RefreshEventEntries();
        RefreshSimulationSummary();
        Map.LoadRoutePathsForWorld();
        Map.RefreshTradeRouteVisuals(null);
    }

    public SimulationControlViewModel Control { get; }
    public SimulationLogViewModel Log { get; }
    public SelectedCityViewModel SelectedCity { get; }
    public SimulationSummaryViewModel Summary { get; }
    public SimulationJournalViewModel Journal { get; }
    public MapViewModel Map { get; }
    public TradeRouteAuthoringViewModel TradeRouteAuthoring { get; }

    public ICommand StartCommand { get; }
    public ICommand PauseCommand { get; }
    public ICommand ResetCommand { get; }
    public ICommand SetNormalSpeedCommand { get; }
    public ICommand SetFastSpeedCommand { get; }
    public ICommand SetVeryFastSpeedCommand { get; }
    public ICommand SetTurboSpeedCommand { get; }
    public ICommand SelectSettlementCommand { get; }
    public ICommand OpenSelectedCityCommand { get; }
    public ICommand TriggerFireEventCommand { get; }
    public ICommand TriggerDiseaseEventCommand { get; }
    public ICommand TriggerRatInfestationEventCommand { get; }
    public ICommand TriggerArtistsPerformanceEventCommand { get; }
    public ICommand TriggerPortStormEventCommand { get; }
    public ICommand ToggleRandomEventGenerationCommand { get; }
    public ICommand SaveCommand { get; }
    public ICommand LoadCommand { get; }

    public IReadOnlyList<City> Cities => _world.Cities;
    public string SettlementCountText => $"Поселений: {_world.Cities.Count}";
    public int Day => Control.Day;
    public int Hour => Control.Hour;
    public bool IsRunning => Control.IsRunning;
    public string SimulationState => Control.SimulationState;
    public string CurrentSimulationSpeedDisplay => Control.CurrentSimulationSpeedDisplay;
    public ObservableCollection<string> TechnicalLogEntries => Log.TechnicalLogEntries;
    public ObservableCollection<string> ActiveEventEntries => Log.ActiveEventEntries;
    public ObservableCollection<string> CompletedEventEntries => Log.CompletedEventEntries;
    public bool HasTechnicalLogEntries => Log.HasTechnicalLogEntries;
    public bool HasActiveEventEntries => Log.HasActiveEventEntries;
    public bool HasCompletedEventEntries => Log.HasCompletedEventEntries;
    public decimal DailyFoodEventDelta => SelectedCity.DailyFoodEventDelta;
    public CityWorkforceDiagnosticsViewModel CityWorkforceDiagnostics => new(_world, _city);

    public bool IsCityPanelVisible
    {
        get => _isCityPanelVisible;
        set => SetProperty(ref _isCityPanelVisible, value);
    }

    public int SelectedCityTabIndex
    {
        get => _selectedCityTabIndex;
        set => SetProperty(ref _selectedCityTabIndex, value);
    }

    public bool IsRandomEventGenerationEnabled
    {
        get => _isRandomEventGenerationEnabled;
        private set
        {
            if (SetProperty(ref _isRandomEventGenerationEnabled, value))
            {
                OnPropertyChanged(nameof(RandomEventGenerationStatusDisplay));
                OnPropertyChanged(nameof(RandomEventGenerationToggleButtonText));
            }
        }
    }

    public string RandomEventGenerationStatusDisplay => IsRandomEventGenerationEnabled ? "включены" : "выключены";
    public string RandomEventGenerationToggleButtonText => IsRandomEventGenerationEnabled ? "Выключить случайные события" : "Включить случайные события";

    private void SelectSettlement(string? settlementId)
    {
        if (string.IsNullOrWhiteSpace(settlementId)) return;
        var selectedCity = _world.FindCity(settlementId);
        if (selectedCity is null) return;

        _world.SelectedCityId = selectedCity.Id;
        _city = _world.SelectedCity;
        var selectedLocation = _world.FindSettlementMapLocation(selectedCity.Id);
        if (selectedLocation is not null)
        {
            _world.SelectedRegionId = selectedLocation.RegionId;
        }

        var eventState = _worldSimulationService.ExportEventState();
        var manager = eventState.GetManagerOrEmpty(_world.SelectedCityId);
        _eventManager.Restore(manager.ActiveEvents, manager.CompletedEvents);
        Journal.SelectCity(_city.Id, _city.Name);

        RefreshSelectedCityProperties();
        RefreshEventEntries();
        RefreshDailyFoodFlowPreview();
        RefreshSimulationSummary();
    }

    private void OpenSelectedCity()
    {
        IsCityPanelVisible = true;
        SelectedCityTabIndex = 0;
    }

    private void ResetSimulation()
    {
        _world = WorldPresets.CreateDefaultWorld();
        _city = _world.SelectedCity;
        Control.ResetClock();
        Map.ResetAnimationBaseline();
        _dailyWealthFlowResult = null;
        _eventManager.Clear();
        _worldSimulationService.ResetEventState();
        IsRandomEventGenerationEnabled = true;
        Journal.SelectCity(_city.Id, _city.Name);
        Journal.SelectedSimulationJournalFilter = SimulationJournalFilterOption.All;
        Journal.SelectedSimulationJournalEntry = null;
        Log.ClearSimulationEntries();
        Journal.Clear();
        Map.ClearSimulationCollections();
        SetLastImportantChange("reset");

        RefreshWorldCollectionsAfterLoad();
        TradeRouteAuthoring.ResetForWorldReset();
        RefreshSelectedCityProperties();
        RefreshClockProperties();
        RefreshDailyFoodFlowPreview();
        RefreshEventEntries();
        RefreshSimulationSummary();
        AddTechnicalLogEntry("Simulation reset.");
    }

    private void OnDayAdvanced(int day)
    {
        var populationStart = _city.Population;
        var cityStateStart = _city.CityState;
        var simulationResult = _worldSimulationService.AdvanceDay(_world, _city.Id, day, IsRandomEventGenerationEnabled);
        if (simulationResult.SelectedCityResult is null) return;

        var selectedManager = _worldSimulationService.ExportEventState().GetManagerOrEmpty(_world.SelectedCityId);
        _eventManager.Restore(selectedManager.ActiveEvents, selectedManager.CompletedEvents);

        _dailyFoodFlowResult = simulationResult.SelectedCityResult.FoodFlow;
        _dailyWealthFlowResult = simulationResult.SelectedCityResult.WealthFlow;

        var presentation = _dailySimulationPresentationService.Build(new DailySimulationPresentationRequest
        {
            Day = day,
            City = _city,
            SimulationResult = simulationResult,
            PopulationStart = populationStart,
            PopulationEnd = _city.Population,
            CityStateStart = cityStateStart,
            CityStateEnd = _city.CityState,
            ActiveEventsCount = _eventManager.ActiveEvents.Count
        });

        foreach (var entry in presentation.TechnicalLogEntries)
        {
            AddTechnicalLogEntry(entry);
        }

        if (presentation.LastImportantChange is not null)
        {
            SetLastImportantChange(presentation.LastImportantChange);
        }

        _city = _world.SelectedCity;
        if (simulationResult.WeeklyTradeFlowResult is not null)
        {
            Map.RefreshTradeRouteVisuals(simulationResult.WeeklyTradeFlowResult);
        }

        RefreshAllCityProperties();
        RefreshEventEntries();
        RefreshDailyFoodFlowPreview();
        RefreshSimulationSummary();
        Journal.Append(presentation.JournalAppendRequest);
    }

    private void TryStartEvent(Func<int, CityEvent> factory)
    {
        var cityEvent = factory(Day);
        if (_eventManager.AddEvent(cityEvent))
        {
            AddTechnicalLogEntry($"Day {Day}: event started: {cityEvent.Name}.");
            SetLastImportantChange($"Day {Day}: event started: {cityEvent.Name}.");
        }
        else
        {
            AddTechnicalLogEntry($"Event is already active: {cityEvent.Name}.");
        }

        RefreshEventEntries();
        RefreshDailyFoodFlowPreview();
        Summary.RefreshFoodBalance();
    }

    private void ToggleRandomEventGeneration()
    {
        IsRandomEventGenerationEnabled = !IsRandomEventGenerationEnabled;
        AddTechnicalLogEntry(IsRandomEventGenerationEnabled ? "Random events enabled." : "Random events disabled.");
        RefreshSimulationSummary();
    }

    private void RefreshEventEntries()
    {
        Log.ActiveEventEntries.Clear();
        foreach (var activeEvent in _eventManager.ActiveEvents)
        {
            var effectSummary = _eventEffectCalculator.Calculate(_city, new[] { activeEvent });
            Log.ActiveEventEntries.Add($"{activeEvent.Name}: осталось {activeEvent.RemainingDays} дн.; эффекты: {_eventEffectTextFormatter.Format(effectSummary)}");
        }

        Log.CompletedEventEntries.Clear();
        foreach (var completedEvent in _eventManager.CompletedEvents)
        {
            Log.CompletedEventEntries.Add($"{completedEvent.Name}: завершено на дне {completedEvent.StartedDay + completedEvent.DurationDays}");
        }

        Log.RefreshActiveAndCompletedAvailability();
        Summary.RefreshActiveEvents();
        RefreshLogProxyProperties();
    }

    private async Task SaveStateAsync()
    {
        try
        {
            await _saveService.SaveAsync(SaveFilePath, _world, _clock, _worldSimulationService.ExportEventState());
            AddTechnicalLogEntry($"Saved: {SaveFilePath}");
            SetLastImportantChange("saved");
            RefreshSimulationSummary();
        }
        catch (Exception ex)
        {
            AddTechnicalLogEntry($"Save error: {ex.Message}");
        }
    }

    private async Task LoadStateAsync()
    {
        try
        {
            if (!File.Exists(SaveFilePath))
            {
                AddTechnicalLogEntry($"Save file not found: {SaveFilePath}");
                return;
            }

            var loaded = await _saveService.LoadAsync(SaveFilePath);
            _world = loaded.World;
            if (_world.EnsureValidSelection(out var selectionReason) && selectionReason is not null)
            {
                AddTechnicalLogEntry($"Selection fixed after load: {selectionReason}");
            }

            if (!_world.TryGetSelectedCity(out var selectedCity))
            {
                AddTechnicalLogEntry("Load error: world has no cities.");
                return;
            }

            _city = selectedCity;
            Journal.SelectCity(_city.Id, _city.Name);
            _clock.RestoreState(loaded.Clock.Day, loaded.Clock.Hour, loaded.Clock.IsRunning, loaded.Clock.AccumulatedRealTime, loaded.Clock.RealTimePerGameHour);
            Control.RestoreTickBaseline();
            _worldSimulationService.ImportEventState(loaded.EventState, _world.SelectedCityId);
            var selectedCityEventManager = loaded.EventState.GetManagerOrEmpty(_world.SelectedCityId);
            _eventManager.Restore(selectedCityEventManager.ActiveEvents, selectedCityEventManager.CompletedEvents);

            RefreshWorldCollectionsAfterLoad();
            RefreshSelectedCityProperties();
            RefreshClockProperties();
            RefreshEventEntries();
            RefreshDailyFoodFlowPreview();
            RefreshSimulationSummary();
            SetLastImportantChange("loaded");
            AddTechnicalLogEntry($"Loaded: {SaveFilePath}");
        }
        catch (Exception ex)
        {
            AddTechnicalLogEntry($"Load error: {ex.Message}");
        }
    }

    private void NotifyTradeRoutesChanged() => SelectedCity.RefreshTradeRoutes();

    private void RefreshWorldCollectionsAfterLoad()
    {
        OnPropertyChanged(nameof(Cities));
        OnPropertyChanged(nameof(SettlementCountText));
        OnPropertyChanged(nameof(CityWorkforceDiagnostics));
        NotifyTradeRoutesChanged();
        TradeRouteAuthoring.RefreshWorldCollections();
        Map.LoadRoutePathsForWorld();
        Map.RefreshTradeRouteVisuals(null);
        Journal.RefreshSimulationJournalFilter();
    }

    private void RefreshClockProperties()
    {
        OnPropertyChanged(nameof(Day));
        OnPropertyChanged(nameof(Hour));
        OnPropertyChanged(nameof(IsRunning));
        OnPropertyChanged(nameof(SimulationState));
        OnPropertyChanged(nameof(CurrentSimulationSpeedDisplay));
        Summary.RefreshClock();
        if (StartCommand is RelayCommand startCommand) startCommand.RaiseCanExecuteChanged();
        if (PauseCommand is RelayCommand pauseCommand) pauseCommand.RaiseCanExecuteChanged();
    }

    private void RefreshSelectedCityProperties()
    {
        SelectedCity.RefreshSelectedCityPanel();
        Map.RefreshSelectedCityProperties();
        OnPropertyChanged(nameof(CityWorkforceDiagnostics));
        if (OpenSelectedCityCommand is RelayCommand openSelectedCityCommand) openSelectedCityCommand.RaiseCanExecuteChanged();
    }

    private void RefreshAllCityProperties()
    {
        SelectedCity.RefreshAllCityProperties();
        Map.RefreshSelectedCityProperties();
        OnPropertyChanged(nameof(DailyFoodEventDelta));
        OnPropertyChanged(nameof(CityWorkforceDiagnostics));
    }

    private void RefreshDailyFoodFlowPreview()
    {
        _dailyFoodFlowResult = _dailyFoodFlowCalculator.Calculate(_city, BuildDailyFoodFlowInputs());
        SelectedCity.RefreshDailyFoodFlowPreview();
        Summary.RefreshFoodBalance();
        OnPropertyChanged(nameof(DailyFoodEventDelta));
    }

    private DailyFoodFlowInputs BuildDailyFoodFlowInputs(CityEventEffectsResult? eventEffects = null)
    {
        var effects = eventEffects ?? _eventEffectCalculator.Calculate(_city, _eventManager.ActiveEvents);
        var profile = _world.FindSettlementEconomyProfile(_city.Id);
        var fishing = _fishingProductionCalculator.Calculate(_city, _eventManager.ActiveEvents);
        var hunting = _huntingProductionCalculator.Calculate(_city);
        var mainlandSupply = _mainlandSupplyProductionCalculator.Calculate(_city, _eventManager.ActiveEvents);
        var agricultureIncome = profile is null ? 0m : _agricultureProductionCalculator.Calculate(_city, profile).FinalOutput;

        return new DailyFoodFlowInputs
        {
            AgricultureIncome = agricultureIncome,
            FishingIncome = fishing.FinalOutput * (profile?.FishingMultiplier ?? 1m),
            HuntingIncome = hunting.FinalOutput * (profile?.HuntingMultiplier ?? 1m),
            MainlandSupplyIncome = mainlandSupply.FinalOutput * (profile?.MainlandSupplyMultiplier ?? 1m) + effects.MainlandSupplyDelta,
            EventDelta = DailyFoodFlowInputs.GothaPlaceholder.EventDelta + effects.FoodDelta
        };
    }

    private void RefreshCityState()
    {
        var newState = _cityStateEvaluator.Evaluate(_city);
        if (_city.CityState == newState) return;
        _city.CityState = newState;
        SelectedCity.RefreshAllCityProperties();
        Map.RefreshSelectedCityProperties();
        Summary.RefreshCityState();
        OnPropertyChanged(nameof(CityWorkforceDiagnostics));
    }

    private void SetLastImportantChange(string message)
    {
        _lastImportantChange = message;
        Summary.RefreshLastImportantChange();
    }

    private void RefreshSimulationSummary() => Summary.RefreshAll();

    private void RefreshLogProxyProperties()
    {
        OnPropertyChanged(nameof(TechnicalLogEntries));
        OnPropertyChanged(nameof(ActiveEventEntries));
        OnPropertyChanged(nameof(CompletedEventEntries));
        OnPropertyChanged(nameof(HasTechnicalLogEntries));
        OnPropertyChanged(nameof(HasActiveEventEntries));
        OnPropertyChanged(nameof(HasCompletedEventEntries));
    }

    private void AddTechnicalLogEntry(string message)
    {
        Log.AddTechnicalEntry(message, MaxTechnicalLogEntries);
        RefreshLogProxyProperties();
    }
}
