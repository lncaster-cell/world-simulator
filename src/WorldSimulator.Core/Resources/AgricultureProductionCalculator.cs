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

        var moodModifier = ProductionBalancePolicy.GetMoodModifier(city.Mood, ProductionDomain.Agriculture);
        var securityModifier = ProductionBalancePolicy.GetSecurityModifier(city.Security, ProductionDomain.Agriculture);
        var stateModifier = ProductionBalancePolicy.GetStateModifier(city.CityState, ProductionDomain.Agriculture);

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
}
