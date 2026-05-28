using System.Windows.Input;
using System.Windows.Threading;
using WorldSimulator.App.Infrastructure;
using WorldSimulator.Core.Time;

namespace WorldSimulator.App.ViewModels;

public sealed class SimulationControlViewModel : ViewModelBase
{
    public static readonly TimeSpan NormalSimulationSpeed = TimeSpan.FromMinutes(5);
    public static readonly TimeSpan FastSimulationSpeed = TimeSpan.FromSeconds(10);
    public static readonly TimeSpan VeryFastSimulationSpeed = TimeSpan.FromSeconds(1);
    public static readonly TimeSpan TurboSimulationSpeed = TimeSpan.FromMilliseconds(1000d / 24d);

    private readonly SimulationClock _clock;
    private readonly DispatcherTimer _timer;
    private readonly Action<string> _log;
    private DateTimeOffset _lastTickUtc;

    public SimulationControlViewModel(SimulationClock clock, Action<string> log)
    {
        _clock = clock;
        _log = log;

        StartCommand = new RelayCommand(Start, () => !IsRunning);
        PauseCommand = new RelayCommand(Pause, () => IsRunning);
        ResetCommand = new RelayCommand(() => ResetRequested?.Invoke());
        SetNormalSpeedCommand = new RelayCommand(() => SetSimulationSpeed(NormalSimulationSpeed));
        SetFastSpeedCommand = new RelayCommand(() => SetSimulationSpeed(FastSimulationSpeed));
        SetVeryFastSpeedCommand = new RelayCommand(() => SetSimulationSpeed(VeryFastSimulationSpeed));
        SetTurboSpeedCommand = new RelayCommand(() => SetSimulationSpeed(TurboSimulationSpeed));

        _lastTickUtc = DateTimeOffset.UtcNow;
        _timer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(250) };
        _timer.Tick += OnTick;
        _timer.Start();
    }

    public event Action? ResetRequested;

    public ICommand StartCommand { get; }
    public ICommand PauseCommand { get; }
    public ICommand ResetCommand { get; }
    public ICommand SetNormalSpeedCommand { get; }
    public ICommand SetFastSpeedCommand { get; }
    public ICommand SetVeryFastSpeedCommand { get; }
    public ICommand SetTurboSpeedCommand { get; }

    public int Day => _clock.Day;
    public int Hour => _clock.Hour;
    public bool IsRunning => _clock.IsRunning;
    public string SimulationState => IsRunning ? "Запущено" : "Пауза";
    public string CurrentSimulationSpeedDisplay => $"Текущая скорость: {GetSpeedDisplay(_clock.RealTimePerGameHour)}";

    public void ResetClock()
    {
        _clock.RestoreState(1, 0, false, TimeSpan.Zero, NormalSimulationSpeed);
        _lastTickUtc = DateTimeOffset.UtcNow;
        RefreshClockProperties();
        OnPropertyChanged(nameof(CurrentSimulationSpeedDisplay));
    }

    public void RestoreTickBaseline()
    {
        _lastTickUtc = DateTimeOffset.UtcNow;
        RefreshClockProperties();
        OnPropertyChanged(nameof(CurrentSimulationSpeedDisplay));
    }

    public void RefreshClockProperties()
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

    private void SetSimulationSpeed(TimeSpan realTimePerGameHour)
    {
        if (_clock.RealTimePerGameHour == realTimePerGameHour)
        {
            return;
        }

        _clock.SetSimulationSpeed(realTimePerGameHour);
        _log($"Скорость симуляции изменена: {GetSpeedDisplay(realTimePerGameHour)}.");
        OnPropertyChanged(nameof(CurrentSimulationSpeedDisplay));
    }

    private void OnTick(object? sender, EventArgs e)
    {
        var now = DateTimeOffset.UtcNow;
        var elapsed = now - _lastTickUtc;
        _lastTickUtc = now;

        _clock.Advance(elapsed);
        RefreshClockProperties();
    }

    private static string GetSpeedDisplay(TimeSpan realTimePerGameHour)
    {
        if (realTimePerGameHour == NormalSimulationSpeed) return "5 минут = 1 игровой час";
        if (realTimePerGameHour == FastSimulationSpeed) return "10 секунд = 1 игровой час";
        if (realTimePerGameHour == VeryFastSimulationSpeed) return "1 секунда = 1 игровой час";
        if (realTimePerGameHour == TurboSimulationSpeed) return "1 секунда = 1 игровой день";
        return $"{realTimePerGameHour.TotalSeconds:0.##} секунд = 1 игровой час";
    }
}
