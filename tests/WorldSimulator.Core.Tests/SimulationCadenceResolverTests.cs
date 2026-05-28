using WorldSimulator.Core.Simulation;
using Xunit;

namespace WorldSimulator.Core.Tests;

public sealed class SimulationCadenceResolverTests
{
    [Theory]
    [InlineData(1)]
    [InlineData(7)]
    [InlineData(30)]
    [InlineData(180)]
    [InlineData(360)]
    public void ShouldRun_Daily_ReturnsTrueForPositiveDays(int day)
    {
        var resolver = new SimulationCadenceResolver();

        Assert.True(resolver.ShouldRun(day, SimulationCadence.Daily));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void ShouldRun_ReturnsFalseForNonPositiveDays(int day)
    {
        var resolver = new SimulationCadenceResolver();

        Assert.False(resolver.ShouldRun(day, SimulationCadence.Daily));
        Assert.False(resolver.ShouldRun(day, SimulationCadence.Weekly));
        Assert.False(resolver.ShouldRun(day, SimulationCadence.Monthly));
        Assert.False(resolver.ShouldRun(day, SimulationCadence.HalfYearly));
        Assert.False(resolver.ShouldRun(day, SimulationCadence.Yearly));
    }

    [Theory]
    [InlineData(7, true)]
    [InlineData(14, true)]
    [InlineData(6, false)]
    [InlineData(8, false)]
    public void ShouldRun_Weekly_UsesSevenDayCadence(int day, bool expected)
    {
        var resolver = new SimulationCadenceResolver();

        Assert.Equal(expected, resolver.ShouldRun(day, SimulationCadence.Weekly));
    }

    [Theory]
    [InlineData(30, true)]
    [InlineData(60, true)]
    [InlineData(29, false)]
    [InlineData(31, false)]
    public void ShouldRun_Monthly_UsesThirtyDayCadence(int day, bool expected)
    {
        var resolver = new SimulationCadenceResolver();

        Assert.Equal(expected, resolver.ShouldRun(day, SimulationCadence.Monthly));
    }

    [Theory]
    [InlineData(180, true)]
    [InlineData(360, true)]
    [InlineData(179, false)]
    [InlineData(181, false)]
    public void ShouldRun_HalfYearly_UsesOneHundredEightyDayCadence(int day, bool expected)
    {
        var resolver = new SimulationCadenceResolver();

        Assert.Equal(expected, resolver.ShouldRun(day, SimulationCadence.HalfYearly));
    }

    [Theory]
    [InlineData(360, true)]
    [InlineData(720, true)]
    [InlineData(359, false)]
    [InlineData(361, false)]
    public void ShouldRun_Yearly_UsesThreeHundredSixtyDayCadence(int day, bool expected)
    {
        var resolver = new SimulationCadenceResolver();

        Assert.Equal(expected, resolver.ShouldRun(day, SimulationCadence.Yearly));
    }

    [Fact]
    public void Constructor_InvalidCalendarPolicy_Throws()
    {
        var policy = new SimulationCalendarPolicy { DaysPerWeek = 0 };

        Assert.Throws<ArgumentOutOfRangeException>(() => new SimulationCadenceResolver(policy));
    }
}
