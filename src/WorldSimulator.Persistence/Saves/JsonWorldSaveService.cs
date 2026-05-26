using System.Text.Json;
using WorldSimulator.Core.Cities;
using WorldSimulator.Core.Events;
using WorldSimulator.Core.Time;
using WorldSimulator.Core.Trade;
using WorldSimulator.Core.World;

namespace WorldSimulator.Persistence.Saves;

public sealed class JsonWorldSaveService
{
    private const string FallbackCityId = "gotha";

    private static readonly JsonSerializerOptions JsonOptions = new() { WriteIndented = true };

    public async Task SaveAsync(string filePath, SimulationWorld world, SimulationClock clock, WorldEventState eventState, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(filePath);
        ArgumentNullException.ThrowIfNull(world);
        ArgumentNullException.ThrowIfNull(clock);
        ArgumentNullException.ThrowIfNull(eventState);

        var saveData = new WorldSaveData
        {
            Version = 2,
            SavedAtUtc = DateTime.UtcNow,
            Clock = new ClockSaveData { Day = clock.Day, Hour = clock.Hour, IsRunning = clock.IsRunning, AccumulatedRealTime = clock.AccumulatedRealTime, RealTimePerGameHour = clock.RealTimePerGameHour },
            World = new SimulationWorldSaveData
            {
                Cities = world.Cities.Select(ToSaveData).ToList(),
                Regions = world.Regions.Select(r => new RegionSaveData { Id = r.Id, DisplayName = r.DisplayName, MapAssetId = r.MapAssetId }).ToList(),
                SettlementMapLocations = world.SettlementMapLocations.Select(x => new SettlementMapLocationSaveData { SettlementId = x.SettlementId, RegionId = x.RegionId, X = x.X, Y = x.Y }).ToList(),
                SettlementEconomyProfiles = world.SettlementEconomyProfiles.Select(x => new SettlementEconomyProfileSaveData { SettlementId = x.SettlementId, AgriculturePotential = x.AgriculturePotential, FishingMultiplier = x.FishingMultiplier, HuntingMultiplier = x.HuntingMultiplier, MainlandSupplyMultiplier = x.MainlandSupplyMultiplier, ResourceGatheringMultiplier = x.ResourceGatheringMultiplier, GoodsCraftingMultiplier = x.GoodsCraftingMultiplier, IsPort = x.IsPort, IsFortress = x.IsFortress, IsCapital = x.IsCapital }).ToList(),
                Caravans = world.Caravans.Select(x => new CaravanSaveData { Id = x.Id, OwnerSettlementId = x.OwnerSettlementId, Type = x.Type.ToString(), Capacity = x.Capacity, RequiredWorkers = x.RequiredWorkers, IsAvailable = x.IsAvailable }).ToList(),
                TradeRoutes = world.TradeRoutes.Select(x => new TradeRouteSaveData { Id = x.Id, FromSettlementId = x.FromSettlementId, ToSettlementId = x.ToSettlementId, Type = x.Type.ToString(), Distance = x.Distance, TravelDays = x.TravelDays, IsEnabled = x.IsEnabled, DifficultyMultiplier = x.DifficultyMultiplier, Points = x.Points.Select(p => new RoutePointSaveData { X = p.X, Y = p.Y }).ToList() }).ToList(),
                TradeShipments = world.TradeShipments.Select(x => new TradeShipmentSaveData { Id = x.Id, CaravanId = x.CaravanId, RouteId = x.RouteId, FromSettlementId = x.FromSettlementId, ToSettlementId = x.ToSettlementId, GoodType = x.GoodType.ToString(), Amount = x.Amount, DepartureDay = x.DepartureDay, ArrivalDay = x.ArrivalDay, ReturnDay = x.ReturnDay, ExporterWealthDelta = x.ExporterWealthDelta, ImporterWealthDelta = x.ImporterWealthDelta, Status = x.Status.ToString() }).ToList(),
                SelectedCityId = world.SelectedCityId,
                SelectedRegionId = world.SelectedRegionId
            },
            Events = new EventSaveData
            {
                ActiveEvents = eventState.GetManagerOrEmpty(world.SelectedCityId).ActiveEvents.Select(ToSaveData).ToList(),
                CompletedEvents = eventState.GetManagerOrEmpty(world.SelectedCityId).CompletedEvents.Select(ToSaveData).ToList(),
                EventsByCityId = eventState.EventManagersByCity.ToDictionary(
                    x => x.Key,
                    x => new CityEventBucketSaveData
                    {
                        ActiveEvents = x.Value.ActiveEvents.Select(ToSaveData).ToList(),
                        CompletedEvents = x.Value.CompletedEvents.Select(ToSaveData).ToList()
                    },
                    StringComparer.Ordinal)
            }
        };

        var directory = Path.GetDirectoryName(filePath);
        if (!string.IsNullOrEmpty(directory)) Directory.CreateDirectory(directory);

        await using var stream = File.Create(filePath);
        await JsonSerializer.SerializeAsync(stream, saveData, JsonOptions, cancellationToken);
    }

    public async Task<WorldLoadResult> LoadAsync(string filePath, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(filePath);
        if (!File.Exists(filePath)) throw new FileNotFoundException($"Save file was not found: '{filePath}'.", filePath);

        WorldSaveData? saveData;
        try
        {
            await using var stream = File.OpenRead(filePath);
            if (stream.Length == 0) throw new InvalidDataException($"Save file '{filePath}' is empty.");
            saveData = await JsonSerializer.DeserializeAsync<WorldSaveData>(stream, JsonOptions, cancellationToken);
        }
        catch (JsonException ex)
        {
            throw new InvalidDataException($"Save file '{filePath}' contains invalid JSON.", ex);
        }

        if (saveData is null) throw new InvalidDataException($"Save file '{filePath}' is empty or malformed.");

        var world = saveData.Version switch
        {
            1 => BuildWorldFromVersion1(saveData, filePath),
            2 => BuildWorldFromVersion2(saveData, filePath),
            _ => throw new InvalidDataException($"Save file '{filePath}' has unsupported version '{saveData.Version}'.")
        };

        ValidateWorld(world, filePath);

        var settings = new SimulationTimeSettings { RealTimePerGameHour = saveData.Clock.RealTimePerGameHour };
        var clock = new SimulationClock(settings);
        clock.RestoreState(saveData.Clock.Day, saveData.Clock.Hour, saveData.Clock.IsRunning, saveData.Clock.AccumulatedRealTime, saveData.Clock.RealTimePerGameHour);

        var eventState = BuildEventState(saveData.Events, world.SelectedCityId);

        return new WorldLoadResult(world, clock, eventState);
    }

    private static WorldEventState BuildEventState(EventSaveData? events, string selectedCityId)
    {
        var state = new WorldEventState();
        if (events?.EventsByCityId is { Count: > 0 })
        {
            foreach (var pair in events.EventsByCityId)
            {
                var manager = new CityEventManager();
                manager.Restore(
                    (pair.Value.ActiveEvents ?? []).Select(ToCoreEvent).ToList(),
                    (pair.Value.CompletedEvents ?? []).Select(ToCoreEvent).ToList());
                state.SetManager(pair.Key, manager);
            }

            return state;
        }

        var fallbackManager = new CityEventManager();
        fallbackManager.Restore(
            (events?.ActiveEvents ?? []).Select(ToCoreEvent).ToList(),
            (events?.CompletedEvents ?? []).Select(ToCoreEvent).ToList());
        state.SetManager(selectedCityId, fallbackManager);
        return state;
    }

    private static SimulationWorld BuildWorldFromVersion2(WorldSaveData saveData, string filePath)
    {
        if (saveData.World is null) throw new InvalidDataException($"Save file '{filePath}' version 2 is missing world data.");

        return new SimulationWorld
        {
            Cities = saveData.World.Cities.Select(x => ToCoreCity(x, filePath)).ToList(),
            Regions = saveData.World.Regions.Select(x => new Region { Id = x.Id, DisplayName = x.DisplayName, MapAssetId = x.MapAssetId }).ToList(),
            SettlementMapLocations = saveData.World.SettlementMapLocations.Select(x => new SettlementMapLocation { SettlementId = x.SettlementId, RegionId = x.RegionId, X = x.X, Y = x.Y }).ToList(),
            SettlementEconomyProfiles = saveData.World.SettlementEconomyProfiles.Select(x => new SettlementEconomyProfile { SettlementId = x.SettlementId, AgriculturePotential = x.AgriculturePotential, FishingMultiplier = x.FishingMultiplier, HuntingMultiplier = x.HuntingMultiplier, MainlandSupplyMultiplier = x.MainlandSupplyMultiplier, ResourceGatheringMultiplier = x.ResourceGatheringMultiplier, GoodsCraftingMultiplier = x.GoodsCraftingMultiplier, IsPort = x.IsPort, IsFortress = x.IsFortress, IsCapital = x.IsCapital }).ToList(),
            Caravans = saveData.World.Caravans.Select(ToCoreCaravan).ToList(),
            TradeRoutes = saveData.World.TradeRoutes.Select(ToCoreTradeRoute).ToList(),
            TradeShipments = saveData.World.TradeShipments.Select(ToCoreTradeShipment).ToList(),
            SelectedCityId = saveData.World.SelectedCityId,
            SelectedRegionId = saveData.World.SelectedRegionId
        };
    }

    private static SimulationWorld BuildWorldFromVersion1(WorldSaveData saveData, string filePath)
    {
        if (saveData.City is null) throw new InvalidDataException($"Save file '{filePath}' version 1 is missing city data.");

        var world = WorldPresets.CreateDefaultWorld();
        var restoredCity = ToCoreCity(saveData.City, filePath);
        var cityIndex = world.Cities.FindIndex(c => c.Id == restoredCity.Id);
        if (cityIndex >= 0) world.Cities[cityIndex] = restoredCity;

        world.SelectedCityId = world.Cities.Any(c => c.Id == restoredCity.Id) ? restoredCity.Id : FallbackCityId;
        world.SelectedRegionId = world.Regions.Any(r => r.Id == world.SelectedRegionId) ? world.SelectedRegionId : world.Regions.First().Id;
        return world;
    }

    private static void ValidateWorld(SimulationWorld world, string filePath)
    {
        if (!world.Cities.Any(c => c.Id == world.SelectedCityId)) throw new InvalidDataException($"Save file '{filePath}' selected city id '{world.SelectedCityId}' does not exist in loaded cities.");
        if (!world.Regions.Any(r => r.Id == world.SelectedRegionId)) throw new InvalidDataException($"Save file '{filePath}' selected region id '{world.SelectedRegionId}' does not exist in loaded regions.");

        var caravanIds = world.Caravans.Select(x => x.Id).ToHashSet(StringComparer.Ordinal);
        var routeIds = world.TradeRoutes.Select(x => x.Id).ToHashSet(StringComparer.Ordinal);
        var settlementIds = world.Cities.Select(x => x.Id).ToHashSet(StringComparer.Ordinal);
        foreach (var shipment in world.TradeShipments)
        {
            if (!caravanIds.Contains(shipment.CaravanId)) throw new InvalidDataException($"Save file '{filePath}' shipment '{shipment.Id}' references unknown caravan '{shipment.CaravanId}'.");
            if (!routeIds.Contains(shipment.RouteId)) throw new InvalidDataException($"Save file '{filePath}' shipment '{shipment.Id}' references unknown route '{shipment.RouteId}'.");
            if (!settlementIds.Contains(shipment.FromSettlementId)) throw new InvalidDataException($"Save file '{filePath}' shipment '{shipment.Id}' references unknown origin settlement '{shipment.FromSettlementId}'.");
            if (!settlementIds.Contains(shipment.ToSettlementId)) throw new InvalidDataException($"Save file '{filePath}' shipment '{shipment.Id}' references unknown destination settlement '{shipment.ToSettlementId}'.");
            if (shipment.ArrivalDay < shipment.DepartureDay) throw new InvalidDataException($"Save file '{filePath}' shipment '{shipment.Id}' has arrival day before departure day.");
            if (shipment.ReturnDay < shipment.ArrivalDay) throw new InvalidDataException($"Save file '{filePath}' shipment '{shipment.Id}' has return day before arrival day.");
        }
    }

    private static CitySaveData ToSaveData(City city) => new()
    {
        Id = city.Id, Name = city.Name, Population = city.Population, Food = city.Food, Wealth = city.Wealth, Mood = city.Mood, Security = city.Security, Crime = city.Crime, Resources = city.Resources, Goods = city.Goods, CityState = city.CityState.ToString()
    };

    private static City ToCoreCity(CitySaveData cityData, string filePath)
    {
        if (!Enum.TryParse<CityState>(cityData.CityState, true, out var parsedCityState))
            throw new InvalidDataException($"Save file '{filePath}' contains unknown city_state '{cityData.CityState}'.");

        return new City(cityData.Id, cityData.Name, cityData.Population, cityData.Food, cityData.Wealth, cityData.Mood, cityData.Security, cityData.Crime, cityData.Resources, cityData.Goods, parsedCityState);
    }

    private static Caravan ToCoreCaravan(CaravanSaveData caravanData)
    {
        if (!Enum.TryParse<CaravanType>(caravanData.Type, true, out var caravanType))
            throw new InvalidDataException($"Unknown caravan type '{caravanData.Type}'.");

        return new Caravan { Id = caravanData.Id, OwnerSettlementId = caravanData.OwnerSettlementId, Type = caravanType, Capacity = caravanData.Capacity, RequiredWorkers = caravanData.RequiredWorkers, IsAvailable = caravanData.IsAvailable };
    }

    private static TradeRoute ToCoreTradeRoute(TradeRouteSaveData routeData)
    {
        if (!Enum.TryParse<CaravanType>(routeData.Type, true, out var caravanType))
            throw new InvalidDataException($"Unknown trade route caravan type '{routeData.Type}'.");

        return new TradeRoute { Id = routeData.Id, FromSettlementId = routeData.FromSettlementId, ToSettlementId = routeData.ToSettlementId, Type = caravanType, Distance = routeData.Distance, TravelDays = routeData.TravelDays, IsEnabled = routeData.IsEnabled, DifficultyMultiplier = routeData.DifficultyMultiplier, Points = (routeData.Points ?? []).Select(p => new RoutePoint { X = p.X, Y = p.Y }).ToList() };
    }

    private static TradeShipment ToCoreTradeShipment(TradeShipmentSaveData shipmentData)
    {
        if (!Enum.TryParse<TradeGoodType>(shipmentData.GoodType, true, out var goodType))
            throw new InvalidDataException($"Unknown shipment good type '{shipmentData.GoodType}'.");
        if (!Enum.TryParse<TradeShipmentStatus>(shipmentData.Status, true, out var status))
            throw new InvalidDataException($"Unknown shipment status '{shipmentData.Status}'.");

        return new TradeShipment
        {
            Id = shipmentData.Id,
            CaravanId = shipmentData.CaravanId,
            RouteId = shipmentData.RouteId,
            FromSettlementId = shipmentData.FromSettlementId,
            ToSettlementId = shipmentData.ToSettlementId,
            GoodType = goodType,
            Amount = shipmentData.Amount,
            DepartureDay = shipmentData.DepartureDay,
            ArrivalDay = shipmentData.ArrivalDay,
            ReturnDay = shipmentData.ReturnDay,
            ExporterWealthDelta = shipmentData.ExporterWealthDelta,
            ImporterWealthDelta = shipmentData.ImporterWealthDelta,
            Status = status
        };
    }

    private static CityEventSaveData ToSaveData(CityEvent cityEvent) => new() { Id = cityEvent.Id, Name = cityEvent.Name, Description = cityEvent.Description, StartedDay = cityEvent.StartedDay, DurationDays = cityEvent.DurationDays, RemainingDays = cityEvent.RemainingDays };
    private static CityEvent ToCoreEvent(CityEventSaveData eventSaveData) => new(eventSaveData.Id, eventSaveData.Name, eventSaveData.Description, eventSaveData.StartedDay, eventSaveData.DurationDays, eventSaveData.RemainingDays);
}
