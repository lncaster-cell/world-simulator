namespace WorldSimulator.Core.Workforce;

public sealed record WorkforceCalculationResult(
    int Children,
    int AdultMen,
    int AdultWomen,
    int Elderly,
    decimal AdultMaleWorkers,
    decimal AdultFemaleWorkers,
    decimal ElderlyWorkers,
    decimal ChildWorkers,
    decimal PotentialWorkers,
    decimal GlobalWorkforceModifier,
    int TotalWorkers);
