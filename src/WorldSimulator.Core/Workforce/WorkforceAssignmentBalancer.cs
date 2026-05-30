namespace WorldSimulator.Core.Workforce;

public sealed class WorkforceAssignmentBalancer
{
    public Dictionary<WorkforceSector, int> Balance(
        CityWorkforceAllocation? currentAllocation,
        IReadOnlyList<WorkforceSectorDemand> demands,
        int totalWorkers,
        SettlementSectorCapacityProfile capacityProfile,
        WorkforceReallocationSettings reallocationSettings)
    {
        ArgumentNullException.ThrowIfNull(demands);
        ArgumentNullException.ThrowIfNull(capacityProfile);
        ArgumentNullException.ThrowIfNull(reallocationSettings);

        var desiredAssignments = BuildDesiredAssignments(demands, totalWorkers, capacityProfile);
        return currentAllocation is null
            ? desiredAssignments
            : SmoothAssignments(currentAllocation, desiredAssignments, totalWorkers, capacityProfile, reallocationSettings);
    }

    public static Dictionary<WorkforceSector, int> BuildDesiredAssignments(
        IReadOnlyList<WorkforceSectorDemand> demands,
        int totalWorkers,
        SettlementSectorCapacityProfile capacityProfile)
    {
        ArgumentNullException.ThrowIfNull(demands);
        ArgumentNullException.ThrowIfNull(capacityProfile);

        var remainingWorkers = totalWorkers;
        var assignments = CreateEmptyAssignments();

        foreach (var demand in demands)
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

    private static Dictionary<WorkforceSector, int> SmoothAssignments(
        CityWorkforceAllocation currentAllocation,
        Dictionary<WorkforceSector, int> desiredAssignments,
        int totalWorkers,
        SettlementSectorCapacityProfile capacityProfile,
        WorkforceReallocationSettings reallocationSettings)
    {
        var assignments = ToDictionary(currentAllocation);
        ReduceAssignmentsToTotal(assignments, totalWorkers);

        var reallocationLimit = reallocationSettings.CalculateDailyReallocationLimit(totalWorkers);
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
}
