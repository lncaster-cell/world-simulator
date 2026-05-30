using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Windows.Input;
using WorldSimulator.App.Infrastructure;
using WorldSimulator.App.Services;
using WorldSimulator.Core.Cities;
using WorldSimulator.Core.Events;
using WorldSimulator.Core.Resources;
using WorldSimulator.Core.Time;
using WorldSimulator.Core.Simulation;
using WorldSimulator.Core.World;
using WorldSimulator.Persistence.Saves;

namespace WorldSimulator.App.ViewModels;

public sealed class MainWindowViewModel : ViewModelBase
{
    private const string SaveFilePath = "data/save/world_save.json";
    private const int MaxTechnicalLogEntries = 500;
    private SimulationWorld _world;
    private City _city;
    private readonly SimulationClock _clock;
    private readonly DailyFoodFlowCalculator _dailyFoodFlowCalculator;
    private readonly FishingProductionCalculator _fishingProductionCalculator;
    private readonly HuntingProductionCalculator _huntingProductionCalculator;
    private readonly AgricultureProductionCalculator _agricultureProductionCalculator;
    private readonly MainlandSupplyProductionCalculator _mainlandSupplyProductionCalculator;
    private readonly CityStateEvaluator _cityStateEvaluator;
    private readonly CityEventManager _eventManager;
    private readonly CityEventEffectCalculator _eventEffectCalculator;
    private readonly WorldSimulationService _worldSimulationService;
    private readonly JsonWorldSaveService _saveService;
    private readonly SimulationJournalService _journalService;
    private DailyFoodFlowResult _dailyFoodFlowResult;
    private DailyWealthFlowResult? _dailyWealthFlowResult;
    private bool _isRandomEventGenerationEnabled = true;
    private string _lastImportantChange = "пока нет.";
    private bool _isCityPanelVisible;
    private int _selectedCityTabIndex;

    public MainWindowViewModel()
    {
        _world = WorldPresets.CreateDefaultWorld();
        _city = _world.SelectedCity;
        _clock = new SimulationClock();
        _dailyFoodFlowCalculator = new DailyFoodFlowCalculator();
        _weeklyCrimeFlowCalculator = new WeeklyCrimeFlowCalculator();
        _cityStateEvaluator = new CityStateEvaluator();
        _eventManager = new CityEventManager();
        _eventEffectCalculator = new CityEventEffectCalculator();
        _eventGenerator = new CityEventGenerator(new SystemRandomProvider());
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
        _saveService = new JsonWorldSaveService();
        _eventEffectTextFormatter = new CityEventEffectTextFormatter();
        _journalService = new SimulationJournalService(_eventEffectTextFormatter);
        _dailySimulationPresentationService = new DailySimulationPresentationService(_eventEffectTextFormatter);
        Journal = new SimulationJournalViewModel(_journalService, _city.Id, _city.Name);
        _dailyFoodFlowResult = _dailyFoodFlowCalculator.Calculate(_city, BuildDailyFoodFlowInputs());
        SelectedCity = new SelectedCityViewModel(
            () => _world,
            () => _city,
            () => _dailyFoodFlowResult,
            () => _dailyWealthFlowResult,
            OpenSelectedCity);

        Control = new SimulationControlViewModel(_clock, AddTechnicalLogEntry);
        Log = new SimulationLogViewModel();
        SelectedCity = new SelectedCityViewModel(() => _world, () => _city, () => _dailyFoodFlowResult, () => _dailyWealthFlowResult);
        Summary = new SimulationSummaryViewModel(Control, SelectedCity, Log, () => IsRandomEventGenerationEnabled, () => _lastImportantChange);
        Control.ResetRequested += ResetSimulation;
        Control.PropertyChanged += (_, e) =>
        {
            if (e.PropertyName is nameof(SimulationControlViewModel.Day) or nameof(SimulationControlViewModel.Hour))
            {
                Summary.RefreshClock();
            }
        };
        Map = new MapViewModel(() => _world, _clock, AddTechnicalLogEntry);
        StartCommand = Control.StartCommand;
        PauseCommand = Control.PauseCommand;
        ResetCommand = Control.ResetCommand;
        SetNormalSpeedCommand = Control.SetNormalSpeedCommand;
        SetFastSpeedCommand = Control.SetFastSpeedCommand;
        SetVeryFastSpeedCommand = Control.SetVeryFastSpeedCommand;
        SetTurboSpeedCommand = Control.SetTurboSpeedCommand;
        SelectSettlementCommand = new RelayCommand<string>(SelectSettlement);
        OpenSelectedCityCommand = SelectedCity.OpenSelectedCityCommand;
        TriggerFireEventCommand = new RelayCommand(() => TryStartEvent(CityEventPresets.CreateFire));
        TriggerDiseaseEventCommand = new RelayCommand(() => TryStartEvent(CityEventPresets.CreateDisease));
        TriggerRatInfestationEventCommand = new RelayCommand(() => TryStartEvent(CityEventPresets.CreateRatInfestation));
        TriggerArtistsPerformanceEventCommand = new RelayCommand(() => TryStartEvent(CityEventPresets.CreateArtistsPerformance));
        TriggerPortStormEventCommand = new RelayCommand(() => TryStartEvent(CityEventPresets.CreatePortStorm));
        ToggleRandomEventGenerationCommand = new RelayCommand(ToggleRandomEventGeneration);
        SaveCommand = new AsyncRelayCommand(SaveStateAsync);
        LoadCommand = new AsyncRelayCommand(LoadStateAsync);
        TradeRouteAuthoring = new TradeRouteAuthoringViewModel(
            () => _world,
            AddTechnicalLogEntry,
            NotifyTradeRoutesChanged,
            () => Map.RefreshTradeRouteVisuals(null),
            new TradeRouteAuthoringService());

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

    public SelectedCityViewModel SelectedCity { get; }

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
    public TradeRouteAuthoringViewModel TradeRouteAuthoring { get; }

    public IReadOnlyList<City> Cities => _world.Cities;

    public string SettlementCountText => $"Поселений: {_world.Cities.Count}";

    public int Day => Control.Day;

    public int Hour => Control.Hour;

    public bool IsRunning => Control.IsRunning;

    public string SimulationState => Control.SimulationState;
    public string CurrentSimulationSpeedDisplay => Control.CurrentSimulationSpeedDisplay;

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
            if (_isRandomEventGenerationEnabled == value)
            {
                return;
            }

            _isRandomEventGenerationEnabled = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(RandomEventGenerationStatusDisplay));
            OnPropertyChanged(nameof(RandomEventGenerationToggleButtonText));
        }
    }

    public string RandomEventGenerationStatusDisplay => IsRandomEventGenerationEnabled ? "включены" : "выключены";

    public string RandomEventGenerationToggleButtonText => IsRandomEventGenerationEnabled
        ? "Выключить случайные события"
        : "Включить случайные события";

    private static string FormatSigned(decimal value)
    {
        return value.ToString("+0.##;-0.##;0");
    }

    private City CreatePreviewCity()
    {
        return new City(_city.Id, _city.Name, _city.Population, _city.Food, _city.Wealth, _city.Mood, _city.Security, _city.Crime, _city.Resources, _city.Goods, _city.CityState);
    }

    private void SelectSettlement(string? settlementId)
    {
        if (string.IsNullOrWhiteSpace(settlementId))
        {
            return;
        }

        var selectedCity = _world.FindCity(settlementId);
        if (selectedCity is null)
        {
            return;
        }

        _world.SelectedCityId = selectedCity.Id;
        _city = _world.SelectedCity;

        var selectedLocation = _world.FindSettlementMapLocation(selectedCity.Id);
        if (selectedLocation is not null)
        {
            _world.SelectedRegionId = selectedLocation.RegionId;
        }

        var eventState = _worldSimulationService.ExportEventState();
        var selectedEventManager = eventState.GetManagerOrEmpty(_world.SelectedCityId);
        _eventManager.Restore(selectedEventManager.ActiveEvents, selectedEventManager.CompletedEvents);

        Journal.SelectCity(_city.Id, _city.Name);

        RefreshSelectedCityProperties();
        RefreshEventEntries();
        Map.RefreshSelectedCityProperties();
        RefreshDailyFoodFlowPreview();
        RefreshSimulationSummary();
    }

    private void OpenSelectedCity()
    {
        IsCityPanelVisible = true;
        SelectedCityTabIndex = 0;
        OnPropertyChanged(nameof(IsCityPanelVisible));
        OnPropertyChanged(nameof(SelectedCityTabIndex));
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

        ClearViewModelSimulationCollections();
        Map.ClearSimulationCollections();
        SetLastImportantChange("симуляция сброшена к началу.");

        RefreshWorldCollectionsAfterLoad();
        TradeRouteAuthoring.ResetForWorldReset();
        RefreshSelectedCityProperties();
        RefreshClockProperties();
        Map.RefreshSelectedCityProperties();
        RefreshDailyFoodFlowPreview();
        RefreshEventEntries();
        OnPropertyChanged(nameof(CurrentSimulationSpeedDisplay));
        RefreshSimulationSummary();

        AddTechnicalLogEntry("Симуляция сброшена: день 1, час 0, мир возвращён к начальному состоянию.");
    }

    private void ClearViewModelSimulationCollections()
    {
        Log.ClearSimulationEntries();
        Journal.Clear();
    }

    private void OnControlPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        switch (e.PropertyName)
        {
            case nameof(SimulationControlViewModel.Day):
            case nameof(SimulationControlViewModel.Hour):
                OnPropertyChanged(nameof(Day));
                OnPropertyChanged(nameof(Hour));
                OnPropertyChanged(nameof(SimulationSummaryDayAndHour));
                break;
            case nameof(SimulationControlViewModel.IsRunning):
                OnPropertyChanged(nameof(IsRunning));
                break;
            case nameof(SimulationControlViewModel.SimulationState):
                OnPropertyChanged(nameof(SimulationState));
                break;
            case nameof(SimulationControlViewModel.CurrentSimulationSpeedDisplay):
                OnPropertyChanged(nameof(CurrentSimulationSpeedDisplay));
                break;
        }
    }

    private void OnDayAdvanced(int day)
    {
        var populationStart = _city.Population;
        var cityStateStart = _city.CityState;

        var simulationResult = _worldSimulationService.AdvanceDay(_world, _city.Id, day, IsRandomEventGenerationEnabled);
        if (simulationResult.SelectedCityResult is null)
        {
            return;
        }

        var eventState = _worldSimulationService.ExportEventState();
        var selectedManager = eventState.GetManagerOrEmpty(_world.SelectedCityId);
        _eventManager.Restore(selectedManager.ActiveEvents, selectedManager.CompletedEvents);

        var result = simulationResult.SelectedCityResult.FoodFlow;
        var resourceGathering = simulationResult.SelectedCityResult.ResourceGathering;
        var goodsCrafting = simulationResult.SelectedCityResult.GoodsCrafting;
        var householdConsumption = simulationResult.SelectedCityResult.HouseholdConsumption;
        var wealthFlow = simulationResult.SelectedCityResult.WealthFlow;
        var eventEffects = simulationResult.SelectedCityEventEffects;
        _dailyFoodFlowResult = result;
        _dailyWealthFlowResult = wealthFlow;

        var populationEnd = _city.Population;
        var cityStateEnd = _city.CityState;
        var presentation = _dailySimulationPresentationService.Build(new DailySimulationPresentationRequest
        {
            Day = day,
            City = _city,
            SimulationResult = simulationResult,
            PopulationStart = populationStart,
            PopulationEnd = populationEnd,
            CityStateStart = cityStateStart,
            CityStateEnd = cityStateEnd,
            ActiveEventsCount = _eventManager.ActiveEvents.Count
        });

        foreach (var technicalLogEntry in presentation.TechnicalLogEntries)
        {
            AddTechnicalLogEntry(technicalLogEntry);
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
        AppendSimulationJournalEntry(day, result, eventEffects, populationStart, populationEnd, cityStateStart, cityStateEnd, simulationResult.ActiveEventNamesBeforeAdvance, journalItems);
    }

    private void TryStartEvent(Func<int, CityEvent> factory)
    {
        var cityEvent = factory(Day);
        var added = _eventManager.AddEvent(cityEvent);

        if (added)
        {
            AddTechnicalLogEntry($"День {Day}: запущено событие “{cityEvent.Name}”.");
            SetLastImportantChange($"День {Day}: запущено событие “{cityEvent.Name}”.");
        }
        else
        {
            AddTechnicalLogEntry($"Событие “{cityEvent.Name}” уже активно.");
        }

        RefreshEventEntries();
        SelectedCity.RefreshDailyFoodFlowPreview();
        Summary.RefreshFoodBalance();
    }


    private void ToggleRandomEventGeneration()
    {
        IsRandomEventGenerationEnabled = !IsRandomEventGenerationEnabled;
        AddTechnicalLogEntry(IsRandomEventGenerationEnabled
            ? "Случайная генерация событий включена."
            : "Случайная генерация событий выключена.");
        RefreshSimulationSummary();
    }

    private void RefreshEventEntries()
    {
        Log.ActiveEventEntries.Clear();
        foreach (var activeEvent in _eventManager.ActiveEvents)
        {
            var effectSummary = _eventEffectCalculator.Calculate(_city, new[] { activeEvent });
            Log.ActiveEventEntries.Add($"{activeEvent.Name}: осталось {activeEvent.RemainingDays} дн.; эффекты: {BuildEffectSummary(effectSummary)}");
        }

        Log.CompletedEventEntries.Clear();
        foreach (var completedEvent in _eventManager.CompletedEvents)
        {
            Log.CompletedEventEntries.Add($"{completedEvent.Name}: завершено на дне {completedEvent.StartedDay + completedEvent.DurationDays}");
        }

        Log.RefreshActiveAndCompletedAvailability();
        Summary.RefreshActiveEvents();
    }

    private async Task SaveStateAsync()
    {
        try
        {
            await _saveService.SaveAsync(SaveFilePath, _world, _clock, _worldSimulationService.ExportEventState());
            AddTechnicalLogEntry($"Состояние сохранено: {SaveFilePath}");
            SetLastImportantChange("состояние сохранено.");
            RefreshSimulationSummary();
        }
        catch (Exception ex)
        {
            AddTechnicalLogEntry($"Ошибка сохранения: {ex.Message}");
        }
    }

    private async Task LoadStateAsync()
    {
        try
        {
            if (!File.Exists(SaveFilePath))
            {
                AddTechnicalLogEntry($"Файл сохранения не найден: {SaveFilePath}");
                return;
            }

            var loaded = await _saveService.LoadAsync(SaveFilePath);
            _world = loaded.World;
            if (_world.EnsureValidSelection(out var selectionReason) && selectionReason is not null)
            {
                AddTechnicalLogEntry($"Выбор города/региона в сохранении был исправлен: {selectionReason}");
            }

            if (!_world.TryGetSelectedCity(out var selectedCity))
            {
                AddTechnicalLogEntry("Ошибка загрузки: в мире отсутствуют города для выбора.");
                return;
            }

            _city = selectedCity;
            Journal.SelectCity(_city.Id, _city.Name);
            _clock.RestoreState(
                loaded.Clock.Day,
                loaded.Clock.Hour,
                loaded.Clock.IsRunning,
                loaded.Clock.AccumulatedRealTime,
                loaded.Clock.RealTimePerGameHour);
            Control.RestoreTickBaseline();
            Journal.SelectCity(_city);

            _worldSimulationService.ImportEventState(loaded.EventState, _world.SelectedCityId);
            var selectedCityEventManager = loaded.EventState.GetManagerOrEmpty(_world.SelectedCityId);
            _eventManager.Restore(selectedCityEventManager.ActiveEvents, selectedCityEventManager.CompletedEvents);
            RefreshEventEntries();
            AddTechnicalLogEntry($"События загружены: активных {selectedCityEventManager.ActiveEvents.Count}, завершённых {selectedCityEventManager.CompletedEvents.Count}.");
            AddTechnicalLogEntry($"Состояние загружено: {SaveFilePath}");
            SetLastImportantChange("состояние загружено.");

            RefreshWorldCollectionsAfterLoad();
            RefreshSelectedCityProperties();
            RefreshClockProperties();
            Map.RefreshSelectedCityProperties();
            RefreshDailyFoodFlowPreview();
            OnPropertyChanged(nameof(CurrentSimulationSpeedDisplay));
            RefreshSimulationSummary();
        }
        catch (Exception ex)
        {
            AddTechnicalLogEntry($"Ошибка загрузки: {ex.Message}");
        }
    }

    private void NotifyTradeRoutesChanged()
    {
        SelectedCity.RefreshTradeRoutes();
    }

    private void RefreshWorldCollectionsAfterLoad()
    {
        OnPropertyChanged(nameof(Cities));
        OnPropertyChanged(nameof(SettlementCountText));
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
        Summary.RefreshClock();

        if (StartCommand is RelayCommand startCommand)
        {
            startCommand.RaiseCanExecuteChanged();
        }

        if (PauseCommand is RelayCommand pauseCommand)
        {
            pauseCommand.RaiseCanExecuteChanged();
        }
    }

    private void RefreshSelectedCityProperties()
    {
        SelectedCity.RefreshSelectedCityPanel();
        Map.RefreshSelectedCityProperties();

        if (OpenSelectedCityCommand is RelayCommand openSelectedCityCommand)
        {
            openSelectedCityCommand.RaiseCanExecuteChanged();
        }
    }

    private void RefreshAllCityProperties()
    {
        SelectedCity.RefreshAllCityProperties();
        Map.RefreshSelectedCityProperties();
    }

    private void RefreshDailyFoodFlowPreview()
    {
        _dailyFoodFlowResult = _dailyFoodFlowCalculator.Calculate(_city, BuildDailyFoodFlowInputs());
        SelectedCity.RefreshDailyFoodFlowPreview();
        Summary.RefreshFoodBalance();
    }


    private DailyFoodFlowInputs BuildDailyFoodFlowInputs(CityEventEffectsResult? eventEffects = null)
    {
        var effects = eventEffects ?? _eventEffectCalculator.Calculate(_city, _eventManager.ActiveEvents);
        var baseInputs = DailyFoodFlowInputs.GothaPlaceholder;
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
            EventDelta = baseInputs.EventDelta + effects.FoodDelta
        };
    }

    private void ApplyDailyEventEffects(CityEventEffectsResult effects, int day)
    {
        _city.Mood += effects.MoodDelta;
        _city.Security += effects.SecurityDelta;
        _city.Crime += effects.CrimeDelta;
        _city.Wealth += effects.WealthDelta;
        _city.Resources += effects.ResourcesDelta;

        if (!effects.HasAnyEffect)
        {
            return;
        }

        var segments = new List<string>();

        if (effects.FoodDelta != 0m)
        {
            segments.Add($"пища {effects.FoodDelta:+0.##;-0.##;0}");
        }

        if (effects.MoodDelta != 0)
        {
            segments.Add($"настроение {effects.MoodDelta:+0;-0;0}");
        }

        if (effects.SecurityDelta != 0)
        {
            segments.Add($"безопасность {effects.SecurityDelta:+0;-0;0}");
        }

        if (effects.CrimeDelta != 0)
        {
            segments.Add($"преступность {effects.CrimeDelta:+0;-0;0}");
        }

        if (effects.WealthDelta != 0m)
        {
            segments.Add($"богатство {effects.WealthDelta:+0.##;-0.##;0}");
        }

        if (effects.ResourcesDelta != 0m)
        {
            segments.Add($"ресурсы {effects.ResourcesDelta:+0.##;-0.##;0}");
        }

        if (effects.MainlandSupplyDelta != 0m)
        {
            segments.Add($"поставки с материка {effects.MainlandSupplyDelta:+0.##;-0.##;0}");
        }

        AddTechnicalLogEntry($"День {day}: применены эффекты событий: {string.Join(", ", segments)}.");

        SelectedCity.RefreshAllCityProperties();
    }

    private void RefreshCityState(int? day = null)
    {
        var previousState = _city.CityState;
        var newState = _cityStateEvaluator.Evaluate(_city);

        if (previousState == newState)
        {
            return;
        }

        _city.CityState = newState;

        if (newState == WorldSimulator.Core.Cities.CityState.Abandoned)
        {
            _city.Mood = 0;
            _city.Security = 0;
            _city.Crime = 0;

            SelectedCity.RefreshAllCityProperties();
        }

        if (day.HasValue)
        {
            AddTechnicalLogEntry($"День {day.Value}: состояние города изменилось: {CityStateTextFormatter.ToRussian(previousState)} → {CityStateTextFormatter.ToRussian(newState)}.");
            SetLastImportantChange($"День {day.Value}: состояние города изменилось: {CityStateTextFormatter.ToRussian(previousState)} → {CityStateTextFormatter.ToRussian(newState)}.");
        }

        SelectedCity.RefreshAllCityProperties();
        Map.RefreshSelectedCityProperties();
        Summary.RefreshCityState();
    }



    private void SetLastImportantChange(string message)
    {
        _lastImportantChange = message;
        Summary.RefreshLastImportantChange();
    }

    private void RefreshSimulationSummary()
    {
        Summary.RefreshAll();
    }

    private void AddTechnicalLogEntry(string message)
    {
        Log.AddTechnicalEntry(message, MaxTechnicalLogEntries);
    }

}
