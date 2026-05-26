using WorldSimulator.Core.Cities;
using WorldSimulator.Core.Resources;
using Xunit;

namespace WorldSimulator.Core.Tests;

public sealed class DailyFoodFlowCalculatorTests
{
    private readonly DailyFoodFlowCalculator _calculator = new();

    [Fact]
    public void Gotha_Population420_Consumes_84_Food_Per_Day()
    {
        var gotha = CityPresets.CreateGotha();

        var consumption = gotha.CalculateDailyFoodConsumption();

        Assert.Equal(84m, consumption);
    }

    [Fact]
    public void Placeholder_Inputs_Produce_Expected_Daily_Balance_For_Gotha()
    {
        var gotha = CityPresets.CreateGotha();
        var inputs = DailyFoodFlowInputs.GothaPlaceholder;

        var result = _calculator.Calculate(gotha, inputs);

        Assert.Equal(1000m, result.StartingFood);
        Assert.Equal(84m, result.PopulationConsumption);
        Assert.Equal(0m, result.AgricultureIncome);
        Assert.Equal(20m, result.FishingIncome);
        Assert.Equal(8m, result.HuntingIncome);
        Assert.Equal(40m, result.MainlandSupplyIncome);
        Assert.Equal(0m, result.EventDelta);
        Assert.Equal(-16m, result.TotalDelta);
        Assert.Equal(984m, result.EndingFood);
    }

    [Fact]
    public void Calculate_Does_Not_Mutate_City()
    {
        var gotha = CityPresets.CreateGotha();

        _ = _calculator.Calculate(gotha, DailyFoodFlowInputs.GothaPlaceholder);

        Assert.Equal(1000m, gotha.Food);
    }

    [Fact]
    public void Apply_Mutates_City_Food()
    {
        var gotha = CityPresets.CreateGotha();
        var result = _calculator.Calculate(gotha, DailyFoodFlowInputs.GothaPlaceholder);

        _calculator.Apply(gotha, result);

        Assert.Equal(984m, gotha.Food);
    }

    [Fact]
    public void EndingFood_Never_Goes_Below_Zero()
    {
        var city = new City(
            id: "city_test",
            name: "Test",
            population: 100,
            food: 10m,
            wealth: 0m,
            mood: 50,
            security: 50,
            crime: 50,
            resources: 0m,
            goods: 0m,
            cityState: CityState.Stagnation);

        var result = _calculator.Calculate(city, new DailyFoodFlowInputs
        {
            AgricultureIncome = 0m,
            FishingIncome = 0m,
            HuntingIncome = 0m,
            MainlandSupplyIncome = 0m,
            EventDelta = 0m
        });

        Assert.Equal(0m, result.EndingFood);
    }

    [Fact]
    public void EventDelta_Can_Reduce_Food()
    {
        var gotha = CityPresets.CreateGotha();

        var result = _calculator.Calculate(gotha, new DailyFoodFlowInputs
        {
            AgricultureIncome = 0m,
            FishingIncome = 20m,
            HuntingIncome = 8m,
            MainlandSupplyIncome = 40m,
            EventDelta = -50m
        });

        Assert.Equal(-66m, result.TotalDelta);
        Assert.Equal(934m, result.EndingFood);
    }

    [Fact]
    public void EventDelta_Can_Increase_Food()
    {
        var gotha = CityPresets.CreateGotha();

        var result = _calculator.Calculate(gotha, new DailyFoodFlowInputs
        {
            AgricultureIncome = 0m,
            FishingIncome = 20m,
            HuntingIncome = 8m,
            MainlandSupplyIncome = 40m,
            EventDelta = 50m
        });

        Assert.Equal(34m, result.TotalDelta);
        Assert.Equal(1034m, result.EndingFood);
    }
    [Fact]
    public void ZeroPopulation_Produces_Zero_TotalDelta()
    {
        var city = CityPresets.CreateGotha();
        city.Population = 0;

        var result = _calculator.Calculate(city, DailyFoodFlowInputs.GothaPlaceholder);

        Assert.Equal(0m, result.TotalDelta);
        Assert.Equal(0m, result.PopulationConsumption);
        Assert.Equal(0m, result.AgricultureIncome);
        Assert.Equal(0m, result.FishingIncome);
        Assert.Equal(0m, result.HuntingIncome);
        Assert.Equal(0m, result.MainlandSupplyIncome);
        Assert.Equal(0m, result.EventDelta);
    }

    [Fact]
    public void ZeroPopulation_Keeps_Food_Unchanged()
    {
        var city = CityPresets.CreateGotha();
        city.Population = 0;
        city.Food = 777m;

        var result = _calculator.Calculate(city, DailyFoodFlowInputs.GothaPlaceholder);

        Assert.Equal(777m, result.StartingFood);
        Assert.Equal(777m, result.EndingFood);
    }

    [Fact]
    public void AgricultureIncome_Is_Included_In_TotalDelta()
    {
        var gotha = CityPresets.CreateGotha();

        var result = _calculator.Calculate(gotha, new DailyFoodFlowInputs
        {
            AgricultureIncome = 10m,
            FishingIncome = 20m,
            HuntingIncome = 8m,
            MainlandSupplyIncome = 40m,
            EventDelta = 0m
        });

        Assert.Equal(-6m, result.TotalDelta);
    }

}
