using WorldSimulator.Core.Trade;

namespace WorldSimulator.Persistence.Saves;

internal static partial class WorldSaveMapper
{
    public static CaravanSaveData ToSaveData(Caravan caravan) => new()
    {
        Id = caravan.Id,
        OwnerSettlementId = caravan.OwnerSettlementId,
        Type = caravan.Type.ToString(),
        Capacity = caravan.Capacity,
        RequiredWorkers = caravan.RequiredWorkers,
        IsAvailable = caravan.IsAvailable,
        PurchaseCost = caravan.PurchaseCost,
        UpkeepPerWeek = caravan.UpkeepPerWeek,
        Status = caravan.Status.ToString()
    };

    public static Caravan ToCoreCaravan(CaravanSaveData caravanData)
    {
        if (!Enum.TryParse<CaravanType>(caravanData.Type, true, out var caravanType))
            throw new InvalidDataException($"Unknown caravan type '{caravanData.Type}'.");
        if (!Enum.TryParse<CaravanStatus>(caravanData.Status, true, out var caravanStatus))
            throw new InvalidDataException($"Unknown caravan status '{caravanData.Status}'.");

        return new Caravan { Id = caravanData.Id, OwnerSettlementId = caravanData.OwnerSettlementId, Type = caravanType, Capacity = caravanData.Capacity, RequiredWorkers = caravanData.RequiredWorkers, IsAvailable = caravanData.IsAvailable, PurchaseCost = caravanData.PurchaseCost, UpkeepPerWeek = caravanData.UpkeepPerWeek, Status = caravanStatus };
    }

    public static TradeRouteSaveData ToSaveData(TradeRoute route) => new()
    {
        Id = route.Id,
        FromSettlementId = route.FromSettlementId,
        ToSettlementId = route.ToSettlementId,
        Type = route.Type.ToString(),
        Distance = route.Distance,
        TravelDays = route.TravelDays,
        DistanceDays = route.DistanceDays,
        IsEnabled = route.IsEnabled,
        DifficultyMultiplier = route.DifficultyMultiplier,
        Points = route.Points.Select(point => new RoutePointSaveData { X = point.X, Y = point.Y }).ToList()
    };

    public static TradeRoute ToCoreTradeRoute(TradeRouteSaveData routeData)
    {
        if (!Enum.TryParse<CaravanType>(routeData.Type, true, out var caravanType))
            throw new InvalidDataException($"Unknown trade route caravan type '{routeData.Type}'.");

        return new TradeRoute
        {
            Id = routeData.Id,
            FromSettlementId = routeData.FromSettlementId,
            ToSettlementId = routeData.ToSettlementId,
            Type = caravanType,
            Distance = routeData.Distance,
            TravelDays = routeData.TravelDays,
            DistanceDays = routeData.DistanceDays ?? WorldSaveDefaults.GetDefaultDistanceDays(routeData.Id),
            IsEnabled = routeData.IsEnabled,
            DifficultyMultiplier = routeData.DifficultyMultiplier,
            Points = routeData.Points?.Select(point => new RoutePoint { X = point.X, Y = point.Y }).ToList() ?? []
        };
    }

    public static TradeShipmentSaveData ToSaveData(TradeShipment shipment) => new()
    {
        Id = shipment.Id,
        CaravanId = shipment.CaravanId,
        RouteId = shipment.RouteId,
        FromSettlementId = shipment.FromSettlementId,
        ToSettlementId = shipment.ToSettlementId,
        GoodType = shipment.GoodType.ToString(),
        Amount = shipment.Amount,
        DepartureDay = shipment.DepartureDay,
        ArrivalDay = shipment.ArrivalDay,
        ReturnDay = shipment.ReturnDay,
        ExporterWealthDelta = shipment.ExporterWealthDelta,
        ImporterWealthDelta = shipment.ImporterWealthDelta,
        Status = shipment.Status.ToString()
    };

    public static TradeShipment ToCoreTradeShipment(TradeShipmentSaveData shipmentData)
    {
        if (!Enum.TryParse<TradeGoodType>(shipmentData.GoodType, true, out var goodType))
            throw new InvalidDataException($"Unknown shipment good type '{shipmentData.GoodType}'.");
        if (!Enum.TryParse<TradeShipmentStatus>(shipmentData.Status, true, out var status))
            throw new InvalidDataException($"Unknown shipment status '{shipmentData.Status}'.");

        return new TradeShipment
        {
            Id = shipmentData.Id,
            CaravanId = shipmentData.CaravanId,
            RouteId = shipmentData.RouteId,
            FromSettlementId = shipmentData.FromSettlementId,
            ToSettlementId = shipmentData.ToSettlementId,
            GoodType = goodType,
            Amount = shipmentData.Amount,
            DepartureDay = shipmentData.DepartureDay,
            ArrivalDay = shipmentData.ArrivalDay,
            ReturnDay = shipmentData.ReturnDay,
            ExporterWealthDelta = shipmentData.ExporterWealthDelta,
            ImporterWealthDelta = shipmentData.ImporterWealthDelta,
            Status = status
        };
    }
}
