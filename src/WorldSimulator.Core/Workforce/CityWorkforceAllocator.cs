using WorldSimulator.Core.Cities;

namespace WorldSimulator.Core.Workforce;

public sealed class CityWorkforceAllocator
{
    private const decimal FoodStressDaysThreshold = 3m;
    private const decimal GoodsShortageThreshold = 80m;
    private const decimal ResourcesShortageThreshold = 80m;
    private const int LowSecurityThreshold = 35;
    private const int HighCrimeThreshold = 60;

    private readonly WorkforceCalculator _workforceCalculator;
    private readonly WorkforceReallocationSettings _reallocationSettings;

    public CityWorkforceAllocator()
        : this(new WorkforceCalculator(), new WorkforceReallocationSettings())
    {
    }

    public CityWorkforceAllocator(WorkforceCalculator workforceCalculator)
        : this(workforceCalculator, new WorkforceReallocationSettings())
    {
    }

    public CityWorkforceAllocator(WorkforceCalculator workforceCalculator, WorkforceReallocationSettings reallocationSettings)
    {
        _workforceCalculator = workforceCalculator;
        _reallocationSettings = reallocationSettings;
    }

    public CityWorkforceAllocation Allocate(
        City city,
        SettlementSectorCapacityProfile capacityProfile,
        WorkforceLawProfile lawProfile)
    {
        ArgumentNullException.ThrowIfNull(city);
        ArgumentNullException.ThrowIfNull(capacityProfile);
        ArgumentNullException.ThrowIfNull(lawProfile);

        var workforce = _workforceCalculator.Calculate(city.Demographics, lawProfile);
        var desiredAssignments = BuildDesiredAssignments(city, capacityProfile, workforce);
        var assignments = city.WorkforceAllocation is null
            ? desiredAssignments
            : SmoothAssignments(city.WorkforceAllocation, desiredAssignments, workforce.TotalWorkers, capacityProfile);

        return BuildAllocation(workforce, assignments);
    }

    private static Dictionary<WorkforceSector, int> BuildDesiredAssignments(
        City city,
        SettlementSectorCapacityProfile capacityProfile,
        WorkforceCalculationResult workforce)
    {
        var remainingWorkers = workforce.TotalWorkers;
        var assignments = CreateEmptyAssignments();

        foreach (var demand in BuildDemands(city, capacityProfile))
        {
            if (remainingWorkers <= 0)
            {
                break;
            }

            var alreadyAssigned = assignments[demand.Sector];
            var capacityLeft = Math.Max(0, capacityProfile.GetCapacity(demand.Sector) - alreadyAssigned);
            var desiredWorkers = Math.Max(0, demand.DesiredWorkers - alreadyAssigned);
            var assigned = Math.Min(Math.Min(capacityLeft, desiredWorkers), remainingWorkers);

            assignments[demand.Sector] += assigned;
            remainingWorkers -= assigned;
        }

        return assignments;
    }

    private Dictionary<WorkforceSector, int> SmoothAssignments(
        CityWorkforceAllocation currentAllocation,
        Dictionary<WorkforceSector, int> desiredAssignments,
        int totalWorkers,
        SettlementSectorCapacityProfile capacityProfile)
    {
        var assignments = ToDictionary(currentAllocation);
        ReduceAssignmentsToTotal(assignments, totalWorkers);

        var reallocationLimit = _reallocationSettings.CalculateDailyReallocationLimit(totalWorkers);
        for (var i = 0; i < reallocationLimit; i++)
        {
            var target = FindBestTargetSector(assignments, desiredAssignments, capacityProfile);
            if (target is null)
            {
                break;
            }

            var source = FindBestSourceSector(assignments, desiredAssignments);
            if (source is not null)
            {
                assignments[source.Value]--;
                assignments[target.Value]++;
                continue;
            }

            if (assignments.Values.Sum() >= totalWorkers)
            {
                break;
            }

            assignments[target.Value]++;
        }

        return assignments;
    }

    private static void ReduceAssignmentsToTotal(Dictionary<WorkforceSector, int> assignments, int totalWorkers)
    {
        var assignedWorkers = assignments.Values.Sum();
        while (assignedWorkers > totalWorkers)
        {
            var source = assignments
                .Where(pair => pair.Value > 0)
                .OrderByDescending(pair => pair.Value)
                .First().Key;
            assignments[source]--;
            assignedWorkers--;
        }
    }

    private static WorkforceSector? FindBestSourceSector(
        IReadOnlyDictionary<WorkforceSector, int> assignments,
        IReadOnlyDictionary<WorkforceSector, int> desiredAssignments)
    {
        return assignments
            .Where(pair => pair.Value > desiredAssignments[pair.Key])
            .OrderByDescending(pair => pair.Value - desiredAssignments[pair.Key])
            .Select(pair => (WorkforceSector?)pair.Key)
            .FirstOrDefault();
    }

    private static WorkforceSector? FindBestTargetSector(
        IReadOnlyDictionary<WorkforceSector, int> assignments,
        IReadOnlyDictionary<WorkforceSector, int> desiredAssignments,
        SettlementSectorCapacityProfile capacityProfile)
    {
        return assignments
            .Where(pair => pair.Value < desiredAssignments[pair.Key])
            .Where(pair => pair.Value < capacityProfile.GetCapacity(pair.Key))
            .OrderByDescending(pair => desiredAssignments[pair.Key] - pair.Value)
            .Select(pair => (WorkforceSector?)pair.Key)
            .FirstOrDefault();
    }

    private static Dictionary<WorkforceSector, int> CreateEmptyAssignments() => new()
    {
        [WorkforceSector.Agriculture] = 0,
        [WorkforceSector.Fishing] = 0,
        [WorkforceSector.Hunting] = 0,
        [WorkforceSector.ResourceGathering] = 0,
        [WorkforceSector.Crafting] = 0,
        [WorkforceSector.Trade] = 0,
        [WorkforceSector.Guards] = 0,
        [WorkforceSector.Maintenance] = 0
    };

    private static Dictionary<WorkforceSector, int> ToDictionary(CityWorkforceAllocation allocation) => new()
    {
        [WorkforceSector.Agriculture] = allocation.AgricultureWorkers,
        [WorkforceSector.Fishing] = allocation.FishingWorkers,
        [WorkforceSector.Hunting] = allocation.HuntingWorkers,
        [WorkforceSector.ResourceGathering] = allocation.ResourceGatheringWorkers,
        [WorkforceSector.Crafting] = allocation.CraftingWorkers,
        [WorkforceSector.Trade] = allocation.TradeWorkers,
        [WorkforceSector.Guards] = allocation.GuardWorkers,
        [WorkforceSector.Maintenance] = allocation.MaintenanceWorkers
    };

    private static CityWorkforceAllocation BuildAllocation(WorkforceCalculationResult workforce, IReadOnlyDictionary<WorkforceSector, int> assignments)
    {
        var assignedWorkers = assignments.Values.Sum();
        return new CityWorkforceAllocation(
            workforce,
            assignments[WorkforceSector.Agriculture],
            assignments[WorkforceSector.Fishing],
            assignments[WorkforceSector.Hunting],
            assignments[WorkforceSector.ResourceGathering],
            assignments[WorkforceSector.Crafting],
            assignments[WorkforceSector.Trade],
            assignments[WorkforceSector.Guards],
            assignments[WorkforceSector.Maintenance],
            Math.Max(0, workforce.TotalWorkers - assignedWorkers));
    }

    private static IReadOnlyList<WorkforceSectorDemand> BuildDemands(City city, SettlementSectorCapacityProfile capacityProfile)
    {
        var foodDays = city.CalculateDailyFoodConsumption() <= 0m
            ? decimal.MaxValue
            : city.Food / city.CalculateDailyFoodConsumption();
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

    private static int Scale(int capacity, decimal ratio)
    {
        return (int)decimal.Round(Math.Max(0, capacity) * Math.Clamp(ratio, 0m, 1m), 0, MidpointRounding.AwayFromZero);
    }

    private sealed record WorkforceSectorDemand(WorkforceSector Sector, int DesiredWorkers, int Priority);
}
