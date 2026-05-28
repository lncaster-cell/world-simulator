using WorldSimulator.Core.Workforce;
using WorldSimulator.Core.World;
using Xunit;

namespace WorldSimulator.Core.Tests;

public sealed class SettlementSectorCapacityProfileTests
{
    [Fact]
    public void DefaultWorld_IncludesSectorCapacityProfilesForEveryCity()
    {
        var world = WorldPresets.CreateDefaultWorld();

        Assert.Equal(world.Cities.Count, world.SettlementSectorCapacityProfiles.Count);
        foreach (var city in world.Cities)
        {
            Assert.NotNull(world.FindSettlementSectorCapacityProfile(city.Id));
        }
    }

    [Theory]
    [InlineData(RiviaSettlementPresets.MlynekId, 150)]
    [InlineData(RiviaSettlementPresets.BrnoId, 110)]
    [InlineData(RiviaSettlementPresets.WodenzId, 180)]
    public void FertileSettlements_HaveHighAgricultureCapacity(string settlementId, int expectedMinimumCapacity)
    {
        var world = WorldPresets.CreateDefaultWorld();
        var profile = world.FindSettlementSectorCapacityProfile(settlementId);

        Assert.NotNull(profile);
        Assert.True(profile!.AgricultureCapacity >= expectedMinimumCapacity);
    }

    [Theory]
    [InlineData(RiviaSettlementPresets.GothaId, 18)]
    [InlineData(RiviaSettlementPresets.HighrockId, 12)]
    public void NonAgrarianSettlements_HaveLowAgricultureCapacity(string settlementId, int expectedMaximumCapacity)
    {
        var world = WorldPresets.CreateDefaultWorld();
        var profile = world.FindSettlementSectorCapacityProfile(settlementId);

        Assert.NotNull(profile);
        Assert.True(profile!.AgricultureCapacity <= expectedMaximumCapacity);
    }

    [Fact]
    public void GetCapacity_ReturnsIdleAsUnbounded()
    {
        var profile = new SettlementSectorCapacityProfile
        {
            SettlementId = "test",
            AgricultureCapacity = 1,
            FishingCapacity = 2,
            HuntingCapacity = 3,
            ResourceGatheringCapacity = 4,
            CraftingCapacity = 5,
            TradeCapacity = 6,
            GuardCapacity = 7,
            MaintenanceCapacity = 8
        };

        Assert.Equal(int.MaxValue, profile.GetCapacity(WorkforceSector.Idle));
    }
}
