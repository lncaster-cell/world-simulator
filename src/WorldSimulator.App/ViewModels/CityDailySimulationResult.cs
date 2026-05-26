using WorldSimulator.Core.Resources;

namespace WorldSimulator.App.ViewModels;

public sealed class CityDailySimulationResult
{
    public required string CityId { get; init; }
    public required string CityName { get; init; }
    public required DailyFoodFlowResult FoodFlow { get; init; }
    public required AgricultureProductionResult Agriculture { get; init; }
    public required ResourceGatheringProductionResult ResourceGathering { get; init; }
    public required GoodsCraftingProductionResult GoodsCrafting { get; init; }
    public required HouseholdConsumptionResult HouseholdConsumption { get; init; }
    public required DailyWealthFlowResult WealthFlow { get; init; }
}
