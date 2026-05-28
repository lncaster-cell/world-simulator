using WorldSimulator.Core.World;
using Xunit;

namespace WorldSimulator.Core.Tests;

public sealed class WorldPresetEconomyProfileTests
{
    [Theory]
    [InlineData("mlynek", 48, 1.10)]
    [InlineData("brno", 42, 0.85)]
    [InlineData("wodenz", 60, 0.90)]
    public void FoodProducerSettlements_HaveSubstantialFoodProduction(string settlementId, double minimumAgriculturePotential, double minimumHuntingMultiplier)
    {
        var world = WorldPresets.CreateDefaultWorld();
        var profile = world.FindSettlementEconomyProfile(settlementId);

        Assert.NotNull(profile);
        Assert.True(profile!.AgriculturePotential >= (decimal)minimumAgriculturePotential);
        Assert.True(profile.HuntingMultiplier >= (decimal)minimumHuntingMultiplier);
    }
}
