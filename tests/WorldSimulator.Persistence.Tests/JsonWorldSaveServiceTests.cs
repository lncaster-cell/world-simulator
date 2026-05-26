using System.Text.Json;
using WorldSimulator.Core.Cities;
using WorldSimulator.Core.Events;
using WorldSimulator.Core.Time;
using WorldSimulator.Core.World;
using WorldSimulator.Persistence.Saves;
using Xunit;

namespace WorldSimulator.Persistence.Tests;

public sealed class JsonWorldSaveServiceTests
{
    [Fact]
    public async Task SaveLoad_Preserves_Full_World_State()
    {
        var service = new JsonWorldSaveService();
        var world = WorldPresets.CreateDefaultWorld();
        var clock = new SimulationClock();
        var eventState = CreateEventStateWithEvents();

        world.Cities[0].Food = 1234;
        world.Cities[1].Mood = 73;
        world.SelectedCityId = world.Cities[1].Id;
        world.SelectedRegionId = world.Regions[0].Id;

        var filePath = TempFile();
        try
        {
            await service.SaveAsync(filePath, world, clock, eventState);
            var loaded = await service.LoadAsync(filePath);

            Assert.Equal(world.Cities.Count, loaded.World.Cities.Count);
            Assert.Equal(1234m, loaded.World.Cities[0].Food);
            Assert.Equal(73, loaded.World.Cities[1].Mood);
            Assert.Equal(world.SelectedCityId, loaded.World.SelectedCityId);
            Assert.Equal(world.SelectedRegionId, loaded.World.SelectedRegionId);
            Assert.Equal(world.Caravans.Count, loaded.World.Caravans.Count);
            Assert.Equal(world.Caravans[0].PurchaseCost, loaded.World.Caravans[0].PurchaseCost);
            Assert.Equal(world.Caravans[0].UpkeepPerWeek, loaded.World.Caravans[0].UpkeepPerWeek);
            Assert.Equal(world.Caravans[0].Status, loaded.World.Caravans[0].Status);
            Assert.Equal(world.SettlementMapLocations.Count, loaded.World.SettlementMapLocations.Count);
            Assert.Equal(world.SettlementEconomyProfiles.Count, loaded.World.SettlementEconomyProfiles.Count);
            var selectedManager = loaded.EventState.GetManagerOrEmpty(loaded.World.SelectedCityId);
            Assert.Single(selectedManager.ActiveEvents);
            Assert.Single(selectedManager.CompletedEvents);
        }
        finally { Cleanup(filePath); }
    }

    [Fact]
    public async Task SaveLoad_Preserves_Clock_State()
    {
        var service = new JsonWorldSaveService();
        var world = WorldPresets.CreateDefaultWorld();
        var clock = new SimulationClock(new SimulationTimeSettings { RealTimePerGameHour = TimeSpan.FromSeconds(2) });
        clock.RestoreState(5, 14, true, TimeSpan.FromSeconds(7), TimeSpan.FromSeconds(2));

        var filePath = TempFile();
        try
        {
            await service.SaveAsync(filePath, world, clock, new WorldEventState());
            var loaded = await service.LoadAsync(filePath);
            Assert.Equal(5, loaded.Clock.Day);
            Assert.Equal(14, loaded.Clock.Hour);
            Assert.True(loaded.Clock.IsRunning);
            Assert.Equal(TimeSpan.FromSeconds(7), loaded.Clock.AccumulatedRealTime);
        }
        finally { Cleanup(filePath); }
    }

    [Fact]
    public async Task LoadAsync_Version1_Migrates_To_World()
    {
        var service = new JsonWorldSaveService();
        var filePath = TempFile();

        var oldSave = new
        {
            Version = 1,
            SavedAtUtc = DateTime.UtcNow,
            Clock = new { Day = 2, Hour = 8, IsRunning = false, AccumulatedRealTime = "00:00:00", RealTimePerGameHour = "00:05:00" },
            City = new { Id = "gotha", Name = "Гота", Population = 777, Food = 999, Wealth = 320, Mood = 55, Security = 60, Crime = 30, Resources = 260, Goods = 140, CityState = "Stagnation" },
            Events = new { ActiveEvents = Array.Empty<object>(), CompletedEvents = Array.Empty<object>() }
        };

        try
        {
            await File.WriteAllTextAsync(filePath, JsonSerializer.Serialize(oldSave));
            var loaded = await service.LoadAsync(filePath);
            Assert.Equal(777, loaded.World.FindCity("gotha")!.Population);
            Assert.Equal("gotha", loaded.World.SelectedCityId);
        }
        finally { Cleanup(filePath); }
    }

    [Fact]
    public async Task LoadAsync_Invalid_CityState_Throws_InvalidDataException()
    {
        var service = new JsonWorldSaveService();
        var filePath = TempFile();

        var badSave = new
        {
            Version = 2,
            SavedAtUtc = DateTime.UtcNow,
            Clock = new { Day = 1, Hour = 0, IsRunning = false, AccumulatedRealTime = "00:00:00", RealTimePerGameHour = "00:05:00" },
            World = new
            {
                Cities = new[] { new { Id = "gotha", Name = "Gotha", Population = 1, Food = 1, Wealth = 1, Mood = 1, Security = 1, Crime = 1, Resources = 1, Goods = 1, CityState = "Nope" } },
                Regions = new[] { new { Id = "rivia", DisplayName = "Rivia", MapAssetId = "x" } },
                SettlementMapLocations = Array.Empty<object>(),
                SettlementEconomyProfiles = Array.Empty<object>(),
                Caravans = Array.Empty<object>(),
                SelectedCityId = "gotha",
                SelectedRegionId = "rivia"
            }
        };

        try
        {
            await File.WriteAllTextAsync(filePath, JsonSerializer.Serialize(badSave));
            await Assert.ThrowsAsync<InvalidDataException>(() => service.LoadAsync(filePath));
        }
        finally { Cleanup(filePath); }
    }



    [Fact]
    public async Task SaveLoad_Preserves_TradeRoutes_AndRoutePoints()
    {
        var service = new JsonWorldSaveService();
        var world = WorldPresets.CreateDefaultWorld();
        var filePath = TempFile();
        try
        {
            await service.SaveAsync(filePath, world, new SimulationClock(), new WorldEventState());
            var loaded = await service.LoadAsync(filePath);
            Assert.Equal(world.TradeRoutes.Count, loaded.World.TradeRoutes.Count);
            Assert.All(loaded.World.TradeRoutes, route => Assert.True(route.Points.Count >= 2));
            Assert.Equal(world.TradeRoutes[0].Points[0].X, loaded.World.TradeRoutes[0].Points[0].X);
            Assert.Equal(world.TradeRoutes[0].Points[0].Y, loaded.World.TradeRoutes[0].Points[0].Y);
        }
        finally { Cleanup(filePath); }
    }
    private static WorldEventState CreateEventStateWithEvents()
    {
        var manager = new CityEventManager();
        manager.Restore([new CityEvent("fire", "Fire", "Warehouse fire", 2, 5, 3)], [new CityEvent("storm", "Storm", "Port storm", 1, 2, 0)]);
        var state = new WorldEventState();
        state.SetManager("novigrad", manager);
        return state;
    }

    private static string TempFile() => Path.Combine(Path.GetTempPath(), $"world-save-{Guid.NewGuid():N}.json");
    private static void Cleanup(string p) { if (File.Exists(p)) File.Delete(p); }
}
