namespace WorldSimulator.Core.Simulation;

public sealed class SimulationCalendarPolicy
{
    public int DaysPerWeek { get; init; } = 7;
    public int DaysPerMonth { get; init; } = 30;
    public int DaysPerHalfYear { get; init; } = 180;
    public int DaysPerYear { get; init; } = 360;

    public void Validate()
    {
        if (DaysPerWeek <= 0) throw new ArgumentOutOfRangeException(nameof(DaysPerWeek), "Days per week must be greater than zero.");
        if (DaysPerMonth <= 0) throw new ArgumentOutOfRangeException(nameof(DaysPerMonth), "Days per month must be greater than zero.");
        if (DaysPerHalfYear <= 0) throw new ArgumentOutOfRangeException(nameof(DaysPerHalfYear), "Days per half-year must be greater than zero.");
        if (DaysPerYear <= 0) throw new ArgumentOutOfRangeException(nameof(DaysPerYear), "Days per year must be greater than zero.");
        if (DaysPerHalfYear > DaysPerYear) throw new ArgumentOutOfRangeException(nameof(DaysPerHalfYear), "Days per half-year must not exceed days per year.");
    }
}
