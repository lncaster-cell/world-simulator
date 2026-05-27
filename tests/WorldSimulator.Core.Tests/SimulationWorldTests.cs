using FluentAssertions;
using WorldSimulator.Core.Trade;
using WorldSimulator.Core.World;

namespace WorldSimulator.Core.Tests;

public sealed class SimulationWorldTests
{

    [Fact]
    public void DefaultWorld_HasRiviaRegion()
    {
        var world = WorldPresets.CreateDefaultWorld();

        world.Regions.Count.Should().BeGreaterThanOrEqualTo(1);
        world.Regions.Should().Contain(x => x.Id == RegionPresets.RiviaRegionId);
    }

    [Fact]
    public void DefaultWorld_SelectedRegionIsRiviaRegion()
    {
        var world = WorldPresets.CreateDefaultWorld();

        world.SelectedRegionId.Should().Be(RegionPresets.RiviaRegionId);
        world.SelectedRegion.DisplayName.Should().Be("Ривия");
        world.SelectedRegion.MapAssetId.Should().Be("rivia_region_map");
    }

    [Fact]
    public void DefaultWorld_AllMapLocationsHaveRegion()
    {
        var world = WorldPresets.CreateDefaultWorld();

        world.SettlementMapLocations.Should().OnlyContain(x => !string.IsNullOrWhiteSpace(x.RegionId));
    }

    [Fact]
    public void DefaultWorld_MapLocationRegionsExist()
    {
        var world = WorldPresets.CreateDefaultWorld();

        world.SettlementMapLocations.Should().OnlyContain(x => world.FindRegion(x.RegionId) != null);
    }

    [Fact]
    public void DefaultWorld_RiviaRegionContainsCurrentSettlements()
    {
        var world = WorldPresets.CreateDefaultWorld();

        world.SettlementMapLocations.Should().OnlyContain(x => x.RegionId == RegionPresets.RiviaRegionId);
    }

    [Fact]
    public void DefaultWorld_MapLocationsStillNormalized()
    {
        var world = WorldPresets.CreateDefaultWorld();

        world.SettlementMapLocations.Should().OnlyContain(x => x.X >= 0m && x.X <= 1m && x.Y >= 0m && x.Y <= 1m);
    }

    [Fact]
    public void DefaultWorld_AllCurrentSettlementsBelongToRivia()
    {
        var world = WorldPresets.CreateDefaultWorld();
        var ids = new[] { "gotha", "highrock", "mlynek", "wardmark", "rivenstal", "gavern", "brno", "wodenz", "thokur_rus" };

        foreach (var id in ids)
        {
            world.FindSettlementMapLocation(id, RegionPresets.RiviaRegionId).Should().NotBeNull();
        }
    }
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

    [Fact]
    public void DefaultWorld_HasSettlementMapLocations()
    {
        var world = WorldPresets.CreateDefaultWorld();

        world.SettlementMapLocations.Count.Should().Be(9);
    }

    [Fact]
    public void DefaultWorld_MapLocationsUseKnownSettlementIds()
    {
        var world = WorldPresets.CreateDefaultWorld();
        var knownIds = world.Cities.Select(c => c.Id).ToHashSet();

        world.SettlementMapLocations.Should().OnlyContain(x => knownIds.Contains(x.SettlementId));
    }

    [Fact]
    public void DefaultWorld_MapLocationCoordinatesAreNormalized()
    {
        var world = WorldPresets.CreateDefaultWorld();

        world.SettlementMapLocations.Should().OnlyContain(x => x.X >= 0m && x.X <= 1m && x.Y >= 0m && x.Y <= 1m);
    }

    [Fact]
    public void DefaultWorld_HasRivenstalCoordinates()
    {
        var world = WorldPresets.CreateDefaultWorld();
        var location = world.FindSettlementMapLocation("rivenstal");

        location.Should().NotBeNull();
        location!.X.Should().Be(0.4824m);
        location.Y.Should().Be(0.4500m);
    }

    [Fact]
    public void DefaultWorld_HasGavernCoordinates()
    {
        var world = WorldPresets.CreateDefaultWorld();
        var location = world.FindSettlementMapLocation("gavern");

        location.Should().NotBeNull();
        location!.X.Should().Be(0.5066m);
        location.Y.Should().Be(0.5963m);
    }

    [Fact]
    public void DefaultWorld_HasMlynekCoordinates()
    {
        var world = WorldPresets.CreateDefaultWorld();
        var location = world.FindSettlementMapLocation("mlynek");

        location.Should().NotBeNull();
        location!.X.Should().Be(0.2833m);
        location.Y.Should().Be(0.2487m);
    }

    [Fact]
    public void DefaultWorld_HasBrnoCoordinates()
    {
        var world = WorldPresets.CreateDefaultWorld();
        var location = world.FindSettlementMapLocation("brno");

        location.Should().NotBeNull();
        location!.X.Should().Be(0.4527m);
        location.Y.Should().Be(0.7448m);
    }

    [Fact]
    public void DefaultWorld_HasWodenzCoordinates()
    {
        var world = WorldPresets.CreateDefaultWorld();
        var location = world.FindSettlementMapLocation("wodenz");

        location.Should().NotBeNull();
        location!.X.Should().Be(0.8036m);
        location.Y.Should().Be(0.9604m);
    }

    [Fact]
    public void DefaultWorld_HasWardmarkCoordinates()
    {
        var world = WorldPresets.CreateDefaultWorld();
        var location = world.FindSettlementMapLocation("wardmark");

        location.Should().NotBeNull();
        location!.X.Should().Be(0.0380m);
        location.Y.Should().Be(0.4027m);
    }

    [Fact]
    public void DefaultWorld_HasHighrockCoordinates()
    {
        var world = WorldPresets.CreateDefaultWorld();
        var location = world.FindSettlementMapLocation("highrock");

        location.Should().NotBeNull();
        location!.X.Should().Be(0.1579m);
        location.Y.Should().Be(0.2179m);
    }

    [Fact]
    public void DefaultWorld_HasThokurRusCoordinates()
    {
        var world = WorldPresets.CreateDefaultWorld();
        var location = world.FindSettlementMapLocation("thokur_rus");

        location.Should().NotBeNull();
        location!.X.Should().Be(0.8652m);
        location.Y.Should().Be(0.4753m);
    }

    [Fact]
    public void DefaultWorld_HasGothaCoordinates()
    {
        var world = WorldPresets.CreateDefaultWorld();
        var location = world.FindSettlementMapLocation("gotha");

        location.Should().NotBeNull();
        location!.X.Should().BeGreaterThanOrEqualTo(0m).And.BeLessThanOrEqualTo(1m);
        location.Y.Should().BeGreaterThanOrEqualTo(0m).And.BeLessThanOrEqualTo(1m);
    }

    [Fact]
    public void DefaultWorld_AllCitiesHaveEconomyProfile()
    {
        var world = WorldPresets.CreateDefaultWorld();
        world.Cities.Should().OnlyContain(c => world.FindSettlementEconomyProfile(c.Id) != null);
    }

    [Fact]
    public void DefaultWorld_AllEconomyProfilesPointToKnownCityIds()
    {
        var world = WorldPresets.CreateDefaultWorld();
        var knownIds = world.Cities.Select(c => c.Id).ToHashSet();
        world.SettlementEconomyProfiles.Should().OnlyContain(x => knownIds.Contains(x.SettlementId));
    }

    [Fact]
    public void DefaultWorld_NonPortVillages_HaveNoMainlandSupplyMultiplier()
    {
        var world = WorldPresets.CreateDefaultWorld();
        var nonPortVillageIds = new[] { "mlynek", "rivenstal", "brno", "wodenz" };
        world.SettlementEconomyProfiles
            .Where(x => nonPortVillageIds.Contains(x.SettlementId))
            .Should().OnlyContain(x => x.MainlandSupplyMultiplier == 0m);
    }

    [Fact]
    public void DefaultWorld_Gotha_IsPort()
    {
        var world = WorldPresets.CreateDefaultWorld();
        world.FindSettlementEconomyProfile("gotha")!.IsPort.Should().BeTrue();
    }

    [Fact]
    public void DefaultWorld_Highrock_IsCapital()
    {
        var world = WorldPresets.CreateDefaultWorld();
        world.FindSettlementEconomyProfile("highrock")!.IsCapital.Should().BeTrue();
    }


    [Fact]
    public void DefaultWorld_HasTradeRoutes()
    {
        var world = WorldPresets.CreateDefaultWorld();
        world.TradeRoutes.Should().NotBeEmpty();
    }

    [Fact]
    public void DefaultWorld_TradeRoutes_AreValid()
    {
        var world = WorldPresets.CreateDefaultWorld();
        var knownIds = world.Cities.Select(c => c.Id).ToHashSet();
        world.TradeRoutes.Select(r => r.Id).Should().OnlyHaveUniqueItems();
        world.TradeRoutes.Should().OnlyContain(r => TradeRouteValidation.IsValidRoute(r, knownIds));
    }

    [Fact]
    public void DefaultWorld_Has_BrnoRivenstal_TradeRoute_WithNormalizedPolyline()
    {
        var world = WorldPresets.CreateDefaultWorld();
        var route = world.TradeRoutes.SingleOrDefault(x => x.Id == "brno_rivenstal_land");
        route.Should().NotBeNull();
        route!.FromSettlementId.Should().Be("brno");
        route.ToSettlementId.Should().Be("rivenstal");
        route.Points.Should().HaveCountGreaterOrEqualTo(2);
        route.Points.Should().OnlyContain(p => p.X >= 0m && p.X <= 1m && p.Y >= 0m && p.Y <= 1m);
    }
}
