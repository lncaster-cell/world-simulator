using WorldSimulator.Core.Time;
using Xunit;

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

        clock.Advance(TimeSpan.FromMinutes(120));

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

    [Fact]
    public void RestoreState_Sets_Clock_State()
    {
        var clock = new SimulationClock();

        clock.RestoreState(5, 6, true, TimeSpan.FromSeconds(45), TimeSpan.FromMinutes(1));

        Assert.Equal(5, clock.Day);
        Assert.Equal(6, clock.Hour);
        Assert.True(clock.IsRunning);
        Assert.Equal(TimeSpan.FromSeconds(45), clock.AccumulatedRealTime);
        Assert.Equal(TimeSpan.FromMinutes(1), clock.RealTimePerGameHour);
    }

    [Fact]
    public void RestoreState_Invalid_State_Throws()
    {
        var clock = new SimulationClock();

        Assert.Throws<ArgumentOutOfRangeException>(() => clock.RestoreState(0, 0, false, TimeSpan.Zero, TimeSpan.FromMinutes(1)));
        Assert.Throws<ArgumentOutOfRangeException>(() => clock.RestoreState(1, 24, false, TimeSpan.Zero, TimeSpan.FromMinutes(1)));
        Assert.Throws<ArgumentOutOfRangeException>(() => clock.RestoreState(1, 0, false, TimeSpan.FromSeconds(-1), TimeSpan.FromMinutes(1)));
        Assert.Throws<ArgumentOutOfRangeException>(() => clock.RestoreState(1, 0, false, TimeSpan.Zero, TimeSpan.Zero));
    }

    [Fact]
    public void SetSimulationSpeed_Zero_Throws()
    {
        var clock = new SimulationClock();

        Assert.Throws<ArgumentOutOfRangeException>(() => clock.SetSimulationSpeed(TimeSpan.Zero));
    }

    [Fact]
    public void SetSimulationSpeed_Negative_Throws()
    {
        var clock = new SimulationClock();

        Assert.Throws<ArgumentOutOfRangeException>(() => clock.SetSimulationSpeed(TimeSpan.FromSeconds(-1)));
    }

    [Fact]
    public void SetSimulationSpeed_Allows_10_Seconds()
    {
        var clock = new SimulationClock();

        clock.SetSimulationSpeed(TimeSpan.FromSeconds(10));

        Assert.Equal(TimeSpan.FromSeconds(10), clock.RealTimePerGameHour);
    }

    [Fact]
    public void SetSimulationSpeed_Allows_1_Second()
    {
        var clock = new SimulationClock();

        clock.SetSimulationSpeed(TimeSpan.FromSeconds(1));

        Assert.Equal(TimeSpan.FromSeconds(1), clock.RealTimePerGameHour);
    }


    [Fact]
    public void Turbo_Speed_Advances_One_Day_In_One_Second()
    {
        var clock = new SimulationClock();
        var dayAdvancedCount = 0;

        clock.DayAdvanced += _ => dayAdvancedCount++;
        clock.SetSimulationSpeed(TimeSpan.FromMilliseconds(1000d / 24d));
        clock.Start();

        clock.Advance(TimeSpan.FromSeconds(1));

        Assert.Equal(2, clock.Day);
        Assert.Equal(0, clock.Hour);
        Assert.Equal(1, dayAdvancedCount);
    }

    [Fact]
    public void SetSimulationSpeed_Preserves_Partial_Hour_Progress()
    {
        var clock = new SimulationClock();
        clock.Start();

        clock.Advance(TimeSpan.FromMinutes(4));
        clock.SetSimulationSpeed(TimeSpan.FromSeconds(10));
        clock.Advance(TimeSpan.FromSeconds(1));

        Assert.Equal(1, clock.Day);
        Assert.Equal(0, clock.Hour);
        Assert.Equal(TimeSpan.FromSeconds(9), clock.AccumulatedRealTime);
    }

    [Fact]
    public void SetSimulationSpeed_To_Turbo_Does_Not_Convert_Normal_Backlog_To_Days()
    {
        var clock = new SimulationClock();
        clock.Start();

        clock.Advance(TimeSpan.FromSeconds(260));
        clock.SetSimulationSpeed(TimeSpan.FromMilliseconds(1000d / 24d));

        Assert.Equal(1, clock.Day);
        Assert.Equal(0, clock.Hour);
        Assert.True(clock.AccumulatedRealTime < clock.RealTimePerGameHour);
    }

    [Fact]
    public void RestoreState_Normalizes_Accumulated_Time_For_Selected_Speed()
    {
        var clock = new SimulationClock();

        clock.RestoreState(5, 6, true, TimeSpan.FromSeconds(260), TimeSpan.FromMilliseconds(1000d / 24d));
        clock.Advance(TimeSpan.Zero);

        Assert.Equal(5, clock.Day);
        Assert.Equal(6, clock.Hour);
        Assert.True(clock.AccumulatedRealTime < clock.RealTimePerGameHour);
    }

    [Fact]
    public void Advance_Uses_New_Speed_After_Change()
    {
        var clock = new SimulationClock();
        clock.Start();
        clock.SetSimulationSpeed(TimeSpan.FromSeconds(10));

        clock.Advance(TimeSpan.FromSeconds(30));

        Assert.Equal(3, clock.Hour);
    }
}
