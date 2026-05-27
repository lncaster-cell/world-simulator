using WorldSimulator.Core.Cities;

namespace WorldSimulator.Core.Resources;

public sealed class WeeklyCrimeFlowCalculator
{
    private readonly FoodStressPolicy _foodStressPolicy = new();

    public WeeklyCrimeFlowResult Calculate(
        City city,
        DailyFoodFlowResult foodFlow,
        HouseholdConsumptionResult householdConsumption)
    {
        var startingCrime = city.Crime;

        if (city.Population <= 0 || city.CityState == CityState.Abandoned)
        {
            return new WeeklyCrimeFlowResult
            {
                StartingCrime = startingCrime,
                FoodPressure = 0,
                GoodsShortagePressure = 0,
                ResourcesShortagePressure = 0,
                MoodPressure = 0,
                SecurityPressure = 0,
                CityStatePressure = 0,
                MentalityPressure = 0,
                LawPressure = 0,
                GlobalEventsPressure = 0,
                SecurityReduction = 0,
                FutureOrderMeasuresReduction = 0,
                RawDelta = 0,
                ClampedDelta = 0,
                EndingCrime = startingCrime
            };
        }

        var foodStress = _foodStressPolicy.Evaluate(foodFlow.EndingFood, foodFlow.PopulationConsumption);

        var foodPressure = foodStress.RiskLevel switch
        {
            FoodRiskLevel.High when foodFlow.EndingFood <= 0m => 6,
            FoodRiskLevel.High => 4,
            FoodRiskLevel.Medium => 2,
            _ => 0
        };

        var goodsShortagePressure = householdConsumption.GoodsShortage <= 0m
            ? 0
            : householdConsumption.GoodsShortage >= householdConsumption.RequiredGoods * 0.5m
                ? 2
                : 1;

        var resourcesShortagePressure = householdConsumption.ResourcesShortage > 0m ? 1 : 0;

        var moodPressure = city.Mood < 15
            ? 4
            : city.Mood < 30
                ? 2
                : city.Mood >= 70
                    ? -1
                    : 0;

        var securityPressure = city.Security < 20
            ? 4
            : city.Security < 40
                ? 2
                : city.Security >= 85
                    ? -3
                    : city.Security >= 70
                        ? -2
                        : 0;

        var cityStatePressure = city.CityState switch
        {
            CityState.Prosperous => -1,
            CityState.Stable => -1,
            CityState.Stagnation => 0,
            CityState.Recovery => 0,
            CityState.EconomicDecline => 2,
            CityState.FoodShortage => 3,
            CityState.Famine => 6,
            CityState.CrimeProblem => 3,
            CityState.Unrest => 5,
            CityState.Collapse => 8,
            CityState.Abandoned => 0,
            _ => 0
        };

        const int mentalityPressure = 0;
        const int lawPressure = 0;
        const int globalEventsPressure = 0;
        var securityReduction = securityPressure < 0 ? -securityPressure : 0;
        const int futureOrderMeasuresReduction = 0;

        var rawDelta =
            foodPressure +
            goodsShortagePressure +
            resourcesShortagePressure +
            moodPressure +
            securityPressure +
            cityStatePressure +
            mentalityPressure +
            lawPressure +
            globalEventsPressure -
            futureOrderMeasuresReduction;

        var clampedDelta = Math.Clamp(rawDelta, -5, 8);
        var endingCrime = Math.Clamp(startingCrime + clampedDelta, 1, 100);

        return new WeeklyCrimeFlowResult
        {
            StartingCrime = startingCrime,
            FoodPressure = foodPressure,
            GoodsShortagePressure = goodsShortagePressure,
            ResourcesShortagePressure = resourcesShortagePressure,
            MoodPressure = moodPressure,
            SecurityPressure = securityPressure,
            CityStatePressure = cityStatePressure,
            MentalityPressure = mentalityPressure,
            LawPressure = lawPressure,
            GlobalEventsPressure = globalEventsPressure,
            SecurityReduction = securityReduction,
            FutureOrderMeasuresReduction = futureOrderMeasuresReduction,
            RawDelta = rawDelta,
            ClampedDelta = clampedDelta,
            EndingCrime = endingCrime
        };
    }
}
