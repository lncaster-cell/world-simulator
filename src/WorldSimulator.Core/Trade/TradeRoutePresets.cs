namespace WorldSimulator.Core.Trade;

public static class TradeRoutePresets
{
    public static List<TradeRoute> CreateDefaultRoutes() =>
    [
        new()
        {
            Id = "highrock_mlynek_land", FromSettlementId = "highrock", ToSettlementId = "mlynek", Type = CaravanType.Land, Distance = 90m, TravelDays = 3, IsEnabled = true,
            Points = [Pt(0.1579m, 0.2179m), Pt(0.2200m, 0.2300m), Pt(0.2833m, 0.2487m)]
        },
        new()
        {
            Id = "mlynek_rivenstal_land", FromSettlementId = "mlynek", ToSettlementId = "rivenstal", Type = CaravanType.Land, Distance = 140m, TravelDays = 4, IsEnabled = true,
            Points = [Pt(0.2833m, 0.2487m), Pt(0.3600m, 0.3300m), Pt(0.4824m, 0.4500m)]
        },
        new()
        {
            Id = "wardmark_rivenstal_land", FromSettlementId = "wardmark", ToSettlementId = "rivenstal", Type = CaravanType.Land, Distance = 160m, TravelDays = 5, IsEnabled = true,
            Points = [Pt(0.0380m, 0.4027m), Pt(0.2200m, 0.4300m), Pt(0.4824m, 0.4500m)]
        },
        new()
        {
            Id = "rivenstal_gavern_land", FromSettlementId = "rivenstal", ToSettlementId = "gavern", Type = CaravanType.Land, Distance = 70m, TravelDays = 2, IsEnabled = true,
            Points = [Pt(0.4824m, 0.4500m), Pt(0.4980m, 0.5250m), Pt(0.5066m, 0.5963m)]
        },
        new()
        {
            Id = "gavern_brno_land", FromSettlementId = "gavern", ToSettlementId = "brno", Type = CaravanType.Land, Distance = 110m, TravelDays = 3, IsEnabled = true,
            Points = [Pt(0.5066m, 0.5963m), Pt(0.4800m, 0.6700m), Pt(0.4527m, 0.7448m)]
        },
        new()
        {
            Id = "rivenstal_brno", FromSettlementId = "rivenstal", ToSettlementId = "brno", Type = CaravanType.Land, Distance = 150m, TravelDays = 4, IsEnabled = true,
            // TODO: replace intermediate point with authored road fork coordinate.
            Points = [Pt(0.4824m, 0.4500m), Pt(0.4680m, 0.6100m), Pt(0.4527m, 0.7448m)]
        },
        new()
        {
            Id = "brno_wodenz_land", FromSettlementId = "brno", ToSettlementId = "wodenz", Type = CaravanType.Land, Distance = 180m, TravelDays = 5, IsEnabled = true,
            Points = [Pt(0.4527m, 0.7448m), Pt(0.6200m, 0.8600m), Pt(0.8036m, 0.9604m)]
        },
        new()
        {
            Id = "gotha_thokur_rus_sea", FromSettlementId = "gotha", ToSettlementId = "thokur_rus", Type = CaravanType.Sea, Distance = 230m, TravelDays = 6, IsEnabled = true,
            Points = [Pt(0.6664m, 0.2322m), Pt(0.7900m, 0.3200m), Pt(0.8800m, 0.4100m), Pt(0.8652m, 0.4753m)]
        },
        new()
        {
            Id = "gotha_rivenstal_sea", FromSettlementId = "gotha", ToSettlementId = "rivenstal", Type = CaravanType.Sea, Distance = 150m, TravelDays = 4, IsEnabled = true,
            Points = [Pt(0.6664m, 0.2322m), Pt(0.6200m, 0.3200m), Pt(0.5600m, 0.3900m), Pt(0.4824m, 0.4500m)]
        }
    ];

    private static RoutePoint Pt(decimal x, decimal y) => new() { X = x, Y = y };
}
