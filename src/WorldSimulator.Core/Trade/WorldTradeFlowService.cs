using WorldSimulator.Core.World;

namespace WorldSimulator.Core.Trade;

public sealed class WorldTradeFlowService
{
    public void ProcessShipments(SimulationWorld world, int currentDay)
    {
        foreach (var shipment in world.TradeShipments
            .Where(s => s.Status != TradeShipmentStatus.Completed)
            .OrderBy(s => s.Id, StringComparer.Ordinal))
        {
            if (shipment.Status == TradeShipmentStatus.InTransitToDestination && currentDay >= shipment.ArrivalDay)
            {
                var importer = world.FindCity(shipment.ToSettlementId);
                if (importer is not null)
                {
                    TradeInventoryPolicy.AddStock(importer, shipment.GoodType, shipment.Amount);
                }

                shipment.Status = TradeShipmentStatus.DeliveredReturning;

                var arrivedCaravan = world.Caravans.FirstOrDefault(c => c.Id == shipment.CaravanId);
                if (arrivedCaravan is not null)
                {
                    arrivedCaravan.Status = CaravanStatus.Returning;
                }
            }

            if (shipment.Status == TradeShipmentStatus.DeliveredReturning && currentDay >= shipment.ReturnDay)
            {
                shipment.Status = TradeShipmentStatus.Completed;

                var completedCaravan = world.Caravans.FirstOrDefault(c => c.Id == shipment.CaravanId);
                if (completedCaravan is not null)
                {
                    completedCaravan.Status = CaravanStatus.Idle;
                }
            }
        }
    }

    public WorldTradeFlowResult RunWeeklyTrade(SimulationWorld world, int currentDay)
    {
        ArgumentNullException.ThrowIfNull(world);

        var transfers = new List<TradeTransferResult>();
        var settlementStats = world.Cities.ToDictionary(c => c.Id, c => new SettlementTradeAccumulator());

        var cities = world.Cities.OrderBy(c => c.Id, StringComparer.Ordinal).ToList();
        var activeCaravans = world.TradeShipments
            .Where(s => s.Status != TradeShipmentStatus.Completed)
            .Select(s => s.CaravanId)
            .ToHashSet(StringComparer.Ordinal);
        var caravans = world.Caravans
            .Where(c => c.IsAvailable && c.Capacity > 0m && !activeCaravans.Contains(c.Id))
            .OrderBy(c => c.Id, StringComparer.Ordinal)
            .ToList();

        foreach (var caravan in caravans)
        {
            var exporter = cities.FirstOrDefault(c => c.Id == caravan.OwnerSettlementId);
            if (exporter is null)
            {
                continue;
            }

            var routes = cities
                .Where(c => c.Id != exporter.Id)
                .Select(c => new
                {
                    Route = TradeRouteValidation.FindEnabledRoute(world, exporter.Id, c.Id, caravan.Type),
                    Importer = c
                })
                .Where(x => x.Route is not null)
                .Select(x => new { Route = x.Route!, x.Importer })
                .DistinctBy(x => x.Route.Id)
                .OrderBy(x => x.Route.Id, StringComparer.Ordinal)
                .ToList();

            foreach (var good in Enum.GetValues<TradeGoodType>())
            {
                var surplus = TradeDemandPolicy.CalculateSurplus(exporter, good);
                if (surplus <= 0m)
                {
                    continue;
                }

                var importerCandidate = routes
                    .Select(candidate => new
                    {
                        candidate.Route,
                        City = candidate.Importer,
                        Deficit = TradeDemandPolicy.CalculateDeficit(candidate.Importer, good)
                    })
                    .Where(candidate => candidate.Deficit > 0m)
                    .OrderByDescending(candidate => candidate.Deficit)
                    .ThenBy(candidate => candidate.City.Id, StringComparer.Ordinal)
                    .FirstOrDefault();
                if (importerCandidate is null)
                {
                    continue;
                }

                var importer = importerCandidate.City;
                var affordableAmount = TradeWealthPolicy.CalculateAffordableAmount(importer.Wealth);
                var amount = decimal.Min(
                    surplus,
                    decimal.Min(
                        importerCandidate.Deficit,
                        decimal.Min(caravan.Capacity, affordableAmount)));
                amount = decimal.Round(amount, 2, MidpointRounding.AwayFromZero);
                if (amount <= 0m)
                {
                    continue;
                }

                var wealthDelta = TradeWealthPolicy.CalculateWealthDelta(amount, importer.Wealth);
                if (wealthDelta <= 0m)
                {
                    continue;
                }

                TradeInventoryPolicy.RemoveStock(exporter, good, amount);
                exporter.Wealth += wealthDelta;
                importer.Wealth -= wealthDelta;

                caravan.Status = CaravanStatus.InTransit;

                var shipment = new TradeShipment
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
                };
                world.TradeShipments.Add(shipment);

                var transfer = new TradeTransferResult(
                    importerCandidate.Route.Id,
                    exporter.Id,
                    importer.Id,
                    exporter.Id,
                    importer.Id,
                    caravan.Id,
                    good,
                    amount,
                    wealthDelta,
                    -wealthDelta);
                transfers.Add(transfer);

                settlementStats[exporter.Id].RegisterExport(good, amount, wealthDelta);
                settlementStats[importer.Id].RegisterImport(good, amount, -wealthDelta);
                break;
            }
        }

        var settlementResults = settlementStats.ToDictionary(x => x.Key, x => x.Value.ToResult(x.Key));

        return new WorldTradeFlowResult(
            transfers,
            settlementResults,
            transfers.Where(t => t.GoodType == TradeGoodType.Food).Sum(t => t.AmountTransferred),
            transfers.Where(t => t.GoodType == TradeGoodType.Goods).Sum(t => t.AmountTransferred),
            transfers.Where(t => t.GoodType == TradeGoodType.Resources).Sum(t => t.AmountTransferred),
            transfers.Sum(t => t.ExporterWealthDelta),
            decimal.Abs(transfers.Sum(t => t.ImporterWealthDelta)));
    }
}
