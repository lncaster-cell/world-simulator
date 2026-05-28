using WorldSimulator.Core.Cities;
using WorldSimulator.Core.Events;
using WorldSimulator.Core.Trade;
using WorldSimulator.Core.World;

namespace WorldSimulator.Persistence.Saves;

internal static class WorldSaveMapper
{
    public static SimulationWorldSaveData ToSaveData(SimulationWorld world) => new()
    {
        Cities = world.Cities.Select(ToSaveData).ToList(),
        CitiesById = world.Cities.ToDictionary(city => city.Id, ToSaveData, StringComparer.Ordinal),
        Regions = world.Regions.Select(ToSaveData).ToList(),
        SettlementMapLocations = world.SettlementMapLocations.Select(ToSaveData).ToList(),
        SettlementEconomyProfiles = world.SettlementEconomyProfiles.Select(ToSaveData).ToList(),
        Caravans = world.Caravans.Select(ToSaveData).ToList(),
        TradeRoutes = world.TradeRoutes.Select(ToSaveData).ToList(),
        TradeShipments = world.TradeShipments.Select(ToSaveData).ToList(),
        SelectedCityId = world.SelectedCityId,
        SelectedRegionId = world.SelectedRegionId
    };

    public static EventSaveData ToSaveData(WorldEventState eventState, string selectedCityId) => new()
    {
        ActiveEvents = eventState.GetManagerOrEmpty(selectedCityId).ActiveEvents.Select(ToSaveData).ToList(),
        CompletedEvents = eventState.GetManagerOrEmpty(selectedCityId).CompletedEvents.Select(ToSaveData).ToList(),
        EventsByCityId = eventState.EventManagersByCity.ToDictionary(
            x => x.Key,
            x => new CityEventBucketSaveData
            {
                ActiveEvents = x.Value.ActiveEvents.Select(ToSaveData).ToList(),
                CompletedEvents = x.Value.CompletedEvents.Select(ToSaveData).ToList()
            },
            StringComparer.Ordinal)
    };

    public static SimulationWorld ToCoreWorld(SimulationWorldSaveData worldData, string filePath)
    {
        var loadedCities = ResolveCities(worldData, filePath);
        var regions = (worldData.Regions ?? []).Select(ToCoreRegion).ToList();
        var settlementMapLocations = (worldData.SettlementMapLocations ?? []).Select(ToCoreSettlementMapLocation).ToList();
        var settlementEconomyProfiles = (worldData.SettlementEconomyProfiles ?? []).Select(ToCoreSettlementEconomyProfile).ToList();
        var caravans = (worldData.Caravans ?? []).Select(ToCoreCaravan).ToList();
        var tradeRoutes = (worldData.TradeRoutes ?? []).Select(ToCoreTradeRoute).ToList();

        if (loadedCities.Count == 0) throw new InvalidDataException($"Save file '{filePath}' has no cities.");
        if (regions.Count == 0) throw new InvalidDataException($"Save file '{filePath}' has no regions.");

        var selectedCityId = loadedCities.Any(c => c.Id == worldData.SelectedCityId)
            ? worldData.SelectedCityId
            : loadedCities.First().Id;
        var selectedRegionId = regions.Any(r => r.Id == worldData.SelectedRegionId)
            ? worldData.SelectedRegionId
            : regions.First().Id;

        return new SimulationWorld
        {
            Cities = loadedCities,
            Regions = regions,
            SettlementMapLocations = settlementMapLocations,
            SettlementEconomyProfiles = settlementEconomyProfiles,
            Caravans = caravans,
            TradeRoutes = tradeRoutes,
            TradeShipments = (worldData.TradeShipments ?? []).Select(ToCoreTradeShipment).ToList(),
            SelectedCityId = selectedCityId,
            SelectedRegionId = selectedRegionId
        };
    }

    public static WorldEventState ToCoreEventState(EventSaveData? events, string selectedCityId)
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

    private static List<City> ResolveCities(SimulationWorldSaveData worldData, string filePath)
    {
        if (worldData.CitiesById is { Count: > 0 })
        {
            return worldData.CitiesById.Values.Select(x => ToCoreCity(x, filePath)).ToList();
        }

        return (worldData.Cities ?? []).Select(x => ToCoreCity(x, filePath)).ToList();
    }

    public static CitySaveData ToSaveData(City city) => new()
    {
        Id = city.Id,
        Name = city.Name,
        Population = city.Population,
        Food = city.Food,
        Wealth = city.Wealth,
        Mood = city.Mood,
        Security = city.Security,
        Crime = city.Crime,
        Resources = city.Resources,
        Goods = city.Goods,
        CityState = city.CityState.ToString(),
        Infrastructure = ToSaveData(city.Infrastructure)
    };

    private static CityInfrastructureSaveData ToSaveData(CityInfrastructure infrastructure) => new()
    {
        HousingLevel = infrastructure.HousingLevel,
        UrbanLevel = infrastructure.UrbanLevel,
        ProductionLevel = infrastructure.ProductionLevel,
        MilitaryLevel = infrastructure.MilitaryLevel
    };

    public static RegionSaveData ToSaveData(Region region) => new()
    {
        Id = region.Id,
        DisplayName = region.DisplayName,
        MapAssetId = region.MapAssetId
    };

    public static SettlementMapLocationSaveData ToSaveData(SettlementMapLocation location) => new()
    {
        SettlementId = location.SettlementId,
        RegionId = location.RegionId,
        X = location.X,
        Y = location.Y
    };

    public static SettlementEconomyProfileSaveData ToSaveData(SettlementEconomyProfile profile) => new()
    {
        SettlementId = profile.SettlementId,
        AgriculturePotential = profile.AgriculturePotential,
        FishingMultiplier = profile.FishingMultiplier,
        HuntingMultiplier = profile.HuntingMultiplier,
        MainlandSupplyMultiplier = profile.MainlandSupplyMultiplier,
        ResourceGatheringMultiplier = profile.ResourceGatheringMultiplier,
        GoodsCraftingMultiplier = profile.GoodsCraftingMultiplier,
        IsPort = profile.IsPort,
        IsFortress = profile.IsFortress,
        IsCapital = profile.IsCapital
    };

    public static CaravanSaveData ToSaveData(Caravan caravan) => new()
    {
        Id = caravan.Id,
        OwnerSettlementId = caravan.OwnerSettlementId,
        Type = caravan.Type.ToString(),
        Capacity = caravan.Capacity,
        RequiredWorkers = caravan.RequiredWorkers,
        IsAvailable = caravan.IsAvailable,
        PurchaseCost = caravan.PurchaseCost,
        UpkeepPerWeek = caravan.UpkeepPerWeek,
        Status = caravan.Status.ToString()
    };

    private static City ToCoreCity(CitySaveData cityData, string filePath)
    {
        if (!Enum.TryParse<CityState>(cityData.CityState, true, out var parsedCityState))
            throw new InvalidDataException($"Save file '{filePath}' contains unknown city_state '{cityData.CityState}'.");
        if (cityData.Infrastructure is null)
            throw new InvalidDataException($"Save file '{filePath}' city '{cityData.Id}' is missing infrastructure data.");

        return new City(cityData.Id, cityData.Name, cityData.Population, cityData.Food, cityData.Wealth, cityData.Mood, cityData.Security, cityData.Crime, cityData.Resources, cityData.Goods, parsedCityState, ToCoreInfrastructure(cityData.Infrastructure));
    }

    private static CityInfrastructure ToCoreInfrastructure(CityInfrastructureSaveData infrastructureData) => new()
    {
        HousingLevel = infrastructureData.HousingLevel,
        UrbanLevel = infrastructureData.UrbanLevel,
        ProductionLevel = infrastructureData.ProductionLevel,
        MilitaryLevel = infrastructureData.MilitaryLevel
    };

    public static Region ToCoreRegion(RegionSaveData regionData) => new()
    {
        Id = regionData.Id,
        DisplayName = regionData.DisplayName,
        MapAssetId = regionData.MapAssetId
    };

    public static SettlementMapLocation ToCoreSettlementMapLocation(SettlementMapLocationSaveData locationData) => new()
    {
        SettlementId = locationData.SettlementId,
        RegionId = locationData.RegionId,
        X = locationData.X,
        Y = locationData.Y
    };

    public static SettlementEconomyProfile ToCoreSettlementEconomyProfile(SettlementEconomyProfileSaveData profileData) => new()
    {
        SettlementId = profileData.SettlementId,
        AgriculturePotential = profileData.AgriculturePotential,
        FishingMultiplier = profileData.FishingMultiplier,
        HuntingMultiplier = profileData.HuntingMultiplier,
        MainlandSupplyMultiplier = profileData.MainlandSupplyMultiplier,
        ResourceGatheringMultiplier = profileData.ResourceGatheringMultiplier,
        GoodsCraftingMultiplier = profileData.GoodsCraftingMultiplier,
        IsPort = profileData.IsPort,
        IsFortress = profileData.IsFortress,
        IsCapital = profileData.IsCapital
    };

    public static Caravan ToCoreCaravan(CaravanSaveData caravanData)
    {
        if (!Enum.TryParse<CaravanType>(caravanData.Type, true, out var caravanType))
            throw new InvalidDataException($"Unknown caravan type '{caravanData.Type}'.");
        if (!Enum.TryParse<CaravanStatus>(caravanData.Status, true, out var caravanStatus))
            throw new InvalidDataException($"Unknown caravan status '{caravanData.Status}'.");

        return new Caravan { Id = caravanData.Id, OwnerSettlementId = caravanData.OwnerSettlementId, Type = caravanType, Capacity = caravanData.Capacity, RequiredWorkers = caravanData.RequiredWorkers, IsAvailable = caravanData.IsAvailable, PurchaseCost = caravanData.PurchaseCost, UpkeepPerWeek = caravanData.UpkeepPerWeek, Status = caravanStatus };
    }

    public static TradeRouteSaveData ToSaveData(TradeRoute route) => new()
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

    public static TradeRoute ToCoreTradeRoute(TradeRouteSaveData routeData)
    {
        if (!Enum.TryParse<CaravanType>(routeData.Type, true, out var caravanType))
            throw new InvalidDataException($"Unknown trade route caravan type '{routeData.Type}'.");

        return new TradeRoute
        {
            Id = routeData.Id,
            FromSettlementId = routeData.FromSettlementId,
            ToSettlementId = routeData.ToSettlementId,
            Type = caravanType,
            Distance = routeData.Distance,
            TravelDays = routeData.TravelDays,
            DistanceDays = routeData.DistanceDays ?? WorldSaveDefaults.GetDefaultDistanceDays(routeData.Id),
            IsEnabled = routeData.IsEnabled,
            DifficultyMultiplier = routeData.DifficultyMultiplier,
            Points = routeData.Points?.Select(point => new RoutePoint { X = point.X, Y = point.Y }).ToList() ?? []
        };
    }

    public static TradeShipmentSaveData ToSaveData(TradeShipment shipment) => new()
    {
        Id = shipment.Id,
        CaravanId = shipment.CaravanId,
        RouteId = shipment.RouteId,
        FromSettlementId = shipment.FromSettlementId,
        ToSettlementId = shipment.ToSettlementId,
        GoodType = shipment.GoodType.ToString(),
        Amount = shipment.Amount,
        DepartureDay = shipment.DepartureDay,
        ArrivalDay = shipment.ArrivalDay,
        ReturnDay = shipment.ReturnDay,
        ExporterWealthDelta = shipment.ExporterWealthDelta,
        ImporterWealthDelta = shipment.ImporterWealthDelta,
        Status = shipment.Status.ToString()
    };

    public static TradeShipment ToCoreTradeShipment(TradeShipmentSaveData shipmentData)
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

    public static CityEventSaveData ToSaveData(CityEvent cityEvent) => new() { Id = cityEvent.Id, Name = cityEvent.Name, Description = cityEvent.Description, StartedDay = cityEvent.StartedDay, DurationDays = cityEvent.DurationDays, RemainingDays = cityEvent.RemainingDays };
    public static CityEvent ToCoreEvent(CityEventSaveData eventSaveData) => new(eventSaveData.Id, eventSaveData.Name, eventSaveData.Description, eventSaveData.StartedDay, eventSaveData.DurationDays, eventSaveData.RemainingDays);
}
