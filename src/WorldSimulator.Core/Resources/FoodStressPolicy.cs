namespace WorldSimulator.Core.Resources;

public enum FoodRiskLevel
{
    None = 0,
    Low = 1,
    Medium = 2,
    High = 3
}

public sealed class FoodStressPolicy
{
    public const decimal InfiniteFoodDays = 999m;
    public const decimal MediumRiskDaysThreshold = 5m;
    public const decimal HighRiskDaysThreshold = 2m;

    public FoodStressEvaluation Evaluate(decimal endingFood, decimal populationConsumption)
    {
        var foodDays = populationConsumption <= 0m
            ? InfiniteFoodDays
            : endingFood / populationConsumption;

        var riskLevel = endingFood <= 0m
            ? FoodRiskLevel.High
            : foodDays < HighRiskDaysThreshold
                ? FoodRiskLevel.High
                : foodDays < MediumRiskDaysThreshold
                    ? FoodRiskLevel.Medium
                    : FoodRiskLevel.None;

        var normalizedRisk = riskLevel switch
        {
            FoodRiskLevel.None => 0m,
            FoodRiskLevel.Low => 0.33m,
            FoodRiskLevel.Medium => 0.66m,
            FoodRiskLevel.High => 1m,
            _ => 0m
        };

        return new FoodStressEvaluation(foodDays, normalizedRisk, riskLevel);
    }
}

public readonly record struct FoodStressEvaluation(
    decimal FoodDays,
    decimal NormalizedRisk,
    FoodRiskLevel RiskLevel);
