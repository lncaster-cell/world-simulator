using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using System.Windows.Threading;
using WorldSimulator.App.Infrastructure;
using WorldSimulator.Core.Cities;
using WorldSimulator.Core.Events;
using WorldSimulator.Core.Resources;
using WorldSimulator.Core.Time;
using WorldSimulator.Persistence.Saves;

namespace WorldSimulator.App.ViewModels;

public sealed class MainWindowViewModel : INotifyPropertyChanged
{
    private static readonly TimeSpan NormalSimulationSpeed = TimeSpan.FromMinutes(5);
    private static readonly TimeSpan FastSimulationSpeed = TimeSpan.FromSeconds(10);
    private static readonly TimeSpan VeryFastSimulationSpeed = TimeSpan.FromSeconds(1);

    private const string SaveFilePath = "data/save/world_save.json";
    private City _city;
    private readonly SimulationClock _clock;
    private readonly DailyFoodFlowCalculator _dailyFoodFlowCalculator;
    private readonly CityStateEvaluator _cityStateEvaluator;
    private readonly CityEventManager _eventManager;
    private readonly CityEventEffectCalculator _eventEffectCalculator;
    private readonly JsonWorldSaveService _saveService;
    private readonly DispatcherTimer _timer;
    private DateTimeOffset _lastTickUtc;
    private DailyFoodFlowResult _dailyFoodFlowResult;

    public MainWindowViewModel()
    {
        _city = CityPresets.CreateGotha();
        _clock = new SimulationClock();
        _dailyFoodFlowCalculator = new DailyFoodFlowCalculator();
        _cityStateEvaluator = new CityStateEvaluator();
        _eventManager = new CityEventManager();
        _eventEffectCalculator = new CityEventEffectCalculator();
        _saveService = new JsonWorldSaveService();
        _dailyFoodFlowResult = _dailyFoodFlowCalculator.Calculate(_city, BuildDailyFoodFlowInputs());

        StartCommand = new RelayCommand(Start, () => !_clock.IsRunning);
        PauseCommand = new RelayCommand(Pause, () => _clock.IsRunning);
        SetNormalSpeedCommand = new RelayCommand(SetNormalSpeed);
        SetFastSpeedCommand = new RelayCommand(SetFastSpeed);
        SetVeryFastSpeedCommand = new RelayCommand(SetVeryFastSpeed);
        SelectGothaCommand = new RelayCommand(SelectGotha);
        OpenSelectedCityCommand = new RelayCommand(OpenSelectedCity, () => IsGothaSelected);
        TriggerFireEventCommand = new RelayCommand(() => TryStartEvent(CityEventPresets.CreateFire));
        TriggerDiseaseEventCommand = new RelayCommand(() => TryStartEvent(CityEventPresets.CreateDisease));
        TriggerRatInfestationEventCommand = new RelayCommand(() => TryStartEvent(CityEventPresets.CreateRatInfestation));
        TriggerArtistsPerformanceEventCommand = new RelayCommand(() => TryStartEvent(CityEventPresets.CreateArtistsPerformance));
        TriggerPortStormEventCommand = new RelayCommand(() => TryStartEvent(CityEventPresets.CreatePortStorm));
        SaveCommand = new RelayCommand(SaveState);
        LoadCommand = new RelayCommand(LoadState);

        _lastTickUtc = DateTimeOffset.UtcNow;
        _timer = new DispatcherTimer
        {
            Interval = TimeSpan.FromMilliseconds(250)
        };

        _clock.DayAdvanced += OnDayAdvanced;
        _timer.Tick += OnTick;
        _timer.Start();

        RefreshCityState();
        RefreshDailyFoodFlowPreview();
        RefreshEventEntries();
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    public ICommand StartCommand { get; }

    public ICommand PauseCommand { get; }
    public ICommand SetNormalSpeedCommand { get; }
    public ICommand SetFastSpeedCommand { get; }
    public ICommand SetVeryFastSpeedCommand { get; }

    public ICommand SelectGothaCommand { get; }

    public ICommand OpenSelectedCityCommand { get; }

    public ICommand TriggerFireEventCommand { get; }
    public ICommand TriggerDiseaseEventCommand { get; }
    public ICommand TriggerRatInfestationEventCommand { get; }
    public ICommand TriggerArtistsPerformanceEventCommand { get; }
    public ICommand TriggerPortStormEventCommand { get; }
    public ICommand SaveCommand { get; }
    public ICommand LoadCommand { get; }

    public int Day => _clock.Day;

    public int Hour => _clock.Hour;

    public bool IsRunning => _clock.IsRunning;

    public string SimulationState => IsRunning ? "Запущено" : "Пауза";
    public string CurrentSimulationSpeedDisplay => $"Текущая скорость: {GetSpeedDisplay(_clock.RealTimePerGameHour)}";

    public string CityName => _city.Name;

    public string CityState => _city.CityState.ToString();

    public string CityStateDisplay => ToRussianCityState(_city.CityState);

    public int Population => _city.Population;

    public decimal Food => _city.Food;

    public decimal Wealth => _city.Wealth;

    public int Mood => _city.Mood;

    public int Security => _city.Security;

    public int Crime => _city.Crime;

    public decimal Resources => _city.Resources;

    public decimal Goods => _city.Goods;

    public decimal DailyFoodConsumption => _city.CalculateDailyFoodConsumption();

    public decimal DailyFoodStartingFood => _dailyFoodFlowResult.StartingFood;

    public decimal DailyFoodPopulationConsumption => _dailyFoodFlowResult.PopulationConsumption;

    public decimal DailyFoodFishingIncome => _dailyFoodFlowResult.FishingIncome;

    public decimal DailyFoodHuntingIncome => _dailyFoodFlowResult.HuntingIncome;

    public decimal DailyFoodMainlandSupplyIncome => _dailyFoodFlowResult.MainlandSupplyIncome;

    public decimal DailyFoodEventDelta => _dailyFoodFlowResult.EventDelta;

    public decimal DailyFoodTotalDelta => _dailyFoodFlowResult.TotalDelta;

    public decimal DailyFoodEndingFood => _dailyFoodFlowResult.EndingFood;

    public string DailyFoodPopulationConsumptionDisplay => $"-{DailyFoodPopulationConsumption:0.##}";

    public string DailyFoodFishingIncomeDisplay => FormatSigned(DailyFoodFishingIncome);

    public string DailyFoodHuntingIncomeDisplay => FormatSigned(DailyFoodHuntingIncome);

    public string DailyFoodMainlandSupplyIncomeDisplay => FormatSigned(DailyFoodMainlandSupplyIncome);

    public string DailyFoodEventDeltaDisplay => FormatSigned(DailyFoodEventDelta);

    public string DailyFoodTotalDeltaDisplay => FormatSigned(DailyFoodTotalDelta);

    public ObservableCollection<string> TechnicalLogEntries { get; } = new();

    public bool HasTechnicalLogEntries => TechnicalLogEntries.Count > 0;

    public ObservableCollection<string> ActiveEventEntries { get; } = new();

    public ObservableCollection<string> CompletedEventEntries { get; } = new();

    public bool HasActiveEventEntries => _eventManager.ActiveEvents.Count > 0;

    public bool HasCompletedEventEntries => _eventManager.CompletedEvents.Count > 0;

    public bool IsGothaSelected { get; private set; }

    public bool IsCityPanelVisible { get; private set; }

    public int SelectedCityTabIndex { get; private set; }

    public string SelectedCityName => IsGothaSelected ? _city.Name : string.Empty;

    public string SelectedCityProfile => IsGothaSelected
        ? "Гота — малый пограничный прибрежный портовый город"
        : string.Empty;

    private static string FormatSigned(decimal value)
    {
        return value.ToString("+0.##;-0.##;0");
    }

    private void SelectGotha()
    {
        IsGothaSelected = true;
        RefreshSelectedCityProperties();
    }

    private void OpenSelectedCity()
    {
        if (!IsGothaSelected)
        {
            return;
        }

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

    private void SetNormalSpeed() => SetSimulationSpeed(NormalSimulationSpeed);

    private void SetFastSpeed() => SetSimulationSpeed(FastSimulationSpeed);

    private void SetVeryFastSpeed() => SetSimulationSpeed(VeryFastSimulationSpeed);

    private void SetSimulationSpeed(TimeSpan realTimePerGameHour)
    {
        if (_clock.RealTimePerGameHour == realTimePerGameHour)
        {
            return;
        }

        _clock.SetSimulationSpeed(realTimePerGameHour);
        TechnicalLogEntries.Add($"Скорость симуляции изменена: {GetSpeedDisplay(realTimePerGameHour)}.");
        OnPropertyChanged(nameof(CurrentSimulationSpeedDisplay));
        OnPropertyChanged(nameof(HasTechnicalLogEntries));
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

        return $"{realTimePerGameHour.TotalSeconds:0.##} секунд = 1 игровой час";
    }

    private void OnDayAdvanced(int day)
    {
        var completedEvents = _eventManager.AdvanceDay();
        foreach (var completedEvent in completedEvents)
        {
            TechnicalLogEntries.Add($"День {day}: завершено событие “{completedEvent.Name}”.");
        }

        var eventEffects = _eventEffectCalculator.Calculate(_city, _eventManager.ActiveEvents);

        var result = _dailyFoodFlowCalculator.Calculate(_city, BuildDailyFoodFlowInputs(eventEffects));
        _dailyFoodFlowCalculator.Apply(_city, result);

        ApplyDailyEventEffects(eventEffects, day);
        RefreshCityState(day);

        TechnicalLogEntries.Add(
            $"День {day}: пища {result.StartingFood:0.##} → {result.EndingFood:0.##}; баланс {result.TotalDelta:+0.##;-0.##;0} (потребление -{result.PopulationConsumption:0.##}, рыбалка {result.FishingIncome:+0.##;-0.##;0}, охота {result.HuntingIncome:+0.##;-0.##;0}, поставки {result.MainlandSupplyIncome:+0.##;-0.##;0}, события {result.EventDelta:+0.##;-0.##;0}).");

        OnPropertyChanged(nameof(Food));
        RefreshEventEntries();
        OnPropertyChanged(nameof(HasTechnicalLogEntries));

        RefreshDailyFoodFlowPreview();
        RefreshEventEntries();
    }

    private void TryStartEvent(Func<int, CityEvent> factory)
    {
        var cityEvent = factory(Day);
        var added = _eventManager.AddEvent(cityEvent);

        if (added)
        {
            TechnicalLogEntries.Add($"День {Day}: запущено событие “{cityEvent.Name}”.");
        }
        else
        {
            TechnicalLogEntries.Add($"Событие “{cityEvent.Name}” уже активно.");
        }

        RefreshEventEntries();
        OnPropertyChanged(nameof(HasTechnicalLogEntries));
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

    private void SaveState()
    {
        try
        {
            _saveService.SaveAsync(SaveFilePath, _city, _clock).GetAwaiter().GetResult();
            TechnicalLogEntries.Add($"Состояние сохранено: {SaveFilePath}");
        }
        catch (Exception ex)
        {
            TechnicalLogEntries.Add($"Ошибка сохранения: {ex.Message}");
        }

        OnPropertyChanged(nameof(HasTechnicalLogEntries));
    }

    private void LoadState()
    {
        try
        {
            if (!File.Exists(SaveFilePath))
            {
                TechnicalLogEntries.Add($"Файл сохранения не найден: {SaveFilePath}");
                OnPropertyChanged(nameof(HasTechnicalLogEntries));
                return;
            }

            var loaded = _saveService.LoadAsync(SaveFilePath).GetAwaiter().GetResult();
            _city = loaded.City;
            _clock.RestoreState(
                loaded.Clock.Day,
                loaded.Clock.Hour,
                loaded.Clock.IsRunning,
                loaded.Clock.AccumulatedRealTime,
                loaded.Clock.RealTimePerGameHour);
            _lastTickUtc = DateTimeOffset.UtcNow;

            _eventManager.Clear();
            RefreshEventEntries();
            TechnicalLogEntries.Add("События пока не сохраняются и были очищены после загрузки.");
            TechnicalLogEntries.Add($"Состояние загружено: {SaveFilePath}");

            RefreshAllCityProperties();
            RefreshClockProperties();
            RefreshSelectedCityProperties();
            RefreshDailyFoodFlowPreview();
            OnPropertyChanged(nameof(CurrentSimulationSpeedDisplay));
            OnPropertyChanged(nameof(HasTechnicalLogEntries));
        }
        catch (Exception ex)
        {
            TechnicalLogEntries.Add($"Ошибка загрузки: {ex.Message}");
            OnPropertyChanged(nameof(HasTechnicalLogEntries));
        }
    }

    private void RefreshClockProperties()
    {
        OnPropertyChanged(nameof(Day));
        OnPropertyChanged(nameof(Hour));
        OnPropertyChanged(nameof(IsRunning));
        OnPropertyChanged(nameof(SimulationState));

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
        OnPropertyChanged(nameof(IsGothaSelected));
        OnPropertyChanged(nameof(SelectedCityName));
        OnPropertyChanged(nameof(SelectedCityProfile));
        OnPropertyChanged(nameof(CityStateDisplay));

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
        OnPropertyChanged(nameof(Wealth));
        OnPropertyChanged(nameof(Mood));
        OnPropertyChanged(nameof(Security));
        OnPropertyChanged(nameof(Crime));
        OnPropertyChanged(nameof(Resources));
        OnPropertyChanged(nameof(Goods));
        OnPropertyChanged(nameof(DailyFoodConsumption));
    }

    private void RefreshDailyFoodFlowPreview()
    {
        _dailyFoodFlowResult = _dailyFoodFlowCalculator.Calculate(_city, BuildDailyFoodFlowInputs());

        OnPropertyChanged(nameof(DailyFoodStartingFood));
        OnPropertyChanged(nameof(DailyFoodPopulationConsumption));
        OnPropertyChanged(nameof(DailyFoodFishingIncome));
        OnPropertyChanged(nameof(DailyFoodHuntingIncome));
        OnPropertyChanged(nameof(DailyFoodMainlandSupplyIncome));
        OnPropertyChanged(nameof(DailyFoodEventDelta));
        OnPropertyChanged(nameof(DailyFoodTotalDelta));
        OnPropertyChanged(nameof(DailyFoodEndingFood));
        OnPropertyChanged(nameof(DailyFoodPopulationConsumptionDisplay));
        OnPropertyChanged(nameof(DailyFoodFishingIncomeDisplay));
        OnPropertyChanged(nameof(DailyFoodHuntingIncomeDisplay));
        OnPropertyChanged(nameof(DailyFoodMainlandSupplyIncomeDisplay));
        OnPropertyChanged(nameof(DailyFoodEventDeltaDisplay));
        OnPropertyChanged(nameof(DailyFoodTotalDeltaDisplay));
    }

    private DailyFoodFlowInputs BuildDailyFoodFlowInputs(CityEventEffectsResult? eventEffects = null)
    {
        var effects = eventEffects ?? _eventEffectCalculator.Calculate(_city, _eventManager.ActiveEvents);
        var baseInputs = DailyFoodFlowInputs.GothaPlaceholder;

        return new DailyFoodFlowInputs
        {
            FishingIncome = baseInputs.FishingIncome,
            HuntingIncome = baseInputs.HuntingIncome,
            MainlandSupplyIncome = baseInputs.MainlandSupplyIncome + effects.MainlandSupplyDelta,
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

        TechnicalLogEntries.Add($"День {day}: применены эффекты событий: {string.Join(", ", segments)}.");

        OnPropertyChanged(nameof(Mood));
        OnPropertyChanged(nameof(Security));
        OnPropertyChanged(nameof(Crime));
        OnPropertyChanged(nameof(Wealth));
        OnPropertyChanged(nameof(Resources));
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

        if (day.HasValue)
        {
            TechnicalLogEntries.Add($"День {day.Value}: состояние города изменилось: {ToRussianCityState(previousState)} → {ToRussianCityState(newState)}.");
            OnPropertyChanged(nameof(HasTechnicalLogEntries));
        }

        OnPropertyChanged(nameof(CityState));
        OnPropertyChanged(nameof(CityStateDisplay));
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

    private void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}
