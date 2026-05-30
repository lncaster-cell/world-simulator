using WorldSimulator.Core.Cities;
using WorldSimulator.Core.Events;
using WorldSimulator.Core.Resources;
using WorldSimulator.Core.Workforce;
using WorldSimulator.Core.World;

namespace WorldSimulator.Core.Simulation;

public sealed class CityStepState
{
    public CityStepState(SettlementEconomyProfile profile)
    {
        Profile = profile;
    }

    public SettlementEconomyProfile Profile { get; }
    public CityEventManager EventManager { get; set; } = new();
    public CityEventEffectsResult EventEffects { get; set; } = CityEventEffectsResult.None;
    public IReadOnlyCollection<CityEvent> ActiveEvents { get; set; } = Array.Empty<CityEvent>();
    public CityWorkforceAllocation? WorkforceAllocation { get; set; }
    public DailyFoodFlowResult? FoodFlow { get; set; }
    public AgricultureProductionResult? Agriculture { get; set; }
    public ResourceGatheringProductionResult? ResourceGathering { get; set; }
    public GoodsCraftingProductionResult? GoodsCrafting { get; set; }
    public HouseholdConsumptionResult? HouseholdConsumption { get; set; }
    public DailyWealthFlowResult? WealthFlow { get; set; }
    public CityDailySimulationResult? CityResult { get; set; }
    public PopulationChangeResult? PopulationChange { get; set; }
}
