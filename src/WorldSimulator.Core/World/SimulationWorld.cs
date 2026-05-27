using WorldSimulator.Core.Cities;
using WorldSimulator.Core.Trade;

namespace WorldSimulator.Core.World;

public sealed class SimulationWorld
{
    public required List<City> Cities { get; init; }
    public required List<Region> Regions { get; init; }
    public required List<SettlementMapLocation> SettlementMapLocations { get; init; }
    public required List<SettlementEconomyProfile> SettlementEconomyProfiles { get; init; }
    public required List<Caravan> Caravans { get; init; }
    public required List<TradeRoute> TradeRoutes { get; init; }
    public required List<TradeShipment> TradeShipments { get; init; }
    public required string SelectedCityId { get; set; }
    public required string SelectedRegionId { get; set; }

    public City SelectedCity => TryGetSelectedCity(out var city)
        ? city
        : throw new InvalidOperationException(
            $"Selected city '{SelectedCityId}' was not found. Available cities: {Cities.Count}.");
    public Region SelectedRegion => TryGetSelectedRegion(out var region)
        ? region
        : throw new InvalidOperationException(
            $"Selected region '{SelectedRegionId}' was not found. Available regions: {Regions.Count}.");

    public bool TryGetSelectedCity(out City city)
    {
        city = Cities.FirstOrDefault(c => c.Id == SelectedCityId)!;
        return city is not null;
    }

    public bool TryGetSelectedRegion(out Region region)
    {
        region = Regions.FirstOrDefault(x => x.Id == SelectedRegionId)!;
        return region is not null;
    }

    public bool EnsureValidSelection(out string? reason)
    {
        reason = null;

        if (TryGetSelectedCity(out _) && TryGetSelectedRegion(out _))
        {
            return true;
        }

        var originalCityId = SelectedCityId;
        var originalRegionId = SelectedRegionId;
        var selectedCityWasMissing = !TryGetSelectedCity(out _);
        var selectedRegionWasMissing = !TryGetSelectedRegion(out _);

        if (selectedCityWasMissing && Cities.Count > 0)
        {
            SelectedCityId = Cities[0].Id;
        }

        if (selectedRegionWasMissing && Regions.Count > 0)
        {
            SelectedRegionId = Regions[0].Id;
        }

        reason =
            $"World selection was invalid. City '{originalCityId}' exists: {!selectedCityWasMissing}; " +
            $"region '{originalRegionId}' exists: {!selectedRegionWasMissing}. " +
            $"Fallback city='{SelectedCityId}' (cities={Cities.Count}), region='{SelectedRegionId}' (regions={Regions.Count}).";

        return TryGetSelectedCity(out _) && TryGetSelectedRegion(out _);
    }

    public City? FindCity(string id)
    {
        return Cities.FirstOrDefault(c => c.Id == id);
    }

    public Region? FindRegion(string id)
    {
        return Regions.FirstOrDefault(x => x.Id == id);
    }

    public SettlementMapLocation? FindSettlementMapLocation(string settlementId)
    {
        return SettlementMapLocations.FirstOrDefault(x => x.SettlementId == settlementId);
    }

    public SettlementMapLocation? FindSettlementMapLocation(string settlementId, string regionId)
    {
        return SettlementMapLocations.FirstOrDefault(x =>
            x.SettlementId == settlementId &&
            x.RegionId == regionId);
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
