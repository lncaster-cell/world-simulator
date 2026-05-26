namespace WorldSimulator.Core.Trade;

public static class CaravanPresets
{
    public const decimal LandCapacity = 50m;
    public const decimal LandPurchaseCost = 100m;
    public const decimal LandUpkeepPerWeek = 2m;
    public const int LandRequiredWorkers = 5;

    public const decimal SeaCapacity = 200m;
    public const decimal SeaPurchaseCost = 300m;
    public const decimal SeaUpkeepPerWeek = 5m;
    public const int SeaRequiredWorkers = 10;

    public static Caravan Create(string id, string ownerSettlementId, CaravanType type) => type switch
    {
        CaravanType.Land => new Caravan { Id = id, OwnerSettlementId = ownerSettlementId, Type = type, Capacity = LandCapacity, RequiredWorkers = LandRequiredWorkers, IsAvailable = true, PurchaseCost = LandPurchaseCost, UpkeepPerWeek = LandUpkeepPerWeek, Status = CaravanStatus.Idle },
        CaravanType.Sea => new Caravan { Id = id, OwnerSettlementId = ownerSettlementId, Type = type, Capacity = SeaCapacity, RequiredWorkers = SeaRequiredWorkers, IsAvailable = true, PurchaseCost = SeaPurchaseCost, UpkeepPerWeek = SeaUpkeepPerWeek, Status = CaravanStatus.Idle },
        _ => throw new ArgumentOutOfRangeException(nameof(type), type, "Unsupported caravan type")
    };
}
