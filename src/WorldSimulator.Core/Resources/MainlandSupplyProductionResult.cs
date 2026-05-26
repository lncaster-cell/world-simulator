namespace WorldSimulator.Core.Resources;

public sealed class MainlandSupplyProductionResult
{
    public required decimal NaturalSupplyPotential { get; init; }

    public required int InfrastructureLevel { get; init; }
    public required decimal InfrastructureModifier { get; init; }
    public required decimal InfrastructureCapacity { get; init; }

    public required decimal SecurityModifier { get; init; }
    public required decimal WealthModifier { get; init; }
    public required decimal StateModifier { get; init; }
    public required decimal StormModifier { get; init; }

    public required decimal FinalOutput { get; init; }
}
