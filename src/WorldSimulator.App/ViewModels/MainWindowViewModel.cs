using System.Collections.ObjectModel;
using System.Globalization;
using System.ComponentModel;
using System.IO;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Input;
using System.Windows.Threading;
using WorldSimulator.App.Infrastructure;
using WorldSimulator.Core.Cities;
using WorldSimulator.Core.Events;
using WorldSimulator.Core.Resources;
using WorldSimulator.Core.Time;
using WorldSimulator.Core.Simulation;
using WorldSimulator.Core.World;
using WorldSimulator.Core.Trade;
using WorldSimulator.Persistence.Saves;

namespace WorldSimulator.App.ViewModels;

public sealed class MainWindowViewModel : INotifyPropertyChanged
{
    private static readonly TimeSpan NormalSimulationSpeed = TimeSpan.FromMinutes(5);
    private static readonly TimeSpan FastSimulationSpeed = TimeSpan.FromSeconds(10);
    private static readonly TimeSpan VeryFastSimulationSpeed = TimeSpan.FromSeconds(1);
    private static readonly TimeSpan TurboSimulationSpeed = TimeSpan.FromMilliseconds(1000d / 24d);

    private const string SaveFilePath = "data/save/world_save.json";
    private const int MaxTechnicalLogEntries = 500;
    private const int MaxSimulationJournalDays = 500;
    private const int MaxVisibleCaravanMovementMarkers = 12;
    private const string GothaCityId = "gotha";
    private SimulationWorld _world;
    private City _city;
    private readonly SimulationClock _clock;
    private readonly DailyFoodFlowCalculator _dailyFoodFlowCalculator;
    private readonly FishingProductionCalculator _fishingProductionCalculator = new();
    private readonly HuntingProductionCalculator _huntingProductionCalculator = new();
    private readonly AgricultureProductionCalculator _agricultureProductionCalculator = new();
    private readonly MainlandSupplyProductionCalculator _mainlandSupplyProductionCalculator = new();
    private readonly GoodsCraftingProductionCalculator _goodsCraftingProductionCalculator = new();
    private readonly ResourceGatheringProductionCalculator _resourceGatheringProductionCalculator = new();
    private readonly HouseholdConsumptionCalculator _householdConsumptionCalculator = new();
    private readonly DailyWealthFlowCalculator _dailyWealthFlowCalculator = new();
    private readonly WeeklyCrimeFlowCalculator _weeklyCrimeFlowCalculator;
    private readonly CityStateEvaluator _cityStateEvaluator;
    private readonly CityEventManager _eventManager;
    private readonly CityEventEffectCalculator _eventEffectCalculator;
    private readonly CityEventGenerator _eventGenerator;
    private readonly WorldSimulationService _worldSimulationService;
    private readonly JsonWorldSaveService _saveService;
    private readonly DispatcherTimer _timer;
    private DateTimeOffset _lastTickUtc;
    private DailyFoodFlowResult _dailyFoodFlowResult;
    private DailyWealthFlowResult? _dailyWealthFlowResult;
    private bool _isRandomEventGenerationEnabled = true;
    private string _lastImportantChange = "пока нет.";
    private bool _isMapCalibrationModeEnabled;
    private SimulationJournalFilterOption _selectedSimulationJournalFilter = SimulationJournalFilterOption.All;
    private SimulationJournalEntry? _selectedSimulationJournalEntry;
    private double? _lastMapCalibrationX;
    private double? _lastMapCalibrationY;
    private string _selectedJournalCityId = GothaCityId;
    private readonly List<TradeRouteVisualViewModel> _tradeRouteVisuals = [];
    private readonly DispatcherTimer _tradeMarkerAnimationTimer;
    private DateTimeOffset _lastTradeMarkerAnimationTickUtc;
    private TradeRoute? _selectedTradeRouteForAuthoring;
    private City? _routeAuthoringOriginSettlement;
    private City? _routeAuthoringDestinationCandidateSettlement;
    private City? _activeRouteAuthoringDestinationSettlement;
    private string _routeAuthoringRouteIdDisplay = "RouteId: —";
    private string? _currentDraftDestinationId;
    private readonly Dictionary<string, List<MapPointViewModel>> _routeAuthoringDraftPointsByDestinationId = [];
    private readonly HashSet<string> _loadedCaravanPathRouteIds = new(StringComparer.OrdinalIgnoreCase);
    private bool _isTradeRouteAuthoringModeEnabled;
    private decimal _selectedTradeRouteDistanceDays = 1m;
    private string _selectedTradeRouteDistanceDaysInput = "1.0";
    private bool _isTradeRoutesOverlayVisible;
    private bool _isLoadedRoutePathsDebugVisible;

    public MainWindowViewModel()
    {
        _world = WorldPresets.CreateDefaultWorld();
        LoadRoutePathsForWorld();
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
        _dailyFoodFlowResult = _dailyFoodFlowCalculator.Calculate(_city, BuildDailyFoodFlowInputs());

        StartCommand = new RelayCommand(Start, () => !_clock.IsRunning);
        PauseCommand = new RelayCommand(Pause, () => _clock.IsRunning);
        SetNormalSpeedCommand = new RelayCommand(SetNormalSpeed);
        SetFastSpeedCommand = new RelayCommand(SetFastSpeed);
        SetVeryFastSpeedCommand = new RelayCommand(SetVeryFastSpeed);
        SetTurboSpeedCommand = new RelayCommand(SetTurboSpeed);
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
        ToggleMapCalibrationModeCommand = new RelayCommand(ToggleMapCalibrationMode);
        AddTradeRoutePointCommand = new RelayCommand<MapPointViewModel>(AddTradeRoutePoint);
        UndoTradeRoutePointCommand = new RelayCommand(UndoTradeRoutePoint);
        ClearTradeRoutePointsCommand = new RelayCommand(ClearTradeRoutePoints);
        SaveTradeRoutePointsCommand = new RelayCommand(SaveTradeRoutePoints);
        CopyTradeRoutePointsCommand = new RelayCommand(CopyTradeRoutePoints);
        AddRouteAuthoringDestinationCommand = new RelayCommand(AddRouteAuthoringDestination);

        _lastTickUtc = DateTimeOffset.UtcNow;
        _timer = new DispatcherTimer
        {
            Interval = TimeSpan.FromMilliseconds(250)
        };
        _tradeMarkerAnimationTimer = new DispatcherTimer
        {
            Interval = TimeSpan.FromMilliseconds(100)
        };

        _clock.DayAdvanced += OnDayAdvanced;
        _timer.Tick += OnTick;
        _tradeMarkerAnimationTimer.Tick += OnTradeMarkerAnimationTick;
        _timer.Start();
        _tradeMarkerAnimationTimer.Start();

        EditedTradeRoutePoints.CollectionChanged += (_, _) =>
        {
            OnPropertyChanged(nameof(EditedTradeRoutePointCount));
            OnPropertyChanged(nameof(EditedTradeRoutePolylinePoints));
        };
        SelectedTradeRouteForAuthoring = _world.TradeRoutes.FirstOrDefault();

        RefreshCityState();
        RefreshDailyFoodFlowPreview();
        RefreshEventEntries();
        RefreshSimulationSummary();
        RefreshTradeRouteVisuals(null);
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    public ICommand StartCommand { get; }

    public ICommand PauseCommand { get; }

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
    public ICommand ToggleMapCalibrationModeCommand { get; }
    public ICommand AddTradeRoutePointCommand { get; }
    public ICommand UndoTradeRoutePointCommand { get; }
    public ICommand ClearTradeRoutePointsCommand { get; }
    public ICommand SaveTradeRoutePointsCommand { get; }
    public ICommand CopyTradeRoutePointsCommand { get; }
    public ICommand AddRouteAuthoringDestinationCommand { get; }

    public IReadOnlyList<City> Cities => _world.Cities;

    public string SettlementCountText => $"Поселений: {_world.Cities.Count}";

    public int Day => _clock.Day;

    public int Hour => _clock.Hour;

    public bool IsRunning => _clock.IsRunning;

    public string SimulationState => IsRunning ? "Запущено" : "Пауза";
    public string CurrentSimulationSpeedDisplay => $"Текущая скорость: {GetSpeedDisplay(_clock.RealTimePerGameHour)}";

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
    public ObservableCollection<CaravanMovementMarkerViewModel> ActiveCaravanMovementMarkers { get; } = [];
    public IReadOnlyList<TradeRouteVisualViewModel> TradeRouteVisuals => _tradeRouteVisuals;
    public IReadOnlyList<TradeRouteVisualViewModel> DebugLoadedRoutePathVisuals => _tradeRouteVisuals.Where(x => x.IsLoadedPath && x.Points.Count >= 2).ToList();
    public IReadOnlyList<TradeRoute> TradeRoutes => _world.TradeRoutes;
    public IReadOnlyList<City> RouteAuthoringSettlements => _world.Cities;
    public IReadOnlyList<City> AvailableRouteAuthoringDestinations => _world.Cities;
    public ObservableCollection<City> RouteAuthoringDestinationSettlements { get; } = [];
    public ObservableCollection<MapPointViewModel> EditedTradeRoutePoints { get; } = [];
    public int EditedTradeRoutePointCount => EditedTradeRoutePoints.Count;
    public int EditedTradeRouteIntermediatePointCount => EditedTradeRoutePoints.Count;
    public int RouteAuthoringDestinationCount => RouteAuthoringDestinationSettlements.Count;
    public bool CanAddMoreRouteAuthoringDestinations => RouteAuthoringDestinationSettlements.Count < 8;
    public bool HasSelectedTradeRouteForAuthoring => SelectedTradeRouteForAuthoring is not null;
    public List<RoutePoint> EditedTradeRoutePolylinePoints => BuildFullRoutePoints();
    public string RouteAuthoringRouteIdDisplay => _routeAuthoringRouteIdDisplay;

    public string SimulationSummaryDayAndHour => $"День {Day}, час {Hour}";

    public string SimulationSummaryCityState => $"{CityName}: {CityStateDisplay}";

    public string SimulationSummaryFoodBalance => $"Пищевой баланс: {DailyFoodTotalDelta:+0.##;-0.##;0}/день, запас {Food:0.##}";

    public string SimulationSummaryActiveEvents => $"Активные события: {_eventManager.ActiveEvents.Count}";

    public string SimulationSummaryRandomEventsStatus => $"Случайные события: {RandomEventGenerationStatusDisplay}";

    public string SimulationSummaryLastImportantChange => $"Последнее изменение: {_lastImportantChange}";

    public string CityName => _city.Name;

    public string CityState => _city.CityState.ToString();

    public string CityStateDisplay => ToRussianCityState(_city.CityState);

    public int Population => _city.Population;

    public decimal Food => _city.Food;

    public decimal Wealth => _city.Wealth;

    public string WealthTooltip => BuildWealthTooltip();

    public int Mood => _city.Mood;

    public int Security => _city.Security;

    public int Crime => _city.Crime;

    public decimal Resources => _city.Resources;

    public decimal Goods => _city.Goods;

    public decimal DailyFoodConsumption => _city.CalculateDailyFoodConsumption();

    public decimal DailyFoodStartingFood => _dailyFoodFlowResult.StartingFood;

    public decimal DailyFoodPopulationConsumption => _dailyFoodFlowResult.PopulationConsumption;

    public decimal DailyFoodAgricultureIncome => _dailyFoodFlowResult.AgricultureIncome;

    public decimal DailyFoodFishingIncome => _dailyFoodFlowResult.FishingIncome;

    public decimal DailyFoodHuntingIncome => _dailyFoodFlowResult.HuntingIncome;

    public decimal DailyFoodMainlandSupplyIncome => _dailyFoodFlowResult.MainlandSupplyIncome;

    public decimal DailyFoodEventDelta => _dailyFoodFlowResult.EventDelta;

    public decimal DailyFoodTotalDelta => _dailyFoodFlowResult.TotalDelta;

    public decimal DailyFoodEndingFood => _dailyFoodFlowResult.EndingFood;

    public string DailyFoodPopulationConsumptionDisplay => $"-{DailyFoodPopulationConsumption:0.##}";

    public string DailyFoodAgricultureIncomeDisplay => FormatSigned(DailyFoodAgricultureIncome);

    public string DailyFoodFishingIncomeDisplay => FormatSigned(DailyFoodFishingIncome);

    public string DailyFoodHuntingIncomeDisplay => FormatSigned(DailyFoodHuntingIncome);

    public string DailyFoodMainlandSupplyIncomeDisplay => FormatSigned(DailyFoodMainlandSupplyIncome);

    public string DailyFoodEventDeltaDisplay => FormatSigned(DailyFoodEventDelta);

    public string DailyFoodTotalDeltaDisplay => FormatSigned(DailyFoodTotalDelta);

    public string FoodBalanceTooltip =>
        _dailyFoodFlowResult is null
            ? "Пищевой баланс ещё не рассчитан."
            : $"Пища сейчас: {Food:0.##}{Environment.NewLine}{Environment.NewLine}" +
              $"Прогноз на день:{Environment.NewLine}" +
              $"Начальная пища: {DailyFoodStartingFood:0.##}{Environment.NewLine}" +
              $"Потребление населения: {DailyFoodPopulationConsumptionDisplay}{Environment.NewLine}" +
              $"Земледелие: {DailyFoodAgricultureIncomeDisplay}{Environment.NewLine}" +
              $"Рыбалка: {DailyFoodFishingIncomeDisplay}{Environment.NewLine}" +
              $"Охота: {DailyFoodHuntingIncomeDisplay}{Environment.NewLine}" +
              $"Поставки с материка: {DailyFoodMainlandSupplyIncomeDisplay}{Environment.NewLine}" +
              $"События: {DailyFoodEventDeltaDisplay}{Environment.NewLine}{Environment.NewLine}" +
              $"Дневной баланс: {DailyFoodTotalDeltaDisplay}{Environment.NewLine}" +
              $"Ожидаемая пища после дня: {DailyFoodEndingFood:0.##}";

    public string GoodsTooltip
    {
        get
        {
            var resourceGathering = _resourceGatheringProductionCalculator.Calculate(_city);
            var previewCity = CreatePreviewCity();
            previewCity.Resources += resourceGathering.FinalOutput;

            var goodsCrafting = _goodsCraftingProductionCalculator.Calculate(previewCity);
            previewCity.Resources -= goodsCrafting.ResourcesConsumed;
            previewCity.Goods += goodsCrafting.GoodsProduced;

            var householdConsumption = _householdConsumptionCalculator.Calculate(previewCity);
            var expectedBalance = goodsCrafting.GoodsProduced - householdConsumption.GoodsConsumed;

            return $"Товары:{Environment.NewLine}" +
                   $"Текущий запас: {Goods:0.##}{Environment.NewLine}{Environment.NewLine}" +
                   $"Производство:{Environment.NewLine}" +
                   $"Производство товаров: +{goodsCrafting.GoodsProduced:0.##}{Environment.NewLine}" +
                   $"Расход ресурсов на производство: -{goodsCrafting.ResourcesConsumed:0.##}{Environment.NewLine}{Environment.NewLine}" +
                   $"Бытовое потребление:{Environment.NewLine}" +
                   $"Потребность населения: -{householdConsumption.RequiredGoods:0.##}{Environment.NewLine}" +
                   $"Фактически потреблено: -{householdConsumption.GoodsConsumed:0.##}{Environment.NewLine}" +
                   $"Дефицит: {householdConsumption.GoodsShortage:0.##}{Environment.NewLine}{Environment.NewLine}" +
                   $"Итог:{Environment.NewLine}" +
                   $"Ожидаемый баланс товаров за день: {expectedBalance:+0.##;-0.##;0}";
        }
    }


    public string CrimeFlowTooltip
    {
        get
        {
            var foodFlow = _dailyFoodFlowCalculator.Calculate(_city, BuildDailyFoodFlowInputs());
            var householdConsumption = _householdConsumptionCalculator.Calculate(_city);
            var crimeFlow = _weeklyCrimeFlowCalculator.Calculate(_city, foodFlow, householdConsumption);

            return $"Преступность:{Environment.NewLine}" +
                   $"Текущее значение: {Crime}{Environment.NewLine}{Environment.NewLine}" +
                   $"Недельные причины роста/снижения:{Environment.NewLine}" +
                   $"Пища: {FormatSigned(crimeFlow.FoodPressure)}{Environment.NewLine}" +
                   $"Товары: {FormatSigned(crimeFlow.GoodsShortagePressure)}{Environment.NewLine}" +
                   $"Ресурсы: {FormatSigned(crimeFlow.ResourcesShortagePressure)}{Environment.NewLine}" +
                   $"Настроение: {FormatSigned(crimeFlow.MoodPressure)}{Environment.NewLine}" +
                   $"Безопасность: {FormatSigned(crimeFlow.SecurityPressure)}{Environment.NewLine}" +
                   $"Состояние города: {FormatSigned(crimeFlow.CityStatePressure)}{Environment.NewLine}" +
                   $"Менталитет: {FormatSigned(crimeFlow.MentalityPressure)}{Environment.NewLine}" +
                   $"Законы: {FormatSigned(crimeFlow.LawPressure)}{Environment.NewLine}" +
                   $"Глобальные события: {FormatSigned(crimeFlow.GlobalEventsPressure)}{Environment.NewLine}" +
                   $"Меры порядка: -{crimeFlow.FutureOrderMeasuresReduction:0}{Environment.NewLine}{Environment.NewLine}" +
                   $"Итог недели: {FormatSigned(crimeFlow.ClampedDelta)}{Environment.NewLine}" +
                   $"Ожидаемая преступность: {crimeFlow.EndingCrime}";
        }
    }

    public string ResourcesTooltip
    {
        get
        {
            var resourceGathering = _resourceGatheringProductionCalculator.Calculate(_city);
            var previewCity = CreatePreviewCity();
            previewCity.Resources += resourceGathering.FinalOutput;

            var goodsCrafting = _goodsCraftingProductionCalculator.Calculate(previewCity);
            previewCity.Resources -= goodsCrafting.ResourcesConsumed;
            previewCity.Goods += goodsCrafting.GoodsProduced;

            var householdConsumption = _householdConsumptionCalculator.Calculate(previewCity);
            var expectedBalance = resourceGathering.FinalOutput - goodsCrafting.ResourcesConsumed - householdConsumption.ResourcesConsumed;

            return $"Ресурсы:{Environment.NewLine}" +
                   $"Текущий запас: {Resources:0.##}{Environment.NewLine}{Environment.NewLine}" +
                   $"Приход:{Environment.NewLine}" +
                   $"Сбор ресурсов: +{resourceGathering.FinalOutput:0.##}{Environment.NewLine}{Environment.NewLine}" +
                   $"Расход:{Environment.NewLine}" +
                   $"Производство товаров: -{goodsCrafting.ResourcesConsumed:0.##}{Environment.NewLine}" +
                   $"Бытовое потребление населения: -{householdConsumption.ResourcesConsumed:0.##}{Environment.NewLine}{Environment.NewLine}" +
                   $"Дефицит бытовых ресурсов: {householdConsumption.ResourcesShortage:0.##}{Environment.NewLine}{Environment.NewLine}" +
                   $"Итог:{Environment.NewLine}" +
                   $"Ожидаемый баланс ресурсов за день: {expectedBalance:+0.##;-0.##;0}";
        }
    }

    public string FishingProductionTooltip
    {
        get
        {
            var result = _fishingProductionCalculator.Calculate(_city, _eventManager.ActiveEvents);

            return $"Рыбалка:{Environment.NewLine}" +
                   $"Природный потенциал: {result.NaturalPotential:0.##}{Environment.NewLine}" +
                   $"Инфраструктура: уровень {result.InfrastructureLevel}, модификатор ×{result.InfrastructureModifier:0.##}{Environment.NewLine}" +
                   $"Мощность инфраструктуры: {result.InfrastructureCapacity:0.##}{Environment.NewLine}" +
                   $"Рабочие: {result.AssignedWorkers} / {result.RequiredWorkers}{Environment.NewLine}" +
                   $"Заполнение рабочих: {result.WorkerCoverage:P0}{Environment.NewLine}" +
                   $"Сверхштатные рабочие: {result.ExtraWorkers}{Environment.NewLine}" +
                   $"Бонус сверхштата: {result.OverstaffBonus:+0.##;-0.##;0}{Environment.NewLine}" +
                   $"Шторм: ×{result.StormModifier:0.##}{Environment.NewLine}" +
                   $"Состояние города: ×{result.StateModifier:0.##}{Environment.NewLine}{Environment.NewLine}" +
                   $"Итог рыбалки: {result.FinalOutput:+0.##;-0.##;0} пищи/день";
        }
    }

    public ObservableCollection<string> TechnicalLogEntries { get; } = new();

    public bool HasTechnicalLogEntries => TechnicalLogEntries.Count > 0;

    public ObservableCollection<string> ActiveEventEntries { get; } = new();
    public ObservableCollection<SimulationJournalEntry> SimulationJournalEntries { get; } = new();
    public ObservableCollection<SimulationJournalEntry> FilteredSimulationJournalEntries { get; } = new();

    public ObservableCollection<string> CompletedEventEntries { get; } = new();
    public IReadOnlyList<SimulationJournalFilterOption> SimulationJournalFilters { get; } = SimulationJournalFilterOption.AllOptions;
    public SimulationJournalFilterOption SelectedSimulationJournalFilter
    {
        get => _selectedSimulationJournalFilter;
        set
        {
            if (_selectedSimulationJournalFilter == value) return;
            _selectedSimulationJournalFilter = value;
            OnPropertyChanged();
            RefreshSimulationJournalFilter();
        }
    }

    public SimulationJournalEntry? SelectedSimulationJournalEntry
    {
        get => _selectedSimulationJournalEntry;
        set
        {
            if (_selectedSimulationJournalEntry == value) return;
            _selectedSimulationJournalEntry = value;
            OnPropertyChanged();
        }
    }

    public bool HasActiveEventEntries => _eventManager.ActiveEvents.Count > 0;

    public bool HasCompletedEventEntries => _eventManager.CompletedEvents.Count > 0;


    public bool IsCityPanelVisible { get; private set; }

    public int SelectedCityTabIndex { get; private set; }

    public string SelectedCityName => _city.Name;

    public string SelectedRegionName => _world.SelectedRegion.DisplayName;

    public IReadOnlyList<SettlementMapMarkerViewModel> SettlementMapMarkers => BuildSettlementMapMarkers();

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

    public string LastMapCalibrationPointDisplay => _lastMapCalibrationX.HasValue && _lastMapCalibrationY.HasValue
        ? $"Последняя точка карты: X={_lastMapCalibrationX.Value:0.0000}, Y={_lastMapCalibrationY.Value:0.0000}"
        : "Последняя точка карты: нет";

    public string SelectedCityProfile => $"{_city.Name} — профиль поселения";

    public string SelectedJournalCityId
    {
        get => _selectedJournalCityId;
        private set
        {
            if (_selectedJournalCityId == value)
            {
                return;
            }

            _selectedJournalCityId = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(CurrentJournalCityName));
            OnPropertyChanged(nameof(CityJournalTitle));
            RefreshSimulationJournalFilter();
        }
    }

    public string CurrentJournalCityName => _city.Name;

    public string CityJournalTitle => $"Летопись города: {CurrentJournalCityName}";

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

        SelectedJournalCityId = _city.Id;

        RefreshAllCityProperties();
        RefreshSelectedCityProperties();
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


    public void RegisterMapCalibrationPoint(double relativeX, double relativeY)
    {
        _lastMapCalibrationX = relativeX;
        _lastMapCalibrationY = relativeY;

        AddTechnicalLogEntry($"Калибровка карты: Гота X={relativeX:0.0000}, Y={relativeY:0.0000}");
        OnPropertyChanged(nameof(LastMapCalibrationPointDisplay));
    }

    private void ToggleMapCalibrationMode()
    {
        IsMapCalibrationModeEnabled = !IsMapCalibrationModeEnabled;
        AddTechnicalLogEntry(IsMapCalibrationModeEnabled
            ? "Режим калибровки карты включен."
            : "Режим калибровки карты выключен.");
    }

    public void RegisterTradeRouteAuthoringPoint(double relativeX, double relativeY)
    {
        if (!IsTradeRouteAuthoringModeEnabled)
        {
            return;
        }
        if (RouteAuthoringOriginSettlement is null || ActiveRouteAuthoringDestinationSettlement is null)
        {
            AddTechnicalLogEntry("Выберите пункт отправления и активный пункт назначения.");
            return;
        }

        AddTradeRoutePoint(new MapPointViewModel { X = Math.Clamp(relativeX, 0d, 1d), Y = Math.Clamp(relativeY, 0d, 1d) });
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
            AddTechnicalLogEntry("Маршрут не сохранён: выберите пункт отправления и активный пункт назначения.");
            return;
        }
        if (RouteAuthoringOriginSettlement.Id == ActiveRouteAuthoringDestinationSettlement.Id)
        {
            AddTechnicalLogEntry("Маршрут не сохранён: отправление и назначение совпадают.");
            return;
        }
        if (!decimal.TryParse(SelectedTradeRouteDistanceDaysInput, NumberStyles.Number, CultureInfo.InvariantCulture, out var parsedDistanceDays))
        {
            AddTechnicalLogEntry("Маршрут не сохранён: укажите корректное значение дней пути.");
            return;
        }
        if (parsedDistanceDays < 0.1m)
        {
            AddTechnicalLogEntry("Маршрут не сохранён: дней пути должно быть не меньше 0.1.");
            return;
        }
        SelectedTradeRouteDistanceDays = parsedDistanceDays;
        var fullPoints = BuildFullRoutePoints();
        if (fullPoints.Count < 2)
        {
            AddTechnicalLogEntry("Маршрут не сохранён: нужно минимум 2 точки.");
            return;
        }

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

        var routeIndex = existingRoute is null ? -1 : _world.TradeRoutes.FindIndex(x => x.Id == existingRoute.Id);
        if (routeIndex >= 0)
        {
            _world.TradeRoutes[routeIndex] = updatedRoute;
        }
        else
        {
            _world.TradeRoutes.Add(updatedRoute);
        }
        SelectedTradeRouteForAuthoring = updatedRoute;

        OnPropertyChanged(nameof(TradeRoutes));
        OnPropertyChanged(nameof(SelectedTradeRouteForAuthoring));
        OnPropertyChanged(nameof(EditedTradeRoutePointCount));
        OnPropertyChanged(nameof(EditedTradeRoutePolylinePoints));
        RefreshTradeRouteVisuals(null);
        AddTechnicalLogEntry($"Маршрут {updatedRoute.Id}: сохранено {fullPoints.Count} точек, дней пути {SelectedTradeRouteDistanceDays:0.###}.");
    }

    private void CopyTradeRoutePoints()
    {
        if (RouteAuthoringOriginSettlement is null || ActiveRouteAuthoringDestinationSettlement is null)
        {
            AddTechnicalLogEntry("Копирование Points недоступно: выберите пункт отправления и активный пункт назначения.");
            return;
        }
        var fullPoints = BuildFullRoutePoints();
        var routeId = SelectedTradeRouteForAuthoring?.Id ?? $"{RouteAuthoringOriginSettlement.Id}_{ActiveRouteAuthoringDestinationSettlement.Id}";
        var lines = fullPoints.Select(x =>
            $"    new RoutePoint {{ X = {Math.Clamp(x.X, 0m, 1m).ToString("0.0000", CultureInfo.InvariantCulture)}m, Y = {Math.Clamp(x.Y, 0m, 1m).ToString("0.0000", CultureInfo.InvariantCulture)}m }}");
        var text = $"RouteId: {routeId}{Environment.NewLine}From: {RouteAuthoringOriginSettlement.Id}{Environment.NewLine}To: {ActiveRouteAuthoringDestinationSettlement.Id}{Environment.NewLine}DistanceDays: {SelectedTradeRouteDistanceDays:0.0###}{Environment.NewLine}{Environment.NewLine}Points ={Environment.NewLine}[{Environment.NewLine}{string.Join($",{Environment.NewLine}", lines)}{Environment.NewLine}]";
        Clipboard.SetText(text);
        AddTechnicalLogEntry($"Маршрут {routeId}: Points скопированы.");
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

    private TradeRoute? FindRouteBetween(City from, City to) => _world.TradeRoutes.FirstOrDefault(route =>
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
        var location = _world.FindSettlementMapLocation(cityId);
        if (location is not null)
        {
            point = new RoutePoint { X = location.X, Y = location.Y };
            return true;
        }
        point = new RoutePoint { X = 0m, Y = 0m };
        return false;
    }

    private static CaravanType InferRouteType(string fromId, string toId)
        => fromId == "thokur_rus" || toId == "thokur_rus" ? CaravanType.Sea : CaravanType.Land;

    private void AddRouteAuthoringDestination()
    {
        if (RouteAuthoringOriginSettlement is null)
        {
            AddTechnicalLogEntry("Сначала выберите пункт отправления.");
            return;
        }
        var destination = RouteAuthoringDestinationCandidateSettlement;
        if (destination is null) return;
        if (destination.Id == RouteAuthoringOriginSettlement.Id)
        {
            AddTechnicalLogEntry("Пункт назначения не может совпадать с пунктом отправления.");
            return;
        }
        if (RouteAuthoringDestinationSettlements.Any(x => x.Id == destination.Id))
        {
            AddTechnicalLogEntry("Пункт назначения уже добавлен.");
            return;
        }
        if (RouteAuthoringDestinationSettlements.Count >= 8)
        {
            AddTechnicalLogEntry("Можно выбрать максимум 8 пунктов назначения.");
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
        AddTechnicalLogEntry("Источник маршрутов изменён, список назначений очищен.");
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
            RefreshTradeRouteVisuals(simulationResult.WeeklyTradeFlowResult);
        }
        RefreshAllCityProperties();
        OnPropertyChanged(nameof(Food)); OnPropertyChanged(nameof(Resources)); OnPropertyChanged(nameof(Wealth)); OnPropertyChanged(nameof(WealthTooltip)); OnPropertyChanged(nameof(SettlementMapMarkers)); OnPropertyChanged(nameof(FoodBalanceTooltip)); OnPropertyChanged(nameof(FishingProductionTooltip)); OnPropertyChanged(nameof(ResourcesTooltip)); OnPropertyChanged(nameof(GoodsTooltip)); OnPropertyChanged(nameof(CrimeFlowTooltip)); OnPropertyChanged(nameof(EconomyStocksTooltip)); OnPropertyChanged(nameof(Resources)); OnPropertyChanged(nameof(Goods));
        RefreshEventEntries(); RefreshDailyFoodFlowPreview(); RefreshSimulationSummary();
        AppendSimulationJournalEntry(day, result, eventEffects, populationStart, populationEnd, cityStateStart, cityStateEnd, simulationResult.ActiveEventNamesBeforeAdvance, journalItems);
    }

    private void RefreshTradeRouteVisuals(WorldTradeFlowResult? weeklyTradeResult)
    {
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
        foreach (var route in _world.TradeRoutes)
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
                     .Where(x => x.Points.Count >= 2 && _world.TradeRoutes.Any(r => string.Equals(r.Id, x.RouteId, StringComparison.Ordinal) && r.HasLoadedPath))
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


    private void LoadRoutePathsForWorld()
    {
        _loadedCaravanPathRouteIds.Clear();
        var path = TryFindRoutePathsJsonPath();
        if (path is null)
        {
            AddTechnicalLogEntry("route_paths.json не загружен: движение караванов по карте отключено.");
            return;
        }

        AddTechnicalLogEntry($"route_paths.json найден: {path}");
        var loader = new RoutePathLoader();
        var result = loader.TryLoadAndApply(path, _world.TradeRoutes);
        if (!result.Success)
        {
            AddTechnicalLogEntry($"route_paths.json not applied: {result.ErrorMessage ?? "unknown error"}.");
            AddTechnicalLogEntry("route_paths.json not applied: caravan movement markers disabled.");
            return;
        }

        foreach (var routeId in result.AppliedRouteIds)
        {
            _loadedCaravanPathRouteIds.Add(routeId);
        }

        var unmatched = Math.Max(0, result.ParsedPathCount - result.AppliedRouteCount);
        AddTechnicalLogEntry($"route_paths.json parsed paths count: {result.ParsedPathCount}; applied route count: {result.AppliedRouteCount}; unmatched: {unmatched}.");
        if (result.AppliedRouteCount == 0)
        {
            AddTechnicalLogEntry("route_paths.json not applied: caravan movement markers disabled.");
        }
    }

    private static string? TryFindRoutePathsJsonPath()
    {
        var relativePath = Path.Combine("data", "regions", "rivia", "routes", "v1", "route_paths.json");
        var directPath = Path.Combine(AppContext.BaseDirectory, relativePath);
        if (File.Exists(directPath))
        {
            return directPath;
        }

        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir is not null)
        {
            var candidate = Path.Combine(dir.FullName, relativePath);
            if (File.Exists(candidate))
            {
                return candidate;
            }

            dir = dir.Parent;
        }

        return null;
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
            _clock.RestoreState(
                loaded.Clock.Day,
                loaded.Clock.Hour,
                loaded.Clock.IsRunning,
                loaded.Clock.AccumulatedRealTime,
                loaded.Clock.RealTimePerGameHour);
            _lastTickUtc = DateTimeOffset.UtcNow;

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
            RefreshSelectedCityProperties();
            RefreshDailyFoodFlowPreview();
            OnPropertyChanged(nameof(CurrentSimulationSpeedDisplay));
            RefreshSimulationSummary();
        }
        catch (Exception ex)
        {
            AddTechnicalLogEntry($"Ошибка загрузки: {ex.Message}");
        }
    }

    private void RefreshWorldCollectionsAfterLoad()
    {
        OnPropertyChanged(nameof(Cities));
        OnPropertyChanged(nameof(SettlementCountText));
        OnPropertyChanged(nameof(TradeRoutes));
        OnPropertyChanged(nameof(RouteAuthoringSettlements));
        OnPropertyChanged(nameof(AvailableRouteAuthoringDestinations));
        SelectedTradeRouteForAuthoring = _world.TradeRoutes.FirstOrDefault();
        RefreshTradeRouteVisuals(null);
        RefreshSimulationJournalFilter();
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

    private void RefreshSelectedCityProperties()
    {
        OnPropertyChanged(nameof(SelectedCityName));
        OnPropertyChanged(nameof(SelectedRegionName));
        OnPropertyChanged(nameof(SelectedCityProfile));
        OnPropertyChanged(nameof(CityStateDisplay));
        OnPropertyChanged(nameof(SettlementMapMarkers));

        if (OpenSelectedCityCommand is RelayCommand openSelectedCityCommand)
        {
            openSelectedCityCommand.RaiseCanExecuteChanged();
        }
    }

    private void RefreshAllCityProperties()
    {
        OnPropertyChanged(nameof(CityName));
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
        OnPropertyChanged(nameof(SettlementMapMarkers));
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


    private IReadOnlyList<SettlementMapMarkerViewModel> BuildSettlementMapMarkers()
    {
        var citiesById = _world.Cities.ToDictionary(c => c.Id, StringComparer.Ordinal);

        return _world.SettlementMapLocations
            .Where(location =>
                location.RegionId == _world.SelectedRegionId &&
                citiesById.ContainsKey(location.SettlementId))
            .Select(location =>
            {
                var city = citiesById[location.SettlementId];
                return new SettlementMapMarkerViewModel
                {
                    SettlementId = city.Id,
                    DisplayName = city.Name,
                    X = location.X,
                    Y = location.Y,
                    IsSelected = city.Id == _world.SelectedCityId
                };
            })
            .ToList();
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
        OnPropertyChanged(nameof(SettlementMapMarkers));
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
        if (eventEffects.HasAnyEffect)
        {
            items.Add(new SimulationJournalItem
            {
                Category = SimulationJournalCategory.Effects,
                Title = "Применены эффекты событий",
                Details = $"Настроение {eventEffects.MoodDelta:+0;-0;0}, безопасность {eventEffects.SecurityDelta:+0;-0;0}, преступность {eventEffects.CrimeDelta:+0;-0;0}."
            });
        }

        if (populationStart != populationEnd)
        {
            items.Add(new SimulationJournalItem
            {
                Category = SimulationJournalCategory.Population,
                Title = "Изменение населения",
                Details = $"Население {populationStart} → {populationEnd} ({populationEnd - populationStart:+0;-0;0}), причина: {_city.CityState switch { WorldSimulator.Core.Cities.CityState.Famine => "голод", _ => "состояние города" }}."
            });
        }

        if (cityStateStart != cityStateEnd)
        {
            items.Add(new SimulationJournalItem
            {
                Category = SimulationJournalCategory.CityState,
                Title = "Состояние города изменилось",
                Details = $"{ToRussianCityState(cityStateStart)} → {ToRussianCityState(cityStateEnd)}."
            });
        }

        items.Add(new SimulationJournalItem
        {
            Category = SimulationJournalCategory.Food,
            Title = "Пищевой баланс дня",
            Details = BuildFoodCalculationText(foodResult)
        });

        var summary = cityStateEnd == WorldSimulator.Core.Cities.CityState.Abandoned
            ? "Город опустел."
            : BuildJournalSummary(foodResult, items, populationStart, populationEnd);

        var entry = new SimulationJournalEntry
        {
            Day = day,
            CityId = _city.Id,
            CityName = _city.Name,
            CityState = ToRussianCityState(cityStateEnd),
            PopulationStart = populationStart,
            PopulationEnd = populationEnd,
            PopulationDelta = populationEnd - populationStart,
            FoodStart = foodResult.StartingFood,
            FoodEnd = foodResult.EndingFood,
            FoodDelta = foodResult.TotalDelta,
            ActiveEventsCount = _eventManager.ActiveEvents.Count,
            Summary = summary,
            FoodCalculation = BuildFoodCalculationText(foodResult),
            EffectsTooltip = activeEventNamesBeforeAdvance.Count == 0 ? "Событий нет." : $"События дня: {string.Join(", ", activeEventNamesBeforeAdvance)}.",
            Items = items
        };

        SimulationJournalEntries.Add(entry);
        if (SimulationJournalEntries.Count > MaxSimulationJournalDays)
        {
            SimulationJournalEntries.RemoveAt(0);
        }

        RefreshSimulationJournalFilter();
    }

    private void RefreshSimulationJournalFilter()
    {
        FilteredSimulationJournalEntries.Clear();
        foreach (var entry in SimulationJournalEntries.Where(entry => IsEntryInSelectedJournalCity(entry) && MatchesCurrentFilter(entry)))
        {
            FilteredSimulationJournalEntries.Add(entry);
        }
    }

    private bool IsEntryInSelectedJournalCity(SimulationJournalEntry entry)
    {
        return string.Equals(entry.CityId, SelectedJournalCityId, StringComparison.OrdinalIgnoreCase);
    }

    private bool MatchesCurrentFilter(SimulationJournalEntry entry)
    {
        return SelectedSimulationJournalFilter.Value switch
        {
            SimulationJournalFilter.All => true,
            SimulationJournalFilter.Events => entry.Items.Any(i => i.Category is SimulationJournalCategory.Event or SimulationJournalCategory.Effects),
            SimulationJournalFilter.Population => entry.Items.Any(i => i.Category == SimulationJournalCategory.Population),
            SimulationJournalFilter.Food => entry.Items.Any(i => i.Category == SimulationJournalCategory.Food),
            SimulationJournalFilter.CityState => entry.Items.Any(i => i.Category == SimulationJournalCategory.CityState),
            SimulationJournalFilter.System => entry.Items.Any(i => i.Category == SimulationJournalCategory.System),
            SimulationJournalFilter.Errors => entry.Items.Any(i => i.Category == SimulationJournalCategory.Error),
            SimulationJournalFilter.MapAndDebug => entry.Items.Any(i => i.Category is SimulationJournalCategory.Map or SimulationJournalCategory.Debug),
            _ => true
        };
    }

    private static string BuildFoodCalculationText(DailyFoodFlowResult result) =>
        $"Потребление: -{result.PopulationConsumption:0.##}.{Environment.NewLine}Земледелие: {result.AgricultureIncome:+0.##;-0.##;0}.{Environment.NewLine}Рыбалка: {result.FishingIncome:+0.##;-0.##;0}.{Environment.NewLine}Охота: {result.HuntingIncome:+0.##;-0.##;0}.{Environment.NewLine}Поставки: {result.MainlandSupplyIncome:+0.##;-0.##;0}.{Environment.NewLine}События: {result.EventDelta:+0.##;-0.##;0}.";

    private static string BuildJournalSummary(DailyFoodFlowResult foodResult, IReadOnlyList<SimulationJournalItem> items, int populationStart, int populationEnd)
    {
        var eventItem = items.FirstOrDefault(i => i.Category == SimulationJournalCategory.Event);
        if (populationStart != populationEnd)
        {
            return $"Население {populationStart} → {populationEnd}. Пища {foodResult.TotalDelta:+0.##;-0.##;0}.";
        }

        if (eventItem is not null)
        {
            return $"{eventItem.Title}. Пища {foodResult.TotalDelta:+0.##;-0.##;0}.";
        }

        return $"Пища {foodResult.TotalDelta:+0.##;-0.##;0}, событий нет.";
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

    private void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}

public sealed class SimulationJournalFilterOption
{
    public static readonly SimulationJournalFilterOption All = new(SimulationJournalFilter.All, "Все");
    public static readonly SimulationJournalFilterOption Events = new(SimulationJournalFilter.Events, "События");
    public static readonly SimulationJournalFilterOption Population = new(SimulationJournalFilter.Population, "Население");
    public static readonly SimulationJournalFilterOption Food = new(SimulationJournalFilter.Food, "Пища");
    public static readonly SimulationJournalFilterOption CityState = new(SimulationJournalFilter.CityState, "Состояние");
    public static readonly SimulationJournalFilterOption System = new(SimulationJournalFilter.System, "Система");
    public static readonly SimulationJournalFilterOption Errors = new(SimulationJournalFilter.Errors, "Ошибки");
    public static readonly SimulationJournalFilterOption MapAndDebug = new(SimulationJournalFilter.MapAndDebug, "Карта/отладка");
    public static readonly IReadOnlyList<SimulationJournalFilterOption> AllOptions = [All, Events, Population, Food, CityState, System, Errors, MapAndDebug];

    public SimulationJournalFilterOption(SimulationJournalFilter value, string title)
    {
        Value = value;
        Title = title;
    }

    public SimulationJournalFilter Value { get; }
    public string Title { get; }
}
