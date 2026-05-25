namespace WorldSimulator.Core.Events;

public static class CityEventPresets
{
    public static CityEvent CreateFire(int currentDay) => new(
        id: "fire",
        name: "Пожар",
        description: "В одном из кварталов вспыхнул пожар, требующий срочного реагирования.",
        startedDay: currentDay,
        durationDays: 1);

    public static CityEvent CreateDisease(int currentDay) => new(
        id: "disease",
        name: "Болезнь",
        description: "В городе отмечен рост заболеваемости среди жителей.",
        startedDay: currentDay,
        durationDays: 3);

    public static CityEvent CreateRatInfestation(int currentDay) => new(
        id: "rat_infestation",
        name: "Нашествие крыс",
        description: "В складских районах замечено массовое нашествие крыс.",
        startedDay: currentDay,
        durationDays: 2);

    public static CityEvent CreateArtistsPerformance(int currentDay) => new(
        id: "artists_performance",
        name: "Выступление артистов",
        description: "На городской площади проходит выступление приезжих артистов.",
        startedDay: currentDay,
        durationDays: 1);

    public static CityEvent CreatePortStorm(int currentDay) => new(
        id: "port_storm",
        name: "Шторм у порта",
        description: "Сильный шторм осложнил работу порта и прибрежных служб.",
        startedDay: currentDay,
        durationDays: 2);
}
