namespace WorldSimulator.Core.Resources;

public sealed class AgricultureProductionResult
{
    public required decimal NaturalPotential { get; init; }

    public required int RequiredWorkers { get; init; }
    public required int AssignedWorkers { get; init; }
    public required decimal WorkerCoverage { get; init; }

    public required decimal MoodModifier { get; init; }
    public required decimal SecurityModifier { get; init; }
    public required decimal StateModifier { get; init; }

    public required decimal FinalOutput { get; init; }
}
