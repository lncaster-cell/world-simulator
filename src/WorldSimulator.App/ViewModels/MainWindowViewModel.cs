using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using System.Windows.Threading;
using WorldSimulator.App.Infrastructure;
using WorldSimulator.Core.Cities;
using WorldSimulator.Core.Time;

namespace WorldSimulator.App.ViewModels;

public sealed class MainWindowViewModel : INotifyPropertyChanged
{
    private readonly City _city;
    private readonly SimulationClock _clock;
    private readonly DispatcherTimer _timer;
    private DateTimeOffset _lastTickUtc;

    public MainWindowViewModel()
    {
        _city = CityPresets.CreateGotha();
        _clock = new SimulationClock();

        StartCommand = new RelayCommand(Start, () => !_clock.IsRunning);
        PauseCommand = new RelayCommand(Pause, () => _clock.IsRunning);

        _lastTickUtc = DateTimeOffset.UtcNow;
        _timer = new DispatcherTimer
        {
            Interval = TimeSpan.FromMilliseconds(250)
        };

        _timer.Tick += OnTick;
        _timer.Start();
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    public ICommand StartCommand { get; }

    public ICommand PauseCommand { get; }

    public int Day => _clock.Day;

    public int Hour => _clock.Hour;

    public bool IsRunning => _clock.IsRunning;

    public string SimulationState => IsRunning ? "Running" : "Paused";

    public string CityName => _city.Name;

    public string CityState => _city.CityState.ToString();

    public int Population => _city.Population;

    public decimal Food => _city.Food;

    public decimal Wealth => _city.Wealth;

    public int Mood => _city.Mood;

    public int Security => _city.Security;

    public int Crime => _city.Crime;

    public decimal Resources => _city.Resources;

    public decimal Goods => _city.Goods;

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

        if (StartCommand is RelayCommand startCommand)
        {
            startCommand.RaiseCanExecuteChanged();
        }

        if (PauseCommand is RelayCommand pauseCommand)
        {
            pauseCommand.RaiseCanExecuteChanged();
        }
    }

    private void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}
