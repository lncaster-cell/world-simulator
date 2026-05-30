using WorldSimulator.Core.Cities;
using WorldSimulator.Core.Workforce;
using Xunit;

namespace WorldSimulator.Core.Tests;

public sealed class CityWorkforceAllocatorTests
{
    [Fact]
    public void Allocate_UsesSectorCapacitiesAndKeepsIdleWorkers()
    {
        var city = CreateCity(population: 1000, food: 1000m, resources: 500m, goods: 500m, security: 60, crime: 10);
        var capacity = new SettlementSectorCapacityProfile
        {
            SettlementId = city.Id,
            AgricultureCapacity = 50,
            FishingCapacity = 0,
            HuntingCapacity = 10,
            ResourceGatheringCapacity = 20,
            CraftingCapacity = 20,
            TradeCapacity = 10,
            GuardCapacity = 10,
            MaintenanceCapacity = 10
        };
        var lawProfile = CreateFullWorkLawProfile();

        var allocation = new CityWorkforceAllocator().Allocate(city, capacity, lawProfile);

        Assert.True(allocation.Workforce.TotalWorkers > allocation.AssignedWorkers);
        Assert.Equal(allocation.Workforce.TotalWorkers - allocation.AssignedWorkers, allocation.IdleWorkers);
        Assert.True(allocation.AgricultureWorkers <= capacity.AgricultureCapacity);
        Assert.True(allocation.CraftingWorkers <= capacity.CraftingCapacity);
        Assert.True(allocation.GuardWorkers <= capacity.GuardCapacity);
    }

    [Fact]
    public void Allocate_PrioritizesFoodSectorsDuringFoodStress()
    {
        var stableCity = CreateCity(population: 400, food: 1000m, resources: 500m, goods: 500m, security: 60, crime: 10);
        var hungryCity = CreateCity(population: 400, food: 50m, resources: 500m, goods: 500m, security: 60, crime: 10);
        var capacity = CreateBroadCapacity(stableCity.Id);
        var lawProfile = CreateFullWorkLawProfile();
        var allocator = new CityWorkforceAllocator();

        var stableAllocation = allocator.Allocate(stableCity, capacity, lawProfile);
        var hungryAllocation = allocator.Allocate(hungryCity, capacity, lawProfile);

        Assert.True(hungryAllocation.AgricultureWorkers >= stableAllocation.AgricultureWorkers);
        Assert.True(hungryAllocation.HuntingWorkers >= stableAllocation.HuntingWorkers);
    }

    [Fact]
    public void Allocate_PrioritizesGuardsDuringSecurityProblem()
    {
        var stableCity = CreateCity(population: 400, food: 1000m, resources: 500m, goods: 500m, security: 70, crime: 10);
        var unsafeCity = CreateCity(population: 400, food: 1000m, resources: 500m, goods: 500m, security: 20, crime: 70);
        var capacity = CreateBroadCapacity(stableCity.Id, guardCapacity: 120);
        var lawProfile = CreateFullWorkLawProfile();
        var allocator = new CityWorkforceAllocator();

        var stableAllocation = allocator.Allocate(stableCity, capacity, lawProfile);
        var unsafeAllocation = allocator.Allocate(unsafeCity, capacity, lawProfile);

        Assert.True(unsafeAllocation.GuardWorkers >= stableAllocation.GuardWorkers);
    }

    [Fact]
    public void Allocate_WithExistingAllocation_LimitsDailyReallocation()
    {
        var city = CreateCity(population: 400, food: 1000m, resources: 500m, goods: 500m, security: 70, crime: 10);
        var capacity = CreateBroadCapacity(city.Id, guardCapacity: 160);
        var lawProfile = CreateFullWorkLawProfile();
        var allocator = new CityWorkforceAllocator(
            new WorkforceCalculator(),
            new WorkforceReallocationSettings
            {
                MinimumDailyReallocationWorkers = 2,
                MaximumDailyReallocationShare = 0.05m
            });

        var stableAllocation = allocator.Allocate(city, capacity, lawProfile);
        city.SetWorkforceAllocation(stableAllocation);
        city.Security = 10;
        city.Crime = 90;

        var unsafeAllocation = allocator.Allocate(city, capacity, lawProfile);
        var reallocationLimit = new WorkforceReallocationSettings
        {
            MinimumDailyReallocationWorkers = 2,
            MaximumDailyReallocationShare = 0.05m
        }.CalculateDailyReallocationLimit(stableAllocation.Workforce.TotalWorkers);

        Assert.True(unsafeAllocation.GuardWorkers > stableAllocation.GuardWorkers);
        Assert.True(unsafeAllocation.GuardWorkers - stableAllocation.GuardWorkers <= reallocationLimit);
    }

    [Fact]
    public void Allocate_WithoutExistingAllocation_CanUseDesiredAllocationImmediately()
    {
        var city = CreateCity(population: 400, food: 20m, resources: 500m, goods: 500m, security: 60, crime: 10);
        var capacity = CreateBroadCapacity(city.Id);
        var lawProfile = CreateFullWorkLawProfile();

        var allocation = new CityWorkforceAllocator().Allocate(city, capacity, lawProfile);

        Assert.True(allocation.AgricultureWorkers > 0);
        Assert.True(allocation.AssignedWorkers > 0);
    }

    [Fact]
    public void DemandPolicy_BuildDemands_PrioritizesFoodSectorsDuringFoodStress()
    {
        var stableCity = CreateCity(population: 400, food: 1000m, resources: 500m, goods: 500m, security: 60, crime: 10);
        var hungryCity = CreateCity(population: 400, food: 20m, resources: 500m, goods: 500m, security: 60, crime: 10);
        var capacity = CreateBroadCapacity(stableCity.Id);
        var policy = new WorkforceSectorDemandPolicy();

        var stableAgricultureDemand = policy.BuildDemands(stableCity, capacity)
            .Single(demand => demand.Sector == WorkforceSector.Agriculture);
        var hungryAgricultureDemand = policy.BuildDemands(hungryCity, capacity)
            .Single(demand => demand.Sector == WorkforceSector.Agriculture);

        Assert.True(hungryAgricultureDemand.DesiredWorkers > stableAgricultureDemand.DesiredWorkers);
        Assert.True(hungryAgricultureDemand.Priority > stableAgricultureDemand.Priority);
    }

    [Fact]
    public void DemandPolicy_BuildDemands_PrioritizesGuardsDuringSecurityProblem()
    {
        var stableCity = CreateCity(population: 400, food: 1000m, resources: 500m, goods: 500m, security: 70, crime: 10);
        var unsafeCity = CreateCity(population: 400, food: 1000m, resources: 500m, goods: 500m, security: 20, crime: 70);
        var capacity = CreateBroadCapacity(stableCity.Id, guardCapacity: 120);
        var policy = new WorkforceSectorDemandPolicy();

        var stableGuardDemand = policy.BuildDemands(stableCity, capacity)
            .Single(demand => demand.Sector == WorkforceSector.Guards);
        var unsafeGuardDemand = policy.BuildDemands(unsafeCity, capacity)
            .Single(demand => demand.Sector == WorkforceSector.Guards);

        Assert.True(unsafeGuardDemand.DesiredWorkers > stableGuardDemand.DesiredWorkers);
        Assert.True(unsafeGuardDemand.Priority > stableGuardDemand.Priority);
    }

    [Fact]
    public void Balancer_WithoutExistingAllocation_AssignsWorkersByDemandPriorityAndCapacity()
    {
        var capacity = CreateBroadCapacity("test");
        var demands = new[]
        {
            new WorkforceSectorDemand(WorkforceSector.Crafting, 80, 90),
            new WorkforceSectorDemand(WorkforceSector.Agriculture, 120, 80),
            new WorkforceSectorDemand(WorkforceSector.Guards, 80, 70)
        };

        var assignments = new WorkforceAssignmentBalancer().Balance(
            currentAllocation: null,
            demands: demands,
            totalWorkers: 100,
            capacityProfile: capacity,
            reallocationSettings: new WorkforceReallocationSettings());

        Assert.Equal(80, assignments[WorkforceSector.Crafting]);
        Assert.Equal(20, assignments[WorkforceSector.Agriculture]);
        Assert.Equal(0, assignments[WorkforceSector.Guards]);
        Assert.Equal(100, assignments.Values.Sum());
    }

    [Fact]
    public void Balancer_WithExistingAllocation_LimitsDailyReallocation()
    {
        var capacity = CreateBroadCapacity("test", guardCapacity: 120);
        var currentAllocation = CreateAllocation(totalWorkers: 100, agricultureWorkers: 100, guardWorkers: 0);
        var demands = new[]
        {
            new WorkforceSectorDemand(WorkforceSector.Guards, 100, 100),
            new WorkforceSectorDemand(WorkforceSector.Agriculture, 0, 10)
        };
        var reallocationSettings = new WorkforceReallocationSettings
        {
            MinimumDailyReallocationWorkers = 2,
            MaximumDailyReallocationShare = 0.05m
        };

        var assignments = new WorkforceAssignmentBalancer().Balance(
            currentAllocation: currentAllocation,
            demands: demands,
            totalWorkers: 100,
            capacityProfile: capacity,
            reallocationSettings: reallocationSettings);

        Assert.Equal(5, assignments[WorkforceSector.Guards]);
        Assert.Equal(95, assignments[WorkforceSector.Agriculture]);
        Assert.Equal(100, assignments.Values.Sum());
    }

    private static WorkforceLawProfile CreateFullWorkLawProfile() => new()
    {
        AdultMaleWorkRate = 1m,
        AdultFemaleWorkRate = 1m,
        ElderlyWorkRate = 0m,
        ChildLaborRate = 0m,
        GlobalWorkforceModifier = 1m
    };

    private static CityWorkforceAllocation CreateAllocation(int totalWorkers, int agricultureWorkers, int guardWorkers) => new(
        new WorkforceCalculationResult(
            Children: 0,
            AdultMen: totalWorkers / 2,
            AdultWomen: totalWorkers - totalWorkers / 2,
            Elderly: 0,
            AdultMaleWorkers: totalWorkers / 2,
            AdultFemaleWorkers: totalWorkers - totalWorkers / 2,
            ElderlyWorkers: 0m,
            ChildWorkers: 0m,
            PotentialWorkers: totalWorkers,
            GlobalWorkforceModifier: 1m,
            TotalWorkers: totalWorkers),
        agricultureWorkers,
        FishingWorkers: 0,
        HuntingWorkers: 0,
        ResourceGatheringWorkers: 0,
        CraftingWorkers: 0,
        TradeWorkers: 0,
        GuardWorkers: guardWorkers,
        MaintenanceWorkers: 0,
        IdleWorkers: Math.Max(0, totalWorkers - agricultureWorkers - guardWorkers));

    private static SettlementSectorCapacityProfile CreateBroadCapacity(string settlementId, int guardCapacity = 80) => new()
    {
        SettlementId = settlementId,
        AgricultureCapacity = 120,
        FishingCapacity = 0,
        HuntingCapacity = 60,
        ResourceGatheringCapacity = 80,
        CraftingCapacity = 80,
        TradeCapacity = 80,
        GuardCapacity = guardCapacity,
        MaintenanceCapacity = 80
    };

    private static City CreateCity(int population, decimal food, decimal resources, decimal goods, int security, int crime)
    {
        var demographics = new CityPopulationDemographics();
        demographics.RaceGroups.Add(new RacePopulationGroup
        {
            RaceId = "human",
            Children = 0,
            AdultMen = population / 2,
            AdultWomen = population - population / 2,
            Elderly = 0
        });

        return new City(
            "test",
            "Test",
            population,
            food,
            300m,
            55,
            security,
            crime,
            resources,
            goods,
            CityState.Stable,
            demographics: demographics);
    }
}
