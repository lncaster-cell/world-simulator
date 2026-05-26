using FluentAssertions;
using WorldSimulator.Core.Cities;
using WorldSimulator.Core.Resources;

namespace WorldSimulator.Core.Tests;

public sealed class WeeklyCrimeFlowCalculatorTests
{
    private readonly WeeklyCrimeFlowCalculator _calculator = new();

    [Fact]
    public void Crime_IsClampedToMinimumOne()
    {
        var city = CreateCity();

        city.Crime = 0;
        city.Crime.Should().Be(1);

        city.Crime = -10;
        city.Crime.Should().Be(1);
    }

    [Fact]
    public void StableGoodConditions_ReduceCrimeButNotBelowOne()
    {
        var city = CreateCity(crime: 2, mood: 80, security: 90, state: CityState.Stable);

        var result = _calculator.Calculate(city, CreateFoodFlow(100m, 10m), CreateConsumption());

        result.EndingCrime.Should().Be(1);
        result.ClampedDelta.Should().BeLessThan(0);
    }

    [Fact]
    public void FoodShortage_IncreasesCrimeStrongly()
    {
        var city = CreateCity(crime: 20);

        var result = _calculator.Calculate(city, CreateFoodFlow(0m, 10m), CreateConsumption());

        result.FoodPressure.Should().Be(6);
        result.EndingCrime.Should().BeGreaterThan(20);
    }

    [Fact]
    public void GoodsAndResourcesShortage_IncreaseCrimeWeakly()
    {
        var city = CreateCity(crime: 20);
        var weakResult = _calculator.Calculate(city, CreateFoodFlow(100m, 10m), CreateConsumption(goodsShortage: 3m, requiredGoods: 10m, resourcesShortage: 1m));
        var foodResult = _calculator.Calculate(city, CreateFoodFlow(0m, 10m), CreateConsumption());

        weakResult.GoodsShortagePressure.Should().Be(1);
        weakResult.ResourcesShortagePressure.Should().Be(1);
        (weakResult.GoodsShortagePressure + weakResult.ResourcesShortagePressure)
            .Should().BeLessThan(foodResult.FoodPressure);
    }

    [Fact]
    public void LowMood_IncreasesCrime()
    {
        var city = CreateCity(crime: 20, mood: 25);

        var result = _calculator.Calculate(city, CreateFoodFlow(100m, 10m), CreateConsumption());

        result.MoodPressure.Should().Be(2);
        result.EndingCrime.Should().BeGreaterThan(20);
    }

    [Fact]
    public void LowSecurity_IncreasesCrime()
    {
        var city = CreateCity(crime: 20, security: 35);

        var result = _calculator.Calculate(city, CreateFoodFlow(100m, 10m), CreateConsumption());

        result.SecurityPressure.Should().Be(2);
        result.EndingCrime.Should().BeGreaterThan(20);
    }

    [Fact]
    public void HighSecurity_ReducesCrime()
    {
        var city = CreateCity(crime: 20, security: 90, mood: 50, state: CityState.Stagnation);

        var result = _calculator.Calculate(city, CreateFoodFlow(100m, 10m), CreateConsumption());

        result.SecurityPressure.Should().Be(-3);
        result.EndingCrime.Should().BeLessThan(20);
    }

    [Fact]
    public void Collapse_IncreasesCrimeStrongly()
    {
        var city = CreateCity(crime: 20, state: CityState.Collapse);

        var result = _calculator.Calculate(city, CreateFoodFlow(100m, 10m), CreateConsumption());

        result.CityStatePressure.Should().Be(8);
        result.EndingCrime.Should().BeGreaterThan(20);
    }

    [Fact]
    public void WeeklyDelta_IsClamped()
    {
        var badCity = CreateCity(crime: 50, mood: 0, security: 0, state: CityState.Collapse);
        var badResult = _calculator.Calculate(badCity, CreateFoodFlow(0m, 10m), CreateConsumption(goodsShortage: 100m, requiredGoods: 100m, resourcesShortage: 100m));

        var goodCity = CreateCity(crime: 50, mood: 100, security: 100, state: CityState.Prosperous);
        var goodResult = _calculator.Calculate(goodCity, CreateFoodFlow(100m, 10m), CreateConsumption());

        badResult.ClampedDelta.Should().BeLessOrEqualTo(8);
        goodResult.ClampedDelta.Should().BeGreaterOrEqualTo(-5);
    }

    [Fact]
    public void AbandonedOrEmptyCity_DoesNotChangeCrime()
    {
        var abandonedCity = CreateCity(crime: 33, state: CityState.Abandoned);
        var abandonedResult = _calculator.Calculate(abandonedCity, CreateFoodFlow(0m, 10m), CreateConsumption(goodsShortage: 100m, requiredGoods: 100m, resourcesShortage: 100m));

        var emptyCity = CreateCity(crime: 44, population: 0, state: CityState.Stable);
        var emptyResult = _calculator.Calculate(emptyCity, CreateFoodFlow(0m, 10m), CreateConsumption(goodsShortage: 100m, requiredGoods: 100m, resourcesShortage: 100m));

        abandonedResult.Changed.Should().BeFalse();
        abandonedResult.EndingCrime.Should().Be(33);
        emptyResult.Changed.Should().BeFalse();
        emptyResult.EndingCrime.Should().Be(44);
    }

    private static City CreateCity(int crime = 20, int mood = 50, int security = 50, CityState state = CityState.Stagnation, int population = 1000) =>
        new("id", "name", population, 100m, 100m, mood, security, crime, 10m, 10m, state);

    private static DailyFoodFlowResult CreateFoodFlow(decimal endingFood, decimal consumption) => new()
    {
        StartingFood = endingFood,
        PopulationConsumption = consumption,
        FishingIncome = 0m,
        HuntingIncome = 0m,
        AgricultureIncome = 0m,
        MainlandSupplyIncome = 0m,
        EventDelta = 0m,
        TotalDelta = 0m,
        EndingFood = endingFood
    };

    private static HouseholdConsumptionResult CreateConsumption(decimal goodsShortage = 0m, decimal requiredGoods = 10m, decimal resourcesShortage = 0m) => new()
    {
        Population = 1000,
        GoodsConsumptionPerPerson = 0.05m,
        RequiredGoods = requiredGoods,
        GoodsAvailable = requiredGoods - goodsShortage,
        GoodsConsumed = requiredGoods - goodsShortage,
        GoodsShortage = goodsShortage,
        ResourcesConsumptionPerPerson = 0.03m,
        RequiredResources = 10m,
        ResourcesAvailable = 10m - resourcesShortage,
        ResourcesConsumed = 10m - resourcesShortage,
        ResourcesShortage = resourcesShortage
    };
}
