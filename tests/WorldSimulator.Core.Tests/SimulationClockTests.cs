using WorldSimulator.Core.Time;

namespace WorldSimulator.Core.Tests;

public sealed class SimulationClockTests
{
    [Fact]
    public void Clock_Starts_At_Day1_Hour0()
    {
        var clock = new SimulationClock();

        Assert.Equal(1, clock.Day);
        Assert.Equal(0, clock.Hour);
        Assert.False(clock.IsRunning);
    }

    [Fact]
    public void Pause_Prevents_Advancement()
    {
        var clock = new SimulationClock();
        clock.Start();
        clock.Pause();

        clock.Advance(TimeSpan.FromMinutes(5));

        Assert.Equal(1, clock.Day);
        Assert.Equal(0, clock.Hour);
    }

    [Fact]
    public void Start_Allows_Advancement()
    {
        var clock = new SimulationClock();
        clock.Start();

        clock.Advance(TimeSpan.FromMinutes(5));

        Assert.Equal(1, clock.Day);
        Assert.Equal(1, clock.Hour);
    }

    [Fact]
    public void Default_RealTimePerGameHour_Is_5_Minutes()
    {
        var clock = new SimulationClock();
        clock.Start();

        clock.Advance(TimeSpan.FromMinutes(5));

        Assert.Equal(1, clock.Hour);
    }

    [Fact]
    public void TwentyFour_Hours_Advance_Day()
    {
        var clock = new SimulationClock();
        clock.Start();

        clock.Advance(TimeSpan.FromHours(2));

        for (var i = 0; i < 22; i++)
        {
            clock.Advance(TimeSpan.FromMinutes(5));
        }

        Assert.Equal(2, clock.Day);
        Assert.Equal(0, clock.Hour);
    }

    [Fact]
    public void Accumulated_Time_Can_Advance_Multiple_Hours()
    {
        var clock = new SimulationClock();
        clock.Start();

        clock.Advance(TimeSpan.FromMinutes(16));

        Assert.Equal(3, clock.Hour);
        Assert.Equal(TimeSpan.FromMinutes(1), clock.AccumulatedRealTime);
    }

    [Fact]
    public void Invalid_RealTimePerGameHour_Throws()
    {
        var settings = new SimulationTimeSettings();

        Assert.Throws<ArgumentOutOfRangeException>(() => settings.RealTimePerGameHour = TimeSpan.Zero);
        Assert.Throws<ArgumentOutOfRangeException>(() => settings.RealTimePerGameHour = TimeSpan.FromMinutes(-1));
    }

    [Fact]
    public void Raises_Hour_And_Day_Events()
    {
        var clock = new SimulationClock();
        clock.Start();

        var hours = new List<(int Day, int Hour)>();
        var days = new List<int>();

        clock.HourAdvanced += (day, hour) => hours.Add((day, hour));
        clock.DayAdvanced += day => days.Add(day);

        clock.Advance(TimeSpan.FromHours(2));

        Assert.Equal(24, hours.Count);
        Assert.Single(days);
        Assert.Equal(2, days[0]);
        Assert.Equal((2, 0), hours[^1]);
    }
}
