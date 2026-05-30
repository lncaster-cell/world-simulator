using System.Collections.ObjectModel;
using System.IO;
using System.Windows.Input;
using System.Windows.Threading;
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
    private static readonly TimeSpan NormalSimulationSpeed = TimeSpan.FromMinutes(5);
    private static readonly TimeSpan FastSimulationSpeed = TimeSpan.FromSeconds(10);
    private static readonly TimeSpan VeryFastSimulationSpeed = TimeSpan.FromSeconds(1);
    private static readonly TimeSpan TurboSimulationSpeed = TimeSpan.FromMilliseconds(1000d / 24d);

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
    private readonly DispatcherTimer _timer;
    private DateTimeOffset _lastTickUtc;
    private DailyFoodFlowResult _dailyFoodFlowResult;
    private DailyWealthFlowResult? _dailyWealthFlowResult;
    private bool _isRandomEventGenerationEnabled = true;
    private string _lastImportantChange = "пока нет.";

    public MainWindowViewModel()
    {
        var runtime = WorldSimulationRuntime.CreateDefault();

        _world = runtime.World;
        _city = runtime.SelectedCity;
        _clock = runtime.Clock;
        _dailyFoodFlowCalculator = runtime.DailyFoodFlowCalculator;
        _fishingProductionCalculator = runtime.FishingProductionCalculator;
        _huntingProductionCalculator = runtime.HuntingProductionCalculator;
        _agricultureProductionCalculator = runtime.AgricultureProductionCalculator;
        _mainlandSupplyProductionCalculator = runtime.MainlandSupplyProductionCalculator;
        _cityStateEvaluator = runtime.CityStateEvaluator;
        _eventManager = runtime.EventManager;
        _eventEffectCalculator = runtime.EventEffectCalculator;
        _worldSimulationService = runtime.WorldSimulationService;
        _saveService = runtime.SaveService;
        _journalService = runtime.JournalService;

        Journal = new SimulationJournalViewModel(_journalService, _city.Id, _city.Name);
        _dailyFoodFlowResult = _dailyFoodFlowCalculator.Calculate(_city, BuildDailyFoodFlowInputs());

        Control = new SimulationControlViewModel(_clock, AddTechnicalLogEntry);
        Control.ResetRequested += ResetSimulation;
        Control.PropertyChanged += (_, e) =>
        {
            if (e.PropertyName is nameof(SimulationControlViewModel.Day) or nameof(SimulationControlViewModel.Hour))
            {
                OnPropertyChanged(nameof(SimulationSummaryDayAndHour));
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
        OpenSelectedCityCommand = new RelayCommand(OpenSelectedCity, () => !string.IsNullOrWhiteSpace(_world.SelectedCityId));
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

        _lastTickUtc = DateTimeOffset.UtcNow;
        _timer = new DispatcherTimer
        {
            Interval = TimeSpan.FromMilliseconds(250)
        };
        _clock.DayAdvanced += OnDayAdvanced;
        _timer.Tick += OnTick;
        _timer.Start();

        TradeRouteAuthoring.SelectedTradeRouteForAuthoring = _world.TradeRoutes.FirstOrDefault();

        RefreshCityState();
        RefreshDailyFoodFlowPreview();
        RefreshEventEntries();
        RefreshSimulationSummary();
        Map.LoadRoutePathsForWorld();
        Map.RefreshTradeRouteVisuals(null);
    }

    public SimulationControlViewModel Control { get; }

    public SimulationJournalViewModel Journal { get; }

    public MapViewModel Map { get; }

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

    public string SimulationSummaryTitle => "Сводка симуляции";
    public string SelectedCityProfile => $"{_city.Name} — профиль поселения";
    public CityWorkforceDiagnosticsViewModel CityWorkforceDiagnostics => new(_world, _city);

    public string EconomyStocksTooltip => $"{ResourcesTooltip}{Environment.NewLine}{Environment.NewLine}{GoodsTooltip}";

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

        RefreshAllCityProperties();
        RefreshEventEntries();
        Map.RefreshSelectedCityProperties();
        RefreshSelectedCityPanelProperties();
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


    private void Start()
    {
        _clock.Start();
        _lastTickUtc = DateTimeOffset.UtcNow;
        RefreshClockProperties();
    }

    private void Pause()
    {
        _clock.Pause();
        RefreshClockProperties();
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
        RefreshAllCityProperties();
        RefreshClockProperties();
        Map.RefreshSelectedCityProperties();
        RefreshSelectedCityPanelProperties();
        RefreshDailyFoodFlowPreview();
        RefreshEventEntries();
        OnPropertyChanged(nameof(CurrentSimulationSpeedDisplay));
        RefreshSimulationSummary();

        AddTechnicalLogEntry("Симуляция сброшена: день 1, час 0, мир возвращён к начальному состоянию.");
    }

    private void ClearViewModelSimulationCollections()
    {
        TechnicalLogEntries.Clear();
        ActiveEventEntries.Clear();
        CompletedEventEntries.Clear();
        Journal.Clear();
        OnPropertyChanged(nameof(HasTechnicalLogEntries));
        OnPropertyChanged(nameof(HasActiveEventEntries));
        OnPropertyChanged(nameof(HasCompletedEventEntries));
    }

    private void SetNormalSpeed() => SetSimulationSpeed(NormalSimulationSpeed);

    private void SetFastSpeed() => SetSimulationSpeed(FastSimulationSpeed);

    private void SetVeryFastSpeed() => SetSimulationSpeed(VeryFastSimulationSpeed);

    private void SetTurboSpeed() => SetSimulationSpeed(TurboSimulationSpeed);

    private void SetSimulationSpeed(TimeSpan realTimePerGameHour)
    {
        if (_clock.RealTimePerGameHour == realTimePerGameHour)
        {
            return;
        }

        _clock.SetSimulationSpeed(realTimePerGameHour);
        AddTechnicalLogEntry($"Скорость симуляции изменена: {GetSpeedDisplay(realTimePerGameHour)}.");
        OnPropertyChanged(nameof(CurrentSimulationSpeedDisplay));
    }

    private static string GetSpeedDisplay(TimeSpan realTimePerGameHour)
    {
        if (realTimePerGameHour == NormalSimulationSpeed)
        {
            return "5 минут = 1 игровой час";
        }

        if (realTimePerGameHour == FastSimulationSpeed)
        {
            return "10 секунд = 1 игровой час";
        }

        if (realTimePerGameHour == VeryFastSimulationSpeed)
        {
            return "1 секунда = 1 игровой час";
        }

        if (realTimePerGameHour == TurboSimulationSpeed)
        {
            return "1 секунда = 1 игровой день";
        }

        return $"{realTimePerGameHour.TotalSeconds:0.##} секунд = 1 игровой час";
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

        var result = simulationResult.SelectedCityResult.FoodFlow;
        var resourceGathering = simulationResult.SelectedCityResult.ResourceGathering;
        var goodsCrafting = simulationResult.SelectedCityResult.GoodsCrafting;
        var householdConsumption = simulationResult.SelectedCityResult.HouseholdConsumption;
        var wealthFlow = simulationResult.SelectedCityResult.WealthFlow;
        var eventEffects = simulationResult.SelectedCityEventEffects;
        _dailyFoodFlowResult = result;
        _dailyWealthFlowResult = wealthFlow;

        if (simulationResult.SelectedCityCrimeFlow?.Changed == true)
        {
            AddTechnicalLogEntry($"День {day}: преступность {simulationResult.SelectedCityCrimeFlow.StartingCrime} → {simulationResult.SelectedCityCrimeFlow.EndingCrime}; недельный баланс {simulationResult.SelectedCityCrimeFlow.ClampedDelta:+0;-0;0}.");
        }

        if (wealthFlow.TotalDelta != 0m)
        { AddTechnicalLogEntry($"День {day}: благосостояние {wealthFlow.StartingWealth:0.##} → {wealthFlow.EndingWealth:0.##}; баланс {wealthFlow.TotalDelta:+0.##;-0.##;0}." +
                                 $" Дефициты: еда {wealthFlow.FoodShortagePenalty:+0.##;-0.##;0}, товары {wealthFlow.GoodsShortagePenalty:+0.##;-0.##;0}, ресурсы {wealthFlow.ResourcesShortagePenalty:+0.##;-0.##;0}."); }

        if (goodsCrafting.GoodsProduced > 0m || goodsCrafting.ResourcesConsumed > 0m)
        { AddTechnicalLogEntry($"День {day}: товары +{goodsCrafting.GoodsProduced:0.##} произведены из ресурсов -{goodsCrafting.ResourcesConsumed:0.##}."); }
        else if (goodsCrafting.ResourcesAvailable <= 0m)
        { AddTechnicalLogEntry($"День {day}: производство товаров остановлено — нет ресурсов."); }

        if (householdConsumption.GoodsConsumed > 0m || householdConsumption.ResourcesConsumed > 0m)
        { AddTechnicalLogEntry($"День {day}: население потребило товары -{householdConsumption.GoodsConsumed:0.##} и ресурсы -{householdConsumption.ResourcesConsumed:0.##}."); }

        if (householdConsumption.HasAnyShortage)
        { AddTechnicalLogEntry($"День {day}: бытовой дефицит: товары не хватает {householdConsumption.GoodsShortage:0.##}, ресурсы не хватает {householdConsumption.ResourcesShortage:0.##}."); }

        var journalItems = new List<SimulationJournalItem>();
        foreach (var completedEvent in simulationResult.CompletedEvents)
        {
            AddTechnicalLogEntry($"День {day}: завершено событие “{completedEvent.Name}”.");
            SetLastImportantChange($"День {day}: событие “{completedEvent.Name}” завершилось.");
            journalItems.Add(new SimulationJournalItem { Category = SimulationJournalCategory.Event, Title = $"Завершилось событие: {completedEvent.Name}", Details = $"Событие “{completedEvent.Name}” завершилось на дне {day}." });
        }

        if (simulationResult.SelectedCityPopulationChange?.PopulationDelta is int delta && delta != 0)
        {
            var p = simulationResult.SelectedCityPopulationChange;
            AddTechnicalLogEntry($"День {day}: население изменилось {p.StartingPopulation} → {p.EndingPopulation} ({p.PopulationDelta:+0;-0;0}), причина: {p.Reason}.");
            SetLastImportantChange($"День {day}: население изменилось {p.StartingPopulation} → {p.EndingPopulation} ({p.PopulationDelta:+0;-0;0}), причина: {p.Reason}.");
        }

        if (simulationResult.GeneratedEvent is not null)
        {
            AddTechnicalLogEntry($"День {day}: случайное событие “{simulationResult.GeneratedEvent.Name}” началось в городе.");
            SetLastImportantChange($"День {day}: случайное событие “{simulationResult.GeneratedEvent.Name}” началось в городе.");
            journalItems.Add(new SimulationJournalItem { Category = SimulationJournalCategory.Event, Title = $"Началось событие: {simulationResult.GeneratedEvent.Name}", Details = $"Случайное событие “{simulationResult.GeneratedEvent.Name}” началось в городе." });
        }

        AddTechnicalLogEntry($"День {day}: пища {result.StartingFood:0.##} → {result.EndingFood:0.##}; баланс {result.TotalDelta:+0.##;-0.##;0} (потребление -{result.PopulationConsumption:0.##}, земледелие {result.AgricultureIncome:+0.##;-0.##;0}, рыбалка {result.FishingIncome:+0.##;-0.##;0}, охота {result.HuntingIncome:+0.##;-0.##;0}, поставки {result.MainlandSupplyIncome:+0.##;-0.##;0}, события {result.EventDelta:+0.##;-0.##;0}).");

        var populationEnd = _city.Population;
        var cityStateEnd = _city.CityState;
        _city = _world.SelectedCity;
        if (simulationResult.WeeklyTradeFlowResult is not null)
        {
            Map.RefreshTradeRouteVisuals(simulationResult.WeeklyTradeFlowResult);
        }
        RefreshAllCityProperties();
        OnPropertyChanged(nameof(Food)); OnPropertyChanged(nameof(Resources)); OnPropertyChanged(nameof(Wealth)); OnPropertyChanged(nameof(WealthTooltip)); Map.RefreshSelectedCityProperties(); OnPropertyChanged(nameof(FoodBalanceTooltip)); OnPropertyChanged(nameof(FishingProductionTooltip)); OnPropertyChanged(nameof(ResourcesTooltip)); OnPropertyChanged(nameof(GoodsTooltip)); OnPropertyChanged(nameof(CrimeFlowTooltip)); OnPropertyChanged(nameof(EconomyStocksTooltip)); OnPropertyChanged(nameof(Resources)); OnPropertyChanged(nameof(Goods));
        RefreshEventEntries(); RefreshDailyFoodFlowPreview(); RefreshSimulationSummary();
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
        OnPropertyChanged(nameof(FoodBalanceTooltip));
        OnPropertyChanged(nameof(FishingProductionTooltip));
        OnPropertyChanged(nameof(ResourcesTooltip));
        OnPropertyChanged(nameof(GoodsTooltip));
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
        ActiveEventEntries.Clear();
        foreach (var activeEvent in _eventManager.ActiveEvents)
        {
            var effectSummary = _eventEffectCalculator.Calculate(_city, new[] { activeEvent });
            ActiveEventEntries.Add($"{activeEvent.Name}: осталось {activeEvent.RemainingDays} дн.; эффекты: {BuildEffectSummary(effectSummary)}");
        }

        CompletedEventEntries.Clear();
        foreach (var completedEvent in _eventManager.CompletedEvents)
        {
            CompletedEventEntries.Add($"{completedEvent.Name}: завершено на дне {completedEvent.StartedDay + completedEvent.DurationDays}");
        }

        OnPropertyChanged(nameof(HasActiveEventEntries));
        OnPropertyChanged(nameof(HasCompletedEventEntries));
    }

    private void OnTick(object? sender, EventArgs e)
    {
        var now = DateTimeOffset.UtcNow;
        var elapsed = now - _lastTickUtc;
        _lastTickUtc = now;

        _clock.Advance(elapsed);

        RefreshClockProperties();
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
            _lastTickUtc = DateTimeOffset.UtcNow;
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
            RefreshAllCityProperties();
            RefreshClockProperties();
            Map.RefreshSelectedCityProperties();
            RefreshSelectedCityPanelProperties();
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
        OnPropertyChanged(nameof(TradeRoutes));
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
        OnPropertyChanged(nameof(SimulationSummaryDayAndHour));

        if (StartCommand is RelayCommand startCommand)
        {
            startCommand.RaiseCanExecuteChanged();
        }

        if (PauseCommand is RelayCommand pauseCommand)
        {
            pauseCommand.RaiseCanExecuteChanged();
        }
    }

    private void RefreshSelectedCityPanelProperties()
    {
        OnPropertyChanged(nameof(SelectedCityName));
        OnPropertyChanged(nameof(SelectedRegionName));
        OnPropertyChanged(nameof(SelectedCityProfile));
        OnPropertyChanged(nameof(CityWorkforceDiagnostics));
        OnPropertyChanged(nameof(CityStateDisplay));
        Map.RefreshSelectedCityProperties();

        if (OpenSelectedCityCommand is RelayCommand openSelectedCityCommand)
        {
            openSelectedCityCommand.RaiseCanExecuteChanged();
        }
    }

    private void RefreshAllCityProperties()
    {
        OnPropertyChanged(nameof(CityName));
        OnPropertyChanged(nameof(CityInfrastructureRows));
        OnPropertyChanged(nameof(CityWorkforceDiagnostics));
        OnPropertyChanged(nameof(CityState));
        OnPropertyChanged(nameof(CityStateDisplay));
        OnPropertyChanged(nameof(Population));
        OnPropertyChanged(nameof(Food));
        OnPropertyChanged(nameof(FoodBalanceTooltip));
        OnPropertyChanged(nameof(FishingProductionTooltip));
        OnPropertyChanged(nameof(ResourcesTooltip));
        OnPropertyChanged(nameof(GoodsTooltip));
        OnPropertyChanged(nameof(Wealth));
        OnPropertyChanged(nameof(Mood));
        OnPropertyChanged(nameof(Security));
        OnPropertyChanged(nameof(Crime));
        OnPropertyChanged(nameof(CrimeFlowTooltip));
        OnPropertyChanged(nameof(Resources));
        OnPropertyChanged(nameof(Goods));
        OnPropertyChanged(nameof(DailyFoodConsumption));
        OnPropertyChanged(nameof(WealthTooltip));
        Map.RefreshSelectedCityProperties();
    }

    private void RefreshDailyFoodFlowPreview()
    {
        _dailyFoodFlowResult = _dailyFoodFlowCalculator.Calculate(_city, BuildDailyFoodFlowInputs());

        OnPropertyChanged(nameof(DailyFoodStartingFood));
        OnPropertyChanged(nameof(DailyFoodPopulationConsumption));
        OnPropertyChanged(nameof(DailyFoodAgricultureIncome));
        OnPropertyChanged(nameof(DailyFoodFishingIncome));
        OnPropertyChanged(nameof(DailyFoodHuntingIncome));
        OnPropertyChanged(nameof(DailyFoodMainlandSupplyIncome));
        OnPropertyChanged(nameof(DailyFoodEventDelta));
        OnPropertyChanged(nameof(DailyFoodTotalDelta));
        OnPropertyChanged(nameof(DailyFoodEndingFood));
        OnPropertyChanged(nameof(DailyFoodPopulationConsumptionDisplay));
        OnPropertyChanged(nameof(DailyFoodAgricultureIncomeDisplay));
        OnPropertyChanged(nameof(DailyFoodFishingIncomeDisplay));
        OnPropertyChanged(nameof(DailyFoodHuntingIncomeDisplay));
        OnPropertyChanged(nameof(DailyFoodMainlandSupplyIncomeDisplay));
        OnPropertyChanged(nameof(DailyFoodEventDeltaDisplay));
        OnPropertyChanged(nameof(DailyFoodTotalDeltaDisplay));
        OnPropertyChanged(nameof(FoodBalanceTooltip));
        OnPropertyChanged(nameof(FishingProductionTooltip));
        OnPropertyChanged(nameof(ResourcesTooltip));
        OnPropertyChanged(nameof(GoodsTooltip));
        OnPropertyChanged(nameof(SimulationSummaryFoodBalance));
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

        OnPropertyChanged(nameof(Mood));
        OnPropertyChanged(nameof(Security));
        OnPropertyChanged(nameof(Crime));
        OnPropertyChanged(nameof(Wealth));
        OnPropertyChanged(nameof(Resources));
        OnPropertyChanged(nameof(ResourcesTooltip));
        OnPropertyChanged(nameof(GoodsTooltip));
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

            OnPropertyChanged(nameof(Mood));
            OnPropertyChanged(nameof(Security));
            OnPropertyChanged(nameof(Crime));
        }

        if (day.HasValue)
        {
            AddTechnicalLogEntry($"День {day.Value}: состояние города изменилось: {ToRussianCityState(previousState)} → {ToRussianCityState(newState)}.");
            SetLastImportantChange($"День {day.Value}: состояние города изменилось: {ToRussianCityState(previousState)} → {ToRussianCityState(newState)}.");
        }

        OnPropertyChanged(nameof(CityState));
        OnPropertyChanged(nameof(CityStateDisplay));
        OnPropertyChanged(nameof(FishingProductionTooltip));
        OnPropertyChanged(nameof(WealthTooltip));
        Map.RefreshSelectedCityProperties();
        OnPropertyChanged(nameof(SimulationSummaryCityState));
    }



    private string BuildWealthTooltip()
    {
        var flow = _dailyWealthFlowResult ?? new DailyWealthFlowResult
        {
            StartingWealth = Wealth, PortTradeBonus = 0m, GoodsProductionBonus = 0m, ConsumptionCoverageBonus = 0m,
            FoodShortagePenalty = 0m, GoodsShortagePenalty = 0m, ResourcesShortagePenalty = 0m, SecurityModifierDelta = 0m, CrimePenalty = 0m, CityStateDelta = 0m, TotalDelta = 0m, EndingWealth = Wealth
        };

        return $"Благосостояние:{Environment.NewLine}" +
               $"Текущее значение: {Wealth:0.##}{Environment.NewLine}{Environment.NewLine}" +
               $"Прогноз на день:{Environment.NewLine}" +
               $"Портовая торговля: {flow.PortTradeBonus:+0.##;-0.##;0}{Environment.NewLine}" +
               $"Производство товаров: {flow.GoodsProductionBonus:+0.##;-0.##;0}{Environment.NewLine}" +
               $"Покрытие бытовых потребностей: {flow.ConsumptionCoverageBonus:+0.##;-0.##;0}{Environment.NewLine}{Environment.NewLine}" +
               $"Штрафы:{Environment.NewLine}" +
               $"Нехватка еды: {flow.FoodShortagePenalty:+0.##;-0.##;0}{Environment.NewLine}" +
               $"Дефицит товаров: {flow.GoodsShortagePenalty:+0.##;-0.##;0}{Environment.NewLine}" +
               $"Дефицит ресурсов: {flow.ResourcesShortagePenalty:+0.##;-0.##;0}{Environment.NewLine}" +
               $"Безопасность: {flow.SecurityModifierDelta:+0.##;-0.##;0}{Environment.NewLine}" +
               $"Преступность: {flow.CrimePenalty:+0.##;-0.##;0}{Environment.NewLine}" +
               $"Состояние города: {flow.CityStateDelta:+0.##;-0.##;0}{Environment.NewLine}{Environment.NewLine}" +
               $"Итоговый баланс: {flow.TotalDelta:+0.##;-0.##;0}{Environment.NewLine}" +
               $"Ожидаемое благосостояние после дня: {flow.EndingWealth:0.##}";
    }

    private void SetLastImportantChange(string message)
    {
        _lastImportantChange = message;
        OnPropertyChanged(nameof(SimulationSummaryLastImportantChange));
    }

    private void RefreshSimulationSummary()
    {
        OnPropertyChanged(nameof(SimulationSummaryTitle));
        OnPropertyChanged(nameof(SimulationSummaryDayAndHour));
        OnPropertyChanged(nameof(SimulationSummaryCityState));
        OnPropertyChanged(nameof(SimulationSummaryFoodBalance));
        OnPropertyChanged(nameof(SimulationSummaryActiveEvents));
        OnPropertyChanged(nameof(SimulationSummaryRandomEventsStatus));
        OnPropertyChanged(nameof(SimulationSummaryLastImportantChange));
    }

    private void AppendSimulationJournalEntry(
        int day,
        DailyFoodFlowResult foodResult,
        CityEventEffectsResult eventEffects,
        int populationStart,
        int populationEnd,
        WorldSimulator.Core.Cities.CityState cityStateStart,
        WorldSimulator.Core.Cities.CityState cityStateEnd,
        IReadOnlyList<string> activeEventNamesBeforeAdvance,
        List<SimulationJournalItem> items)
    {
        Journal.Append(new SimulationJournalAppendRequest
        {
            Day = day,
            City = _city,
            FoodResult = foodResult,
            EventEffects = eventEffects,
            PopulationStart = populationStart,
            PopulationEnd = populationEnd,
            CityStateStart = cityStateStart,
            CityStateEnd = cityStateEnd,
            ActiveEventsCount = _eventManager.ActiveEvents.Count,
            ActiveEventNamesBeforeAdvance = activeEventNamesBeforeAdvance,
            Items = items
        });
    }

    private static string ToRussianCityState(WorldSimulator.Core.Cities.CityState cityState)
    {
        return cityState switch
        {
            WorldSimulator.Core.Cities.CityState.Stable => "Стабильность",
            WorldSimulator.Core.Cities.CityState.Prosperous => "Процветание",
            WorldSimulator.Core.Cities.CityState.Stagnation => "Стагнация",
            WorldSimulator.Core.Cities.CityState.FoodShortage => "Нехватка пищи",
            WorldSimulator.Core.Cities.CityState.Famine => "Голод",
            WorldSimulator.Core.Cities.CityState.EconomicDecline => "Экономический спад",
            WorldSimulator.Core.Cities.CityState.CrimeProblem => "Проблемы с преступностью",
            WorldSimulator.Core.Cities.CityState.Unrest => "Беспорядки",
            WorldSimulator.Core.Cities.CityState.Recovery => "Восстановление",
            WorldSimulator.Core.Cities.CityState.Collapse => "Коллапс",
            WorldSimulator.Core.Cities.CityState.Abandoned => "Опустевший город",
            _ => cityState.ToString()
        };
    }

    private static string BuildEffectSummary(CityEventEffectsResult effects)
    {
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

        return segments.Count == 0 ? "без эффектов" : string.Join(", ", segments);
    }

    private void AddTechnicalLogEntry(string message)
    {
        TechnicalLogEntries.Add(message);

        if (TechnicalLogEntries.Count > MaxTechnicalLogEntries)
        {
            TechnicalLogEntries.RemoveAt(0);
        }

        OnPropertyChanged(nameof(HasTechnicalLogEntries));
    }

}
