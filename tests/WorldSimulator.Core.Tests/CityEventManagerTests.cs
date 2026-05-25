using WorldSimulator.Core.Events;
using Xunit;

namespace WorldSimulator.Core.Tests;

public sealed class CityEventManagerTests
{
    [Fact]
    public void CityEvent_IsCreatedAsActive()
    {
        var cityEvent = CityEventPresets.CreateFire(currentDay: 1);

        Assert.True(cityEvent.IsActive);
        Assert.False(cityEvent.IsCompleted);
    }

    [Fact]
    public void AdvanceDay_DecreasesRemainingDays()
    {
        var cityEvent = CityEventPresets.CreateDisease(currentDay: 1);

        cityEvent.AdvanceDay();

        Assert.Equal(2, cityEvent.RemainingDays);
    }

    [Fact]
    public void Event_CompletesAfterDurationDays()
    {
        var cityEvent = CityEventPresets.CreateDisease(currentDay: 1);

        cityEvent.AdvanceDay();
        cityEvent.AdvanceDay();
        cityEvent.AdvanceDay();

        Assert.Equal(0, cityEvent.RemainingDays);
        Assert.True(cityEvent.IsCompleted);
    }

    [Fact]
    public void AdvanceDay_MovesCompletedEventsToCompletedList()
    {
        var manager = new CityEventManager();
        var cityEvent = CityEventPresets.CreateFire(currentDay: 1);

        manager.AddEvent(cityEvent);

        manager.AdvanceDay();

        Assert.Empty(manager.ActiveEvents);
        Assert.Single(manager.CompletedEvents);
        Assert.Equal("fire", manager.CompletedEvents[0].Id);
    }

    [Fact]
    public void AddEvent_RejectsDuplicateActiveEventId()
    {
        var manager = new CityEventManager();

        var addedFirst = manager.AddEvent(CityEventPresets.CreateFire(currentDay: 1));
        var addedSecond = manager.AddEvent(CityEventPresets.CreateFire(currentDay: 1));

        Assert.True(addedFirst);
        Assert.False(addedSecond);
        Assert.Single(manager.ActiveEvents);
    }

    [Fact]
    public void FirePreset_HasRussianName_AndOneDayDuration()
    {
        var cityEvent = CityEventPresets.CreateFire(currentDay: 1);

        Assert.Equal("Пожар", cityEvent.Name);
        Assert.Equal(1, cityEvent.DurationDays);
    }

    [Fact]
    public void DiseasePreset_HasThreeDayDuration()
    {
        var cityEvent = CityEventPresets.CreateDisease(currentDay: 1);

        Assert.Equal(3, cityEvent.DurationDays);
    }
}
