using WorldSimulator.Core.Events;
using Xunit;

namespace WorldSimulator.Core.Tests;

public sealed class WorldEventStateTests
{
    [Fact]
    public void ReplaceWith_CopiesManagers_AndIsIndependentFromSourceManagers()
    {
        var sourceManager = new CityEventManager();
        sourceManager.AddEvent(new CityEvent("active-1", "Active", "D", startedDay: 1, durationDays: 5, remainingDays: 5));
        sourceManager.Restore(
            sourceManager.ActiveEvents,
            [new CityEvent("completed-1", "Completed", "D", startedDay: 1, durationDays: 1, remainingDays: 0)]);

        var source = new Dictionary<string, CityEventManager>(StringComparer.Ordinal)
        {
            ["city-1"] = sourceManager
        };

        var state = new WorldEventState();
        state.ReplaceWith(source);

        sourceManager.AddEvent(new CityEvent("active-2", "Active2", "D", startedDay: 2, durationDays: 2, remainingDays: 2));
        sourceManager.Restore(
            sourceManager.ActiveEvents,
            [.. sourceManager.CompletedEvents, new CityEvent("completed-2", "Completed2", "D", 2, 1, 0)]);

        var copiedManager = state.EventManagersByCity["city-1"];

        Assert.NotSame(sourceManager, copiedManager);
        Assert.Equal(["active-1"], copiedManager.ActiveEvents.Select(e => e.Id).ToArray());
        Assert.Equal(["completed-1"], copiedManager.CompletedEvents.Select(e => e.Id).ToArray());
    }
}
