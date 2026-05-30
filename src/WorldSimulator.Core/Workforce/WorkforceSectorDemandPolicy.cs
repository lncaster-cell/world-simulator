using WorldSimulator.Core.Cities;

namespace WorldSimulator.Core.Workforce;

public sealed class WorkforceSectorDemandPolicy
{
    private const decimal FoodStressDaysThreshold = 3m;
    private const decimal GoodsShortageThreshold = 80m;
    private const decimal ResourcesShortageThreshold = 80m;
    private const int LowSecurityThreshold = 35;
    private const int HighCrimeThreshold = 60;

    public IReadOnlyList<WorkforceSectorDemand> BuildDemands(City city, SettlementSectorCapacityProfile capacityProfile)
    {
        ArgumentNullException.ThrowIfNull(city);
        ArgumentNullException.ThrowIfNull(capacityProfile);

        var dailyFoodConsumption = city.CalculateDailyFoodConsumption();
        var foodDays = dailyFoodConsumption <= 0m
            ? decimal.MaxValue
            : city.Food / dailyFoodConsumption;
        var hasFoodStress = foodDays < FoodStressDaysThreshold;
        var hasGoodsShortage = city.Goods < GoodsShortageThreshold;
        var hasResourcesShortage = city.Resources < ResourcesShortageThreshold;
        var hasSecurityProblem = city.Security < LowSecurityThreshold || city.Crime >= HighCrimeThreshold;

        var demands = new List<WorkforceSectorDemand>
        {
            new(WorkforceSector.Agriculture, Scale(capacityProfile.AgricultureCapacity, hasFoodStress ? 1.00m : 0.72m), hasFoodStress ? 100 : 80),
            new(WorkforceSector.Fishing, Scale(capacityProfile.FishingCapacity, hasFoodStress ? 0.95m : 0.65m), hasFoodStress ? 95 : 70),
            new(WorkforceSector.Hunting, Scale(capacityProfile.HuntingCapacity, hasFoodStress ? 0.90m : 0.50m), hasFoodStress ? 90 : 55),
            new(WorkforceSector.Guards, Scale(capacityProfile.GuardCapacity, hasSecurityProblem ? 0.90m : 0.40m), hasSecurityProblem ? 85 : 35),
            new(WorkforceSector.ResourceGathering, Scale(capacityProfile.ResourceGatheringCapacity, hasResourcesShortage ? 0.85m : 0.55m), hasResourcesShortage ? 75 : 45),
            new(WorkforceSector.Crafting, Scale(capacityProfile.CraftingCapacity, hasGoodsShortage ? 0.85m : 0.55m), hasGoodsShortage ? 70 : 45),
            new(WorkforceSector.Trade, Scale(capacityProfile.TradeCapacity, hasFoodStress || hasGoodsShortage || hasResourcesShortage ? 0.70m : 0.45m), hasFoodStress ? 65 : 40),
            new(WorkforceSector.Maintenance, Scale(capacityProfile.MaintenanceCapacity, 0.55m), 30)
        };

        return demands
            .Where(demand => demand.DesiredWorkers > 0)
            .OrderByDescending(demand => demand.Priority)
            .ThenByDescending(demand => demand.DesiredWorkers)
            .ToList();
    }

    public static int Scale(int capacity, decimal ratio)
    {
        return (int)decimal.Round(Math.Max(0, capacity) * Math.Clamp(ratio, 0m, 1m), 0, MidpointRounding.AwayFromZero);
    }
}

public sealed record WorkforceSectorDemand(WorkforceSector Sector, int DesiredWorkers, int Priority);
