using WorldSimulator.Core.Cities;
using WorldSimulator.Core.Events;

namespace WorldSimulator.Core.Resources;

public sealed class MainlandSupplyProductionCalculator
{
    private const decimal NaturalSupplyPotential = 40m;
    private const int InfrastructureLevel = 2;
    private const decimal StormPenaltyModifier = 0.25m;

    public MainlandSupplyProductionResult Calculate(City city, IReadOnlyCollection<CityEvent> activeEvents)
    {
        ArgumentNullException.ThrowIfNull(city);
        ArgumentNullException.ThrowIfNull(activeEvents);

        var infrastructureModifier = GetInfrastructureModifier(InfrastructureLevel);
        var infrastructureCapacity = NaturalSupplyPotential * infrastructureModifier;
        var securityModifier = GetSecurityModifier(city.Security);
        var wealthModifier = GetWealthModifier(city.Wealth);
        var stormModifier = activeEvents.Any(e => e.Id == "port_storm")
            ? StormPenaltyModifier
            : 1.00m;

        var stateModifier = city.Population <= 0 || city.CityState == CityState.Abandoned
            ? 0.00m
            : GetStateModifier(city.CityState);

        var finalOutputRaw = infrastructureCapacity * securityModifier * wealthModifier * stateModifier * stormModifier;
        var finalOutput = decimal.Round(Math.Max(0m, finalOutputRaw), 2);

        return new MainlandSupplyProductionResult
        {
            NaturalSupplyPotential = NaturalSupplyPotential,
            InfrastructureLevel = InfrastructureLevel,
            InfrastructureModifier = infrastructureModifier,
            InfrastructureCapacity = infrastructureCapacity,
            SecurityModifier = securityModifier,
            WealthModifier = wealthModifier,
            StateModifier = stateModifier,
            StormModifier = stormModifier,
            FinalOutput = finalOutput
        };
    }

    private static decimal GetInfrastructureModifier(int level) => level switch
    {
        0 => 0.40m,
        1 => 0.75m,
        2 => 1.00m,
        3 => 1.25m,
        _ => throw new ArgumentOutOfRangeException(nameof(level), level, "Unsupported mainland supply infrastructure level.")
    };

    private static decimal GetSecurityModifier(int security) => security switch
    {
        >= 70 => 1.05m,
        >= 40 => 1.00m,
        >= 20 => 0.75m,
        _ => 0.45m
    };

    private static decimal GetWealthModifier(decimal wealth) => wealth switch
    {
        >= 400m => 1.10m,
        >= 200m => 1.00m,
        >= 100m => 0.85m,
        _ => 0.60m
    };

    private static decimal GetStateModifier(CityState state) => state switch
    {
        CityState.Abandoned => 0.00m,
        CityState.Collapse => 0.20m,
        CityState.Famine => 0.70m,
        CityState.FoodShortage => 0.80m,
        CityState.Unrest => 0.50m,
        CityState.CrimeProblem => 0.75m,
        CityState.EconomicDecline => 0.75m,
        CityState.Stagnation => 0.90m,
        CityState.Stable => 1.00m,
        CityState.Prosperous => 1.10m,
        CityState.Recovery => 0.90m,
        _ => throw new ArgumentOutOfRangeException(nameof(state), state, "Unsupported city state.")
    };
}
