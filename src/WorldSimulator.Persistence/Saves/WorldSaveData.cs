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

    // v1 compatibility
    public CitySaveData? City { get; set; }

    public EventSaveData Events { get; set; } = new();
}

public sealed class SimulationWorldSaveData
{
    public List<CitySaveData> Cities { get; set; } = new();
    public List<RegionSaveData> Regions { get; set; } = new();
    public List<SettlementMapLocationSaveData> SettlementMapLocations { get; set; } = new();
    public List<SettlementEconomyProfileSaveData> SettlementEconomyProfiles { get; set; } = new();
    public List<CaravanSaveData> Caravans { get; set; } = new();
    public string SelectedCityId { get; set; } = string.Empty;
    public string SelectedRegionId { get; set; } = string.Empty;
}

public sealed class ClockSaveData
{
    public int Day { get; set; }
    public int Hour { get; set; }
    public bool IsRunning { get; set; }
    public TimeSpan AccumulatedRealTime { get; set; }
    public TimeSpan RealTimePerGameHour { get; set; }
}

public sealed class CitySaveData
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public int Population { get; set; }
    public decimal Food { get; set; }
    public decimal Wealth { get; set; }
    public int Mood { get; set; }
    public int Security { get; set; }
    public int Crime { get; set; }
    public decimal Resources { get; set; }
    public decimal Goods { get; set; }
    public string CityState { get; set; } = string.Empty;
}

public sealed class RegionSaveData
{
    public string Id { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public string MapAssetId { get; set; } = string.Empty;
}

public sealed class SettlementMapLocationSaveData
{
    public string SettlementId { get; set; } = string.Empty;
    public string RegionId { get; set; } = string.Empty;
    public decimal X { get; set; }
    public decimal Y { get; set; }
}

public sealed class SettlementEconomyProfileSaveData
{
    public string SettlementId { get; set; } = string.Empty;
    public decimal AgriculturePotential { get; set; }
    public decimal FishingMultiplier { get; set; }
    public decimal HuntingMultiplier { get; set; }
    public decimal MainlandSupplyMultiplier { get; set; }
    public decimal ResourceGatheringMultiplier { get; set; }
    public decimal GoodsCraftingMultiplier { get; set; }
    public bool IsPort { get; set; }
    public bool IsFortress { get; set; }
    public bool IsCapital { get; set; }
}

public sealed class CaravanSaveData
{
    public string Id { get; set; } = string.Empty;
    public string OwnerSettlementId { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty;
    public decimal Capacity { get; set; }
    public int RequiredWorkers { get; set; }
    public bool IsAvailable { get; set; }
}

public sealed class EventSaveData
{
    public List<CityEventSaveData> ActiveEvents { get; set; } = new();
    public List<CityEventSaveData> CompletedEvents { get; set; } = new();
    public Dictionary<string, CityEventBucketSaveData> EventsByCityId { get; set; } = new();
}

public sealed class CityEventBucketSaveData
{
    public List<CityEventSaveData> ActiveEvents { get; set; } = new();
    public List<CityEventSaveData> CompletedEvents { get; set; } = new();
}

public sealed class CityEventSaveData
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public int StartedDay { get; set; }
    public int DurationDays { get; set; }
    public int RemainingDays { get; set; }
}

public sealed record WorldLoadResult(
    SimulationWorld World,
    SimulationClock Clock,
    IReadOnlyList<CityEvent> ActiveEvents,
    IReadOnlyList<CityEvent> CompletedEvents);
