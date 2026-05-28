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

    private static WorkforceLawProfile CreateFullWorkLawProfile() => new()
    {
        AdultMaleWorkRate = 1m,
        AdultFemaleWorkRate = 1m,
        ElderlyWorkRate = 0m,
        ChildLaborRate = 0m,
        GlobalWorkforceModifier = 1m
    };

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
