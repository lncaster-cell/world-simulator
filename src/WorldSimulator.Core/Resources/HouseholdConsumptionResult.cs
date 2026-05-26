namespace WorldSimulator.Core.Resources;

public sealed class HouseholdConsumptionResult
{
    public required int Population { get; init; }

    public required decimal GoodsConsumptionPerPerson { get; init; }
    public required decimal RequiredGoods { get; init; }
    public required decimal GoodsAvailable { get; init; }
    public required decimal GoodsConsumed { get; init; }
    public required decimal GoodsShortage { get; init; }

    public required decimal ResourcesConsumptionPerPerson { get; init; }
    public required decimal RequiredResources { get; init; }
    public required decimal ResourcesAvailable { get; init; }
    public required decimal ResourcesConsumed { get; init; }
    public required decimal ResourcesShortage { get; init; }

    public bool HasGoodsShortage => GoodsShortage > 0m;
    public bool HasResourcesShortage => ResourcesShortage > 0m;
    public bool HasAnyShortage => HasGoodsShortage || HasResourcesShortage;
}
