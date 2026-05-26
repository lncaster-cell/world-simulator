namespace WorldSimulator.Core.Resources;

public sealed class ResourceGatheringProductionResult
{
    public required decimal NaturalPotential { get; init; }
    public required int RequiredWorkers { get; init; }
    public required int AssignedWorkers { get; init; }
    public required decimal WorkerCoverage { get; init; }
    public required int ExtraWorkers { get; init; }
    public required decimal OverstaffBonus { get; init; }
    public required decimal SecurityModifier { get; init; }
    public required decimal StateModifier { get; init; }
    public required decimal FinalOutput { get; init; }
}
