using WorldSimulator.Core.Cities;
using WorldSimulator.Core.Events;
using WorldSimulator.Core.Resources;
using WorldSimulator.Core.World;
using WorldSimulator.Core.Trade;

namespace WorldSimulator.Core.Simulation;

public sealed class WorldSimulationService
{
    private readonly DailyFoodFlowCalculator _dailyFoodFlowCalculator;
    private readonly FishingProductionCalculator _fishingProductionCalculator;
    private readonly HuntingProductionCalculator _huntingProductionCalculator;
    private readonly AgricultureProductionCalculator _agricultureProductionCalculator;
    private readonly MainlandSupplyProductionCalculator _mainlandSupplyProductionCalculator;
    private readonly GoodsCraftingProductionCalculator _goodsCraftingProductionCalculator;
    private readonly ResourceGatheringProductionCalculator _resourceGatheringProductionCalculator;
    private readonly HouseholdConsumptionCalculator _householdConsumptionCalculator;
    private readonly DailyWealthFlowCalculator _dailyWealthFlowCalculator;
    private readonly WeeklyCrimeFlowCalculator _weeklyCrimeFlowCalculator;
    private readonly WorldTradeFlowService _worldTradeFlowService;
    private readonly CaravanHiringService _caravanHiringService;
    private readonly CityStateEvaluator _cityStateEvaluator;
    private readonly PopulationChangeCalculator _populationChangeCalculator;
    private readonly CityEventManager _defaultEventManager;
    private readonly CityEventEffectCalculator _eventEffectCalculator;
    private readonly CityEventGenerator _eventGenerator;
    private readonly WorldEventState _eventState = new();

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
        _dailyFoodFlowCalculator = dailyFoodFlowCalculator;
        _fishingProductionCalculator = fishingProductionCalculator;
        _huntingProductionCalculator = huntingProductionCalculator;
        _agricultureProductionCalculator = agricultureProductionCalculator;
        _mainlandSupplyProductionCalculator = mainlandSupplyProductionCalculator;
        _goodsCraftingProductionCalculator = goodsCraftingProductionCalculator;
        _resourceGatheringProductionCalculator = resourceGatheringProductionCalculator;
        _householdConsumptionCalculator = householdConsumptionCalculator;
        _dailyWealthFlowCalculator = dailyWealthFlowCalculator;
        _weeklyCrimeFlowCalculator = weeklyCrimeFlowCalculator;
        _worldTradeFlowService = worldTradeFlowService;
        _caravanHiringService = caravanHiringService;
        _cityStateEvaluator = cityStateEvaluator;
        _populationChangeCalculator = populationChangeCalculator;
        _defaultEventManager = eventManager;
        _eventEffectCalculator = eventEffectCalculator;
        _eventGenerator = eventGenerator;
    }

    public WorldDayAdvanceResult AdvanceDay(SimulationWorld world, string selectedCityId, int day, bool randomEventsEnabled)
    {
        var selectedCityResult = default(CityDailySimulationResult);
        var selectedCityEventEffects = CityEventEffectsResult.None;
        var selectedCityPopulationChange = default(PopulationChangeResult);
        var selectedCityCrimeFlow = default(WeeklyCrimeFlowResult);
        EnsureSelectedCityEventManagerBinding(selectedCityId);
        var selectedCityEventManager = GetOrCreateCityEventManager(selectedCityId);
        var activeEventNamesBeforeAdvance = selectedCityEventManager.ActiveEvents.Select(e => e.Name).ToList();

        WorldTradeFlowResult? weeklyTradeFlowResult = null;

        _worldTradeFlowService.ProcessShipments(world, day);

        foreach (var city in world.Cities)
        {
            var profile = world.FindSettlementEconomyProfile(city.Id);
            if (profile is null) continue;

            var isSelectedCity = city.Id == selectedCityId;
            var eventManager = GetOrCreateCityEventManager(city.Id);
            var eventEffects = _eventEffectCalculator.Calculate(city, eventManager.ActiveEvents);
            var activeEvents = eventManager.ActiveEvents;
            var cityResult = SimulateCityDay(city, profile, eventEffects, activeEvents);
            city.CityState = _cityStateEvaluator.Evaluate(city);

            if (IsWeeklyUpdateDay(day))
            {
                var householdConsumption = _householdConsumptionCalculator.Calculate(city);
                var crimeFlow = _weeklyCrimeFlowCalculator.Calculate(city, cityResult.FoodFlow, householdConsumption);
                city.Crime = crimeFlow.EndingCrime;
                if (isSelectedCity)
                {
                    selectedCityCrimeFlow = crimeFlow;
                }
            }

            var populationChange = _populationChangeCalculator.Calculate(city);
            if (populationChange.PopulationDelta != 0)
            {
                city.Population = populationChange.EndingPopulation;
            }

            eventManager.AdvanceDay();

            if (randomEventsEnabled && city.Population > 0 && city.CityState != CityState.Abandoned)
            {
                var generationResult = _eventGenerator.TryGenerate(day, eventManager.ActiveEvents);
                if (generationResult.WasGenerated && generationResult.Event is not null)
                {
                    eventManager.AddEvent(generationResult.Event);
                }
            }

            if (!isSelectedCity) continue;

            selectedCityResult = cityResult;
            selectedCityEventEffects = eventEffects;
            selectedCityPopulationChange = populationChange;
        }

        if (IsWeeklyUpdateDay(day))
        {
            _caravanHiringService.EvaluateAndHire(world);
            weeklyTradeFlowResult = _worldTradeFlowService.RunWeeklyTrade(world, day);
        }

        var completedEvents = selectedCityEventManager.CompletedEvents;
        var generatedEvent = selectedCityEventManager.ActiveEvents
            .OrderByDescending(e => e.StartedDay)
            .FirstOrDefault(e => e.StartedDay == day);

        return new WorldDayAdvanceResult(selectedCityResult, selectedCityEventEffects, selectedCityPopulationChange, selectedCityCrimeFlow, weeklyTradeFlowResult, completedEvents, generatedEvent, activeEventNamesBeforeAdvance);
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

    private CityEventManager GetOrCreateCityEventManager(string cityId)
    {
        return _eventState.GetOrCreateManager(cityId);
    }

    private void EnsureSelectedCityEventManagerBinding(string selectedCityId)
    {
        if (_eventState.EventManagersByCity.TryGetValue(selectedCityId, out _))
        {
            return;
        }

        _eventState.SetManager(selectedCityId, _defaultEventManager);
    }

    private CityDailySimulationResult SimulateCityDay(City city, SettlementEconomyProfile profile, CityEventEffectsResult eventEffects, IReadOnlyCollection<CityEvent> activeEvents)
    {
        var agriculture = _agricultureProductionCalculator.Calculate(city, profile);
        var fishing = _fishingProductionCalculator.Calculate(city, activeEvents);
        var hunting = _huntingProductionCalculator.Calculate(city);
        var mainlandSupply = _mainlandSupplyProductionCalculator.Calculate(city, activeEvents);
        var foodInputs = new DailyFoodFlowInputs
        {
            AgricultureIncome = agriculture.FinalOutput,
            FishingIncome = fishing.FinalOutput * profile.FishingMultiplier,
            HuntingIncome = hunting.FinalOutput * profile.HuntingMultiplier,
            MainlandSupplyIncome = mainlandSupply.FinalOutput * profile.MainlandSupplyMultiplier + eventEffects.MainlandSupplyDelta,
            EventDelta = eventEffects.FoodDelta
        };
        var foodFlow = _dailyFoodFlowCalculator.Calculate(city, foodInputs);
        _dailyFoodFlowCalculator.Apply(city, foodFlow);

        var resourceGathering = _resourceGatheringProductionCalculator.Calculate(city);
        var gatheredResources = decimal.Round(resourceGathering.FinalOutput * profile.ResourceGatheringMultiplier, 2, MidpointRounding.AwayFromZero);
        city.Resources += gatheredResources;

        city.Mood += eventEffects.MoodDelta;
        city.Security += eventEffects.SecurityDelta;
        city.Crime += eventEffects.CrimeDelta;
        city.Wealth += eventEffects.WealthDelta;
        city.Resources += eventEffects.ResourcesDelta;

        var goodsCrafting = _goodsCraftingProductionCalculator.Calculate(city);
        var goodsProduced = decimal.Round(goodsCrafting.GoodsProduced * profile.GoodsCraftingMultiplier, 2, MidpointRounding.AwayFromZero);
        var resourcesConsumed = decimal.Round(goodsCrafting.ResourcesConsumed * profile.GoodsCraftingMultiplier, 2, MidpointRounding.AwayFromZero);
        resourcesConsumed = Math.Min(resourcesConsumed, city.Resources);
        city.Resources -= resourcesConsumed;
        city.Goods += goodsProduced;

        var scaledGoods = new GoodsCraftingProductionResult
        {
            NaturalPotential = goodsCrafting.NaturalPotential,
            RequiredWorkers = goodsCrafting.RequiredWorkers,
            AssignedWorkers = goodsCrafting.AssignedWorkers,
            WorkerCoverage = goodsCrafting.WorkerCoverage,
            ExtraWorkers = goodsCrafting.ExtraWorkers,
            OverstaffBonus = goodsCrafting.OverstaffBonus,
            MoodModifier = goodsCrafting.MoodModifier,
            SecurityModifier = goodsCrafting.SecurityModifier,
            StateModifier = goodsCrafting.StateModifier,
            ResourceCostPerGoods = goodsCrafting.ResourceCostPerGoods,
            PotentialGoodsOutput = goodsCrafting.PotentialGoodsOutput * profile.GoodsCraftingMultiplier,
            ResourcesNeeded = goodsCrafting.ResourcesNeeded * profile.GoodsCraftingMultiplier,
            ResourcesAvailable = goodsCrafting.ResourcesAvailable,
            ResourcesConsumed = resourcesConsumed,
            GoodsProduced = goodsProduced
        };

        var householdConsumption = _householdConsumptionCalculator.Calculate(city);
        city.Goods -= householdConsumption.GoodsConsumed;
        city.Resources -= householdConsumption.ResourcesConsumed;

        var wealthFlow = _dailyWealthFlowCalculator.Calculate(city, foodFlow, scaledGoods, householdConsumption);
        city.Wealth = wealthFlow.EndingWealth;

        return new CityDailySimulationResult(
            city.Id,
            city.Name,
            foodFlow,
            agriculture,
            new ResourceGatheringProductionResult
            {
                NaturalPotential = resourceGathering.NaturalPotential,
                RequiredWorkers = resourceGathering.RequiredWorkers,
                AssignedWorkers = resourceGathering.AssignedWorkers,
                WorkerCoverage = resourceGathering.WorkerCoverage,
                ExtraWorkers = resourceGathering.ExtraWorkers,
                OverstaffBonus = resourceGathering.OverstaffBonus,
                SecurityModifier = resourceGathering.SecurityModifier,
                StateModifier = resourceGathering.StateModifier,
                FinalOutput = gatheredResources
            },
            scaledGoods,
            householdConsumption,
            wealthFlow);
    }

    private static bool IsWeeklyUpdateDay(int day) => day > 0 && day % 7 == 0;
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
