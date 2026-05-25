namespace WorldSimulator.Core.Cities;

/// <summary>
/// Stable aggregated summary of the current city condition.
/// </summary>
public enum CityState
{
    Stable,
    Prosperous,
    Stagnation,
    FoodShortage,
    Famine,
    EconomicDecline,
    CrimeProblem,
    Unrest,
    Recovery,
    Collapse
}
