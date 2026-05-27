using WorldSimulator.Core.Resources;
using Xunit;

namespace WorldSimulator.Core.Tests;

public sealed class WorkforceBonusCalculatorTests
{
    [Fact]
    public void CalculateOverstaffBonus_WhenExtraWorkersIsZero_ReturnsZero()
    {
        var result = WorkforceBonusCalculator.CalculateOverstaffBonus(10m, 0.05m, 0, 30);

        Assert.Equal(0m, result);
    }

    [Fact]
    public void CalculateOverstaffBonus_WhenRequiredWorkersIsZero_ReturnsZero()
    {
        var result = WorkforceBonusCalculator.CalculateOverstaffBonus(10m, 0.05m, 5, 0);

        Assert.Equal(0m, result);
    }

    [Fact]
    public void CalculateOverstaffBonus_WhenExtraWorkersGreaterThanZero_ReturnsExpectedBonus()
    {
        var result = WorkforceBonusCalculator.CalculateOverstaffBonus(10m, 0.05m, 10, 30);

        Assert.Equal(0.125m, result);
    }
}
