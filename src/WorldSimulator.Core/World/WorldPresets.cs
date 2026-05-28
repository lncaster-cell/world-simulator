using WorldSimulator.Core.Cities;
using WorldSimulator.Core.Trade;

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
            Regions =
            [
                RegionPresets.CreateRiviaRegion()
            ],
            SettlementMapLocations = RiviaSettlementPresets.CreateSettlementMapLocations(),
            SettlementEconomyProfiles =
            [
                new SettlementEconomyProfile { SettlementId = "gotha", AgriculturePotential = 8m, FishingMultiplier = 1.00m, HuntingMultiplier = 0.50m, MainlandSupplyMultiplier = 1.00m, ResourceGatheringMultiplier = 0.60m, GoodsCraftingMultiplier = 1.00m, IsPort = true, IsFortress = false, IsCapital = false },
                new SettlementEconomyProfile { SettlementId = "highrock", AgriculturePotential = 6m, FishingMultiplier = 0m, HuntingMultiplier = 0.40m, MainlandSupplyMultiplier = 0m, ResourceGatheringMultiplier = 0.90m, GoodsCraftingMultiplier = 0.80m, IsPort = false, IsFortress = true, IsCapital = true },
                new SettlementEconomyProfile { SettlementId = "mlynek", AgriculturePotential = 48m, FishingMultiplier = 0m, HuntingMultiplier = 1.10m, MainlandSupplyMultiplier = 0m, ResourceGatheringMultiplier = 0.90m, GoodsCraftingMultiplier = 0.35m, IsPort = false, IsFortress = false, IsCapital = false },
                new SettlementEconomyProfile { SettlementId = "wardmark", AgriculturePotential = 10m, FishingMultiplier = 0m, HuntingMultiplier = 0.70m, MainlandSupplyMultiplier = 0m, ResourceGatheringMultiplier = 0.85m, GoodsCraftingMultiplier = 0.30m, IsPort = false, IsFortress = true, IsCapital = false },
                new SettlementEconomyProfile { SettlementId = "rivenstal", AgriculturePotential = 34m, FishingMultiplier = 0m, HuntingMultiplier = 0.50m, MainlandSupplyMultiplier = 0m, ResourceGatheringMultiplier = 0.45m, GoodsCraftingMultiplier = 0.45m, IsPort = false, IsFortress = false, IsCapital = false },
                new SettlementEconomyProfile { SettlementId = "gavern", AgriculturePotential = 12m, FishingMultiplier = 0m, HuntingMultiplier = 0.50m, MainlandSupplyMultiplier = 0m, ResourceGatheringMultiplier = 1.15m, GoodsCraftingMultiplier = 0.80m, IsPort = false, IsFortress = false, IsCapital = false },
                new SettlementEconomyProfile { SettlementId = "brno", AgriculturePotential = 42m, FishingMultiplier = 0m, HuntingMultiplier = 0.85m, MainlandSupplyMultiplier = 0m, ResourceGatheringMultiplier = 0.35m, GoodsCraftingMultiplier = 0.20m, IsPort = false, IsFortress = false, IsCapital = false },
                new SettlementEconomyProfile { SettlementId = "wodenz", AgriculturePotential = 60m, FishingMultiplier = 0m, HuntingMultiplier = 0.90m, MainlandSupplyMultiplier = 0m, ResourceGatheringMultiplier = 0.40m, GoodsCraftingMultiplier = 0.35m, IsPort = false, IsFortress = false, IsCapital = false },
                new SettlementEconomyProfile { SettlementId = "thokur_rus", AgriculturePotential = 2m, FishingMultiplier = 0.70m, HuntingMultiplier = 0.10m, MainlandSupplyMultiplier = 0.20m, ResourceGatheringMultiplier = 0.15m, GoodsCraftingMultiplier = 0.10m, IsPort = true, IsFortress = false, IsCapital = false }
            ],
            Caravans =
            [
                CaravanPresets.Create("gotha_land_1", "gotha", CaravanType.Land),
                CaravanPresets.Create("gotha_land_2", "gotha", CaravanType.Land),
                CaravanPresets.Create("gotha_sea_1", "gotha", CaravanType.Sea),
                CaravanPresets.Create("gotha_sea_2", "gotha", CaravanType.Sea),
                CaravanPresets.Create("highrock_land_1", "highrock", CaravanType.Land),
                CaravanPresets.Create("highrock_land_2", "highrock", CaravanType.Land),
                CaravanPresets.Create("mlynek_land_1", "mlynek", CaravanType.Land),
                CaravanPresets.Create("wardmark_land_1", "wardmark", CaravanType.Land),
                CaravanPresets.Create("rivenstal_land_1", "rivenstal", CaravanType.Land),
                CaravanPresets.Create("rivenstal_land_2", "rivenstal", CaravanType.Land),
                CaravanPresets.Create("gavern_land_1", "gavern", CaravanType.Land),
                CaravanPresets.Create("gavern_land_2", "gavern", CaravanType.Land),
                CaravanPresets.Create("brno_land_1", "brno", CaravanType.Land),
                CaravanPresets.Create("wodenz_land_1", "wodenz", CaravanType.Land),
                CaravanPresets.Create("thokur_rus_sea_1", "thokur_rus", CaravanType.Sea)
            ],
            TradeRoutes = TradeRoutePresets.CreateDefaultRoutes(),
            TradeShipments = [],
            SelectedCityId = "gotha",
            SelectedRegionId = RegionPresets.RiviaRegionId
        };
    }
}
