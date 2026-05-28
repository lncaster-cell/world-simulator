namespace WorldSimulator.Core.Simulation;

public sealed class SimulationCadenceResolver
{
    private readonly SimulationCalendarPolicy _calendarPolicy;

    public SimulationCadenceResolver()
        : this(new SimulationCalendarPolicy())
    {
    }

    public SimulationCadenceResolver(SimulationCalendarPolicy calendarPolicy)
    {
        _calendarPolicy = calendarPolicy ?? throw new ArgumentNullException(nameof(calendarPolicy));
        _calendarPolicy.Validate();
    }

    public bool ShouldRun(int day, SimulationCadence cadence)
    {
        if (day <= 0)
        {
            return false;
        }

        return cadence switch
        {
            SimulationCadence.Daily => true,
            SimulationCadence.Weekly => day % _calendarPolicy.DaysPerWeek == 0,
            SimulationCadence.Monthly => day % _calendarPolicy.DaysPerMonth == 0,
            SimulationCadence.HalfYearly => day % _calendarPolicy.DaysPerHalfYear == 0,
            SimulationCadence.Yearly => day % _calendarPolicy.DaysPerYear == 0,
            _ => throw new ArgumentOutOfRangeException(nameof(cadence), cadence, "Unsupported simulation cadence.")
        };
    }
}
