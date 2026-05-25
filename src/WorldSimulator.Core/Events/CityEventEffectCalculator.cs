using WorldSimulator.Core.Cities;

namespace WorldSimulator.Core.Events;

public sealed class CityEventEffectCalculator
{
    public CityEventEffectsResult Calculate(City city, IReadOnlyCollection<CityEvent> activeEvents)
    {
        ArgumentNullException.ThrowIfNull(city);
        ArgumentNullException.ThrowIfNull(activeEvents);

        var foodDelta = 0m;
        var moodDelta = 0;
        var securityDelta = 0;
        var crimeDelta = 0;
        var wealthDelta = 0m;
        var resourcesDelta = 0m;
        var mainlandSupplyDelta = 0m;

        foreach (var activeEvent in activeEvents)
        {
            switch (activeEvent.Id)
            {
                case "fire":
                    wealthDelta += -20m;
                    resourcesDelta += -10m;
                    moodDelta += -5;
                    break;
                case "disease":
                    moodDelta += -8;
                    securityDelta += -2;
                    break;
                case "rat_infestation":
                    foodDelta += -25m;
                    moodDelta += -3;
                    break;
                case "artists_performance":
                    moodDelta += 10;
                    crimeDelta += -2;
                    break;
                case "port_storm":
                    mainlandSupplyDelta += -30m;
                    moodDelta += -2;
                    break;
            }
        }

        return new CityEventEffectsResult
        {
            FoodDelta = foodDelta,
            MoodDelta = moodDelta,
            SecurityDelta = securityDelta,
            CrimeDelta = crimeDelta,
            WealthDelta = wealthDelta,
            ResourcesDelta = resourcesDelta,
            MainlandSupplyDelta = mainlandSupplyDelta
        };
    }
}
