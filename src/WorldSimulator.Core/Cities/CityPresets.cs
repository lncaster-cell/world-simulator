namespace WorldSimulator.Core.Cities;

public static class CityPresets
{
    /// <summary>
    /// Gotha start preset for MVP 0.1.
    /// </summary>
    public static City CreateGotha() => new(
        id: "city_gotha",
        name: "Гота",
        population: 420,
        food: 1000m,
        wealth: 320m,
        mood: 55,
        security: 60,
        crime: 30,
        resources: 260m,
        goods: 140m,
        cityState: CityState.Stagnation);
}
