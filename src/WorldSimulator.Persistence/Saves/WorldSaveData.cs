using WorldSimulator.Core.Cities;
using WorldSimulator.Core.Events;
using WorldSimulator.Core.Time;
using WorldSimulator.Core.World;

namespace WorldSimulator.Persistence.Saves;

public sealed class WorldSaveData
{
    public int Version { get; set; } = 2;
    public DateTime SavedAtUtc { get; set; }
    public ClockSaveData Clock { get; set; } = new();
    public SimulationWorldSaveData? World { get; set; }
    public CitySaveData? City { get; set; }
    public EventSaveData Events { get; set; } = new();
}

public sealed class SimulationWorldSaveData
{
    public List<CitySaveData> Cities { get; set; } = new();
    public Dictionary<string, CitySaveData> CitiesById { get; set; } = new(StringComparer.Ordinal);
    public List<RegionSaveData> Regions { get; set; } = new();
    public List<SettlementMapLocationSaveData> SettlementMapLocations { get; set; } = new();
    public List<SettlementEconomyProfileSaveData> SettlementEconomyProfiles { get; set; } = new();
    public List<CaravanSaveData> Caravans { get; set; } = new();
    public List<TradeRouteSaveData> TradeRoutes { get; set; } = new();
    public List<TradeShipmentSaveData> TradeShipments { get; set; } = new();
    public string SelectedCityId { get; set; } = string.Empty;
    public string SelectedRegionId { get; set; } = string.Empty;
}

public sealed class ClockSaveData { public int Day { get; set; } public int Hour { get; set; } public bool IsRunning { get; set; } public TimeSpan AccumulatedRealTime { get; set; } public TimeSpan RealTimePerGameHour { get; set; } }
public sealed class CitySaveData { public string Id { get; set; } = string.Empty; public string Name { get; set; } = string.Empty; public int Population { get; set; } public decimal Food { get; set; } public decimal Wealth { get; set; } public int Mood { get; set; } public int Security { get; set; } public int Crime { get; set; } public decimal Resources { get; set; } public decimal Goods { get; set; } public string CityState { get; set; } = string.Empty; }
public sealed class RegionSaveData { public string Id { get; set; } = string.Empty; public string DisplayName { get; set; } = string.Empty; public string MapAssetId { get; set; } = string.Empty; }
public sealed class SettlementMapLocationSaveData { public string SettlementId { get; set; } = string.Empty; public string RegionId { get; set; } = string.Empty; public decimal X { get; set; } public decimal Y { get; set; } }
public sealed class SettlementEconomyProfileSaveData { public string SettlementId { get; set; } = string.Empty; public decimal AgriculturePotential { get; set; } public decimal FishingMultiplier { get; set; } public decimal HuntingMultiplier { get; set; } public decimal MainlandSupplyMultiplier { get; set; } public decimal ResourceGatheringMultiplier { get; set; } public decimal GoodsCraftingMultiplier { get; set; } public bool IsPort { get; set; } public bool IsFortress { get; set; } public bool IsCapital { get; set; } }
public sealed class CaravanSaveData { public string Id { get; set; } = string.Empty; public string OwnerSettlementId { get; set; } = string.Empty; public string Type { get; set; } = string.Empty; public decimal Capacity { get; set; } public int RequiredWorkers { get; set; } public bool IsAvailable { get; set; } public decimal PurchaseCost { get; set; } public decimal UpkeepPerWeek { get; set; } public string Status { get; set; } = string.Empty; }
public sealed class EventSaveData { public List<CityEventSaveData> ActiveEvents { get; set; } = new(); public List<CityEventSaveData> CompletedEvents { get; set; } = new(); public Dictionary<string, CityEventBucketSaveData> EventsByCityId { get; set; } = new(); }
public sealed class CityEventBucketSaveData { public List<CityEventSaveData> ActiveEvents { get; set; } = new(); public List<CityEventSaveData> CompletedEvents { get; set; } = new(); }
public sealed class CityEventSaveData { public string Id { get; set; } = string.Empty; public string Name { get; set; } = string.Empty; public string Description { get; set; } = string.Empty; public int StartedDay { get; set; } public int DurationDays { get; set; } public int RemainingDays { get; set; } }

public sealed record WorldLoadResult(SimulationWorld World, SimulationClock Clock, WorldEventState EventState);

public sealed class TradeRouteSaveData { public string Id { get; set; } = string.Empty; public string FromSettlementId { get; set; } = string.Empty; public string ToSettlementId { get; set; } = string.Empty; public string Type { get; set; } = string.Empty; public decimal Distance { get; set; } public int TravelDays { get; set; } public decimal? DistanceDays { get; set; } public bool IsEnabled { get; set; } public decimal DifficultyMultiplier { get; set; } = 1m; public List<RoutePointSaveData> Points { get; set; } = new(); }
public sealed class RoutePointSaveData { public decimal X { get; set; } public decimal Y { get; set; } }
public sealed class TradeShipmentSaveData { public string Id { get; set; } = string.Empty; public string CaravanId { get; set; } = string.Empty; public string RouteId { get; set; } = string.Empty; public string FromSettlementId { get; set; } = string.Empty; public string ToSettlementId { get; set; } = string.Empty; public string GoodType { get; set; } = string.Empty; public decimal Amount { get; set; } public int DepartureDay { get; set; } public int ArrivalDay { get; set; } public int ReturnDay { get; set; } public decimal ExporterWealthDelta { get; set; } public decimal ImporterWealthDelta { get; set; } public string Status { get; set; } = string.Empty; }
