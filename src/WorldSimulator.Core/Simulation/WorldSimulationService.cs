using WorldSimulator.Core.Cities;
using WorldSimulator.Core.Events;
using WorldSimulator.Core.Resources;
using WorldSimulator.Core.Trade;
using WorldSimulator.Core.World;

namespace WorldSimulator.Core.Simulation;

public sealed class WorldSimulationService
{
    private readonly CityEventManager _defaultEventManager;
    private readonly WorldEventState _eventState = new();
    private readonly WorldSimulationStepOrder _stepOrder;

    public WorldSimulationService(
        DailyFoodFlowCalculator dailyFoodFlowCalculator,
        FishingProductionCalculator fishingProductionCalculator,
        HuntingProductionCalculator huntingProductionCalculator,
        AgricultureProductionCalculator agricultureProductionCalculator,
        MainlandSupplyProductionCalculator mainlandSupplyProductionCalculator,
        GoodsCraftingProductionCalculator goodsCraftingProductionCalculator,
        ResourceGatheringProductionCalculator resourceGatheringProductionCalculator,
        HouseholdConsumptionCalculator householdConsumptionCalculator,
        DailyWealthFlowCalculator dailyWealthFlowCalculator,
        WeeklyCrimeFlowCalculator weeklyCrimeFlowCalculator,
        WorldTradeFlowService worldTradeFlowService,
        CaravanHiringService caravanHiringService,
        CityStateEvaluator cityStateEvaluator,
        PopulationChangeCalculator populationChangeCalculator,
        CityEventManager eventManager,
        CityEventEffectCalculator eventEffectCalculator,
        CityEventGenerator eventGenerator)
    {
        _defaultEventManager = eventManager;
        var tradeSimulationStep = new TradeSimulationStep(worldTradeFlowService, caravanHiringService);
        _stepOrder = WorldSimulationStepOrder.CreateDefault(
            tradeSimulationStep,
            new CityEventSimulationStep(eventEffectCalculator, eventGenerator),
            new FoodSimulationStep(
                dailyFoodFlowCalculator,
                fishingProductionCalculator,
                huntingProductionCalculator,
                agricultureProductionCalculator,
                mainlandSupplyProductionCalculator),
            new WealthSimulationStep(
                resourceGatheringProductionCalculator,
                goodsCraftingProductionCalculator,
                householdConsumptionCalculator,
                dailyWealthFlowCalculator),
            new CityStateSimulationStep(cityStateEvaluator),
            new CrimeSimulationStep(householdConsumptionCalculator, weeklyCrimeFlowCalculator),
            new PopulationSimulationStep(populationChangeCalculator));
    }

    public WorldDayAdvanceResult AdvanceDay(SimulationWorld world, string selectedCityId, int day, bool randomEventsEnabled)
    {
        var context = new WorldSimulationContext(selectedCityId, randomEventsEnabled, _eventState, _defaultEventManager);
        context.SetCurrentDay(day);
        context.EnsureSelectedCityEventManagerBinding();
        context.CaptureActiveEventNamesBeforeAdvance();

        _stepOrder.ExecuteBeforeCitySteps(world, day);

        foreach (var city in world.Cities)
        {
            var profile = world.FindSettlementEconomyProfile(city.Id);
            if (profile is null) continue;

            context.GetOrCreateCityState(city, profile);
            ExecuteCitySteps(world, city, day, context);
        }

        context.SetWeeklyTradeFlowResult(_stepOrder.ExecuteAfterCitySteps(world, day));

        return context.CreateResult();
    }

    public WorldEventState ExportEventState()
    {
        return _eventState.CreateSnapshot();
    }

    public void ImportEventState(WorldEventState eventState, string selectedCityId)
    {
        ArgumentNullException.ThrowIfNull(eventState);
        ArgumentException.ThrowIfNullOrWhiteSpace(selectedCityId);

        _eventState.ReplaceWith(eventState.EventManagersByCity);
        EnsureSelectedCityEventManagerBinding(selectedCityId);
    }

    public void ResetEventState()
    {
        _eventState.ReplaceWith(new Dictionary<string, CityEventManager>(StringComparer.Ordinal));
    }

    private void ExecuteCitySteps(SimulationWorld world, City city, int day, WorldSimulationContext context)
    {
        var stepIndex = 0;

        void Next()
        {
            if (stepIndex >= _stepOrder.CitySteps.Count)
            {
                return;
            }

            var step = _stepOrder.CitySteps[stepIndex++];
            step.Execute(world, city, day, context, Next);
        }

        Next();
    }

    private void EnsureSelectedCityEventManagerBinding(string selectedCityId)
    {
        if (_eventState.EventManagersByCity.TryGetValue(selectedCityId, out _))
        {
            return;
        }

        _eventState.SetManager(selectedCityId, _defaultEventManager);
    }
}

public sealed record WorldDayAdvanceResult(
    CityDailySimulationResult? SelectedCityResult,
    CityEventEffectsResult SelectedCityEventEffects,
    PopulationChangeResult? SelectedCityPopulationChange,
    WeeklyCrimeFlowResult? SelectedCityCrimeFlow,
    WorldTradeFlowResult? WeeklyTradeFlowResult,
    IReadOnlyList<CityEvent> CompletedEvents,
    CityEvent? GeneratedEvent,
    IReadOnlyList<string> ActiveEventNamesBeforeAdvance);

public sealed record CityDailySimulationResult(
    string CityId,
    string CityName,
    DailyFoodFlowResult FoodFlow,
    AgricultureProductionResult Agriculture,
    ResourceGatheringProductionResult ResourceGathering,
    GoodsCraftingProductionResult GoodsCrafting,
    HouseholdConsumptionResult HouseholdConsumption,
    DailyWealthFlowResult WealthFlow);
