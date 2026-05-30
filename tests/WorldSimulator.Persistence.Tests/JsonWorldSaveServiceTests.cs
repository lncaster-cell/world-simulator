using System.Text.Json;
using System.Text.Json.Nodes;
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


    [Fact]
    public async Task LoadAsync_Does_Not_Copy_EventBucket_From_Other_City_To_Selected_City()
    {
        var service = new JsonWorldSaveService();
        var world = WorldPresets.CreateDefaultWorld();
        var sourceCityId = world.Cities[0].Id;
        var selectedCityId = world.Cities[1].Id;
        world.SelectedCityId = selectedCityId;
        var eventState = CreateEventStateWithEvents(sourceCityId);
        var filePath = TempFile();

        try
        {
            await service.SaveAsync(filePath, world, new SimulationClock(), eventState);

            var loaded = await service.LoadAsync(filePath);

            var selectedManager = loaded.EventState.GetManagerOrEmpty(selectedCityId);
            Assert.Empty(selectedManager.ActiveEvents);
            Assert.Empty(selectedManager.CompletedEvents);

            var sourceManager = loaded.EventState.GetManagerOrEmpty(sourceCityId);
            Assert.Single(sourceManager.ActiveEvents);
            Assert.Equal("fire", sourceManager.ActiveEvents[0].Id);
            Assert.Single(sourceManager.CompletedEvents);
            Assert.Equal("storm", sourceManager.CompletedEvents[0].Id);
        }
        finally { Cleanup(filePath); }
    }

    [Fact]
    public async Task LoadAsync_Old_Event_Format_Without_EventBuckets_Restores_Selected_City_Events()
    {
        var service = new JsonWorldSaveService();
        var world = WorldPresets.CreateDefaultWorld();
        var selectedCityId = world.Cities[1].Id;
        world.SelectedCityId = selectedCityId;
        var eventState = CreateEventStateWithEvents(selectedCityId);
        var filePath = TempFile();

        try
        {
            await service.SaveAsync(filePath, world, new SimulationClock(), eventState);
            var root = await ReadSavedJsonAsync(filePath);
            root["Events"]!.AsObject().Remove("EventsByCityId");
            await File.WriteAllTextAsync(filePath, root.ToJsonString());

            var loaded = await service.LoadAsync(filePath);

            var selectedManager = loaded.EventState.GetManagerOrEmpty(selectedCityId);
            Assert.Single(selectedManager.ActiveEvents);
            Assert.Equal("fire", selectedManager.ActiveEvents[0].Id);
            Assert.Single(selectedManager.CompletedEvents);
            Assert.Equal("storm", selectedManager.CompletedEvents[0].Id);
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
    public async Task LoadAsync_Legacy_World_With_CitiesById_Only_Restores_Collections_And_Selections()
    {
        var service = new JsonWorldSaveService();
        var world = WorldPresets.CreateDefaultWorld();
        var selectedCity = world.Cities[1];
        selectedCity.Food = 654m;
        var filePath = TempFile();

        try
        {
            await service.SaveAsync(filePath, world, new SimulationClock(), new WorldEventState());
            var root = await ReadSavedJsonAsync(filePath);
            root["Version"] = 2;
            var worldNode = root["World"]!.AsObject();
            worldNode["Cities"] = new JsonArray();
            worldNode["Regions"] = new JsonArray();
            worldNode["SettlementMapLocations"] = new JsonArray();
            worldNode["SettlementEconomyProfiles"] = new JsonArray();
            worldNode["SettlementSectorCapacityProfiles"] = new JsonArray();
            worldNode["Caravans"] = new JsonArray();
            worldNode["TradeRoutes"] = new JsonArray();
            worldNode["SelectedCityId"] = string.Empty;
            worldNode["SelectedRegionId"] = string.Empty;
            await File.WriteAllTextAsync(filePath, root.ToJsonString());

            var loaded = await service.LoadAsync(filePath);

            Assert.Equal(world.Cities.Count, loaded.World.Cities.Count);
            Assert.Equal(654m, loaded.World.Cities.First(city => city.Id == selectedCity.Id).Food);
            Assert.Equal(world.Regions.Count, loaded.World.Regions.Count);
            Assert.Equal(world.SettlementMapLocations.Count, loaded.World.SettlementMapLocations.Count);
            Assert.Equal(world.SettlementEconomyProfiles.Count, loaded.World.SettlementEconomyProfiles.Count);
            Assert.Equal(world.SettlementSectorCapacityProfiles.Count, loaded.World.SettlementSectorCapacityProfiles.Count);
            Assert.Equal(world.Caravans.Count, loaded.World.Caravans.Count);
            Assert.Equal(world.TradeRoutes.Count, loaded.World.TradeRoutes.Count);
            Assert.Equal(world.Cities[0].Id, loaded.World.SelectedCityId);
            Assert.Equal(world.Regions[0].Id, loaded.World.SelectedRegionId);
        }
        finally { Cleanup(filePath); }
    }

    [Fact]
    public async Task LoadAsync_Legacy_World_Restores_City_Infrastructure_Demographics_And_Sector_Capacity()
    {
        var service = new JsonWorldSaveService();
        var world = WorldPresets.CreateDefaultWorld();
        var city = world.Cities[0];
        var filePath = TempFile();

        try
        {
            await service.SaveAsync(filePath, world, new SimulationClock(), new WorldEventState());
            var root = await ReadSavedJsonAsync(filePath);
            root["Version"] = 3;
            var worldNode = root["World"]!.AsObject();
            worldNode["SettlementSectorCapacityProfiles"] = new JsonArray();
            RemoveCityInfrastructureAndDemographics(worldNode);
            await File.WriteAllTextAsync(filePath, root.ToJsonString());

            var loaded = await service.LoadAsync(filePath);
            var loadedCity = loaded.World.Cities.First(x => x.Id == city.Id);

            Assert.NotNull(loadedCity.Infrastructure);
            Assert.Equal(CityInfrastructure.MinLevel, loadedCity.Infrastructure.HousingLevel);
            Assert.Equal(CityInfrastructure.MinLevel, loadedCity.Infrastructure.UrbanLevel);
            Assert.Equal(CityInfrastructure.MinLevel, loadedCity.Infrastructure.ProductionLevel);
            Assert.Equal(CityInfrastructure.MinLevel, loadedCity.Infrastructure.MilitaryLevel);
            Assert.Equal(city.Population, loadedCity.Demographics.TotalPopulation);
            Assert.Single(loadedCity.Demographics.RaceGroups);
            Assert.Equal("human", loadedCity.Demographics.RaceGroups[0].RaceId);
            Assert.Equal(world.SettlementSectorCapacityProfiles.Count, loaded.World.SettlementSectorCapacityProfiles.Count);
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

    [Fact]
    public async Task LoadAsync_Invalid_World_Throws_InvalidDataException()
    {
        var service = new JsonWorldSaveService();
        var world = WorldPresets.CreateDefaultWorld();
        var filePath = TempFile();
        try
        {
            await service.SaveAsync(filePath, world, new SimulationClock(), new WorldEventState());
            var root = await ReadSavedJsonAsync(filePath);
            root["World"]!["TradeShipments"] = new JsonArray(new JsonObject
            {
                ["Id"] = "invalid-shipment",
                ["CaravanId"] = "missing-caravan",
                ["RouteId"] = world.TradeRoutes[0].Id,
                ["FromSettlementId"] = world.TradeRoutes[0].FromSettlementId,
                ["ToSettlementId"] = world.TradeRoutes[0].ToSettlementId,
                ["GoodType"] = TradeGoodType.Food.ToString(),
                ["Amount"] = 10m,
                ["DepartureDay"] = 1,
                ["ArrivalDay"] = 2,
                ["ReturnDay"] = 3,
                ["ExporterWealthDelta"] = 0.1m,
                ["ImporterWealthDelta"] = -0.1m,
                ["Status"] = TradeShipmentStatus.InTransitToDestination.ToString()
            });
            await File.WriteAllTextAsync(filePath, root.ToJsonString());

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
            Version = 4,
            SavedAtUtc = DateTime.UtcNow,
            Clock = new { Day = 1, Hour = 0, IsRunning = false, AccumulatedRealTime = "00:00:00", RealTimePerGameHour = "00:05:00" },
            World = new
            {
                Cities = new[]
                {
                    new
                    {
                        Id = "gotha",
                        Name = "Gotha",
                        Population = 1,
                        Food = 1m,
                        Wealth = 1m,
                        Mood = 1,
                        Security = 1,
                        Crime = 1,
                        Resources = 1m,
                        Goods = 1m,
                        CityState = "Stagnation",
                        Demographics = new { RaceGroups = Array.Empty<object>() }
                    }
                },
                CitiesById = new Dictionary<string, object>(),
                Regions = new[] { new { Id = "rivia", DisplayName = "Rivia", MapAssetId = "x" } },
                SettlementMapLocations = Array.Empty<object>(),
                SettlementEconomyProfiles = Array.Empty<object>(),
                SettlementSectorCapacityProfiles = Array.Empty<object>(),
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


    private static void RemoveCityInfrastructureAndDemographics(JsonObject worldNode)
    {
        foreach (var cityNode in worldNode["Cities"]!.AsArray())
        {
            RemoveCityInfrastructureAndDemographics(cityNode);
        }

        foreach (var cityNode in worldNode["CitiesById"]!.AsObject().Select(pair => pair.Value))
        {
            RemoveCityInfrastructureAndDemographics(cityNode);
        }
    }

    private static void RemoveCityInfrastructureAndDemographics(JsonNode? cityNode)
    {
        if (cityNode is not JsonObject cityObject)
        {
            return;
        }

        cityObject.Remove("Infrastructure");
        cityObject.Remove("Demographics");
    }

    private static WorldEventState CreateEventStateWithEvents(string cityId)
    {
        var manager = new CityEventManager();
        manager.Restore([new CityEvent("fire", "Fire", "Warehouse fire", 2, 5, 3)], [new CityEvent("storm", "Storm", "Port storm", 1, 2, 0)]);
        var state = new WorldEventState();
        state.SetManager(cityId, manager);
        return state;
    }

    private static async Task<JsonObject> ReadSavedJsonAsync(string filePath)
    {
        var json = await File.ReadAllTextAsync(filePath);
        return JsonNode.Parse(json)!.AsObject();
    }

    private static string TempFile() => Path.Combine(Path.GetTempPath(), $"world-save-{Guid.NewGuid():N}.json");
    private static void Cleanup(string p) { if (File.Exists(p)) File.Delete(p); }
}
