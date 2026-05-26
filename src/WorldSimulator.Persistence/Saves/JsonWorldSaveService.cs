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

    public async Task SaveAsync(string filePath, SimulationWorld world, SimulationClock clock, CityEventManager eventManager, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(filePath);
        ArgumentNullException.ThrowIfNull(world);
        ArgumentNullException.ThrowIfNull(clock);
        ArgumentNullException.ThrowIfNull(eventManager);

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
                SelectedCityId = world.SelectedCityId,
                SelectedRegionId = world.SelectedRegionId
            },
            Events = new EventSaveData
            {
                ActiveEvents = eventManager.ActiveEvents.Select(ToSaveData).ToList(),
                CompletedEvents = eventManager.CompletedEvents.Select(ToSaveData).ToList()
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

        var activeEvents = (saveData.Events?.ActiveEvents ?? []).Select(ToCoreEvent).ToList();
        var completedEvents = (saveData.Events?.CompletedEvents ?? []).Select(ToCoreEvent).ToList();

        return new WorldLoadResult(world, clock, activeEvents, completedEvents);
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

    private static CityEventSaveData ToSaveData(CityEvent cityEvent) => new() { Id = cityEvent.Id, Name = cityEvent.Name, Description = cityEvent.Description, StartedDay = cityEvent.StartedDay, DurationDays = cityEvent.DurationDays, RemainingDays = cityEvent.RemainingDays };
    private static CityEvent ToCoreEvent(CityEventSaveData eventSaveData) => new(eventSaveData.Id, eventSaveData.Name, eventSaveData.Description, eventSaveData.StartedDay, eventSaveData.DurationDays, eventSaveData.RemainingDays);
}
