namespace WorldSimulator.Core.Events;

public sealed class CityEventManager
{
    private readonly List<CityEvent> _activeEvents = new();
    private readonly List<CityEvent> _completedEvents = new();

    public IReadOnlyList<CityEvent> ActiveEvents => _activeEvents;

    public IReadOnlyList<CityEvent> CompletedEvents => _completedEvents;

    public bool AddEvent(CityEvent cityEvent)
    {
        ArgumentNullException.ThrowIfNull(cityEvent);

        if (_activeEvents.Any(e => e.Id == cityEvent.Id))
        {
            return false;
        }

        _activeEvents.Add(cityEvent);
        return true;
    }

    public IReadOnlyList<CityEvent> AdvanceDay()
    {
        var newlyCompleted = new List<CityEvent>();

        foreach (var activeEvent in _activeEvents)
        {
            activeEvent.AdvanceDay();

            if (activeEvent.IsCompleted)
            {
                newlyCompleted.Add(activeEvent);
            }
        }

        if (newlyCompleted.Count == 0)
        {
            return newlyCompleted;
        }

        foreach (var completedEvent in newlyCompleted)
        {
            _activeEvents.Remove(completedEvent);
            _completedEvents.Add(completedEvent);
        }

        return newlyCompleted;
    }

    public void Clear()
    {
        _activeEvents.Clear();
        _completedEvents.Clear();
    }

    public void Restore(IEnumerable<CityEvent>? activeEvents, IEnumerable<CityEvent>? completedEvents)
    {
        var activeSnapshot = activeEvents?.ToList() ?? new List<CityEvent>();
        var completedSnapshot = completedEvents?.ToList() ?? new List<CityEvent>();

        _activeEvents.Clear();
        _completedEvents.Clear();

        foreach (var cityEvent in activeSnapshot)
        {
            if (_activeEvents.Any(e => e.Id == cityEvent.Id))
            {
                continue;
            }

            _activeEvents.Add(cityEvent);
        }

        _completedEvents.AddRange(completedSnapshot);
    }
}
