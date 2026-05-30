using WorldSimulator.Core.Cities;
using WorldSimulator.Core.Time;
using WorldSimulator.Core.Trade;
using WorldSimulator.Core.World;
using WorldSimulator.Persistence.Saves;
using Xunit;
using static WorldSimulator.Persistence.Tests.WorldSaveTestHelpers;

namespace WorldSimulator.Persistence.Tests;

public sealed class JsonWorldSaveServiceRoundtripTests
{
    [Fact]
    public async Task LoadAsync_RoundTrip_Preserves_Current_World_State()
    {
        var service = new JsonWorldSaveService();
        var world = WorldPresets.CreateDefaultWorld();
        var clock = new SimulationClock(new SimulationTimeSettings { RealTimePerGameHour = TimeSpan.FromSeconds(3) });
        clock.RestoreState(9, 18, true, TimeSpan.FromSeconds(11), TimeSpan.FromSeconds(3));
        var eventState = CreateEventStateWithEvents(world.SelectedCityId);

        world.Cities[0].Food = 1234m;
        world.Cities[0].Infrastructure.HousingLevel = 2;
        world.Cities[0].Infrastructure.UrbanLevel = 3;
        world.Cities[0].Infrastructure.ProductionLevel = 4;
        world.Cities[0].Infrastructure.MilitaryLevel = 5;
        world.Cities[0].Demographics.ReplaceWith([
            new RacePopulationGroup
            {
                RaceId = "human",
                Children = 80,
                AdultMen = 150,
                AdultWomen = 160,
                Elderly = 30
            }
        ]);
        world.Cities[1].Mood = 73;
        world.SelectedCityId = world.Cities[0].Id;
        world.SelectedRegionId = world.Regions[0].Id;
        world.TradeShipments.Add(new TradeShipment
        {
            Id = "s1",
            CaravanId = world.Caravans[0].Id,
            RouteId = world.TradeRoutes[0].Id,
            FromSettlementId = world.TradeRoutes[0].FromSettlementId,
            ToSettlementId = world.TradeRoutes[0].ToSettlementId,
            GoodType = TradeGoodType.Food,
            Amount = 10m,
            DepartureDay = 7,
            ArrivalDay = 10,
            ReturnDay = 13,
            ExporterWealthDelta = 0.2m,
            ImporterWealthDelta = -0.2m,
            Status = TradeShipmentStatus.InTransitToDestination
        });

        var filePath = TempFile();
        try
        {
            await service.SaveAsync(filePath, world, clock, eventState);
            var loaded = await service.LoadAsync(filePath);

            Assert.Equal(9, loaded.Clock.Day);
            Assert.Equal(18, loaded.Clock.Hour);
            Assert.True(loaded.Clock.IsRunning);
            Assert.Equal(world.Cities.Count, loaded.World.Cities.Count);
            Assert.Equal(1234m, loaded.World.Cities[0].Food);
            Assert.Equal(73, loaded.World.Cities[1].Mood);
            Assert.Equal(world.SelectedCityId, loaded.World.SelectedCityId);
            Assert.Equal(world.SelectedRegionId, loaded.World.SelectedRegionId);
            Assert.Equal(world.Caravans.Count, loaded.World.Caravans.Count);
            Assert.Equal(world.Caravans[0].PurchaseCost, loaded.World.Caravans[0].PurchaseCost);
            Assert.Equal(world.Caravans[0].UpkeepPerWeek, loaded.World.Caravans[0].UpkeepPerWeek);
            Assert.Equal(world.TradeRoutes.Count, loaded.World.TradeRoutes.Count);
            Assert.Equal(world.SettlementSectorCapacityProfiles.Count, loaded.World.SettlementSectorCapacityProfiles.Count);
            Assert.Single(loaded.World.TradeShipments);

            var loadedInfrastructure = loaded.World.Cities[0].Infrastructure;
            Assert.Equal(2, loadedInfrastructure.HousingLevel);
            Assert.Equal(3, loadedInfrastructure.UrbanLevel);
            Assert.Equal(4, loadedInfrastructure.ProductionLevel);
            Assert.Equal(5, loadedInfrastructure.MilitaryLevel);

            var loadedDemographics = loaded.World.Cities[0].Demographics;
            Assert.Equal(420, loadedDemographics.TotalPopulation);
            Assert.Single(loadedDemographics.RaceGroups);
            Assert.Equal("human", loadedDemographics.RaceGroups[0].RaceId);
            Assert.Equal(150, loadedDemographics.RaceGroups[0].AdultMen);
            Assert.Equal(160, loadedDemographics.RaceGroups[0].AdultWomen);

            var selectedManager = loaded.EventState.GetManagerOrEmpty(loaded.World.SelectedCityId);
            Assert.Single(selectedManager.ActiveEvents);
            Assert.Single(selectedManager.CompletedEvents);
        }
        finally { Cleanup(filePath); }
    }
}
