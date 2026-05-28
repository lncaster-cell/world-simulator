using WorldSimulator.Core.World;

namespace WorldSimulator.Persistence.Saves;

internal static class WorldSaveValidator
{
    public static void ValidateWorld(SimulationWorld world, string filePath)
    {
        ArgumentNullException.ThrowIfNull(world);

        if (world.Cities.Count == 0)
            throw new InvalidDataException($"Save file '{filePath}' has no cities.");
        if (world.Regions.Count == 0)
            throw new InvalidDataException($"Save file '{filePath}' has no regions.");
        if (world.SettlementMapLocations.Count == 0)
            throw new InvalidDataException($"Save file '{filePath}' has no settlement map locations.");
        if (world.SettlementEconomyProfiles.Count == 0)
            throw new InvalidDataException($"Save file '{filePath}' has no settlement economy profiles.");
        if (world.SettlementSectorCapacityProfiles.Count == 0)
            throw new InvalidDataException($"Save file '{filePath}' has no settlement sector capacity profiles.");
        if (world.Caravans.Count == 0)
            throw new InvalidDataException($"Save file '{filePath}' has no caravans.");
        if (world.TradeRoutes.Count == 0)
            throw new InvalidDataException($"Save file '{filePath}' has no trade routes.");

        if (!world.EnsureValidSelection(out _))
            throw new InvalidDataException(
                $"Save file '{filePath}' has invalid world selection and fallback could not be applied. " +
                $"Cities: {world.Cities.Count}, Regions: {world.Regions.Count}, SelectedCityId: '{world.SelectedCityId}', SelectedRegionId: '{world.SelectedRegionId}'.");

        ValidateRequiredReferences(world, filePath);
        ValidateShipments(world, filePath);
    }

    private static void ValidateRequiredReferences(SimulationWorld world, string filePath)
    {
        var cityIds = world.Cities.Select(x => x.Id).ToHashSet(StringComparer.Ordinal);
        var regionIds = world.Regions.Select(x => x.Id).ToHashSet(StringComparer.Ordinal);

        foreach (var location in world.SettlementMapLocations)
        {
            if (!cityIds.Contains(location.SettlementId)) throw new InvalidDataException($"Save file '{filePath}' settlement map location references unknown settlement '{location.SettlementId}'.");
            if (!regionIds.Contains(location.RegionId)) throw new InvalidDataException($"Save file '{filePath}' settlement map location for '{location.SettlementId}' references unknown region '{location.RegionId}'.");
        }

        foreach (var profile in world.SettlementEconomyProfiles)
        {
            if (!cityIds.Contains(profile.SettlementId)) throw new InvalidDataException($"Save file '{filePath}' economy profile references unknown settlement '{profile.SettlementId}'.");
        }

        foreach (var profile in world.SettlementSectorCapacityProfiles)
        {
            if (!cityIds.Contains(profile.SettlementId)) throw new InvalidDataException($"Save file '{filePath}' sector capacity profile references unknown settlement '{profile.SettlementId}'.");
        }

        foreach (var caravan in world.Caravans)
        {
            if (!cityIds.Contains(caravan.OwnerSettlementId)) throw new InvalidDataException($"Save file '{filePath}' caravan '{caravan.Id}' references unknown owner settlement '{caravan.OwnerSettlementId}'.");
        }

        foreach (var route in world.TradeRoutes)
        {
            if (!cityIds.Contains(route.FromSettlementId)) throw new InvalidDataException($"Save file '{filePath}' route '{route.Id}' references unknown origin settlement '{route.FromSettlementId}'.");
            if (!cityIds.Contains(route.ToSettlementId)) throw new InvalidDataException($"Save file '{filePath}' route '{route.Id}' references unknown destination settlement '{route.ToSettlementId}'.");
            if (route.DistanceDays < 0.1m) throw new InvalidDataException($"Save file '{filePath}' route '{route.Id}' has invalid distance days '{route.DistanceDays}'.");
        }
    }

    private static void ValidateShipments(SimulationWorld world, string filePath)
    {
        var caravanIds = world.Caravans.Select(x => x.Id).ToHashSet(StringComparer.Ordinal);
        var routeIds = world.TradeRoutes.Select(x => x.Id).ToHashSet(StringComparer.Ordinal);
        var settlementIds = world.Cities.Select(x => x.Id).ToHashSet(StringComparer.Ordinal);
        foreach (var shipment in world.TradeShipments)
        {
            if (!caravanIds.Contains(shipment.CaravanId)) throw new InvalidDataException($"Save file '{filePath}' shipment '{shipment.Id}' references unknown caravan '{shipment.CaravanId}'.");
            if (!routeIds.Contains(shipment.RouteId)) throw new InvalidDataException($"Save file '{filePath}' shipment '{shipment.Id}' references unknown route '{shipment.RouteId}'.");
            if (!settlementIds.Contains(shipment.FromSettlementId)) throw new InvalidDataException($"Save file '{filePath}' shipment '{shipment.Id}' references unknown origin settlement '{shipment.FromSettlementId}'.");
            if (!settlementIds.Contains(shipment.ToSettlementId)) throw new InvalidDataException($"Save file '{filePath}' shipment '{shipment.Id}' references unknown destination settlement '{shipment.ToSettlementId}'.");
            if (shipment.ArrivalDay < shipment.DepartureDay) throw new InvalidDataException($"Save file '{filePath}' shipment '{shipment.Id}' has arrival day before departure day.");
            if (shipment.ReturnDay < shipment.ArrivalDay) throw new InvalidDataException($"Save file '{filePath}' shipment '{shipment.Id}' has return day before arrival day.");
        }
    }
}
