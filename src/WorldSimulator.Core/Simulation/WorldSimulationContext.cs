using WorldSimulator.Core.Cities;
using WorldSimulator.Core.Events;
using WorldSimulator.Core.Resources;
using WorldSimulator.Core.Trade;
using WorldSimulator.Core.World;

namespace WorldSimulator.Core.Simulation;

public sealed class WorldSimulationContext
{
    private readonly WorldEventState _eventState;
    private readonly CityEventManager _defaultEventManager;
    private readonly SimulationCadenceResolver _cadenceResolver;
    private readonly Dictionary<string, CityStepState> _cityStates = new(StringComparer.Ordinal);

    public WorldSimulationContext(
        string selectedCityId,
        bool randomEventsEnabled,
        WorldEventState eventState,
        CityEventManager defaultEventManager,
        SimulationCadenceResolver cadenceResolver)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(selectedCityId);
        ArgumentNullException.ThrowIfNull(eventState);
        ArgumentNullException.ThrowIfNull(defaultEventManager);
        ArgumentNullException.ThrowIfNull(cadenceResolver);

        SelectedCityId = selectedCityId;
        RandomEventsEnabled = randomEventsEnabled;
        _eventState = eventState;
        _defaultEventManager = defaultEventManager;
        _cadenceResolver = cadenceResolver;
    }

    public string SelectedCityId { get; }
    public bool RandomEventsEnabled { get; }
    public CityDailySimulationResult? SelectedCityResult { get; private set; }
    public CityEventEffectsResult SelectedCityEventEffects { get; private set; } = CityEventEffectsResult.None;
    public PopulationChangeResult? SelectedCityPopulationChange { get; private set; }
    public WeeklyCrimeFlowResult? SelectedCityCrimeFlow { get; private set; }
    public WorldTradeFlowResult? WeeklyTradeFlowResult { get; private set; }
    public IReadOnlyList<string> ActiveEventNamesBeforeAdvance { get; private set; } = Array.Empty<string>();
    public IReadOnlyList<CityEvent> SelectedCityCompletedEventsToday { get; private set; } = Array.Empty<CityEvent>();

    public bool IsSelectedCity(City city) => city.Id == SelectedCityId;

    public bool ShouldRun(SimulationCadence cadence) => _cadenceResolver.ShouldRun(CurrentDay, cadence);

    public void CaptureActiveEventNamesBeforeAdvance()
    {
        var selectedCityEventManager = GetOrCreateCityEventManager(SelectedCityId);
        ActiveEventNamesBeforeAdvance = selectedCityEventManager.ActiveEvents.Select(e => e.Name).ToList();
    }

    public CityEventManager GetOrCreateCityEventManager(string cityId)
    {
        return _eventState.GetOrCreateManager(cityId);
    }

    public void EnsureSelectedCityEventManagerBinding()
    {
        if (_eventState.EventManagersByCity.TryGetValue(SelectedCityId, out _))
        {
            return;
        }

        _eventState.SetManager(SelectedCityId, _defaultEventManager);
    }

    public CityStepState GetOrCreateCityState(City city, SettlementEconomyProfile profile)
    {
        ArgumentNullException.ThrowIfNull(city);
        ArgumentNullException.ThrowIfNull(profile);

        if (_cityStates.TryGetValue(city.Id, out var state))
        {
            return state;
        }

        state = new CityStepState(profile);
        _cityStates.Add(city.Id, state);
        return state;
    }

    public CityStepState GetCityState(City city)
    {
        ArgumentNullException.ThrowIfNull(city);

        if (_cityStates.TryGetValue(city.Id, out var state))
        {
            return state;
        }

        throw new InvalidOperationException($"City simulation state for '{city.Id}' was not initialized.");
    }

    public void CaptureCityResult(City city, CityStepState state)
    {
        if (!IsSelectedCity(city))
        {
            return;
        }

        SelectedCityResult = state.CityResult;
        SelectedCityEventEffects = state.EventEffects;
        SelectedCityPopulationChange = state.PopulationChange;
    }

    public void CaptureCompletedEvents(City city, IReadOnlyList<CityEvent> newlyCompleted)
    {
        ArgumentNullException.ThrowIfNull(city);
        ArgumentNullException.ThrowIfNull(newlyCompleted);

        if (!IsSelectedCity(city))
        {
            return;
        }

        SelectedCityCompletedEventsToday = newlyCompleted.ToList();
    }

    public void CaptureCrimeFlow(City city, WeeklyCrimeFlowResult crimeFlow)
    {
        if (IsSelectedCity(city))
        {
            SelectedCityCrimeFlow = crimeFlow;
        }
    }

    public void SetWeeklyTradeFlowResult(WorldTradeFlowResult? weeklyTradeFlowResult)
    {
        WeeklyTradeFlowResult = weeklyTradeFlowResult;
    }

    public WorldDayAdvanceResult CreateResult()
    {
        var selectedCityEventManager = GetOrCreateCityEventManager(SelectedCityId);
        var generatedEvent = selectedCityEventManager.ActiveEvents
            .OrderByDescending(e => e.StartedDay)
            .FirstOrDefault(e => e.StartedDay == CurrentDay);

        return new WorldDayAdvanceResult(
            SelectedCityResult,
            SelectedCityEventEffects,
            SelectedCityPopulationChange,
            SelectedCityCrimeFlow,
            WeeklyTradeFlowResult,
            SelectedCityCompletedEventsToday,
            generatedEvent,
            ActiveEventNamesBeforeAdvance);
    }

    public int CurrentDay { get; private set; }

    public void SetCurrentDay(int day)
    {
        CurrentDay = day;
    }
}
