using WorldSimulator.Core.Cities;
using WorldSimulator.Core.Events;
using WorldSimulator.Core.Resources;

namespace WorldSimulator.Core.Tests;

public sealed class CityEventEffectCalculatorTests
{
    private readonly CityEventEffectCalculator _calculator = new();

    [Fact]
    public void NoActiveEvents_GivesZeroEffects()
    {
        var city = CityPresets.CreateGotha();

        var result = _calculator.Calculate(city, Array.Empty<CityEvent>());

        Assert.Equal(0m, result.FoodDelta);
        Assert.Equal(0, result.MoodDelta);
        Assert.Equal(0, result.SecurityDelta);
        Assert.Equal(0, result.CrimeDelta);
        Assert.Equal(0m, result.WealthDelta);
        Assert.Equal(0m, result.ResourcesDelta);
        Assert.Equal(0m, result.MainlandSupplyDelta);
    }

    [Fact]
    public void RatInfestation_GivesFoodAndMoodPenalty()
    {
        var city = CityPresets.CreateGotha();
        var activeEvents = new[] { CityEventPresets.CreateRatInfestation(currentDay: 1) };

        var result = _calculator.Calculate(city, activeEvents);

        Assert.Equal(-25m, result.FoodDelta);
        Assert.Equal(-3, result.MoodDelta);
    }

    [Fact]
    public void PortStorm_GivesMainlandAndMoodPenalty()
    {
        var city = CityPresets.CreateGotha();
        var activeEvents = new[] { CityEventPresets.CreatePortStorm(currentDay: 1) };

        var result = _calculator.Calculate(city, activeEvents);

        Assert.Equal(-30m, result.MainlandSupplyDelta);
        Assert.Equal(-2, result.MoodDelta);
    }

    [Fact]
    public void ArtistsPerformance_GivesMoodBonusAndCrimeReduction()
    {
        var city = CityPresets.CreateGotha();
        var activeEvents = new[] { CityEventPresets.CreateArtistsPerformance(currentDay: 1) };

        var result = _calculator.Calculate(city, activeEvents);

        Assert.Equal(10, result.MoodDelta);
        Assert.Equal(-2, result.CrimeDelta);
    }

    [Fact]
    public void MultipleActiveEvents_StackEffects()
    {
        var city = CityPresets.CreateGotha();
        var activeEvents = new[]
        {
            CityEventPresets.CreateRatInfestation(currentDay: 1),
            CityEventPresets.CreatePortStorm(currentDay: 1)
        };

        var result = _calculator.Calculate(city, activeEvents);

        Assert.Equal(-25m, result.FoodDelta);
        Assert.Equal(-5, result.MoodDelta);
        Assert.Equal(-30m, result.MainlandSupplyDelta);
    }

    [Fact]
    public void UnknownEventId_IsIgnored()
    {
        var city = CityPresets.CreateGotha();
        var unknownEvent = new CityEvent("unknown", "Неизвестно", string.Empty, 1, 1);

        var result = _calculator.Calculate(city, new[] { unknownEvent });

        Assert.False(result.HasAnyEffect);
    }

    [Fact]
    public void PortStorm_ModifiesMainlandSupplyFromFortyToTen()
    {
        var city = CityPresets.CreateGotha();
        var effects = _calculator.Calculate(city, new[] { CityEventPresets.CreatePortStorm(currentDay: 1) });
        var baseInputs = DailyFoodFlowInputs.GothaPlaceholder;

        var modifiedMainlandSupply = baseInputs.MainlandSupplyIncome + effects.MainlandSupplyDelta;

        Assert.Equal(10m, modifiedMainlandSupply);
    }

    [Fact]
    public void RatInfestation_ModifiesEventDeltaFromZeroToMinusTwentyFive()
    {
        var city = CityPresets.CreateGotha();
        var effects = _calculator.Calculate(city, new[] { CityEventPresets.CreateRatInfestation(currentDay: 1) });
        var baseInputs = DailyFoodFlowInputs.GothaPlaceholder;

        var modifiedEventDelta = baseInputs.EventDelta + effects.FoodDelta;

        Assert.Equal(-25m, modifiedEventDelta);
    }
}
