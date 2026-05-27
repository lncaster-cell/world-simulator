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
    public async Task SaveLoad_Preserves_All_Cities_With_Different_State()
    {
        var service = new JsonWorldSaveService();
        var world = WorldPresets.CreateDefaultWorld();
        var clock = new SimulationClock();
        var eventState = new WorldEventState();

        for (var i = 0; i < world.Cities.Count; i++)
        {
            var city = world.Cities[i];
            city.Population = 1000 + (i * 111);
            city.Food = 50 + (i * 7);
            city.Wealth = 120 + (i * 13);
            city.Mood = 30 + (i * 5);
            city.Security = 70 - i;
            city.Crime = 10 + (i * 2);
            city.Resources = 200 + (i * 17);
            city.Goods = 90 + (i * 9);
        }

        world.Cities[0].CityState = CityState.Prosperity;
        world.Cities[1].CityState = CityState.Decline;
        world.Cities[2].CityState = CityState.Stagnation;
        world.SelectedCityId = world.Cities[2].Id;

        var filePath = TempFile();
        try
        {
            await service.SaveAsync(filePath, world, clock, eventState);
            var loaded = await service.LoadAsync(filePath);

            Assert.Equal(world.Cities.Count, loaded.World.Cities.Count);
            var loadedById = loaded.World.Cities.ToDictionary(x => x.Id, StringComparer.Ordinal);
            foreach (var expected in world.Cities)
            {
                var actual = loadedById[expected.Id];
                Assert.Equal(expected.Name, actual.Name);
                Assert.Equal(expected.Population, actual.Population);
                Assert.Equal(expected.Food, actual.Food);
                Assert.Equal(expected.Wealth, actual.Wealth);
                Assert.Equal(expected.Mood, actual.Mood);
                Assert.Equal(expected.Security, actual.Security);
                Assert.Equal(expected.Crime, actual.Crime);
                Assert.Equal(expected.Resources, actual.Resources);
                Assert.Equal(expected.Goods, actual.Goods);
                Assert.Equal(expected.CityState, actual.CityState);
            }

            Assert.Equal(world.SelectedCityId, loaded.World.SelectedCityId);
            Assert.Equal(world.SelectedCityId, loaded.World.SelectedCity.Id);
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
    public async Task LoadAsync_Invalid_CaravanStatus_Throws_InvalidDataException()
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
                Cities = new[]
                {
                    new { Id = "gotha", Name = "Gotha", Population = 1, Food = 1, Wealth = 1, Mood = 1, Security = 1, Crime = 1, Resources = 1, Goods = 1, CityState = "Stagnation" },
                    new { Id = "highrock", Name = "Highrock", Population = 1, Food = 1, Wealth = 1, Mood = 1, Security = 1, Crime = 1, Resources = 1, Goods = 1, CityState = "Stagnation" }
                },
                Regions = new[] { new { Id = "rivia", DisplayName = "Rivia", MapAssetId = "x" } },
                SettlementMapLocations = Array.Empty<object>(),
                SettlementEconomyProfiles = Array.Empty<object>(),
                Caravans = new[]
                {
                    new
                    {
                        Id = "caravan-1",
                        OwnerSettlementId = "gotha",
                        Type = "Cart",
                        Capacity = 100m,
                        RequiredWorkers = 2,
                        IsAvailable = true,
                        PurchaseCost = 1000m,
                        UpkeepPerWeek = 50m,
                        Status = "BrokenStatus"
                    }
                },
                TradeRoutes = Array.Empty<object>(),
                TradeShipments = Array.Empty<object>(),
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
            Assert.All(loaded.World.TradeRoutes, route => Assert.True(route.DistanceDays > 0m));
            Assert.Equal(world.TradeRoutes[0].Points[0].X, loaded.World.TradeRoutes[0].Points[0].X);
            Assert.Equal(world.TradeRoutes[0].Points[0].Y, loaded.World.TradeRoutes[0].Points[0].Y);
            Assert.Equal(world.TradeRoutes[0].DistanceDays, loaded.World.TradeRoutes[0].DistanceDays);
        }
        finally { Cleanup(filePath); }
    }

    [Fact]
    public async Task LoadAsync_TradeRouteWithoutDistanceDays_UsesPresetFallback()
    {
        var service = new JsonWorldSaveService();
        var filePath = TempFile();
        var presetRoute = TradeRoutePresets.CreateDefaultRoutes().First();

        var save = new
        {
            Version = 2,
            SavedAtUtc = DateTime.UtcNow,
            Clock = new { Day = 1, Hour = 0, IsRunning = false, AccumulatedRealTime = "00:00:00", RealTimePerGameHour = "00:05:00" },
            World = new
            {
                Cities = new[] { new { Id = "gotha", Name = "Gotha", Population = 1, Food = 1, Wealth = 1, Mood = 1, Security = 1, Crime = 1, Resources = 1, Goods = 1, CityState = "Stagnation" }, new { Id = "highrock", Name = "Highrock", Population = 1, Food = 1, Wealth = 1, Mood = 1, Security = 1, Crime = 1, Resources = 1, Goods = 1, CityState = "Stagnation" }, new { Id = "mlynek", Name = "Mlynek", Population = 1, Food = 1, Wealth = 1, Mood = 1, Security = 1, Crime = 1, Resources = 1, Goods = 1, CityState = "Stagnation" } },
                Regions = new[] { new { Id = "rivia", DisplayName = "Rivia", MapAssetId = "x" } },
                SettlementMapLocations = Array.Empty<object>(),
                SettlementEconomyProfiles = Array.Empty<object>(),
                Caravans = Array.Empty<object>(),
                TradeRoutes = new[] { new { Id = presetRoute.Id, presetRoute.FromSettlementId, presetRoute.ToSettlementId, Type = presetRoute.Type.ToString(), Distance = presetRoute.Distance, TravelDays = presetRoute.TravelDays, IsEnabled = true, DifficultyMultiplier = 1m, Points = new[] { new { X = 0.1m, Y = 0.1m }, new { X = 0.2m, Y = 0.2m } } } },
                TradeShipments = Array.Empty<object>(),
                SelectedCityId = "gotha",
                SelectedRegionId = "rivia"
            }
        };

        try
        {
            await File.WriteAllTextAsync(filePath, JsonSerializer.Serialize(save));
            var loaded = await service.LoadAsync(filePath);
            Assert.Equal(presetRoute.DistanceDays, loaded.World.TradeRoutes[0].DistanceDays);
        }
        finally { Cleanup(filePath); }
    }

    [Fact]
    public async Task SaveLoad_Smoke_Roundtrip_Preserves_All_Sections()
    {
        var service = new JsonWorldSaveService();
        var world = WorldPresets.CreateDefaultWorld();
        var clock = new SimulationClock(new SimulationTimeSettings { RealTimePerGameHour = TimeSpan.FromSeconds(3) });
        clock.RestoreState(9, 18, true, TimeSpan.FromSeconds(11), TimeSpan.FromSeconds(3));

        world.SelectedCityId = world.Cities[0].Id;
        world.SelectedRegionId = world.Regions[0].Id;
        world.Regions[0].DisplayName = "Test Region";
        world.SettlementMapLocations[0].X = 0.42m;
        world.SettlementEconomyProfiles[0].IsCapital = true;
        world.Caravans[0].Status = CaravanStatus.Broken;

        world.TradeShipments.Add(new TradeShipment
        {
            Id = "smoke-shipment",
            CaravanId = world.Caravans[0].Id,
            RouteId = world.TradeRoutes[0].Id,
            FromSettlementId = world.TradeRoutes[0].FromSettlementId,
            ToSettlementId = world.TradeRoutes[0].ToSettlementId,
            GoodType = TradeGoodType.Resources,
            Amount = 25m,
            DepartureDay = 9,
            ArrivalDay = 12,
            ReturnDay = 14,
            ExporterWealthDelta = 0.3m,
            ImporterWealthDelta = -0.3m,
            Status = TradeShipmentStatus.InTransitToDestination
        });

        var eventState = CreateEventStateWithEvents();
        var filePath = TempFile();

        try
        {
            await service.SaveAsync(filePath, world, clock, eventState);
            var loaded = await service.LoadAsync(filePath);

            Assert.Equal(9, loaded.Clock.Day);
            Assert.Equal(18, loaded.Clock.Hour);
            Assert.Equal(world.SelectedCityId, loaded.World.SelectedCityId);
            Assert.Equal(world.SelectedRegionId, loaded.World.SelectedRegionId);
            Assert.Equal("Test Region", loaded.World.Regions[0].DisplayName);
            Assert.Equal(0.42m, loaded.World.SettlementMapLocations[0].X);
            Assert.True(loaded.World.SettlementEconomyProfiles[0].IsCapital);
            Assert.Equal(CaravanStatus.Broken, loaded.World.Caravans[0].Status);
            Assert.Equal(world.TradeRoutes[0].Id, loaded.World.TradeRoutes[0].Id);
            Assert.Single(loaded.World.TradeShipments);
            Assert.Equal("smoke-shipment", loaded.World.TradeShipments[0].Id);

            var selectedManager = loaded.EventState.GetManagerOrEmpty(loaded.World.SelectedCityId);
            Assert.Single(selectedManager.ActiveEvents);
            Assert.Single(selectedManager.CompletedEvents);
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
