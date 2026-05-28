using WorldSimulator.Core.Trade;

namespace WorldSimulator.Core.World;

public static class WorldPresets
{
    public static SimulationWorld CreateDefaultWorld()
    {
        return new SimulationWorld
        {
            Cities = RiviaSettlementPresets.CreateCities(),
            Regions =
            [
                RegionPresets.CreateRiviaRegion()
            ],
            SettlementMapLocations = RiviaSettlementPresets.CreateMapLocations(RegionPresets.RiviaRegionId),
            SettlementEconomyProfiles = RiviaSettlementPresets.CreateEconomyProfiles(),
            SettlementSectorCapacityProfiles = RiviaSettlementPresets.CreateSectorCapacityProfiles(),
            Caravans =
            [
                CaravanPresets.Create("gotha_land_1", RiviaSettlementPresets.GothaId, CaravanType.Land),
                CaravanPresets.Create("gotha_land_2", RiviaSettlementPresets.GothaId, CaravanType.Land),
                CaravanPresets.Create("gotha_sea_1", RiviaSettlementPresets.GothaId, CaravanType.Sea),
                CaravanPresets.Create("gotha_sea_2", RiviaSettlementPresets.GothaId, CaravanType.Sea),
                CaravanPresets.Create("highrock_land_1", RiviaSettlementPresets.HighrockId, CaravanType.Land),
                CaravanPresets.Create("highrock_land_2", RiviaSettlementPresets.HighrockId, CaravanType.Land),
                CaravanPresets.Create("mlynek_land_1", RiviaSettlementPresets.MlynekId, CaravanType.Land),
                CaravanPresets.Create("wardmark_land_1", RiviaSettlementPresets.WardmarkId, CaravanType.Land),
                CaravanPresets.Create("rivenstal_land_1", RiviaSettlementPresets.RivenstalId, CaravanType.Land),
                CaravanPresets.Create("rivenstal_land_2", RiviaSettlementPresets.RivenstalId, CaravanType.Land),
                CaravanPresets.Create("gavern_land_1", RiviaSettlementPresets.GavernId, CaravanType.Land),
                CaravanPresets.Create("gavern_land_2", RiviaSettlementPresets.GavernId, CaravanType.Land),
                CaravanPresets.Create("brno_land_1", RiviaSettlementPresets.BrnoId, CaravanType.Land),
                CaravanPresets.Create("wodenz_land_1", RiviaSettlementPresets.WodenzId, CaravanType.Land),
                CaravanPresets.Create("thokur_rus_sea_1", RiviaSettlementPresets.ThokurRusId, CaravanType.Sea)
            ],
            TradeRoutes = TradeRoutePresets.CreateDefaultRoutes(),
            TradeShipments = [],
            SelectedCityId = RiviaSettlementPresets.GothaId,
            SelectedRegionId = RegionPresets.RiviaRegionId
        };
    }
}
