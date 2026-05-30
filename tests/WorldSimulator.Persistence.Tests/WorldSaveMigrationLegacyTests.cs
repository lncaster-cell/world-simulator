using System.Text.Json;
using WorldSimulator.Core.Events;
using WorldSimulator.Core.Time;
using WorldSimulator.Core.World;
using WorldSimulator.Persistence.Saves;
using Xunit;
using static WorldSimulator.Persistence.Tests.WorldSaveTestHelpers;

namespace WorldSimulator.Persistence.Tests;

public sealed class WorldSaveMigrationLegacyTests
{
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
            Assert.Equal(4, document.RootElement.GetProperty("Version").GetInt32());
        }
        finally { Cleanup(filePath); }
    }

    [Fact]
    public async Task LoadAsync_Save_Version_Newer_Than_Current_Throws_InvalidDataException()
    {
        var service = new JsonWorldSaveService();
        var filePath = TempFile();
        var save = new
        {
            Version = 99,
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
    public async Task LoadAsync_Old_Save_Version_Migrates_To_Current_World()
    {
        var service = new JsonWorldSaveService();
        var world = WorldPresets.CreateDefaultWorld();
        var city = world.Cities[0];
        city.Food = 987m;
        var filePath = TempFile();
        var save = new
        {
            Version = 1,
            SavedAtUtc = DateTime.UtcNow,
            Clock = new { Day = 4, Hour = 8, IsRunning = true, AccumulatedRealTime = "00:00:07", RealTimePerGameHour = "00:00:03" },
            City = new
            {
                city.Id,
                city.Name,
                city.Population,
                city.Food,
                city.Wealth,
                city.Mood,
                city.Security,
                city.Crime,
                city.Resources,
                city.Goods,
                CityState = city.CityState.ToString(),
                Infrastructure = new
                {
                    city.Infrastructure.HousingLevel,
                    city.Infrastructure.UrbanLevel,
                    city.Infrastructure.ProductionLevel,
                    city.Infrastructure.MilitaryLevel
                }
            },
            Events = new { ActiveEvents = Array.Empty<object>(), CompletedEvents = Array.Empty<object>(), EventsByCityId = new Dictionary<string, object>() }
        };

        try
        {
            await File.WriteAllTextAsync(filePath, JsonSerializer.Serialize(save));

            var loaded = await service.LoadAsync(filePath);

            Assert.Equal(4, loaded.Clock.Day);
            Assert.True(loaded.Clock.IsRunning);
            Assert.Equal(world.Cities.Count, loaded.World.Cities.Count);
            Assert.Equal(987m, loaded.World.Cities[0].Food);
            Assert.Equal(world.Regions.Count, loaded.World.Regions.Count);
            Assert.Equal(world.TradeRoutes.Count, loaded.World.TradeRoutes.Count);
            Assert.Equal(world.SettlementSectorCapacityProfiles.Count, loaded.World.SettlementSectorCapacityProfiles.Count);
            Assert.Equal(city.Population, loaded.World.Cities[0].Demographics.TotalPopulation);
            Assert.Equal(city.Id, loaded.World.SelectedCityId);
        }
        finally { Cleanup(filePath); }
    }

    [Fact]
    public async Task LoadAsync_Restores_Missing_Route_Fields_From_Defaults()
    {
        var service = new JsonWorldSaveService();
        var world = WorldPresets.CreateDefaultWorld();
        var route = world.TradeRoutes[0];
        var filePath = TempFile();
        try
        {
            await service.SaveAsync(filePath, world, new SimulationClock(), new WorldEventState());
            var root = await ReadSavedJsonAsync(filePath);
            root["Version"] = 2;
            var routeNode = root["World"]!["TradeRoutes"]![0]!.AsObject();
            routeNode.Remove("DistanceDays");
            routeNode.Remove("Points");
            routeNode["DifficultyMultiplier"] = 0m;
            await File.WriteAllTextAsync(filePath, root.ToJsonString());

            var loaded = await service.LoadAsync(filePath);
            var loadedRoute = loaded.World.TradeRoutes.First(x => x.Id == route.Id);

            Assert.Equal(route.DistanceDays, loadedRoute.DistanceDays);
            Assert.Equal(1m, loadedRoute.DifficultyMultiplier);
            Assert.NotEmpty(loadedRoute.Points);
        }
        finally { Cleanup(filePath); }
    }
}
