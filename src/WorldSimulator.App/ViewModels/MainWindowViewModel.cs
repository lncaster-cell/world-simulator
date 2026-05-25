using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using System.Windows.Threading;
using WorldSimulator.App.Infrastructure;
using WorldSimulator.Core.Cities;
using WorldSimulator.Core.Resources;
using WorldSimulator.Core.Time;

namespace WorldSimulator.App.ViewModels;

public sealed class MainWindowViewModel : INotifyPropertyChanged
{
    private readonly City _city;
    private readonly SimulationClock _clock;
    private readonly DailyFoodFlowCalculator _dailyFoodFlowCalculator;
    private readonly DispatcherTimer _timer;
    private DateTimeOffset _lastTickUtc;
    private DailyFoodFlowResult _dailyFoodFlowPreview;

    public MainWindowViewModel()
    {
        _city = CityPresets.CreateGotha();
        _clock = new SimulationClock();
        _dailyFoodFlowCalculator = new DailyFoodFlowCalculator();
        _dailyFoodFlowPreview = _dailyFoodFlowCalculator.Calculate(_city, DailyFoodFlowInputs.GothaPlaceholder);

        StartCommand = new RelayCommand(Start, () => !_clock.IsRunning);
        PauseCommand = new RelayCommand(Pause, () => _clock.IsRunning);
        SelectGothaCommand = new RelayCommand(SelectGotha);
        OpenSelectedCityCommand = new RelayCommand(OpenSelectedCity, () => IsGothaSelected);

        _lastTickUtc = DateTimeOffset.UtcNow;
        _timer = new DispatcherTimer
        {
            Interval = TimeSpan.FromMilliseconds(250)
        };

        _timer.Tick += OnTick;
        _timer.Start();

        RefreshDailyFoodFlowPreview();
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    public ICommand StartCommand { get; }

    public ICommand PauseCommand { get; }

    public ICommand SelectGothaCommand { get; }

    public ICommand OpenSelectedCityCommand { get; }

    public int Day => _clock.Day;

    public int Hour => _clock.Hour;

    public bool IsRunning => _clock.IsRunning;

    public string SimulationState => IsRunning ? "Запущено" : "Пауза";

    public string CityName => _city.Name;

    public string CityState => _city.CityState.ToString();

    public string CityStateDisplay => _city.CityState switch
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
        _ => _city.CityState.ToString()
    };

    public int Population => _city.Population;

    public decimal Food => _city.Food;

    public decimal Wealth => _city.Wealth;

    public int Mood => _city.Mood;

    public int Security => _city.Security;

    public int Crime => _city.Crime;

    public decimal Resources => _city.Resources;

    public decimal Goods => _city.Goods;

    public decimal DailyFoodConsumption => _city.CalculateDailyFoodConsumption();
    public decimal DailyFoodStartingFood => _dailyFoodFlowPreview.StartingFood;
    public decimal DailyFoodPopulationConsumption => _dailyFoodFlowPreview.PopulationConsumption;
    public decimal DailyFoodFishingIncome => _dailyFoodFlowPreview.FishingIncome;
    public decimal DailyFoodHuntingIncome => _dailyFoodFlowPreview.HuntingIncome;
    public decimal DailyFoodMainlandSupplyIncome => _dailyFoodFlowPreview.MainlandSupplyIncome;
    public decimal DailyFoodEventDelta => _dailyFoodFlowPreview.EventDelta;
    public decimal DailyFoodTotalDelta => _dailyFoodFlowPreview.TotalDelta;
    public decimal DailyFoodEndingFood => _dailyFoodFlowPreview.EndingFood;

    public string DailyFoodPopulationConsumptionDisplay => $"-{DailyFoodPopulationConsumption:0.##}";
    public string DailyFoodFishingIncomeDisplay => $"{DailyFoodFishingIncome:+0.##;-0.##;0}";
    public string DailyFoodHuntingIncomeDisplay => $"{DailyFoodHuntingIncome:+0.##;-0.##;0}";
    public string DailyFoodMainlandSupplyIncomeDisplay => $"{DailyFoodMainlandSupplyIncome:+0.##;-0.##;0}";
    public string DailyFoodEventDeltaDisplay => $"{DailyFoodEventDelta:+0.##;-0.##;0}";
    public string DailyFoodTotalDeltaDisplay => $"{DailyFoodTotalDelta:+0.##;-0.##;0}";

    public bool IsGothaSelected { get; private set; }

    public bool IsCityPanelVisible { get; private set; }

    public int SelectedCityTabIndex { get; private set; }

    public string SelectedCityName => IsGothaSelected ? _city.Name : string.Empty;

    public string SelectedCityProfile => IsGothaSelected
        ? "Гота — малый пограничный прибрежный портовый город"
        : string.Empty;


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

    private void OnTick(object? sender, EventArgs e)
    {
        var now = DateTimeOffset.UtcNow;
        var elapsed = now - _lastTickUtc;
        _lastTickUtc = now;

        _clock.Advance(elapsed);

        RefreshClockProperties();
    }

    private void RefreshClockProperties()
    {
        OnPropertyChanged(nameof(Day));
        OnPropertyChanged(nameof(Hour));
        OnPropertyChanged(nameof(IsRunning));
        OnPropertyChanged(nameof(SimulationState));
        OnPropertyChanged(nameof(CityStateDisplay));

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

    private void RefreshDailyFoodFlowPreview()
    {
        _dailyFoodFlowPreview = _dailyFoodFlowCalculator.Calculate(_city, DailyFoodFlowInputs.GothaPlaceholder);

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

    private void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}
