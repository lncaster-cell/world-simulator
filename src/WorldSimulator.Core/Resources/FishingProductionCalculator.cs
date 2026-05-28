using WorldSimulator.Core.Cities;
using WorldSimulator.Core.Events;

namespace WorldSimulator.Core.Resources;

public sealed class FishingProductionCalculator
{
    private const decimal NaturalPotential = 18m;
    private const int InfrastructureLevel = 2;
    private const decimal OverstaffBonusCap = 0.10m;
    private const decimal StormPenaltyModifier = 0.15m;

    public FishingProductionResult Calculate(City city, IReadOnlyCollection<CityEvent> activeEvents)
    {
        ArgumentNullException.ThrowIfNull(city);

        return Calculate(city, activeEvents, Math.Max(0, city.Population / 10));
    }

    public FishingProductionResult Calculate(City city, int assignedWorkers)
    {
        return Calculate(city, Array.Empty<CityEvent>(), assignedWorkers);
    }

    public FishingProductionResult Calculate(City city, IReadOnlyCollection<CityEvent> activeEvents, int assignedWorkers)
    {
        ArgumentNullException.ThrowIfNull(city);
        ArgumentNullException.ThrowIfNull(activeEvents);

        var infrastructureModifier = GetInfrastructureModifier(InfrastructureLevel);
        var infrastructureCapacity = NaturalPotential * infrastructureModifier;
        var requiredWorkers = GetRequiredWorkers(InfrastructureLevel);
        var stateModifier = GetStateModifier(city.CityState);
        var stormModifier = activeEvents.Any(e => e.Id == "port_storm")
            ? StormPenaltyModifier
            : 1.00m;

        if (city.Population <= 0 || city.CityState == CityState.Abandoned)
        {
            return new FishingProductionResult
            {
                NaturalPotential = NaturalPotential,
                InfrastructureLevel = InfrastructureLevel,
                InfrastructureModifier = infrastructureModifier,
                InfrastructureCapacity = infrastructureCapacity,
                RequiredWorkers = requiredWorkers,
                AssignedWorkers = 0,
                WorkerCoverage = 0m,
                ExtraWorkers = 0,
                OverstaffBonus = 0m,
                StormModifier = stormModifier,
                StateModifier = stateModifier,
                FinalOutput = 0m
            };
        }

        assignedWorkers = Math.Max(0, assignedWorkers);
        var workerCoverage = requiredWorkers > 0
            ? Math.Min((decimal)assignedWorkers / requiredWorkers, 1m)
            : 0m;
        var extraWorkers = Math.Max(assignedWorkers - requiredWorkers, 0);

        var overstaffBonus = 0m;
        if (extraWorkers > 0 && requiredWorkers > 0)
        {
            var overstaffRatio = (decimal)extraWorkers / requiredWorkers;
            overstaffBonus = infrastructureCapacity * OverstaffBonusCap * (1m - (1m / (1m + overstaffRatio)));
        }

        var finalOutputRaw = (infrastructureCapacity * workerCoverage + overstaffBonus) * stormModifier * stateModifier;
        var finalOutput = decimal.Round(Math.Max(0m, finalOutputRaw), 2);

        return new FishingProductionResult
        {
            NaturalPotential = NaturalPotential,
            InfrastructureLevel = InfrastructureLevel,
            InfrastructureModifier = infrastructureModifier,
            InfrastructureCapacity = infrastructureCapacity,
            RequiredWorkers = requiredWorkers,
            AssignedWorkers = assignedWorkers,
            WorkerCoverage = workerCoverage,
            ExtraWorkers = extraWorkers,
            OverstaffBonus = overstaffBonus,
            StormModifier = stormModifier,
            StateModifier = stateModifier,
            FinalOutput = finalOutput
        };
    }

    private static decimal GetInfrastructureModifier(int level)
    {
        return level switch
        {
            0 => 0.50m,
            1 => 0.80m,
            2 => 1.00m,
            3 => 1.25m,
            _ => throw new ArgumentOutOfRangeException(nameof(level), level, "Unsupported fishing infrastructure level.")
        };
    }

    private static int GetRequiredWorkers(int level)
    {
        return level switch
        {
            0 => 10,
            1 => 20,
            2 => 30,
            3 => 40,
            _ => throw new ArgumentOutOfRangeException(nameof(level), level, "Unsupported fishing infrastructure level.")
        };
    }

    private static decimal GetStateModifier(CityState state)
    {
        return state switch
        {
            CityState.Abandoned => 0.00m,
            CityState.Collapse => 0.20m,
            CityState.Famine => 0.60m,
            CityState.FoodShortage => 0.75m,
            CityState.Unrest => 0.60m,
            CityState.CrimeProblem => 0.80m,
            CityState.EconomicDecline => 0.85m,
            CityState.Stagnation => 0.90m,
            CityState.Stable => 1.00m,
            CityState.Prosperous => 1.10m,
            CityState.Recovery => 0.90m,
            _ => throw new ArgumentOutOfRangeException(nameof(state), state, "Unsupported city state.")
        };
    }
}
