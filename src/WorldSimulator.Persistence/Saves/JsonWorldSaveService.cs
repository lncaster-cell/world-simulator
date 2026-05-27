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
                Cities = world.Cities.Select(city => ToSaveData(city)).ToList(),
                CitiesById = world.Cities.ToDictionary(city => city.Id, ToSaveData, StringComparer.Ordinal),
                Regions = world.Regions.Select(r => new RegionSaveData { Id = r.Id, DisplayName = r.DisplayName, MapAssetId = r.MapAssetId }).ToList(),
                SettlementMapLocations = world.SettlementMapLocations.Select(x => new SettlementMapLocationSaveData { SettlementId = x.SettlementId, RegionId = x.RegionId, X = x.X, Y = x.Y }).ToList(),
                SettlementEconomyProfiles = world.SettlementEconomyProfiles.Select(x => new SettlementEconomyProfileSaveData { SettlementId = x.SettlementId, AgriculturePotential = x.AgriculturePotential, FishingMultiplier = x.FishingMultiplier, HuntingMultiplier = x.HuntingMultiplier, MainlandSupplyMultiplier = x.MainlandSupplyMultiplier, ResourceGatheringMultiplier = x.ResourceGatheringMultiplier, GoodsCraftingMultiplier = x.GoodsCraftingMultiplier, IsPort = x.IsPort, IsFortress = x.IsFortress, IsCapital = x.IsCapital }).ToList(),
                Caravans = world.Caravans.Select(x => new CaravanSaveData { Id = x.Id, OwnerSettlementId = x.OwnerSettlementId, Type = x.Type.ToString(), Capacity = x.Capacity, RequiredWorkers = x.RequiredWorkers, IsAvailable = x.IsAvailable, PurchaseCost = x.PurchaseCost, UpkeepPerWeek = x.UpkeepPerWeek, Status = x.Status.ToString() }).ToList(),
                TradeRoutes = world.TradeRoutes.Select(route => ToSaveData(route)).ToList(),
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

            if (!state.EventManagersByCity.ContainsKey(selectedCityId))
            {
                var firstManager = state.EventManagersByCity.Values.First();
                var selectedManager = new CityEventManager();
                selectedManager.Restore(firstManager.ActiveEvents.ToList(), firstManager.CompletedEvents.ToList());
                state.SetManager(selectedCityId, selectedManager);
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

        var defaultWorld = WorldPresets.CreateDefaultWorld();

        var regions = saveData.World.Regions.Any()
            ? saveData.World.Regions.Select(x => new Region { Id = x.Id, DisplayName = x.DisplayName, MapAssetId = x.MapAssetId }).ToList()
            : defaultWorld.Regions;

        var settlementMapLocations = saveData.World.SettlementMapLocations.Any()
            ? saveData.World.SettlementMapLocations.Select(x => new SettlementMapLocation { SettlementId = x.SettlementId, RegionId = x.RegionId, X = x.X, Y = x.Y }).ToList()
            : defaultWorld.SettlementMapLocations;

        var settlementEconomyProfiles = saveData.World.SettlementEconomyProfiles.Any()
            ? saveData.World.SettlementEconomyProfiles.Select(x => new SettlementEconomyProfile { SettlementId = x.SettlementId, AgriculturePotential = x.AgriculturePotential, FishingMultiplier = x.FishingMultiplier, HuntingMultiplier = x.HuntingMultiplier, MainlandSupplyMultiplier = x.MainlandSupplyMultiplier, ResourceGatheringMultiplier = x.ResourceGatheringMultiplier, GoodsCraftingMultiplier = x.GoodsCraftingMultiplier, IsPort = x.IsPort, IsFortress = x.IsFortress, IsCapital = x.IsCapital }).ToList()
            : defaultWorld.SettlementEconomyProfiles;

        var caravans = saveData.World.Caravans.Any()
            ? saveData.World.Caravans.Select(ToCoreCaravan).ToList()
            : defaultWorld.Caravans;

        var tradeRoutes = saveData.World.TradeRoutes.Any()
            ? saveData.World.TradeRoutes.Select(ToCoreTradeRoute).ToList()
            : defaultWorld.TradeRoutes;

        var loadedCities = ResolveCities(saveData.World, filePath);
        var selectedCityId = loadedCities.Any(c => c.Id == saveData.World.SelectedCityId)
            ? saveData.World.SelectedCityId
            : loadedCities.First().Id;
        var selectedRegionId = regions.Any(r => r.Id == saveData.World.SelectedRegionId)
            ? saveData.World.SelectedRegionId
            : regions.First().Id;

        return new SimulationWorld
        {
            Cities = loadedCities,
            Regions = regions,
            SettlementMapLocations = settlementMapLocations,
            SettlementEconomyProfiles = settlementEconomyProfiles,
            Caravans = caravans,
            TradeRoutes = tradeRoutes,
            TradeShipments = saveData.World.TradeShipments.Select(ToCoreTradeShipment).ToList(),
            SelectedCityId = selectedCityId,
            SelectedRegionId = selectedRegionId
        };
    }

    private static List<City> ResolveCities(SimulationWorldSaveData worldData, string filePath)
    {
        if (worldData.CitiesById.Count > 0)
        {
            return worldData.CitiesById.Values.Select(x => ToCoreCity(x, filePath)).ToList();
        }

        return worldData.Cities.Select(x => ToCoreCity(x, filePath)).ToList();
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

        Enum.TryParse<CaravanStatus>(caravanData.Status, true, out var caravanStatus);
        return new Caravan { Id = caravanData.Id, OwnerSettlementId = caravanData.OwnerSettlementId, Type = caravanType, Capacity = caravanData.Capacity, RequiredWorkers = caravanData.RequiredWorkers, IsAvailable = caravanData.IsAvailable, PurchaseCost = caravanData.PurchaseCost, UpkeepPerWeek = caravanData.UpkeepPerWeek, Status = caravanStatus };
    }

    private static TradeRouteSaveData ToSaveData(TradeRoute route) => new()
    {
        Id = route.Id,
        FromSettlementId = route.FromSettlementId,
        ToSettlementId = route.ToSettlementId,
        Type = route.Type.ToString(),
        Distance = route.Distance,
        TravelDays = route.TravelDays,
        DistanceDays = route.DistanceDays,
        IsEnabled = route.IsEnabled,
        DifficultyMultiplier = route.DifficultyMultiplier,
        Points = route.Points.Select(point => new RoutePointSaveData { X = point.X, Y = point.Y }).ToList()
    };

    private static TradeRoute ToCoreTradeRoute(TradeRouteSaveData routeData)
    {
        if (!Enum.TryParse<CaravanType>(routeData.Type, true, out var caravanType))
            throw new InvalidDataException($"Unknown trade route caravan type '{routeData.Type}'.");

        var defaultDistanceDays = TradeRoutePresets.CreateDefaultRoutes()
            .FirstOrDefault(x => string.Equals(x.Id, routeData.Id, StringComparison.Ordinal))
            ?.DistanceDays ?? 1m;

        return new TradeRoute
        {
            Id = routeData.Id,
            FromSettlementId = routeData.FromSettlementId,
            ToSettlementId = routeData.ToSettlementId,
            Type = caravanType,
            Distance = routeData.Distance,
            TravelDays = routeData.TravelDays,
            DistanceDays = routeData.DistanceDays ?? defaultDistanceDays,
            IsEnabled = routeData.IsEnabled,
            DifficultyMultiplier = routeData.DifficultyMultiplier,
            Points = routeData.Points?.Select(point => new RoutePoint { X = point.X, Y = point.Y }).ToList() ?? []
        };
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
