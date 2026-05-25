namespace WorldSimulator.Core.Time;

public sealed class SimulationClock
{
    private const int HoursPerDay = 24;
    private readonly SimulationTimeSettings _settings;
    private TimeSpan _accumulatedRealTime;

    public SimulationClock()
        : this(SimulationTimeSettings.Default)
    {
    }

    public SimulationClock(SimulationTimeSettings settings)
    {
        _settings = settings ?? throw new ArgumentNullException(nameof(settings));
        Day = 1;
        Hour = 0;
        IsRunning = false;
        _accumulatedRealTime = TimeSpan.Zero;
    }

    public int Day { get; private set; }

    public int Hour { get; private set; }

    public bool IsRunning { get; private set; }

    public TimeSpan AccumulatedRealTime => _accumulatedRealTime;

    public TimeSpan RealTimePerGameHour => _settings.RealTimePerGameHour;

    public event Action<int, int>? HourAdvanced;

    public event Action<int>? DayAdvanced;

    public void Start() => IsRunning = true;

    public void Pause() => IsRunning = false;

    public void Advance(TimeSpan elapsedRealTime)
    {
        if (elapsedRealTime < TimeSpan.Zero)
        {
            throw new ArgumentOutOfRangeException(nameof(elapsedRealTime), "Elapsed real time cannot be negative.");
        }

        if (!IsRunning || elapsedRealTime == TimeSpan.Zero)
        {
            return;
        }

        _accumulatedRealTime += elapsedRealTime;

        while (_accumulatedRealTime >= _settings.RealTimePerGameHour)
        {
            _accumulatedRealTime -= _settings.RealTimePerGameHour;
            AdvanceOneHour();
        }
    }

    private void AdvanceOneHour()
    {
        Hour++;

        if (Hour >= HoursPerDay)
        {
            Hour = 0;
            Day++;
            DayAdvanced?.Invoke(Day);
        }

        HourAdvanced?.Invoke(Day, Hour);

        // Design hook: future pending-balance scheduler can be plugged here
        // (or subscribed via HourAdvanced/DayAdvanced) to avoid heavy burst processing.
    }
}
