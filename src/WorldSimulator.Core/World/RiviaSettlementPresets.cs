namespace WorldSimulator.Core.World;

public static class RiviaSettlementPresets
{
    private static readonly Dictionary<string, SettlementMapLocation> SettlementLocations = new(StringComparer.OrdinalIgnoreCase)
    {
        ["gotha"] = Location("gotha", 0.6664m, 0.2322m),
        ["rivenstal"] = Location("rivenstal", 0.4824m, 0.4500m),
        ["gavern"] = Location("gavern", 0.5066m, 0.5963m),
        ["mlynek"] = Location("mlynek", 0.2833m, 0.2487m),
        ["brno"] = Location("brno", 0.4527m, 0.7448m),
        ["wodenz"] = Location("wodenz", 0.8036m, 0.9604m),
        ["wardmark"] = Location("wardmark", 0.0380m, 0.4027m),
        ["highrock"] = Location("highrock", 0.1579m, 0.2179m),
        ["thokur_rus"] = Location("thokur_rus", 0.8652m, 0.4753m)
    };

    public static IReadOnlyDictionary<string, SettlementMapLocation> Locations => SettlementLocations;

    public static List<SettlementMapLocation> CreateSettlementMapLocations()
        => SettlementLocations.Values.Select(Clone).ToList();

    public static bool TryGetLocation(string settlementId, out SettlementMapLocation location)
    {
        if (SettlementLocations.TryGetValue(settlementId, out var preset))
        {
            location = Clone(preset);
            return true;
        }

        location = null!;
        return false;
    }

    private static SettlementMapLocation Location(string settlementId, decimal x, decimal y)
        => new() { SettlementId = settlementId, RegionId = RegionPresets.RiviaRegionId, X = x, Y = y };

    private static SettlementMapLocation Clone(SettlementMapLocation location)
        => new()
        {
            SettlementId = location.SettlementId,
            RegionId = location.RegionId,
            X = location.X,
            Y = location.Y
        };
}
