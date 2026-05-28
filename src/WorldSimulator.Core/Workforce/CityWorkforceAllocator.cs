using WorldSimulator.Core.Cities;

namespace WorldSimulator.Core.Workforce;

public sealed class CityWorkforceAllocator
{
    private const decimal FoodStressDaysThreshold = 3m;
    private const decimal GoodsShortageThreshold = 80m;
    private const decimal ResourcesShortageThreshold = 80m;
    private const int LowSecurityThreshold = 35;
    private const int HighCrimeThreshold = 60;
    private const decimal DailyReassignmentRate = 0.05m;
    private const int MinimumDailyReassignments = 1;

    private readonly WorkforceCalculator _workforceCalculator;

    public CityWorkforceAllocator()
        : this(new WorkforceCalculator())
    {
    }

    public CityWorkforceAllocator(WorkforceCalculator workforceCalculator)
    {
        _workforceCalculator = workforceCalculator;
    }

    public CityWorkforceAllocation Allocate(
        City city,
        SettlementSectorCapacityProfile capacityProfile,
        WorkforceLawProfile lawProfile)
    {
        return Allocate(city, capacityProfile, lawProfile, assignmentState: null);
    }

    public CityWorkforceAllocation Allocate(
        City city,
        SettlementSectorCapacityProfile capacityProfile,
        WorkforceLawProfile lawProfile,
        CityWorkforceAssignmentState? assignmentState)
    {
        ArgumentNullException.ThrowIfNull(city);
        ArgumentNullException.ThrowIfNull(capacityProfile);
        ArgumentNullException.ThrowIfNull(lawProfile);

        var workforce = _workforceCalculator.Calculate(city.Demographics, lawProfile);
        var targetAllocation = BuildTargetAllocation(city, capacityProfile, workforce);

        if (assignmentState is null)
        {
            return targetAllocation;
        }

        if (assignmentState.TotalWorkers <= 0)
        {
            assignmentState.ReplaceWith(targetAllocation);
            return targetAllocation;
        }

        ReconcileTotalWorkerCount(assignmentState, workforce.TotalWorkers);
        MoveTowardTarget(assignmentState, targetAllocation, GetDailyReassignmentLimit(workforce.TotalWorkers));

        return ToAllocation(workforce, assignmentState);
    }

    private static CityWorkforceAllocation BuildTargetAllocation(
        City city,
        SettlementSectorCapacityProfile capacityProfile,
        WorkforceCalculationResult workforce)
    {
        var remainingWorkers = workforce.TotalWorkers;
        var assignments = new Dictionary<WorkforceSector, int>
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
            remainingWorkers);
    }

    private static void ReconcileTotalWorkerCount(CityWorkforceAssignmentState assignmentState, int totalWorkers)
    {
        var delta = totalWorkers - assignmentState.TotalWorkers;
        if (delta > 0)
        {
            assignmentState.IdleWorkers += delta;
            return;
        }

        if (delta < 0)
        {
            RemoveWorkers(assignmentState, -delta);
        }
    }

    private static void RemoveWorkers(CityWorkforceAssignmentState assignmentState, int workersToRemove)
    {
        var remaining = workersToRemove;
        foreach (var sector in RemovalOrder)
        {
            if (remaining <= 0)
            {
                break;
            }

            var current = assignmentState.GetWorkers(sector);
            var removed = Math.Min(current, remaining);
            assignmentState.SetWorkers(sector, current - removed);
            remaining -= removed;
        }
    }

    private static void MoveTowardTarget(
        CityWorkforceAssignmentState assignmentState,
        CityWorkforceAllocation targetAllocation,
        int maxMoves)
    {
        var remainingMoves = maxMoves;
        foreach (var targetSector in TargetPriorityOrder)
        {
            if (remainingMoves <= 0)
            {
                break;
            }

            var targetWorkers = GetWorkers(targetAllocation, targetSector);
            var currentWorkers = assignmentState.GetWorkers(targetSector);
            var deficit = targetWorkers - currentWorkers;
            if (deficit <= 0)
            {
                continue;
            }

            remainingMoves -= MoveIntoSector(assignmentState, targetSector, deficit, remainingMoves, targetAllocation);
        }
    }

    private static int MoveIntoSector(
        CityWorkforceAssignmentState assignmentState,
        WorkforceSector targetSector,
        int deficit,
        int maxMoves,
        CityWorkforceAllocation targetAllocation)
    {
        var movedTotal = 0;
        foreach (var sourceSector in SourcePriorityOrder)
        {
            if (movedTotal >= maxMoves || movedTotal >= deficit)
            {
                break;
            }

            if (sourceSector == targetSector)
            {
                continue;
            }

            var currentSourceWorkers = assignmentState.GetWorkers(sourceSector);
            var targetSourceWorkers = GetWorkers(targetAllocation, sourceSector);
            var surplus = currentSourceWorkers - targetSourceWorkers;
            if (surplus <= 0)
            {
                continue;
            }

            var movable = Math.Min(Math.Min(surplus, deficit - movedTotal), maxMoves - movedTotal);
            assignmentState.SetWorkers(sourceSector, currentSourceWorkers - movable);
            assignmentState.SetWorkers(targetSector, assignmentState.GetWorkers(targetSector) + movable);
            movedTotal += movable;
        }

        return movedTotal;
    }

    private static CityWorkforceAllocation ToAllocation(WorkforceCalculationResult workforce, CityWorkforceAssignmentState assignmentState)
    {
        return new CityWorkforceAllocation(
            workforce,
            assignmentState.AgricultureWorkers,
            assignmentState.FishingWorkers,
            assignmentState.HuntingWorkers,
            assignmentState.ResourceGatheringWorkers,
            assignmentState.CraftingWorkers,
            assignmentState.TradeWorkers,
            assignmentState.GuardWorkers,
            assignmentState.MaintenanceWorkers,
            assignmentState.IdleWorkers);
    }

    private static int GetWorkers(CityWorkforceAllocation allocation, WorkforceSector sector) => sector switch
    {
        WorkforceSector.Agriculture => allocation.AgricultureWorkers,
        WorkforceSector.Fishing => allocation.FishingWorkers,
        WorkforceSector.Hunting => allocation.HuntingWorkers,
        WorkforceSector.ResourceGathering => allocation.ResourceGatheringWorkers,
        WorkforceSector.Crafting => allocation.CraftingWorkers,
        WorkforceSector.Trade => allocation.TradeWorkers,
        WorkforceSector.Guards => allocation.GuardWorkers,
        WorkforceSector.Maintenance => allocation.MaintenanceWorkers,
        WorkforceSector.Idle => allocation.IdleWorkers,
        _ => throw new ArgumentOutOfRangeException(nameof(sector), sector, "Unsupported workforce sector.")
    };

    private static int GetDailyReassignmentLimit(int totalWorkers)
    {
        if (totalWorkers <= 0)
        {
            return 0;
        }

        return Math.Max(MinimumDailyReassignments, (int)decimal.Ceiling(totalWorkers * DailyReassignmentRate));
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

        return
        [
            new WorkforceSectorDemand(WorkforceSector.Agriculture, Scale(capacityProfile.AgricultureCapacity, hasFoodStress ? 1.00m : 0.72m), hasFoodStress ? 100 : 80),
            new WorkforceSectorDemand(WorkforceSector.Fishing, Scale(capacityProfile.FishingCapacity, hasFoodStress ? 0.95m : 0.65m), hasFoodStress ? 95 : 70),
            new WorkforceSectorDemand(WorkforceSector.Hunting, Scale(capacityProfile.HuntingCapacity, hasFoodStress ? 0.90m : 0.50m), hasFoodStress ? 90 : 55),
            new WorkforceSectorDemand(WorkforceSector.Guards, Scale(capacityProfile.GuardCapacity, hasSecurityProblem ? 0.90m : 0.40m), hasSecurityProblem ? 85 : 35),
            new WorkforceSectorDemand(WorkforceSector.ResourceGathering, Scale(capacityProfile.ResourceGatheringCapacity, hasResourcesShortage ? 0.85m : 0.55m), hasResourcesShortage ? 75 : 45),
            new WorkforceSectorDemand(WorkforceSector.Crafting, Scale(capacityProfile.CraftingCapacity, hasGoodsShortage ? 0.85m : 0.55m), hasGoodsShortage ? 70 : 45),
            new WorkforceSectorDemand(WorkforceSector.Trade, Scale(capacityProfile.TradeCapacity, hasFoodStress || hasGoodsShortage || hasResourcesShortage ? 0.70m : 0.45m), hasFoodStress ? 65 : 40),
            new WorkforceSectorDemand(WorkforceSector.Maintenance, Scale(capacityProfile.MaintenanceCapacity, 0.55m), 30)
        ]
        .Where(demand => demand.DesiredWorkers > 0)
        .OrderByDescending(demand => demand.Priority)
        .ThenByDescending(demand => demand.DesiredWorkers)
        .ToList();
    }

    private static int Scale(int capacity, decimal ratio)
    {
        return (int)decimal.Round(Math.Max(0, capacity) * Math.Clamp(ratio, 0m, 1m), 0, MidpointRounding.AwayFromZero);
    }

    private static readonly WorkforceSector[] TargetPriorityOrder =
    [
        WorkforceSector.Agriculture,
        WorkforceSector.Fishing,
        WorkforceSector.Hunting,
        WorkforceSector.Guards,
        WorkforceSector.ResourceGathering,
        WorkforceSector.Crafting,
        WorkforceSector.Trade,
        WorkforceSector.Maintenance
    ];

    private static readonly WorkforceSector[] SourcePriorityOrder =
    [
        WorkforceSector.Idle,
        WorkforceSector.Maintenance,
        WorkforceSector.Trade,
        WorkforceSector.Crafting,
        WorkforceSector.ResourceGathering,
        WorkforceSector.Hunting,
        WorkforceSector.Fishing,
        WorkforceSector.Guards,
        WorkforceSector.Agriculture
    ];

    private static readonly WorkforceSector[] RemovalOrder =
    [
        WorkforceSector.Idle,
        WorkforceSector.Maintenance,
        WorkforceSector.Trade,
        WorkforceSector.Crafting,
        WorkforceSector.ResourceGathering,
        WorkforceSector.Hunting,
        WorkforceSector.Fishing,
        WorkforceSector.Guards,
        WorkforceSector.Agriculture
    ];

    private sealed record WorkforceSectorDemand(WorkforceSector Sector, int DesiredWorkers, int Priority);
}
