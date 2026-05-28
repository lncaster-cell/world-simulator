namespace WorldSimulator.Core.Workforce;

public sealed record CityWorkforceAllocation(
    WorkforceCalculationResult Workforce,
    int AgricultureWorkers,
    int FishingWorkers,
    int HuntingWorkers,
    int ResourceGatheringWorkers,
    int CraftingWorkers,
    int TradeWorkers,
    int GuardWorkers,
    int MaintenanceWorkers,
    int IdleWorkers)
{
    public int AssignedWorkers =>
        AgricultureWorkers +
        FishingWorkers +
        HuntingWorkers +
        ResourceGatheringWorkers +
        CraftingWorkers +
        TradeWorkers +
        GuardWorkers +
        MaintenanceWorkers;
}
