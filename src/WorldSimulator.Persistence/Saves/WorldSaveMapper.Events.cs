using WorldSimulator.Core.Events;

namespace WorldSimulator.Persistence.Saves;

internal static partial class WorldSaveMapper
{
    public static EventSaveData ToSaveData(WorldEventState eventState, string selectedCityId) => new()
    {
        ActiveEvents = eventState.GetManagerOrEmpty(selectedCityId).ActiveEvents.Select(ToSaveData).ToList(),
        CompletedEvents = eventState.GetManagerOrEmpty(selectedCityId).CompletedEvents.Select(ToSaveData).ToList(),
        EventsByCityId = eventState.EventManagersByCity.ToDictionary(
            x => x.Key,
            x => new CityEventBucketSaveData
            {
                ActiveEvents = x.Value.ActiveEvents.Select(ToSaveData).ToList(),
                CompletedEvents = x.Value.CompletedEvents.Select(ToSaveData).ToList()
            },
            StringComparer.Ordinal)
    };

    public static WorldEventState ToCoreEventState(EventSaveData? events, string selectedCityId)
    {
        var state = new WorldEventState();
        if (events?.EventsByCityId is { Count: > 0 })
        {
            foreach (var pair in events.EventsByCityId)
            {
                var manager = new CityEventManager();
                manager.Restore(
                    (pair.Value.ActiveEvents ?? []).Select(ToCoreEvent).ToList(),
                    (pair.Value.CompletedEvents ?? []).Select(ToCoreEvent).ToList());
                state.SetManager(pair.Key, manager);
            }

            if (!state.EventManagersByCity.ContainsKey(selectedCityId))
            {
                var firstManager = state.EventManagersByCity.Values.First();
                var selectedManager = new CityEventManager();
                selectedManager.Restore(firstManager.ActiveEvents.ToList(), firstManager.CompletedEvents.ToList());
                state.SetManager(selectedCityId, selectedManager);
            }

            return state;
        }

        var fallbackManager = new CityEventManager();
        fallbackManager.Restore(
            (events?.ActiveEvents ?? []).Select(ToCoreEvent).ToList(),
            (events?.CompletedEvents ?? []).Select(ToCoreEvent).ToList());
        state.SetManager(selectedCityId, fallbackManager);
        return state;
    }

    public static CityEventSaveData ToSaveData(CityEvent cityEvent) => new() { Id = cityEvent.Id, Name = cityEvent.Name, Description = cityEvent.Description, StartedDay = cityEvent.StartedDay, DurationDays = cityEvent.DurationDays, RemainingDays = cityEvent.RemainingDays };

    public static CityEvent ToCoreEvent(CityEventSaveData eventSaveData) => new(eventSaveData.Id, eventSaveData.Name, eventSaveData.Description, eventSaveData.StartedDay, eventSaveData.DurationDays, eventSaveData.RemainingDays);
}
