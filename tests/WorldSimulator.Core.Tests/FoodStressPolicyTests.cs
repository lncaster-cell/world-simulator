using FluentAssertions;
using WorldSimulator.Core.Resources;

namespace WorldSimulator.Core.Tests;

public sealed class FoodStressPolicyTests
{
    private readonly FoodStressPolicy _policy = new();

    [Fact]
    public void EndingFood_Zero_IsHighRisk()
    {
        var result = _policy.Evaluate(0m, 10m);

        result.FoodDays.Should().Be(0m);
        result.RiskLevel.Should().Be(FoodRiskLevel.High);
        result.NormalizedRisk.Should().Be(1m);
    }

    [Fact]
    public void FoodDays_BelowTwo_IsHighRisk()
    {
        var result = _policy.Evaluate(19m, 10m);

        result.FoodDays.Should().Be(1.9m);
        result.RiskLevel.Should().Be(FoodRiskLevel.High);
        result.NormalizedRisk.Should().Be(1m);
    }

    [Fact]
    public void FoodDays_ExactlyTwo_IsMediumRisk()
    {
        var result = _policy.Evaluate(20m, 10m);

        result.FoodDays.Should().Be(2m);
        result.RiskLevel.Should().Be(FoodRiskLevel.Medium);
        result.NormalizedRisk.Should().Be(0.66m);
    }

    [Fact]
    public void FoodDays_BelowFive_IsMediumRisk()
    {
        var result = _policy.Evaluate(49m, 10m);

        result.FoodDays.Should().Be(4.9m);
        result.RiskLevel.Should().Be(FoodRiskLevel.Medium);
        result.NormalizedRisk.Should().Be(0.66m);
    }

    [Fact]
    public void FoodDays_FiveOrMore_IsNoRisk()
    {
        var fiveDays = _policy.Evaluate(50m, 10m);
        var moreDays = _policy.Evaluate(90m, 10m);

        fiveDays.RiskLevel.Should().Be(FoodRiskLevel.None);
        fiveDays.NormalizedRisk.Should().Be(0m);
        moreDays.RiskLevel.Should().Be(FoodRiskLevel.None);
        moreDays.NormalizedRisk.Should().Be(0m);
    }
}
