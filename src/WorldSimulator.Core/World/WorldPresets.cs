using WorldSimulator.Core.Cities;

namespace WorldSimulator.Core.World;

public static class WorldPresets
{
    public static SimulationWorld CreateDefaultWorld()
    {
        return new SimulationWorld
        {
            Cities =
            [
                CityPresets.CreateGotha(),
                CityPresets.CreateHighrock(),
                CityPresets.CreateMlynek(),
                CityPresets.CreateWardmark(),
                CityPresets.CreateRivenstal(),
                CityPresets.CreateGavern(),
                CityPresets.CreateBrno(),
                CityPresets.CreateWodenz(),
                CityPresets.CreateThokurRus()
            ],
            SettlementMapLocations =
            [
                new SettlementMapLocation { SettlementId = "gotha", X = 0.6664m, Y = 0.2322m },
                new SettlementMapLocation { SettlementId = "rivenstal", X = 0.4824m, Y = 0.4500m },
                new SettlementMapLocation { SettlementId = "gavern", X = 0.5066m, Y = 0.5963m },
                new SettlementMapLocation { SettlementId = "mlynek", X = 0.2833m, Y = 0.2487m },
                new SettlementMapLocation { SettlementId = "brno", X = 0.4527m, Y = 0.7448m },
                new SettlementMapLocation { SettlementId = "wodenz", X = 0.8036m, Y = 0.9604m },
                new SettlementMapLocation { SettlementId = "wardmark", X = 0.0380m, Y = 0.4027m },
                new SettlementMapLocation { SettlementId = "highrock", X = 0.1579m, Y = 0.2179m },
                new SettlementMapLocation { SettlementId = "thokur_rus", X = 0.8652m, Y = 0.4753m }
            ],
            SelectedCityId = "gotha"
        };
    }
}
