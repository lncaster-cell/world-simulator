using WorldSimulator.Core.Cities;

namespace WorldSimulator.Core.Resources;

public sealed class DailyFoodFlowCalculator
{
    public DailyFoodFlowResult Calculate(City city, DailyFoodFlowInputs inputs)
    {
        ArgumentNullException.ThrowIfNull(city);
        ArgumentNullException.ThrowIfNull(inputs);

        var startingFood = city.Food;
        var populationConsumption = city.CalculateDailyFoodConsumption();
        var totalDelta =
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
