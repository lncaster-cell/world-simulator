using System;
namespace WorldSimulator.App.ViewModels;

public sealed class SimulationSummaryViewModel : ViewModelBase
{
    private readonly SimulationControlViewModel _control;
    private readonly SelectedCityViewModel _selectedCity;
    private readonly SimulationLogViewModel _log;
    private readonly Func<bool> _isRandomEventGenerationEnabledProvider;
    private readonly Func<string> _lastImportantChangeProvider;

    public SimulationSummaryViewModel(
        SimulationControlViewModel control,
        SelectedCityViewModel selectedCity,
        SimulationLogViewModel log,
        Func<bool> isRandomEventGenerationEnabledProvider,
        Func<string> lastImportantChangeProvider)
    {
        _control = control;
        _selectedCity = selectedCity;
        _log = log;
        _isRandomEventGenerationEnabledProvider = isRandomEventGenerationEnabledProvider;
        _lastImportantChangeProvider = lastImportantChangeProvider;
    }

    public string SimulationSummaryTitle => "Сводка симуляции";
    public string SimulationSummaryDayAndHour => $"День {_control.Day}, час {_control.Hour}";
    public string SimulationSummaryCityState => $"Состояние: {_selectedCity.CityStateDisplay}";
    public string SimulationSummaryFoodBalance => $"Пища: {_selectedCity.FoodDisplay} ({FormatSigned(_selectedCity.DailyFoodTotalDelta)}/день)";
    public string SimulationSummaryActiveEvents => $"Активных событий: {_log.ActiveEventEntries.Count}";
    public string SimulationSummaryRandomEventsStatus => _isRandomEventGenerationEnabledProvider()
        ? "Случайные события: включены"
        : "Случайные события: выключены";
    public string SimulationSummaryLastImportantChange => $"Последнее важное изменение: {_lastImportantChangeProvider()}";
    public string FoodBalanceTooltip => _selectedCity.FoodBalanceTooltip;
    public string SelectedRegionName => _selectedCity.SelectedRegionName;

    public void RefreshAll()
    {
        OnPropertyChanged(nameof(SimulationSummaryTitle));
        OnPropertyChanged(nameof(SimulationSummaryDayAndHour));
        OnPropertyChanged(nameof(SimulationSummaryCityState));
        OnPropertyChanged(nameof(SimulationSummaryFoodBalance));
        OnPropertyChanged(nameof(SimulationSummaryActiveEvents));
        OnPropertyChanged(nameof(SimulationSummaryRandomEventsStatus));
        OnPropertyChanged(nameof(SimulationSummaryLastImportantChange));
        OnPropertyChanged(nameof(FoodBalanceTooltip));
        OnPropertyChanged(nameof(SelectedRegionName));
    }

    public void RefreshClock()
    {
        OnPropertyChanged(nameof(SimulationSummaryDayAndHour));
    }

    public void RefreshFoodBalance()
    {
        OnPropertyChanged(nameof(SimulationSummaryFoodBalance));
        OnPropertyChanged(nameof(FoodBalanceTooltip));
    }

    public void RefreshCityState()
    {
        OnPropertyChanged(nameof(SimulationSummaryCityState));
    }

    public void RefreshActiveEvents()
    {
        OnPropertyChanged(nameof(SimulationSummaryActiveEvents));
    }

    public void RefreshRandomEventsStatus()
    {
        OnPropertyChanged(nameof(SimulationSummaryRandomEventsStatus));
    }

    public void RefreshLastImportantChange()
    {
        OnPropertyChanged(nameof(SimulationSummaryLastImportantChange));
    }

    private static string FormatSigned(decimal value)
    {
        var rounded = Math.Round(value, 1, MidpointRounding.AwayFromZero);
        return rounded.ToString("+0.#;-0.#;0");
    }
}
