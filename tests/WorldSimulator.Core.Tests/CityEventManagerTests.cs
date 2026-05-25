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
    public void AdvanceDay_TrimsCompletedEventsToMaxLimit()
    {
        var manager = new CityEventManager(maxCompletedEvents: 2);
        manager.AddEvent(new CityEvent("e1", "E1", "D1", startedDay: 1, durationDays: 1, remainingDays: 1));
        manager.AddEvent(new CityEvent("e2", "E2", "D2", startedDay: 2, durationDays: 1, remainingDays: 1));
        manager.AddEvent(new CityEvent("e3", "E3", "D3", startedDay: 3, durationDays: 1, remainingDays: 1));

        manager.AdvanceDay();

        Assert.Equal(2, manager.CompletedEvents.Count);
        Assert.Equal(["e2", "e3"], manager.CompletedEvents.Select(e => e.Id).ToArray());
    }

    [Fact]
    public void Restore_TrimsCompletedEventsToMaxLimit()
    {
        var manager = new CityEventManager(maxCompletedEvents: 2);
        var completed = new[]
        {
            new CityEvent("old", "Old", "D", 1, 1, 0),
            new CityEvent("mid", "Mid", "D", 2, 1, 0),
            new CityEvent("new", "New", "D", 3, 1, 0)
        };

        manager.Restore([], completed);

        Assert.Equal(2, manager.CompletedEvents.Count);
        Assert.Equal(["mid", "new"], manager.CompletedEvents.Select(e => e.Id).ToArray());
    }

    [Fact]
    public void Restore_DoesNotTrimActiveEvents()
    {
        var manager = new CityEventManager(maxCompletedEvents: 1);
        var active = new[]
        {
            new CityEvent("a1", "A1", "D", 1, 2, 1),
            new CityEvent("a2", "A2", "D", 1, 2, 1),
            new CityEvent("a3", "A3", "D", 1, 2, 1)
        };

        manager.Restore(active, []);

        Assert.Equal(3, manager.ActiveEvents.Count);
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
