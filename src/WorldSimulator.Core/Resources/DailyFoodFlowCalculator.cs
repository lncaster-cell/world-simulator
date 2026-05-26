using WorldSimulator.Core.Cities;

namespace WorldSimulator.Core.Resources;

public sealed class DailyFoodFlowCalculator
{
    public DailyFoodFlowResult Calculate(City city, DailyFoodFlowInputs inputs)
    {
        ArgumentNullException.ThrowIfNull(city);
        ArgumentNullException.ThrowIfNull(inputs);

        var startingFood = city.Food;

        if (city.Population <= 0)
        {
            return new DailyFoodFlowResult
            {
                StartingFood = startingFood,
                PopulationConsumption = 0m,
                AgricultureIncome = 0m,
                FishingIncome = 0m,
                HuntingIncome = 0m,
                MainlandSupplyIncome = 0m,
                EventDelta = 0m,
                TotalDelta = 0m,
                EndingFood = startingFood
            };
        }

        var populationConsumption = city.CalculateDailyFoodConsumption();
        var totalDelta =
            inputs.AgricultureIncome +
            inputs.FishingIncome +
            inputs.HuntingIncome +
            inputs.MainlandSupplyIncome +
            inputs.EventDelta -
            populationConsumption;

        var endingFood = Math.Max(0m, startingFood + totalDelta);

        return new DailyFoodFlowResult
        {
            StartingFood = startingFood,
            PopulationConsumption = populationConsumption,
            AgricultureIncome = inputs.AgricultureIncome,
            FishingIncome = inputs.FishingIncome,
            HuntingIncome = inputs.HuntingIncome,
            MainlandSupplyIncome = inputs.MainlandSupplyIncome,
            EventDelta = inputs.EventDelta,
            TotalDelta = totalDelta,
            EndingFood = endingFood
        };
    }

    public void Apply(City city, DailyFoodFlowResult result)
    {
        ArgumentNullException.ThrowIfNull(city);
        ArgumentNullException.ThrowIfNull(result);

        city.Food = result.EndingFood;
    }
}
