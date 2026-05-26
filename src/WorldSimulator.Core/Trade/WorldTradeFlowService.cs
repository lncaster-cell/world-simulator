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

    public void ProcessShipments(SimulationWorld world, int currentDay)
    {
        foreach (var shipment in world.TradeShipments.Where(s => s.Status != TradeShipmentStatus.Completed).OrderBy(s => s.Id, StringComparer.Ordinal))
        {
            if (shipment.Status == TradeShipmentStatus.InTransitToDestination && currentDay >= shipment.ArrivalDay)
            {
                var importer = world.FindCity(shipment.ToSettlementId);
                if (importer is not null)
                {
                    AddStock(importer, shipment.GoodType, shipment.Amount);
                }
                shipment.Status = TradeShipmentStatus.DeliveredReturning;
                var arrivedCaravan = world.Caravans.FirstOrDefault(c => c.Id == shipment.CaravanId);
                if (arrivedCaravan is not null) arrivedCaravan.Status = CaravanStatus.Returning;
            }

            if (shipment.Status == TradeShipmentStatus.DeliveredReturning && currentDay >= shipment.ReturnDay)
            {
                shipment.Status = TradeShipmentStatus.Completed;
                var completedCaravan = world.Caravans.FirstOrDefault(c => c.Id == shipment.CaravanId);
                if (completedCaravan is not null) completedCaravan.Status = CaravanStatus.Idle;
            }
        }
    }

    public WorldTradeFlowResult RunWeeklyTrade(SimulationWorld world, int currentDay)
    {
        ArgumentNullException.ThrowIfNull(world);

        var transfers = new List<TradeTransferResult>();
        var settlementStats = world.Cities.ToDictionary(c => c.Id, c => new SettlementAccumulator());

        var cities = world.Cities.OrderBy(c => c.Id, StringComparer.Ordinal).ToList();
        var activeCaravans = world.TradeShipments.Where(s => s.Status != TradeShipmentStatus.Completed).Select(s => s.CaravanId).ToHashSet(StringComparer.Ordinal);
        var caravans = world.Caravans.Where(c => c.IsAvailable && c.Capacity > 0m && !activeCaravans.Contains(c.Id)).OrderBy(c => c.Id, StringComparer.Ordinal).ToList();

        foreach (var caravan in caravans)
        {
            var exporter = cities.FirstOrDefault(c => c.Id == caravan.OwnerSettlementId);
            if (exporter is null) continue;

            var routes = cities
                .Where(c => c.Id != exporter.Id)
                .Select(c => TradeRouteValidation.FindEnabledRoute(world, exporter.Id, c.Id, caravan.Type))
                .Where(r => r is not null)
                .Cast<TradeRoute>()
                .DistinctBy(r => r.Id)
                .OrderBy(r => r.Id, StringComparer.Ordinal)
                .ToList();

            foreach (var good in Enum.GetValues<TradeGoodType>())
            {
                var surplus = CalculateSurplus(exporter, good);
                if (surplus <= 0m) continue;

                var importerCandidate = routes.Select(r => new { Route = r, City = cities.FirstOrDefault(c => c.Id == r.ToSettlementId) })
                    .Where(x => x.City is not null)
                    .Select(x => new { x.Route, City = x.City!, Deficit = CalculateDeficit(x.City!, good) })
                    .Where(x => x.Deficit > 0m)
                    .OrderByDescending(x => x.Deficit).ThenBy(x => x.City.Id, StringComparer.Ordinal).FirstOrDefault();
                if (importerCandidate is null) continue;

                var importer = importerCandidate.City;
                var affordableAmount = CalculateAffordableAmount(importer.Wealth);
                var amount = decimal.Min(surplus, decimal.Min(importerCandidate.Deficit, decimal.Min(caravan.Capacity, affordableAmount)));
                amount = decimal.Round(amount, 2, MidpointRounding.AwayFromZero);
                if (amount <= 0m) continue;

                var wealthDelta = CalculateWealthDelta(amount, importer.Wealth);
                if (wealthDelta <= 0m) continue;

                RemoveStock(exporter, good, amount);
                exporter.Wealth += wealthDelta;
                importer.Wealth -= wealthDelta;

                caravan.Status = CaravanStatus.InTransit;
                world.TradeShipments.Add(new TradeShipment
                {
                    Id = $"shipment_{caravan.Id}_{currentDay}_{good}",
                    CaravanId = caravan.Id,
                    RouteId = importerCandidate.Route.Id,
                    FromSettlementId = exporter.Id,
                    ToSettlementId = importer.Id,
                    GoodType = good,
                    Amount = amount,
                    DepartureDay = currentDay,
                    ArrivalDay = currentDay + importerCandidate.Route.TravelDays,
                    ReturnDay = currentDay + (2 * importerCandidate.Route.TravelDays),
                    ExporterWealthDelta = wealthDelta,
                    ImporterWealthDelta = -wealthDelta,
                    Status = TradeShipmentStatus.InTransitToDestination
                });

                var transfer = new TradeTransferResult(exporter.Id, importer.Id, caravan.Id, good, amount, wealthDelta, -wealthDelta);
                transfers.Add(transfer);
                settlementStats[exporter.Id].RegisterExport(good, amount, wealthDelta);
                settlementStats[importer.Id].RegisterImport(good, amount, -wealthDelta);
                break;
            }
        }

        var settlementResults = settlementStats.ToDictionary(x => x.Key, x => x.Value.ToResult(x.Key));
        return new WorldTradeFlowResult(transfers, settlementResults,
            transfers.Where(t => t.GoodType == TradeGoodType.Food).Sum(t => t.AmountTransferred),
            transfers.Where(t => t.GoodType == TradeGoodType.Goods).Sum(t => t.AmountTransferred),
            transfers.Where(t => t.GoodType == TradeGoodType.Resources).Sum(t => t.AmountTransferred),
            transfers.Sum(t => t.ExporterWealthDelta),
            decimal.Abs(transfers.Sum(t => t.ImporterWealthDelta)));
    }
    private static decimal CalculateAffordableAmount(decimal importerWealth){ if(importerWealth<=0m)return 0m; if(importerWealth>=MaxWealthDeltaPerTransfer)return decimal.MaxValue; return decimal.Round(importerWealth/WealthPerUnitTransferred,2,MidpointRounding.ToZero);}    
    private static decimal CalculateWealthDelta(decimal amount, decimal importerWealth){ var rawDelta=decimal.Round(amount*WealthPerUnitTransferred,2,MidpointRounding.AwayFromZero); return decimal.Min(importerWealth, decimal.Min(MaxWealthDeltaPerTransfer, rawDelta)); }
    private static decimal CalculateSurplus(City city, TradeGoodType good)=>decimal.Max(0m, GetStock(city, good)-GetReserve(city, good));
    private static decimal CalculateDeficit(City city, TradeGoodType good)=>decimal.Max(0m, GetTarget(city, good)-GetStock(city, good));
    private static decimal GetStock(City city, TradeGoodType good)=>good switch { TradeGoodType.Food=>city.Food, TradeGoodType.Goods=>city.Goods, _=>city.Resources };
    private static void RemoveStock(City city, TradeGoodType good, decimal amount){ if(good==TradeGoodType.Food) city.Food-=amount; else if(good==TradeGoodType.Goods) city.Goods-=amount; else city.Resources-=amount; }
    private static void AddStock(City city, TradeGoodType good, decimal amount){ if(good==TradeGoodType.Food) city.Food+=amount; else if(good==TradeGoodType.Goods) city.Goods+=amount; else city.Resources+=amount; }
    private static decimal GetReserve(City city, TradeGoodType good)=>good switch { TradeGoodType.Food=>decimal.Round(city.CalculateDailyFoodConsumption()*WeeklyFoodDaysReserve,2,MidpointRounding.AwayFromZero), TradeGoodType.Goods=>decimal.Round(city.Population*WeeklyGoodsPerCapitaReserve,2,MidpointRounding.AwayFromZero), _=>decimal.Round(city.Population*WeeklyResourcesPerCapitaReserve,2,MidpointRounding.AwayFromZero)};
    private static decimal GetTarget(City city, TradeGoodType good)=>good switch { TradeGoodType.Food=>decimal.Round(city.CalculateDailyFoodConsumption()*WeeklyFoodDaysTarget,2,MidpointRounding.AwayFromZero), TradeGoodType.Goods=>decimal.Round(city.Population*WeeklyGoodsPerCapitaTarget,2,MidpointRounding.AwayFromZero), _=>decimal.Round(city.Population*WeeklyResourcesPerCapitaTarget,2,MidpointRounding.AwayFromZero)};
    private sealed class SettlementAccumulator{ public decimal FoodExported{get;private set;} public decimal FoodImported{get;private set;} public decimal GoodsExported{get;private set;} public decimal GoodsImported{get;private set;} public decimal ResourcesExported{get;private set;} public decimal ResourcesImported{get;private set;} public decimal WealthDelta{get;private set;} public void RegisterExport(TradeGoodType good, decimal amount, decimal wealthDelta){ WealthDelta+=wealthDelta; if(good==TradeGoodType.Food)FoodExported+=amount; else if(good==TradeGoodType.Goods)GoodsExported+=amount; else ResourcesExported+=amount;} public void RegisterImport(TradeGoodType good, decimal amount, decimal wealthDelta){ WealthDelta+=wealthDelta; if(good==TradeGoodType.Food)FoodImported+=amount; else if(good==TradeGoodType.Goods)GoodsImported+=amount; else ResourcesImported+=amount;} public SettlementTradeFlowResult ToResult(string settlementId)=>new(settlementId,FoodExported,FoodImported,GoodsExported,GoodsImported,ResourcesExported,ResourcesImported,WealthDelta);} 
}
