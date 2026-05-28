using WorldSimulator.Core.World;

namespace WorldSimulator.Core.Cities;

public static class CityPresets
{
    /// <summary>
    /// Gotha start preset for MVP 0.1.
    /// </summary>
    public static City CreateGotha() => RiviaSettlementPresets.CreateCity(RiviaSettlementPresets.GothaId);

    public static City CreateHighrock() => RiviaSettlementPresets.CreateCity(RiviaSettlementPresets.HighrockId);

    public static City CreateMlynek() => RiviaSettlementPresets.CreateCity(RiviaSettlementPresets.MlynekId);

    public static City CreateWardmark() => RiviaSettlementPresets.CreateCity(RiviaSettlementPresets.WardmarkId);

    public static City CreateRivenstal() => RiviaSettlementPresets.CreateCity(RiviaSettlementPresets.RivenstalId);

    public static City CreateGavern() => RiviaSettlementPresets.CreateCity(RiviaSettlementPresets.GavernId);

    public static City CreateBrno() => RiviaSettlementPresets.CreateCity(RiviaSettlementPresets.BrnoId);

    public static City CreateWodenz() => RiviaSettlementPresets.CreateCity(RiviaSettlementPresets.WodenzId);

    public static City CreateThokurRus() => RiviaSettlementPresets.CreateCity(RiviaSettlementPresets.ThokurRusId);
}
