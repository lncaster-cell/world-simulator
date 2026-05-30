using WorldSimulator.Core.Cities;
using WorldSimulator.Core.Events;
using WorldSimulator.Core.Resources;
using WorldSimulator.Core.Simulation;
using WorldSimulator.Core.Time;
using WorldSimulator.Core.Trade;
using WorldSimulator.Core.World;
using WorldSimulator.Persistence.Saves;

namespace WorldSimulator.App.Services;

public sealed class WorldSimulationRuntime
{
    private WorldSimulationRuntime(
        SimulationWorld world,
        City selectedCity,
        SimulationClock clock,
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
        CityStateEvaluator cityStateEvaluator,
        CityEventManager eventManager,
        CityEventEffectCalculator eventEffectCalculator,
        CityEventGenerator eventGenerator,
        WorldSimulationService worldSimulationService,
        JsonWorldSaveService saveService,
        SimulationJournalService journalService)
    {
        World = world;
        SelectedCity = selectedCity;
        Clock = clock;
        DailyFoodFlowCalculator = dailyFoodFlowCalculator;
        FishingProductionCalculator = fishingProductionCalculator;
        HuntingProductionCalculator = huntingProductionCalculator;
        AgricultureProductionCalculator = agricultureProductionCalculator;
        MainlandSupplyProductionCalculator = mainlandSupplyProductionCalculator;
        GoodsCraftingProductionCalculator = goodsCraftingProductionCalculator;
        ResourceGatheringProductionCalculator = resourceGatheringProductionCalculator;
        HouseholdConsumptionCalculator = householdConsumptionCalculator;
        DailyWealthFlowCalculator = dailyWealthFlowCalculator;
        WeeklyCrimeFlowCalculator = weeklyCrimeFlowCalculator;
        CityStateEvaluator = cityStateEvaluator;
        EventManager = eventManager;
        EventEffectCalculator = eventEffectCalculator;
        EventGenerator = eventGenerator;
        WorldSimulationService = worldSimulationService;
        SaveService = saveService;
        JournalService = journalService;
    }

    public SimulationWorld World { get; }

    public City SelectedCity { get; }

    public SimulationClock Clock { get; }

    public DailyFoodFlowCalculator DailyFoodFlowCalculator { get; }

    public FishingProductionCalculator FishingProductionCalculator { get; }

    public HuntingProductionCalculator HuntingProductionCalculator { get; }

    public AgricultureProductionCalculator AgricultureProductionCalculator { get; }

    public MainlandSupplyProductionCalculator MainlandSupplyProductionCalculator { get; }

    public GoodsCraftingProductionCalculator GoodsCraftingProductionCalculator { get; }

    public ResourceGatheringProductionCalculator ResourceGatheringProductionCalculator { get; }

    public HouseholdConsumptionCalculator HouseholdConsumptionCalculator { get; }

    public DailyWealthFlowCalculator DailyWealthFlowCalculator { get; }

    public WeeklyCrimeFlowCalculator WeeklyCrimeFlowCalculator { get; }

    public CityStateEvaluator CityStateEvaluator { get; }

    public CityEventManager EventManager { get; }

    public CityEventEffectCalculator EventEffectCalculator { get; }

    public CityEventGenerator EventGenerator { get; }

    public WorldSimulationService WorldSimulationService { get; }

    public JsonWorldSaveService SaveService { get; }

    public SimulationJournalService JournalService { get; }

    public static WorldSimulationRuntime CreateDefault()
    {
        var world = WorldPresets.CreateDefaultWorld();
        var selectedCity = world.SelectedCity;
        var clock = new SimulationClock();
        var dailyFoodFlowCalculator = new DailyFoodFlowCalculator();
        var fishingProductionCalculator = new FishingProductionCalculator();
        var huntingProductionCalculator = new HuntingProductionCalculator();
        var agricultureProductionCalculator = new AgricultureProductionCalculator();
        var mainlandSupplyProductionCalculator = new MainlandSupplyProductionCalculator();
        var goodsCraftingProductionCalculator = new GoodsCraftingProductionCalculator();
        var resourceGatheringProductionCalculator = new ResourceGatheringProductionCalculator();
        var householdConsumptionCalculator = new HouseholdConsumptionCalculator();
        var dailyWealthFlowCalculator = new DailyWealthFlowCalculator();
        var weeklyCrimeFlowCalculator = new WeeklyCrimeFlowCalculator();
        var cityStateEvaluator = new CityStateEvaluator();
        var eventManager = new CityEventManager();
        var eventEffectCalculator = new CityEventEffectCalculator();
        var eventGenerator = new CityEventGenerator(new SystemRandomProvider());
        var worldSimulationService = new WorldSimulationService(
            dailyFoodFlowCalculator,
            fishingProductionCalculator,
            huntingProductionCalculator,
            agricultureProductionCalculator,
            mainlandSupplyProductionCalculator,
            goodsCraftingProductionCalculator,
            resourceGatheringProductionCalculator,
            householdConsumptionCalculator,
            dailyWealthFlowCalculator,
            weeklyCrimeFlowCalculator,
            new WorldTradeFlowService(),
            new CaravanHiringService(),
            cityStateEvaluator,
            new PopulationChangeCalculator(),
            eventManager,
            eventEffectCalculator,
            eventGenerator);
        var saveService = new JsonWorldSaveService();
        var journalService = new SimulationJournalService();

        return new WorldSimulationRuntime(
            world,
            selectedCity,
            clock,
            dailyFoodFlowCalculator,
            fishingProductionCalculator,
            huntingProductionCalculator,
            agricultureProductionCalculator,
            mainlandSupplyProductionCalculator,
            goodsCraftingProductionCalculator,
            resourceGatheringProductionCalculator,
            householdConsumptionCalculator,
            dailyWealthFlowCalculator,
            weeklyCrimeFlowCalculator,
            cityStateEvaluator,
            eventManager,
            eventEffectCalculator,
            eventGenerator,
            worldSimulationService,
            saveService,
            journalService);
    }
}
