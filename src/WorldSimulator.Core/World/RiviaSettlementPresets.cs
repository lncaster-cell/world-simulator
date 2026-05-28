using WorldSimulator.Core.Cities;

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

    private static readonly RiviaSettlementPreset[] Presets =
    [
        new(
            Id: GothaId,
            DisplayName: "Гота",
            RouteNodeId: "N_GOTHA",
            X: 0.6664m,
            Y: 0.2322m,
            CityStart: new CityStartPreset(420, 1000m, 320m, 55, 60, 30, 260m, 140m, CityState.Stagnation),
            Economy: new SettlementEconomyPreset(8m, 1.00m, 0.50m, 1.00m, 0.60m, 1.00m),
            IsPort: true,
            IsFortress: false,
            IsCapital: false),
        new(
            Id: HighrockId,
            DisplayName: "Highrock",
            RouteNodeId: "N_HIGHROCK",
            X: 0.1579m,
            Y: 0.2179m,
            CityStart: new CityStartPreset(650, 900m, 380m, 55, 75, 8, 750m, 180m, CityState.Stable),
            Economy: new SettlementEconomyPreset(6m, 0m, 0.40m, 0m, 0.90m, 0.80m),
            IsPort: false,
            IsFortress: true,
            IsCapital: true),
        new(
            Id: MlynekId,
            DisplayName: "Mlynek",
            RouteNodeId: "N_MLYNEK",
            X: 0.2833m,
            Y: 0.2487m,
            CityStart: new CityStartPreset(320, 900m, 180m, 58, 55, 10, 420m, 100m, CityState.Stable),
            Economy: new SettlementEconomyPreset(48m, 0m, 1.10m, 0m, 0.90m, 0.35m),
            IsPort: false,
            IsFortress: false,
            IsCapital: false),
        new(
            Id: WardmarkId,
            DisplayName: "Wardmark",
            RouteNodeId: "N_WARDMARK",
            X: 0.0380m,
            Y: 0.4027m,
            CityStart: new CityStartPreset(280, 650m, 170m, 50, 80, 7, 500m, 90m, CityState.Stable),
            Economy: new SettlementEconomyPreset(10m, 0m, 0.70m, 0m, 0.85m, 0.30m),
            IsPort: false,
            IsFortress: true,
            IsCapital: false),
        new(
            Id: RivenstalId,
            DisplayName: "Rivenstal",
            RouteNodeId: "N_RIVENSTAL",
            X: 0.4824m,
            Y: 0.4500m,
            CityStart: new CityStartPreset(360, 1100m, 210m, 60, 58, 9, 300m, 110m, CityState.Stable),
            Economy: new SettlementEconomyPreset(34m, 0m, 0.50m, 0m, 0.45m, 0.45m),
            IsPort: false,
            IsFortress: false,
            IsCapital: false),
        new(
            Id: GavernId,
            DisplayName: "Gavern",
            RouteNodeId: "N_GAVERN",
            X: 0.5066m,
            Y: 0.5963m,
            CityStart: new CityStartPreset(520, 850m, 260m, 52, 52, 16, 800m, 180m, CityState.Stagnation),
            Economy: new SettlementEconomyPreset(12m, 0m, 0.50m, 0m, 1.15m, 0.80m),
            IsPort: false,
            IsFortress: false,
            IsCapital: false),
        new(
            Id: BrnoId,
            DisplayName: "Brno",
            RouteNodeId: "N_BRNO",
            X: 0.4527m,
            Y: 0.7448m,
            CityStart: new CityStartPreset(180, 700m, 120m, 62, 50, 8, 180m, 55m, CityState.Stable),
            Economy: new SettlementEconomyPreset(42m, 0m, 0.85m, 0m, 0.35m, 0.20m),
            IsPort: false,
            IsFortress: false,
            IsCapital: false),
        new(
            Id: WodenzId,
            DisplayName: "Wödenz",
            RouteNodeId: "N_WODENZ",
            X: 0.8036m,
            Y: 0.9604m,
            CityStart: new CityStartPreset(340, 1150m, 160m, 57, 48, 12, 260m, 90m, CityState.Stable),
            Economy: new SettlementEconomyPreset(60m, 0m, 0.90m, 0m, 0.40m, 0.35m),
            IsPort: false,
            IsFortress: false,
            IsCapital: false),
        new(
            Id: ThokurRusId,
            DisplayName: "Thökur-Rus",
            RouteNodeId: "N_TOKRUS",
            X: 0.8652m,
            Y: 0.4753m,
            CityStart: new CityStartPreset(80, 280m, 70m, 50, 35, 18, 90m, 25m, CityState.Stagnation),
            Economy: new SettlementEconomyPreset(2m, 0.70m, 0.10m, 0.20m, 0.15m, 0.10m),
            IsPort: true,
            IsFortress: false,
            IsCapital: false)
    ];

    private static readonly Dictionary<string, RiviaSettlementPreset> PresetsById = Presets.ToDictionary(
        x => x.Id,
        StringComparer.OrdinalIgnoreCase);

    private static readonly Dictionary<string, string> SettlementIdsByRouteNodeId = Presets.ToDictionary(
        x => x.RouteNodeId,
        x => x.Id,
        StringComparer.OrdinalIgnoreCase);

    public static IReadOnlyList<RiviaSettlementPreset> All => Presets;

    public static RiviaSettlementPreset Get(string settlementId)
    {
        if (PresetsById.TryGetValue(settlementId, out var preset))
        {
            return preset;
        }

        throw new ArgumentException($"Unknown Rivia settlement preset '{settlementId}'.", nameof(settlementId));
    }

    public static bool TryGet(string settlementId, out RiviaSettlementPreset preset)
    {
        var found = PresetsById.TryGetValue(settlementId, out var value);
        preset = value!;
        return found;
    }

    public static bool TryGetSettlementIdByRouteNodeId(string routeNodeId, out string settlementId)
    {
        var found = SettlementIdsByRouteNodeId.TryGetValue(routeNodeId, out var value);
        settlementId = value!;
        return found;
    }

    public static List<City> CreateCities() => Presets.Select(CreateCity).ToList();

    public static City CreateCity(string settlementId) => CreateCity(Get(settlementId));

    public static List<SettlementMapLocation> CreateMapLocations(string regionId)
        => Presets.Select(x => x.CreateMapLocation(regionId)).ToList();

    public static List<SettlementEconomyProfile> CreateEconomyProfiles()
        => Presets.Select(x => x.CreateEconomyProfile()).ToList();

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
}

public sealed record RiviaSettlementPreset(
    string Id,
    string DisplayName,
    string RouteNodeId,
    decimal X,
    decimal Y,
    CityStartPreset CityStart,
    SettlementEconomyPreset Economy,
    bool IsPort,
    bool IsFortress,
    bool IsCapital)
{
    public SettlementMapLocation CreateMapLocation(string regionId) => new()
    {
        SettlementId = Id,
        RegionId = regionId,
        X = X,
        Y = Y
    };

    public SettlementEconomyProfile CreateEconomyProfile() => new()
    {
        SettlementId = Id,
        AgriculturePotential = Economy.AgriculturePotential,
        FishingMultiplier = Economy.FishingMultiplier,
        HuntingMultiplier = Economy.HuntingMultiplier,
        MainlandSupplyMultiplier = Economy.MainlandSupplyMultiplier,
        ResourceGatheringMultiplier = Economy.ResourceGatheringMultiplier,
        GoodsCraftingMultiplier = Economy.GoodsCraftingMultiplier,
        IsPort = IsPort,
        IsFortress = IsFortress,
        IsCapital = IsCapital
    };
}

public sealed record CityStartPreset(
    int Population,
    decimal Food,
    decimal Wealth,
    int Mood,
    int Security,
    int Crime,
    decimal Resources,
    decimal Goods,
    CityState CityState);

public sealed record SettlementEconomyPreset(
    decimal AgriculturePotential,
    decimal FishingMultiplier,
    decimal HuntingMultiplier,
    decimal MainlandSupplyMultiplier,
    decimal ResourceGatheringMultiplier,
    decimal GoodsCraftingMultiplier);
