using WorldSimulator.Core.Cities;
using WorldSimulator.Core.Workforce;

namespace WorldSimulator.Core.World;

public static class RiviaSettlementPresets
{
    public const string GothaId = "gotha";
    public const string HighrockId = "highrock";
    public const string MlynekId = "mlynek";
    public const string WardmarkId = "wardmark";
    public const string RivenstalId = "rivenstal";
    public const string GavernId = "gavern";
    public const string BrnoId = "brno";
    public const string WodenzId = "wodenz";
    public const string ThokurRusId = "thokur_rus";

    private static readonly Lazy<Cache> PresetCache = new(BuildCache);

    public static IReadOnlyList<RiviaSettlementPreset> All => PresetCache.Value.Presets;

    public static RiviaSettlementPreset Get(string settlementId)
    {
        if (PresetCache.Value.PresetsById.TryGetValue(settlementId, out var preset))
        {
            return preset;
        }

        throw new ArgumentException($"Unknown Rivia settlement preset '{settlementId}'.", nameof(settlementId));
    }

    public static bool TryGet(string settlementId, out RiviaSettlementPreset preset)
    {
        var found = PresetCache.Value.PresetsById.TryGetValue(settlementId, out var value);
        preset = value!;
        return found;
    }

    public static bool TryGetSettlementIdByRouteNodeId(string routeNodeId, out string settlementId)
    {
        var found = PresetCache.Value.SettlementIdsByRouteNodeId.TryGetValue(routeNodeId, out var value);
        settlementId = value!;
        return found;
    }

    public static List<City> CreateCities() => All.Select(CreateCity).ToList();

    public static City CreateCity(string settlementId) => CreateCity(Get(settlementId));

    public static List<SettlementMapLocation> CreateMapLocations(string regionId)
        => All.Select(x => x.CreateMapLocation(regionId)).ToList();

    public static List<SettlementEconomyProfile> CreateEconomyProfiles()
        => All.Select(x => x.CreateEconomyProfile()).ToList();

    public static List<SettlementSectorCapacityProfile> CreateSectorCapacityProfiles()
        => All.Select(x => x.CreateSectorCapacityProfile()).ToList();

    private static Cache BuildCache()
    {
        var presets = RiviaSettlementPresetDataLoader.LoadPresets();
        return new Cache(
            Presets: presets,
            PresetsById: presets.ToDictionary(x => x.Id, StringComparer.OrdinalIgnoreCase),
            SettlementIdsByRouteNodeId: presets.ToDictionary(x => x.RouteNodeId, x => x.Id, StringComparer.OrdinalIgnoreCase));
    }

    private static City CreateCity(RiviaSettlementPreset preset) => new(
        id: preset.Id,
        name: preset.DisplayName,
        population: preset.CityStart.Population,
        food: preset.CityStart.Food,
        wealth: preset.CityStart.Wealth,
        mood: preset.CityStart.Mood,
        security: preset.CityStart.Security,
        crime: preset.CityStart.Crime,
        resources: preset.CityStart.Resources,
        goods: preset.CityStart.Goods,
        cityState: preset.CityStart.CityState);

    private sealed record Cache(
        IReadOnlyList<RiviaSettlementPreset> Presets,
        Dictionary<string, RiviaSettlementPreset> PresetsById,
        Dictionary<string, string> SettlementIdsByRouteNodeId);
}
