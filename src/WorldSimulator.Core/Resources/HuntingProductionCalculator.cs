using WorldSimulator.Core.Cities;

namespace WorldSimulator.Core.Resources;

public sealed class HuntingProductionCalculator
{
    private const decimal NaturalPotential = 8m;
    private const int RequiredWorkers = 15;
    private const decimal OverstaffBonusCap = 0.05m;

    public HuntingProductionResult Calculate(City city)
    {
        ArgumentNullException.ThrowIfNull(city);

        var securityModifier = GetSecurityModifier(city.Security);
        var stateModifier = GetStateModifier(city.CityState);

        if (city.Population <= 0 || city.CityState == CityState.Abandoned)
        {
            return new HuntingProductionResult
            {
                NaturalPotential = NaturalPotential,
                RequiredWorkers = RequiredWorkers,
                AssignedWorkers = 0,
                WorkerCoverage = 0m,
                ExtraWorkers = 0,
                OverstaffBonus = 0m,
                SecurityModifier = securityModifier,
                StateModifier = stateModifier,
                FinalOutput = 0m
            };
        }

        var assignedWorkers = Math.Max(0, city.Population / 20);
        var workerCoverage = RequiredWorkers > 0
            ? Math.Min((decimal)assignedWorkers / RequiredWorkers, 1m)
            : 0m;

        var extraWorkers = Math.Max(assignedWorkers - RequiredWorkers, 0);
        var overstaffBonus = 0m;

        if (extraWorkers > 0 && RequiredWorkers > 0)
        {
            var overstaffRatio = (decimal)extraWorkers / RequiredWorkers;
            overstaffBonus = NaturalPotential * OverstaffBonusCap * (1m - (1m / (1m + overstaffRatio)));
        }

        var finalOutputRaw = (NaturalPotential * workerCoverage + overstaffBonus) * securityModifier * stateModifier;
        var finalOutput = decimal.Round(Math.Max(0m, finalOutputRaw), 2);

        return new HuntingProductionResult
        {
            NaturalPotential = NaturalPotential,
            RequiredWorkers = RequiredWorkers,
            AssignedWorkers = assignedWorkers,
            WorkerCoverage = workerCoverage,
            ExtraWorkers = extraWorkers,
            OverstaffBonus = overstaffBonus,
            SecurityModifier = securityModifier,
            StateModifier = stateModifier,
            FinalOutput = finalOutput
        };
    }

    private static decimal GetSecurityModifier(int security)
    {
        return security switch
        {
            >= 70 => 1.05m,
            >= 40 => 1.00m,
            >= 20 => 0.75m,
            _ => 0.45m
        };
    }

    private static decimal GetStateModifier(CityState state)
    {
        return state switch
        {
            CityState.Abandoned => 0.00m,
            CityState.Collapse => 0.20m,
            CityState.Famine => 0.80m,
            CityState.FoodShortage => 0.90m,
            CityState.Unrest => 0.55m,
            CityState.CrimeProblem => 0.70m,
            CityState.EconomicDecline => 0.85m,
            CityState.Stagnation => 0.90m,
            CityState.Stable => 1.00m,
            CityState.Prosperous => 1.05m,
            CityState.Recovery => 0.90m,
            _ => throw new ArgumentOutOfRangeException(nameof(state), state, "Unsupported city state.")
        };
    }
}
