namespace WorldSimulator.Core.Events;

public sealed class WorldEventState
{
    private readonly Dictionary<string, CityEventManager> _eventManagersByCity = new(StringComparer.Ordinal);

    public IReadOnlyDictionary<string, CityEventManager> EventManagersByCity => _eventManagersByCity;

    public CityEventManager GetOrCreateManager(string cityId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(cityId);
        if (_eventManagersByCity.TryGetValue(cityId, out var manager))
        {
            return manager;
        }

        manager = new CityEventManager();
        _eventManagersByCity[cityId] = manager;
        return manager;
    }

    public CityEventManager GetManagerOrEmpty(string cityId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(cityId);
        return _eventManagersByCity.TryGetValue(cityId, out var manager) ? manager : new CityEventManager();
    }

    public void SetManager(string cityId, CityEventManager manager)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(cityId);
        ArgumentNullException.ThrowIfNull(manager);
        _eventManagersByCity[cityId] = manager;
    }

    public void ReplaceWith(IReadOnlyDictionary<string, CityEventManager> managersByCity)
    {
        ArgumentNullException.ThrowIfNull(managersByCity);
        _eventManagersByCity.Clear();
        foreach (var pair in managersByCity)
        {
            var sourceManager = pair.Value;
            var managerCopy = new CityEventManager();
            managerCopy.Restore(sourceManager.ActiveEvents.ToArray(), sourceManager.CompletedEvents.ToArray());
            _eventManagersByCity[pair.Key] = managerCopy;
        }
    }
}
