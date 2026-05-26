using WorldSimulator.Core.Cities;
using WorldSimulator.Core.World;

namespace WorldSimulator.Core.Resources;

public sealed class AgricultureProductionCalculator
{
    public AgricultureProductionResult Calculate(City city, SettlementEconomyProfile profile)
    {
        ArgumentNullException.ThrowIfNull(city);
        ArgumentNullException.ThrowIfNull(profile);

        var naturalPotential = profile.AgriculturePotential;
        var requiredWorkers = Math.Max(5, (int)Math.Ceiling(naturalPotential * 1.5m));
        var assignedWorkers = Math.Max(0, city.Population / 12);
        var workerCoverage = requiredWorkers <= 0
            ? 0m
            : Math.Min((decimal)assignedWorkers / requiredWorkers, 1m);

        var moodModifier = GetMoodModifier(city.Mood);
        var securityModifier = GetSecurityModifier(city.Security);
        var stateModifier = GetStateModifier(city.CityState);

        var finalOutput = city.Population <= 0 || city.CityState == CityState.Abandoned
            ? 0m
            : decimal.Round(Math.Max(0m, naturalPotential * workerCoverage * moodModifier * securityModifier * stateModifier), 2);

        return new AgricultureProductionResult
        {
            NaturalPotential = naturalPotential,
            RequiredWorkers = requiredWorkers,
            AssignedWorkers = assignedWorkers,
            WorkerCoverage = workerCoverage,
            MoodModifier = moodModifier,
            SecurityModifier = securityModifier,
            StateModifier = stateModifier,
            FinalOutput = finalOutput
        };
    }

    private static decimal GetMoodModifier(int mood) => mood switch
    {
        >= 70 => 1.05m,
        >= 40 => 1.00m,
        >= 20 => 0.80m,
        _ => 0.55m
    };

    private static decimal GetSecurityModifier(int security) => security switch
    {
        >= 70 => 1.05m,
        >= 40 => 1.00m,
        >= 20 => 0.80m,
        _ => 0.55m
    };

    private static decimal GetStateModifier(CityState state) => state switch
    {
        CityState.Abandoned => 0.00m,
        CityState.Collapse => 0.20m,
        CityState.Famine => 0.70m,
        CityState.FoodShortage => 0.85m,
        CityState.Unrest => 0.65m,
        CityState.CrimeProblem => 0.80m,
        CityState.EconomicDecline => 0.85m,
        CityState.Stagnation => 0.90m,
        CityState.Stable => 1.00m,
        CityState.Prosperous => 1.10m,
        CityState.Recovery => 0.90m,
        _ => throw new ArgumentOutOfRangeException(nameof(state), state, "Unsupported city state.")
    };
}
