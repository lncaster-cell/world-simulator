using WorldSimulator.Core.Cities;

namespace WorldSimulator.Core.Resources;

public sealed class HouseholdConsumptionCalculator
{
    private const decimal GoodsConsumptionPerPerson = 0.05m;
    private const decimal ResourcesConsumptionPerPerson = 0.01m;

    public HouseholdConsumptionResult Calculate(City city)
    {
        ArgumentNullException.ThrowIfNull(city);

        var population = Math.Max(0, city.Population);
        var goodsAvailable = Math.Max(0m, city.Goods);
        var resourcesAvailable = Math.Max(0m, city.Resources);

        var requiredGoods = population <= 0 ? 0m : decimal.Round(Math.Max(0m, population * GoodsConsumptionPerPerson), 2);
        var requiredResources = population <= 0 ? 0m : decimal.Round(Math.Max(0m, population * ResourcesConsumptionPerPerson), 2);

        var goodsConsumed = decimal.Round(Math.Max(0m, Math.Min(goodsAvailable, requiredGoods)), 2);
        var resourcesConsumed = decimal.Round(Math.Max(0m, Math.Min(resourcesAvailable, requiredResources)), 2);

        var goodsShortage = decimal.Round(Math.Max(0m, requiredGoods - goodsAvailable), 2);
        var resourcesShortage = decimal.Round(Math.Max(0m, requiredResources - resourcesAvailable), 2);

        return new HouseholdConsumptionResult
        {
            Population = population,
            GoodsConsumptionPerPerson = GoodsConsumptionPerPerson,
            RequiredGoods = requiredGoods,
            GoodsAvailable = goodsAvailable,
            GoodsConsumed = goodsConsumed,
            GoodsShortage = goodsShortage,
            ResourcesConsumptionPerPerson = ResourcesConsumptionPerPerson,
            RequiredResources = requiredResources,
            ResourcesAvailable = resourcesAvailable,
            ResourcesConsumed = resourcesConsumed,
            ResourcesShortage = resourcesShortage
        };
    }
}
