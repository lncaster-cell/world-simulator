using WorldSimulator.Core.Cities;
using WorldSimulator.Core.World;

namespace WorldSimulator.Core.Trade;

public sealed class WorldTradeFlowService
{
    private const decimal WeeklyFoodDaysReserve = 8m;
    private const decimal WeeklyFoodDaysTarget = 12m;
    private const decimal WeeklyGoodsPerCapitaReserve = 0.15m;
    private const decimal WeeklyGoodsPerCapitaTarget = 0.3m;
    private const decimal WeeklyResourcesPerCapitaReserve = 0.2m;
    private const decimal WeeklyResourcesPerCapitaTarget = 0.4m;
    private const decimal WealthPerUnitTransferred = 0.02m;
    private const decimal MaxWealthDeltaPerTransfer = 2m;

    public WorldTradeFlowResult RunWeeklyTrade(SimulationWorld world)
    {
        ArgumentNullException.ThrowIfNull(world);

        var transfers = new List<TradeTransferResult>();
        var settlementStats = world.Cities.ToDictionary(c => c.Id, c => new SettlementAccumulator());

        var cities = world.Cities.OrderBy(c => c.Id, StringComparer.Ordinal).ToList();
        var caravans = world.Caravans
            .Where(c => c.IsAvailable && c.Capacity > 0m)
            .OrderBy(c => c.Id, StringComparer.Ordinal)
            .ToList();

        foreach (var caravan in caravans)
        {
            var exporter = cities.FirstOrDefault(c => c.Id == caravan.OwnerSettlementId);
            if (exporter is null)
            {
                continue;
            }

            foreach (var good in Enum.GetValues<TradeGoodType>())
            {
                var surplus = CalculateSurplus(exporter, good);
                if (surplus <= 0m)
                {
                    continue;
                }

                var importer = cities
                    .Where(c => c.Id != exporter.Id)
                    .Select(c => new { City = c, Deficit = CalculateDeficit(c, good) })
                    .Where(x => x.Deficit > 0m)
                    .OrderByDescending(x => x.Deficit)
                    .ThenBy(x => x.City.Id, StringComparer.Ordinal)
                    .FirstOrDefault();

                if (importer is null)
                {
                    continue;
                }

                var amount = decimal.Min(surplus, decimal.Min(importer.Deficit, caravan.Capacity));
                amount = decimal.Round(amount, 2, MidpointRounding.AwayFromZero);
                if (amount <= 0m)
                {
                    continue;
                }

                ApplyTransfer(exporter, importer.City, good, amount);

                var wealthDelta = decimal.Min(MaxWealthDeltaPerTransfer, decimal.Round(amount * WealthPerUnitTransferred, 2, MidpointRounding.AwayFromZero));
                exporter.Wealth += wealthDelta;
                importer.City.Wealth = Math.Max(0m, importer.City.Wealth - wealthDelta);

                var transfer = new TradeTransferResult(exporter.Id, importer.City.Id, caravan.Id, good, amount, wealthDelta, -wealthDelta);
                transfers.Add(transfer);

                settlementStats[exporter.Id].RegisterExport(good, amount, wealthDelta);
                settlementStats[importer.City.Id].RegisterImport(good, amount, -wealthDelta);
            }
        }

        var settlementResults = settlementStats.ToDictionary(
            x => x.Key,
            x => x.Value.ToResult(x.Key));

        return new WorldTradeFlowResult(
            transfers,
            settlementResults,
            transfers.Where(t => t.GoodType == TradeGoodType.Food).Sum(t => t.AmountTransferred),
            transfers.Where(t => t.GoodType == TradeGoodType.Goods).Sum(t => t.AmountTransferred),
            transfers.Where(t => t.GoodType == TradeGoodType.Resources).Sum(t => t.AmountTransferred),
            transfers.Sum(t => t.ExporterWealthDelta),
            decimal.Abs(transfers.Sum(t => t.ImporterWealthDelta)));
    }

    private static void ApplyTransfer(City exporter, City importer, TradeGoodType good, decimal amount)
    {
        switch (good)
        {
            case TradeGoodType.Food:
                exporter.Food -= amount;
                importer.Food += amount;
                break;
            case TradeGoodType.Goods:
                exporter.Goods -= amount;
                importer.Goods += amount;
                break;
            case TradeGoodType.Resources:
                exporter.Resources -= amount;
                importer.Resources += amount;
                break;
        }
    }

    private static decimal CalculateSurplus(City city, TradeGoodType good)
        => decimal.Max(0m, GetStock(city, good) - GetReserve(city, good));

    private static decimal CalculateDeficit(City city, TradeGoodType good)
        => decimal.Max(0m, GetTarget(city, good) - GetStock(city, good));

    private static decimal GetStock(City city, TradeGoodType good)
        => good switch
        {
            TradeGoodType.Food => city.Food,
            TradeGoodType.Goods => city.Goods,
            _ => city.Resources
        };

    private static decimal GetReserve(City city, TradeGoodType good)
        => good switch
        {
            TradeGoodType.Food => decimal.Round(city.CalculateDailyFoodConsumption() * WeeklyFoodDaysReserve, 2, MidpointRounding.AwayFromZero),
            TradeGoodType.Goods => decimal.Round(city.Population * WeeklyGoodsPerCapitaReserve, 2, MidpointRounding.AwayFromZero),
            _ => decimal.Round(city.Population * WeeklyResourcesPerCapitaReserve, 2, MidpointRounding.AwayFromZero)
        };

    private static decimal GetTarget(City city, TradeGoodType good)
        => good switch
        {
            TradeGoodType.Food => decimal.Round(city.CalculateDailyFoodConsumption() * WeeklyFoodDaysTarget, 2, MidpointRounding.AwayFromZero),
            TradeGoodType.Goods => decimal.Round(city.Population * WeeklyGoodsPerCapitaTarget, 2, MidpointRounding.AwayFromZero),
            _ => decimal.Round(city.Population * WeeklyResourcesPerCapitaTarget, 2, MidpointRounding.AwayFromZero)
        };

    private sealed class SettlementAccumulator
    {
        public decimal FoodExported { get; private set; }
        public decimal FoodImported { get; private set; }
        public decimal GoodsExported { get; private set; }
        public decimal GoodsImported { get; private set; }
        public decimal ResourcesExported { get; private set; }
        public decimal ResourcesImported { get; private set; }
        public decimal WealthDelta { get; private set; }

        public void RegisterExport(TradeGoodType good, decimal amount, decimal wealthDelta)
        {
            WealthDelta += wealthDelta;
            switch (good)
            {
                case TradeGoodType.Food: FoodExported += amount; break;
                case TradeGoodType.Goods: GoodsExported += amount; break;
                case TradeGoodType.Resources: ResourcesExported += amount; break;
            }
        }

        public void RegisterImport(TradeGoodType good, decimal amount, decimal wealthDelta)
        {
            WealthDelta += wealthDelta;
            switch (good)
            {
                case TradeGoodType.Food: FoodImported += amount; break;
                case TradeGoodType.Goods: GoodsImported += amount; break;
                case TradeGoodType.Resources: ResourcesImported += amount; break;
            }
        }

        public SettlementTradeFlowResult ToResult(string settlementId)
            => new(settlementId, FoodExported, FoodImported, GoodsExported, GoodsImported, ResourcesExported, ResourcesImported, WealthDelta);
    }
}
