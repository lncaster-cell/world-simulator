namespace WorldSimulator.Core.Workforce;

public sealed class CityWorkforceAssignmentState
{
    public int AgricultureWorkers { get; set; }
    public int FishingWorkers { get; set; }
    public int HuntingWorkers { get; set; }
    public int ResourceGatheringWorkers { get; set; }
    public int CraftingWorkers { get; set; }
    public int TradeWorkers { get; set; }
    public int GuardWorkers { get; set; }
    public int MaintenanceWorkers { get; set; }
    public int IdleWorkers { get; set; }

    public int AssignedWorkers =>
        AgricultureWorkers +
        FishingWorkers +
        HuntingWorkers +
        ResourceGatheringWorkers +
        CraftingWorkers +
        TradeWorkers +
        GuardWorkers +
        MaintenanceWorkers;

    public int TotalWorkers => AssignedWorkers + IdleWorkers;

    public int GetWorkers(WorkforceSector sector) => sector switch
    {
        WorkforceSector.Agriculture => AgricultureWorkers,
        WorkforceSector.Fishing => FishingWorkers,
        WorkforceSector.Hunting => HuntingWorkers,
        WorkforceSector.ResourceGathering => ResourceGatheringWorkers,
        WorkforceSector.Crafting => CraftingWorkers,
        WorkforceSector.Trade => TradeWorkers,
        WorkforceSector.Guards => GuardWorkers,
        WorkforceSector.Maintenance => MaintenanceWorkers,
        WorkforceSector.Idle => IdleWorkers,
        _ => throw new ArgumentOutOfRangeException(nameof(sector), sector, "Unsupported workforce sector.")
    };

    public void SetWorkers(WorkforceSector sector, int workers)
    {
        var safeWorkers = Math.Max(0, workers);
        switch (sector)
        {
            case WorkforceSector.Agriculture:
                AgricultureWorkers = safeWorkers;
                break;
            case WorkforceSector.Fishing:
                FishingWorkers = safeWorkers;
                break;
            case WorkforceSector.Hunting:
                HuntingWorkers = safeWorkers;
                break;
            case WorkforceSector.ResourceGathering:
                ResourceGatheringWorkers = safeWorkers;
                break;
            case WorkforceSector.Crafting:
                CraftingWorkers = safeWorkers;
                break;
            case WorkforceSector.Trade:
                TradeWorkers = safeWorkers;
                break;
            case WorkforceSector.Guards:
                GuardWorkers = safeWorkers;
                break;
            case WorkforceSector.Maintenance:
                MaintenanceWorkers = safeWorkers;
                break;
            case WorkforceSector.Idle:
                IdleWorkers = safeWorkers;
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(sector), sector, "Unsupported workforce sector.");
        }
    }

    public void ReplaceWith(CityWorkforceAllocation allocation)
    {
        ArgumentNullException.ThrowIfNull(allocation);

        AgricultureWorkers = allocation.AgricultureWorkers;
        FishingWorkers = allocation.FishingWorkers;
        HuntingWorkers = allocation.HuntingWorkers;
        ResourceGatheringWorkers = allocation.ResourceGatheringWorkers;
        CraftingWorkers = allocation.CraftingWorkers;
        TradeWorkers = allocation.TradeWorkers;
        GuardWorkers = allocation.GuardWorkers;
        MaintenanceWorkers = allocation.MaintenanceWorkers;
        IdleWorkers = allocation.IdleWorkers;
    }
}
