using WorldSimulator.Core.Cities;

namespace WorldSimulator.Core.Resources;

public enum ProductionDomain
{
    Agriculture,
    Goods,
    Resource
}

public static class ProductionBalancePolicy
{
    public static decimal GetMoodModifier(int mood, ProductionDomain domain)
    {
        return domain switch
        {
            ProductionDomain.Agriculture or ProductionDomain.Goods => mood switch
            {
                >= 70 => 1.05m,
                >= 40 => 1.00m,
                >= 20 => 0.80m,
                _ => 0.55m
            },
            ProductionDomain.Resource => 1.00m,
            _ => throw new ArgumentOutOfRangeException(nameof(domain), domain, "Unsupported production domain.")
        };
    }

    public static decimal GetSecurityModifier(int security, ProductionDomain domain)
    {
        return domain switch
        {
            ProductionDomain.Agriculture or ProductionDomain.Goods => security switch
            {
                >= 70 => 1.05m,
                >= 40 => 1.00m,
                >= 20 => 0.80m,
                _ => 0.55m
            },
            ProductionDomain.Resource => security switch
            {
                >= 70 => 1.05m,
                >= 40 => 1.00m,
                >= 20 => 0.75m,
                _ => 0.50m
            },
            _ => throw new ArgumentOutOfRangeException(nameof(domain), domain, "Unsupported production domain.")
        };
    }

    public static decimal GetStateModifier(CityState state, ProductionDomain domain)
    {
        return domain switch
        {
            ProductionDomain.Agriculture => state switch
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
            },
            ProductionDomain.Goods => state switch
            {
                CityState.Abandoned => 0.00m,
                CityState.Collapse => 0.15m,
                CityState.Famine => 0.45m,
                CityState.FoodShortage => 0.65m,
                CityState.Unrest => 0.45m,
                CityState.CrimeProblem => 0.70m,
                CityState.EconomicDecline => 0.75m,
                CityState.Stagnation => 0.85m,
                CityState.Stable => 1.00m,
                CityState.Prosperous => 1.10m,
                CityState.Recovery => 0.90m,
                _ => throw new ArgumentOutOfRangeException(nameof(state), state, "Unsupported city state.")
            },
            ProductionDomain.Resource => state switch
            {
                CityState.Abandoned => 0.00m,
                CityState.Collapse => 0.20m,
                CityState.Famine => 0.65m,
                CityState.FoodShortage => 0.80m,
                CityState.Unrest => 0.55m,
                CityState.CrimeProblem => 0.75m,
                CityState.EconomicDecline => 0.80m,
                CityState.Stagnation => 0.90m,
                CityState.Stable => 1.00m,
                CityState.Prosperous => 1.05m,
                CityState.Recovery => 0.90m,
                _ => throw new ArgumentOutOfRangeException(nameof(state), state, "Unsupported city state.")
            },
            _ => throw new ArgumentOutOfRangeException(nameof(domain), domain, "Unsupported production domain.")
        };
    }
}
