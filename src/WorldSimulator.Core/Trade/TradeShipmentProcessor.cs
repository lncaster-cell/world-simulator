using WorldSimulator.Core.World;

namespace WorldSimulator.Core.Trade;

public sealed class TradeShipmentProcessor
{
    public void ProcessShipments(SimulationWorld world, int currentDay)
    {
        ArgumentNullException.ThrowIfNull(world);

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
}
