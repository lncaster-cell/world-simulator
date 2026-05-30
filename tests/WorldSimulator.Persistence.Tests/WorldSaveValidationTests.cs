using System.Text.Json;
using System.Text.Json.Nodes;
using WorldSimulator.Core.Events;
using WorldSimulator.Core.Time;
using WorldSimulator.Core.Trade;
using WorldSimulator.Core.World;
using WorldSimulator.Persistence.Saves;
using Xunit;
using static WorldSimulator.Persistence.Tests.WorldSaveTestHelpers;

namespace WorldSimulator.Persistence.Tests;

public sealed class WorldSaveValidationTests
{
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
}
