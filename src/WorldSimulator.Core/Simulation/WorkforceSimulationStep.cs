using WorldSimulator.Core.Cities;
using WorldSimulator.Core.Workforce;
using WorldSimulator.Core.World;

namespace WorldSimulator.Core.Simulation;

public sealed class WorkforceSimulationStep : IWorldSimulationStep
{
    private readonly CityWorkforceAllocator _allocator;
    private readonly WorkforceLawProfile _defaultLawProfile;

    public WorkforceSimulationStep(CityWorkforceAllocator allocator, WorkforceLawProfile defaultLawProfile)
    {
        _allocator = allocator;
        _defaultLawProfile = defaultLawProfile;
    }

    public void Execute(SimulationWorld world, City city, int day, WorldSimulationContext context, WorldSimulationStepDelegate next)
    {
        var state = context.GetCityState(city);
        var capacityProfile = world.FindSettlementSectorCapacityProfile(city.Id)
            ?? throw new InvalidOperationException($"Settlement '{city.Id}' is missing a workforce sector capacity profile.");

        state.WorkforceAllocation = _allocator.Allocate(city, capacityProfile, _defaultLawProfile, city.WorkforceAssignments);

        next();
    }
}
