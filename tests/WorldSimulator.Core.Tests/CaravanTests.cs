using FluentAssertions;
using WorldSimulator.Core.Trade;
using WorldSimulator.Core.World;

namespace WorldSimulator.Core.Tests;

public sealed class CaravanTests
{
    [Fact]
    public void DefaultWorld_HasCaravans()
    {
        var world = WorldPresets.CreateDefaultWorld();

        world.Caravans.Should().NotBeEmpty();
    }

    [Fact]
    public void DefaultWorld_EveryCaravanOwnerExistsInCities()
    {
        var world = WorldPresets.CreateDefaultWorld();
        var cityIds = world.Cities.Select(c => c.Id).ToHashSet();

        world.Caravans.Should().OnlyContain(c => cityIds.Contains(c.OwnerSettlementId));
    }

    [Fact]
    public void DefaultWorld_EveryCaravanHasPositiveCapacity()
    {
        var world = WorldPresets.CreateDefaultWorld();

        world.Caravans.Should().OnlyContain(c => c.Capacity > 0m);
    }

    [Fact]
    public void DefaultWorld_EveryCaravanHasPositiveRequiredWorkers()
    {
        var world = WorldPresets.CreateDefaultWorld();

        world.Caravans.Should().OnlyContain(c => c.RequiredWorkers > 0);
    }

    [Fact]
    public void DefaultWorld_CaravanIdsAreUnique()
    {
        var world = WorldPresets.CreateDefaultWorld();

        world.Caravans.Select(c => c.Id).Should().OnlyHaveUniqueItems();
    }

    [Fact]
    public void DefaultWorld_GothaHasLandAndSeaCaravans()
    {
        var world = WorldPresets.CreateDefaultWorld();
        var gothaCaravans = world.GetCaravansForSettlement("gotha");

        gothaCaravans.Should().Contain(c => c.Type == CaravanType.Land);
        gothaCaravans.Should().Contain(c => c.Type == CaravanType.Sea);
    }

    [Fact]
    public void DefaultWorld_ThokurRusHasSeaCaravan()
    {
        var world = WorldPresets.CreateDefaultWorld();
        var thokurRusCaravans = world.GetCaravansForSettlement("thokur_rus");

        thokurRusCaravans.Should().Contain(c => c.Type == CaravanType.Sea);
    }

    [Fact]
    public void DefaultWorld_VillagesHaveLimitedCaravans()
    {
        var world = WorldPresets.CreateDefaultWorld();
        var villageIds = new[] { "mlynek", "wardmark", "rivenstal", "gavern", "brno", "wodenz", "thokur_rus" };

        foreach (var villageId in villageIds)
        {
            world.GetCaravansForSettlement(villageId).Count.Should().BeLessThanOrEqualTo(2);
        }
    }
}
