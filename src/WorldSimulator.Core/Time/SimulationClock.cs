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

    public void SetSimulationSpeed(TimeSpan realTimePerGameHour)
    {
        if (realTimePerGameHour <= TimeSpan.Zero)
        {
            throw new ArgumentOutOfRangeException(nameof(realTimePerGameHour), "RealTimePerGameHour must be greater than zero.");
        }

        _accumulatedRealTime = ScaleAccumulatedRealTime(_accumulatedRealTime, _settings.RealTimePerGameHour, realTimePerGameHour);
        _settings.RealTimePerGameHour = realTimePerGameHour;
    }

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

    public void RestoreState(int day, int hour, bool isRunning, TimeSpan accumulatedRealTime, TimeSpan realTimePerGameHour)
    {
        if (day < 1)
        {
            throw new ArgumentOutOfRangeException(nameof(day), "Day must be at least 1.");
        }

        if (hour is < 0 or >= HoursPerDay)
        {
            throw new ArgumentOutOfRangeException(nameof(hour), "Hour must be between 0 and 23.");
        }

        if (accumulatedRealTime < TimeSpan.Zero)
        {
            throw new ArgumentOutOfRangeException(nameof(accumulatedRealTime), "Accumulated real time cannot be negative.");
        }

        if (realTimePerGameHour <= TimeSpan.Zero)
        {
            throw new ArgumentOutOfRangeException(nameof(realTimePerGameHour), "RealTimePerGameHour must be greater than zero.");
        }

        Day = day;
        Hour = hour;
        IsRunning = isRunning;
        _accumulatedRealTime = NormalizeAccumulatedRealTime(accumulatedRealTime, realTimePerGameHour);
        _settings.RealTimePerGameHour = realTimePerGameHour;
    }

    private static TimeSpan ScaleAccumulatedRealTime(TimeSpan accumulatedRealTime, TimeSpan oldRealTimePerGameHour, TimeSpan newRealTimePerGameHour)
    {
        if (accumulatedRealTime == TimeSpan.Zero)
        {
            return TimeSpan.Zero;
        }

        var normalizedAccumulatedRealTime = NormalizeAccumulatedRealTime(accumulatedRealTime, oldRealTimePerGameHour);
        if (normalizedAccumulatedRealTime == TimeSpan.Zero)
        {
            return TimeSpan.Zero;
        }

        var elapsedHourFraction = normalizedAccumulatedRealTime.Ticks / (double)oldRealTimePerGameHour.Ticks;
        var scaledTicks = (long)Math.Floor(newRealTimePerGameHour.Ticks * elapsedHourFraction);
        return TimeSpan.FromTicks(Math.Clamp(scaledTicks, 0, newRealTimePerGameHour.Ticks - 1));
    }

    private static TimeSpan NormalizeAccumulatedRealTime(TimeSpan accumulatedRealTime, TimeSpan realTimePerGameHour)
    {
        if (accumulatedRealTime < realTimePerGameHour)
        {
            return accumulatedRealTime;
        }

        return TimeSpan.FromTicks(accumulatedRealTime.Ticks % realTimePerGameHour.Ticks);
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
