namespace WorldSimulator.Core.Events;

public sealed class CityEvent
{
    private string _id;
    private string _name;
    private int _durationDays;
    private int _remainingDays;

    public CityEvent(string id, string name, string description, int startedDay, int durationDays)
        : this(id, name, description, startedDay, durationDays, durationDays)
    {
    }

    public CityEvent(string id, string name, string description, int startedDay, int durationDays, int remainingDays)
    {
        Id = id;
        Name = name;
        Description = description ?? string.Empty;
        StartedDay = startedDay;
        DurationDays = durationDays;
        RemainingDays = remainingDays;
    }

    public string Id
    {
        get => _id;
        private set => _id = string.IsNullOrWhiteSpace(value)
            ? throw new ArgumentException("Event id must not be empty.", nameof(value))
            : value;
    }

    public string Name
    {
        get => _name;
        private set => _name = string.IsNullOrWhiteSpace(value)
            ? throw new ArgumentException("Event name must not be empty.", nameof(value))
            : value;
    }

    public string Description { get; }

    public int StartedDay { get; }

    public int DurationDays
    {
        get => _durationDays;
        private set => _durationDays = value < 1
            ? throw new ArgumentOutOfRangeException(nameof(value), "DurationDays must be greater than or equal to 1.")
            : value;
    }

    public int RemainingDays
    {
        get => _remainingDays;
        private set => _remainingDays = value < 0
            ? throw new ArgumentOutOfRangeException(nameof(value), "RemainingDays must be greater than or equal to 0.")
            : value;
    }

    public bool IsActive => RemainingDays > 0;

    public bool IsCompleted => RemainingDays == 0;

    public void AdvanceDay()
    {
        if (RemainingDays == 0)
        {
            return;
        }

        RemainingDays -= 1;
    }
}
