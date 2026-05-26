namespace WorldSimulator.Core.Cities;

public static class CityPresets
{
    /// <summary>
    /// Gotha start preset for MVP 0.1.
    /// </summary>
    public static City CreateGotha() => new(
        id: "gotha",
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

    public static City CreateHighrock() => new(
        id: "highrock",
        name: "Highrock",
        population: 650,
        food: 900m,
        wealth: 380m,
        mood: 55,
        security: 75,
        crime: 8,
        resources: 750m,
        goods: 180m,
        cityState: CityState.Stable);

    public static City CreateMlynek() => new(
        id: "mlynek",
        name: "Mlynek",
        population: 320,
        food: 900m,
        wealth: 180m,
        mood: 58,
        security: 55,
        crime: 10,
        resources: 420m,
        goods: 100m,
        cityState: CityState.Stable);

    public static City CreateWardmark() => new(
        id: "wardmark",
        name: "Wardmark",
        population: 280,
        food: 650m,
        wealth: 170m,
        mood: 50,
        security: 80,
        crime: 7,
        resources: 500m,
        goods: 90m,
        cityState: CityState.Stable);

    public static City CreateRivenstal() => new(
        id: "rivenstal",
        name: "Rivenstal",
        population: 360,
        food: 1100m,
        wealth: 210m,
        mood: 60,
        security: 58,
        crime: 9,
        resources: 300m,
        goods: 110m,
        cityState: CityState.Stable);

    public static City CreateGavern() => new(
        id: "gavern",
        name: "Gavern",
        population: 520,
        food: 850m,
        wealth: 260m,
        mood: 52,
        security: 52,
        crime: 16,
        resources: 800m,
        goods: 180m,
        cityState: CityState.Stagnation);

    public static City CreateBrno() => new(
        id: "brno",
        name: "Brno",
        population: 180,
        food: 700m,
        wealth: 120m,
        mood: 62,
        security: 50,
        crime: 8,
        resources: 180m,
        goods: 55m,
        cityState: CityState.Stable);

    public static City CreateWodenz() => new(
        id: "wodenz",
        name: "Wödenz",
        population: 340,
        food: 1150m,
        wealth: 160m,
        mood: 57,
        security: 48,
        crime: 12,
        resources: 260m,
        goods: 90m,
        cityState: CityState.Stable);

    public static City CreateThokurRus() => new(
        id: "thokur_rus",
        name: "Thökur-Rus",
        population: 80,
        food: 280m,
        wealth: 70m,
        mood: 50,
        security: 35,
        crime: 18,
        resources: 90m,
        goods: 25m,
        cityState: CityState.Stagnation);
}
