using WorldSimulator.Core.Cities;
using WorldSimulator.Core.Trade;

namespace WorldSimulator.Core.World;

public sealed class SimulationWorld
{
    public required List<City> Cities { get; init; }
    public required List<SettlementMapLocation> SettlementMapLocations { get; init; }
    public required List<SettlementEconomyProfile> SettlementEconomyProfiles { get; init; }
    public required List<Caravan> Caravans { get; init; }
    public required string SelectedCityId { get; set; }

    public City SelectedCity => Cities.First(c => c.Id == SelectedCityId);

    public City? FindCity(string id)
    {
        return Cities.FirstOrDefault(c => c.Id == id);
    }

    public SettlementMapLocation? FindSettlementMapLocation(string settlementId)
    {
        return SettlementMapLocations.FirstOrDefault(x => x.SettlementId == settlementId);
    }

    public SettlementEconomyProfile? FindSettlementEconomyProfile(string settlementId)
    {
        return SettlementEconomyProfiles.FirstOrDefault(x => x.SettlementId == settlementId);
    }

    public IReadOnlyList<Caravan> GetCaravansForSettlement(string settlementId)
    {
        return Caravans.Where(x => x.OwnerSettlementId == settlementId).ToList();
    }
}
