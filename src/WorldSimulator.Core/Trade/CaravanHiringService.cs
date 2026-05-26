using WorldSimulator.Core.Cities;
using WorldSimulator.Core.World;

namespace WorldSimulator.Core.Trade;

public sealed class CaravanHiringService
{
    private const decimal SafetyReserve = 100m;
    private const decimal WorkerBudgetRatio = 0.03m;

    public CaravanHiringResult EvaluateAndHire(SimulationWorld world)
    {
        ArgumentNullException.ThrowIfNull(world);

        var activeCaravanIds = world.TradeShipments.Where(s => s.Status != TradeShipmentStatus.Completed).Select(s => s.CaravanId).ToHashSet(StringComparer.Ordinal);
        var results = new List<SettlementCaravanHiringResult>();

        foreach (var city in world.Cities.OrderBy(c => c.Id, StringComparer.Ordinal))
        {
            var cityRoutes = world.TradeRoutes.Where(r => r.IsEnabled && (r.FromSettlementId == city.Id || r.ToSettlementId == city.Id)).ToList();
            var landDemand = EstimateDemand(city, cityRoutes, CaravanType.Land, world);
            var seaDemand = EstimateDemand(city, cityRoutes, CaravanType.Sea, world);

            var availableLandCapacity = CapacityFor(city, world, CaravanType.Land, activeCaravanIds);
            var availableSeaCapacity = CapacityFor(city, world, CaravanType.Sea, activeCaravanIds);

            var hasPressure = landDemand > availableLandCapacity || seaDemand > availableSeaCapacity || HasDeficitOrSurplus(city);
            if (!hasPressure)
            {
                results.Add(SettlementCaravanHiringResult.NoHire(city.Id, "NoTradePressure", landDemand, seaDemand, availableLandCapacity, availableSeaCapacity));
                continue;
            }

            var candidateType = ChooseType(city, world, landDemand, seaDemand);
            if (candidateType is null)
            {
                results.Add(SettlementCaravanHiringResult.NoHire(city.Id, "NoEligibleType", landDemand, seaDemand, availableLandCapacity, availableSeaCapacity));
                continue;
            }

            var draft = CaravanPresets.Create("draft", city.Id, candidateType.Value);
            var projectedUpkeep = world.Caravans.Where(c => c.OwnerSettlementId == city.Id).Sum(c => c.UpkeepPerWeek) + draft.UpkeepPerWeek;
            var postPurchaseWealth = city.Wealth - draft.PurchaseCost;
            var workerBudget = (int)decimal.Floor(city.Population * WorkerBudgetRatio);
            var usedWorkers = world.Caravans.Where(c => c.OwnerSettlementId == city.Id && c.IsAvailable).Sum(c => c.RequiredWorkers);

            if (postPurchaseWealth < SafetyReserve || city.Wealth < draft.PurchaseCost + SafetyReserve)
            {
                results.Add(SettlementCaravanHiringResult.NoHire(city.Id, "SafetyReserve", landDemand, seaDemand, availableLandCapacity, availableSeaCapacity));
                continue;
            }

            if (usedWorkers + draft.RequiredWorkers > workerBudget)
            {
                results.Add(SettlementCaravanHiringResult.NoHire(city.Id, "WorkerBudget", landDemand, seaDemand, availableLandCapacity, availableSeaCapacity));
                continue;
            }

            if (postPurchaseWealth - projectedUpkeep < SafetyReserve)
            {
                results.Add(SettlementCaravanHiringResult.NoHire(city.Id, "UpkeepUnaffordable", landDemand, seaDemand, availableLandCapacity, availableSeaCapacity));
                continue;
            }

            if ((candidateType == CaravanType.Land && landDemand <= availableLandCapacity) || (candidateType == CaravanType.Sea && seaDemand <= availableSeaCapacity))
            {
                results.Add(SettlementCaravanHiringResult.NoHire(city.Id, "CapacityAlreadySufficient", landDemand, seaDemand, availableLandCapacity, availableSeaCapacity));
                continue;
            }

            var nextIndex = world.Caravans.Count(c => c.OwnerSettlementId == city.Id && c.Type == candidateType.Value) + 1;
            var caravan = CaravanPresets.Create($"{city.Id}_{candidateType.Value.ToString().ToLowerInvariant()}_{nextIndex}", city.Id, candidateType.Value);
            world.Caravans.Add(caravan);
            city.Wealth = postPurchaseWealth;
            results.Add(SettlementCaravanHiringResult.Hired(city.Id, caravan.Id, candidateType.Value, draft.PurchaseCost, landDemand, seaDemand, availableLandCapacity, availableSeaCapacity));
        }

        return new CaravanHiringResult(results, results.Count(x => x.WasHired));
    }

    private static decimal EstimateDemand(City city, IReadOnlyCollection<TradeRoute> routes, CaravanType routeType, SimulationWorld world)
    {
        var candidates = routes.Where(r => r.Type == routeType && r.FromSettlementId == city.Id).Select(r => world.FindCity(r.ToSettlementId)).Where(c => c is not null).Cast<City>();
        return decimal.Round(candidates.Sum(other =>
            Math.Max(0m, Deficit(other, TradeGoodType.Food)) +
            Math.Max(0m, Deficit(other, TradeGoodType.Goods)) +
            Math.Max(0m, Deficit(other, TradeGoodType.Resources))), 2, MidpointRounding.AwayFromZero);
    }

    private static decimal CapacityFor(City city, SimulationWorld world, CaravanType type, HashSet<string> activeCaravanIds)
        => world.Caravans.Where(c => c.OwnerSettlementId == city.Id && c.Type == type && c.IsAvailable && c.Status == CaravanStatus.Idle && !activeCaravanIds.Contains(c.Id)).Sum(c => c.Capacity);

    private static bool HasDeficitOrSurplus(City city)
        => Deficit(city, TradeGoodType.Food) > 0m || Deficit(city, TradeGoodType.Goods) > 0m || Deficit(city, TradeGoodType.Resources) > 0m || Surplus(city, TradeGoodType.Food) > 0m || Surplus(city, TradeGoodType.Goods) > 0m || Surplus(city, TradeGoodType.Resources) > 0m;

    private static CaravanType? ChooseType(City city, SimulationWorld world, decimal landDemand, decimal seaDemand)
    {
        var profile = world.FindSettlementEconomyProfile(city.Id);
        var seaAllowed = profile is not null && profile.IsPort && seaDemand > 0m;
        if (landDemand >= seaDemand) return CaravanType.Land;
        return seaAllowed ? CaravanType.Sea : (landDemand > 0m ? CaravanType.Land : null);
    }

    private static decimal Stock(City c, TradeGoodType g) => g switch { TradeGoodType.Food => c.Food, TradeGoodType.Goods => c.Goods, _ => c.Resources };
    private static decimal Reserve(City c, TradeGoodType g) => g switch { TradeGoodType.Food => decimal.Round(c.CalculateDailyFoodConsumption() * 8m, 2), TradeGoodType.Goods => decimal.Round(c.Population * 0.15m, 2), _ => decimal.Round(c.Population * 0.2m, 2) };
    private static decimal Target(City c, TradeGoodType g) => g switch { TradeGoodType.Food => decimal.Round(c.CalculateDailyFoodConsumption() * 12m, 2), TradeGoodType.Goods => decimal.Round(c.Population * 0.3m, 2), _ => decimal.Round(c.Population * 0.4m, 2) };
    private static decimal Deficit(City c, TradeGoodType g) => Math.Max(0m, Target(c, g) - Stock(c, g));
    private static decimal Surplus(City c, TradeGoodType g) => Math.Max(0m, Stock(c, g) - Reserve(c, g));
}

public sealed record CaravanHiringResult(IReadOnlyList<SettlementCaravanHiringResult> Settlements, int TotalHired);
public sealed record SettlementCaravanHiringResult(string SettlementId, bool WasHired, string? CaravanId, CaravanType? Type, decimal PurchaseCost, string Reason, decimal LandDemand, decimal SeaDemand, decimal AvailableLandCapacity, decimal AvailableSeaCapacity)
{
    public static SettlementCaravanHiringResult Hired(string settlementId, string caravanId, CaravanType type, decimal purchaseCost, decimal landDemand, decimal seaDemand, decimal availableLandCapacity, decimal availableSeaCapacity)
        => new(settlementId, true, caravanId, type, purchaseCost, "Hired", landDemand, seaDemand, availableLandCapacity, availableSeaCapacity);

    public static SettlementCaravanHiringResult NoHire(string settlementId, string reason, decimal landDemand, decimal seaDemand, decimal availableLandCapacity, decimal availableSeaCapacity)
        => new(settlementId, false, null, null, 0m, reason, landDemand, seaDemand, availableLandCapacity, availableSeaCapacity);
}
