using FluentAssertions;
using WorldSimulator.Core.Cities;
using WorldSimulator.Core.Trade;
using WorldSimulator.Core.World;

namespace WorldSimulator.Core.Tests;

public sealed class CaravanHiringServiceTests
{
    [Fact]
    public void SeaPreset_HasHigherCapacityAndCostThanLand() { CaravanPresets.SeaCapacity.Should().BeGreaterThan(CaravanPresets.LandCapacity); CaravanPresets.SeaPurchaseCost.Should().BeGreaterThan(CaravanPresets.LandPurchaseCost); }

    [Fact]
    public void Hire_AtMostOnePerCityPerCycle()
    {
        var world = BuildWorld(port:true, seaDemand:true);
        var result = new CaravanHiringService().EvaluateAndHire(world);
        result.Settlements.Count(x => x.WasHired && x.SettlementId == "a").Should().BeLessOrEqualTo(1);
    }

    [Fact]
    public void NonPort_DoesNotHireSea()
    {
        var world = BuildWorld(port:false, seaDemand:true);
        new CaravanHiringService().EvaluateAndHire(world);
        world.Caravans.Should().NotContain(x => x.OwnerSettlementId == "a" && x.Type == CaravanType.Sea);
    }

    private static SimulationWorld BuildWorld(bool port, bool seaDemand)
    {
        var a = new City("a","a",1000,500m,1000m,50,50,10,0m,0m,CityState.Growing);
        var b = new City("b","b",1000,0m,1000m,50,50,10,0m,0m,CityState.Growing);
        return new SimulationWorld{Cities=[a,b],Regions=[],SettlementMapLocations=[],SettlementEconomyProfiles=[new SettlementEconomyProfile{SettlementId="a",IsPort=port},new SettlementEconomyProfile{SettlementId="b",IsPort=true}],Caravans=[],TradeShipments=[],TradeRoutes=[new TradeRoute{Id="a_b_land",FromSettlementId="a",ToSettlementId="b",Type=CaravanType.Land,IsEnabled=true,TravelDays=2,Distance=10m,Points=[new RoutePoint{X=0,Y=0},new RoutePoint{X=1,Y=1}]},new TradeRoute{Id="a_b_sea",FromSettlementId="a",ToSettlementId="b",Type=CaravanType.Sea,IsEnabled=seaDemand,TravelDays=2,Distance=10m,Points=[new RoutePoint{X=0,Y=0},new RoutePoint{X=1,Y=1}]}],SelectedCityId="a",SelectedRegionId="r"};
    }
}
