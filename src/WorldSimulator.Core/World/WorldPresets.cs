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
            SettlementEconomyProfiles =
            [
                new SettlementEconomyProfile { SettlementId = "gotha", AgriculturePotential = 8m, FishingMultiplier = 1.00m, HuntingMultiplier = 0.50m, MainlandSupplyMultiplier = 1.00m, ResourceGatheringMultiplier = 0.60m, GoodsCraftingMultiplier = 1.00m, IsPort = true, IsFortress = false, IsCapital = false },
                new SettlementEconomyProfile { SettlementId = "highrock", AgriculturePotential = 6m, FishingMultiplier = 0m, HuntingMultiplier = 0.40m, MainlandSupplyMultiplier = 0m, ResourceGatheringMultiplier = 0.90m, GoodsCraftingMultiplier = 0.80m, IsPort = false, IsFortress = true, IsCapital = true },
                new SettlementEconomyProfile { SettlementId = "mlynek", AgriculturePotential = 26m, FishingMultiplier = 0m, HuntingMultiplier = 0.80m, MainlandSupplyMultiplier = 0m, ResourceGatheringMultiplier = 0.90m, GoodsCraftingMultiplier = 0.35m, IsPort = false, IsFortress = false, IsCapital = false },
                new SettlementEconomyProfile { SettlementId = "wardmark", AgriculturePotential = 10m, FishingMultiplier = 0m, HuntingMultiplier = 0.70m, MainlandSupplyMultiplier = 0m, ResourceGatheringMultiplier = 0.85m, GoodsCraftingMultiplier = 0.30m, IsPort = false, IsFortress = true, IsCapital = false },
                new SettlementEconomyProfile { SettlementId = "rivenstal", AgriculturePotential = 34m, FishingMultiplier = 0m, HuntingMultiplier = 0.50m, MainlandSupplyMultiplier = 0m, ResourceGatheringMultiplier = 0.45m, GoodsCraftingMultiplier = 0.45m, IsPort = false, IsFortress = false, IsCapital = false },
                new SettlementEconomyProfile { SettlementId = "gavern", AgriculturePotential = 12m, FishingMultiplier = 0m, HuntingMultiplier = 0.50m, MainlandSupplyMultiplier = 0m, ResourceGatheringMultiplier = 1.15m, GoodsCraftingMultiplier = 0.80m, IsPort = false, IsFortress = false, IsCapital = false },
                new SettlementEconomyProfile { SettlementId = "brno", AgriculturePotential = 24m, FishingMultiplier = 0m, HuntingMultiplier = 0.40m, MainlandSupplyMultiplier = 0m, ResourceGatheringMultiplier = 0.35m, GoodsCraftingMultiplier = 0.20m, IsPort = false, IsFortress = false, IsCapital = false },
                new SettlementEconomyProfile { SettlementId = "wodenz", AgriculturePotential = 36m, FishingMultiplier = 0m, HuntingMultiplier = 0.45m, MainlandSupplyMultiplier = 0m, ResourceGatheringMultiplier = 0.40m, GoodsCraftingMultiplier = 0.35m, IsPort = false, IsFortress = false, IsCapital = false },
                new SettlementEconomyProfile { SettlementId = "thokur_rus", AgriculturePotential = 2m, FishingMultiplier = 0.70m, HuntingMultiplier = 0.10m, MainlandSupplyMultiplier = 0.20m, ResourceGatheringMultiplier = 0.15m, GoodsCraftingMultiplier = 0.10m, IsPort = true, IsFortress = false, IsCapital = false }
            ],
            SelectedCityId = "gotha"
        };
    }
}
