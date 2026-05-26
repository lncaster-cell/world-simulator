using FluentAssertions;
using WorldSimulator.Core.World;

namespace WorldSimulator.Core.Tests;

public sealed class SimulationWorldTests
{
    [Fact]
    public void DefaultWorld_ContainsAllRegistrySettlements()
    {
        var world = WorldPresets.CreateDefaultWorld();

        world.Cities.Count.Should().Be(9);
    }

    [Fact]
    public void DefaultWorld_SelectedCityIsGotha()
    {
        var world = WorldPresets.CreateDefaultWorld();

        world.SelectedCityId.Should().Be("gotha");
        world.SelectedCity.Id.Should().Be("gotha");
    }

    [Fact]
    public void DefaultWorld_CityIdsAreUnique()
    {
        var world = WorldPresets.CreateDefaultWorld();

        world.Cities.Select(c => c.Id).Should().OnlyHaveUniqueItems();
    }

    [Fact]
    public void DefaultWorld_AllCitiesHavePopulation()
    {
        var world = WorldPresets.CreateDefaultWorld();

        world.Cities.Should().OnlyContain(c => c.Population > 0);
    }

    [Fact]
    public void DefaultWorld_AllCitiesHaveCrimeAtLeastOne()
    {
        var world = WorldPresets.CreateDefaultWorld();

        world.Cities.Should().OnlyContain(c => c.Crime >= 1);
    }

    [Fact]
    public void DefaultWorld_ContainsCanonicalDisplayNames()
    {
        var world = WorldPresets.CreateDefaultWorld();

        world.Cities.Select(c => c.Name).Should().Contain(["Wödenz", "Thökur-Rus"]);
    }
}
