using System.Text.Json;
using WorldSimulator.Core.Cities;
using WorldSimulator.Core.Events;
using WorldSimulator.Core.Time;
using WorldSimulator.Core.Trade;
using WorldSimulator.Core.World;
using WorldSimulator.Persistence.Saves;
using Xunit;

namespace WorldSimulator.Persistence.Tests;

public sealed class JsonWorldSaveServiceTests
{
    [Fact]
    public async Task SaveLoad_Preserves_Current_World_State()
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
            Assert.Single(loaded.World.TradeShipments);

            var loadedInfrastructure = loaded.World.Cities[0].Infrastructure;
            Assert.Equal(2, loadedInfrastructure.HousingLevel);
            Assert.Equal(3, loadedInfrastructure.UrbanLevel);
            Assert.Equal(4, loadedInfrastructure.ProductionLevel);
            Assert.Equal(5, loadedInfrastructure.MilitaryLevel);

            var selectedManager = loaded.EventState.GetManagerOrEmpty(loaded.World.SelectedCityId);
            Assert.Single(selectedManager.ActiveEvents);
            Assert.Single(selectedManager.CompletedEvents);
        }
        finally { Cleanup(filePath); }
    }

    [Fact]
    public async Task SaveAsync_Writes_Current_Save_Version()
    {
        var service = new JsonWorldSaveService();
        var world = WorldPresets.CreateDefaultWorld();
        var filePath = TempFile();
        try
        {
            await service.SaveAsync(filePath, world, new SimulationClock(), new WorldEventState());
            var json = await File.ReadAllTextAsync(filePath);
            using var document = JsonDocument.Parse(json);
            Assert.Equal(3, document.RootElement.GetProperty("Version").GetInt32());
        }
        finally { Cleanup(filePath); }
    }

    [Fact]
    public async Task LoadAsync_Unsupported_Save_Version_Throws_InvalidDataException()
    {
        var service = new JsonWorldSaveService();
        var filePath = TempFile();
        var save = new
        {
            Version = 2,
            SavedAtUtc = DateTime.UtcNow,
            Clock = new { Day = 1, Hour = 0, IsRunning = false, AccumulatedRealTime = "00:00:00", RealTimePerGameHour = "00:05:00" },
            World = new { Cities = Array.Empty<object>(), Regions = Array.Empty<object>() }
        };

        try
        {
            await File.WriteAllTextAsync(filePath, JsonSerializer.Serialize(save));
            await Assert.ThrowsAsync<InvalidDataException>(() => service.LoadAsync(filePath));
        }
        finally { Cleanup(filePath); }
    }

    [Fact]
    public async Task LoadAsync_Missing_Infrastructure_Throws_InvalidDataException()
    {
        var service = new JsonWorldSaveService();
        var filePath = TempFile();
        var save = new
        {
            Version = 3,
            SavedAtUtc = DateTime.UtcNow,
            Clock = new { Day = 1, Hour = 0, IsRunning = false, AccumulatedRealTime = "00:00:00", RealTimePerGameHour = "00:05:00" },
            World = new
            {
                Cities = new[] { new { Id = "gotha", Name = "Gotha", Population = 1, Food = 1m, Wealth = 1m, Mood = 1, Security = 1, Crime = 1, Resources = 1m, Goods = 1m, CityState = "Stagnation" } },
                CitiesById = new Dictionary<string, object>(),
                Regions = new[] { new { Id = "rivia", DisplayName = "Rivia", MapAssetId = "x" } },
                SettlementMapLocations = Array.Empty<object>(),
                SettlementEconomyProfiles = Array.Empty<object>(),
                Caravans = Array.Empty<object>(),
                TradeRoutes = Array.Empty<object>(),
                TradeShipments = Array.Empty<object>(),
                SelectedCityId = "gotha",
                SelectedRegionId = "rivia"
            },
            Events = new { ActiveEvents = Array.Empty<object>(), CompletedEvents = Array.Empty<object>(), EventsByCityId = new Dictionary<string, object>() }
        };

        try
        {
            await File.WriteAllTextAsync(filePath, JsonSerializer.Serialize(save));
            await Assert.ThrowsAsync<InvalidDataException>(() => service.LoadAsync(filePath));
        }
        finally { Cleanup(filePath); }
    }

    private static WorldEventState CreateEventStateWithEvents(string cityId)
    {
        var manager = new CityEventManager();
        manager.Restore([new CityEvent("fire", "Fire", "Warehouse fire", 2, 5, 3)], [new CityEvent("storm", "Storm", "Port storm", 1, 2, 0)]);
        var state = new WorldEventState();
        state.SetManager(cityId, manager);
        return state;
    }

    private static string TempFile() => Path.Combine(Path.GetTempPath(), $"world-save-{Guid.NewGuid():N}.json");
    private static void Cleanup(string p) { if (File.Exists(p)) File.Delete(p); }
}
