using WorldSimulator.Core.Cities;

namespace WorldSimulator.Core.Workforce;

public sealed class CityWorkforceAllocator
{
    private readonly WorkforceCalculator _workforceCalculator;
    private readonly WorkforceSectorDemandPolicy _demandPolicy;
    private readonly WorkforceAssignmentBalancer _assignmentBalancer;
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
        : this(workforceCalculator, new WorkforceSectorDemandPolicy(), new WorkforceAssignmentBalancer(), reallocationSettings)
    {
    }

    public CityWorkforceAllocator(
        WorkforceCalculator workforceCalculator,
        WorkforceSectorDemandPolicy demandPolicy,
        WorkforceAssignmentBalancer assignmentBalancer,
        WorkforceReallocationSettings reallocationSettings)
    {
        _workforceCalculator = workforceCalculator ?? throw new ArgumentNullException(nameof(workforceCalculator));
        _demandPolicy = demandPolicy ?? throw new ArgumentNullException(nameof(demandPolicy));
        _assignmentBalancer = assignmentBalancer ?? throw new ArgumentNullException(nameof(assignmentBalancer));
        _reallocationSettings = reallocationSettings ?? throw new ArgumentNullException(nameof(reallocationSettings));
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
        var demands = _demandPolicy.BuildDemands(city, capacityProfile);
        var assignments = _assignmentBalancer.Balance(
            city.WorkforceAllocation,
            demands,
            workforce.TotalWorkers,
            capacityProfile,
            _reallocationSettings);

        return BuildAllocation(workforce, assignments);
    }

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
}
