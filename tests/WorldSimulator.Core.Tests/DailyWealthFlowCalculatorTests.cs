using WorldSimulator.Core.Cities;
using WorldSimulator.Core.Resources;
using Xunit;

namespace WorldSimulator.Core.Tests;

public sealed class DailyWealthFlowCalculatorTests
{
    private readonly DailyWealthFlowCalculator _calculator = new();

    [Fact]
    public void StableCityWithProduction_GainsWealth() { var r=Calc(); Assert.True(r.TotalDelta>0m); Assert.True(r.EndingWealth>r.StartingWealth);}    

    [Fact]
    public void GoodsShortage_ReducesWealth()
    {
        var noShort = Calc();
        var shortRes = Calc(goodsShortage: 10m);
        Assert.True(shortRes.GoodsShortagePenalty < 0m);
        Assert.True(shortRes.TotalDelta < noShort.TotalDelta);
    }

    [Fact]
    public void ResourcesShortage_ReducesWealth() => Assert.True(Calc(resourcesShortage: 5m).ResourcesShortagePenalty < 0m);

    [Fact]
    public void FoodShortage_ReducesWealth() => Assert.True(Calc(endingFood: 50m, populationConsumption: 84m).FoodShortagePenalty < 0m);

    [Fact]
    public void HighCrime_ReducesWealth() => Assert.Equal(-3m, Calc(crime: 70).CrimePenalty);

    [Fact]
    public void Prosperous_GetsBonus() => Assert.Equal(1.5m, Calc(state: CityState.Prosperous).CityStateDelta);

    [Fact]
    public void Collapse_StrongPenalty() { var r=Calc(state: CityState.Collapse); Assert.Equal(-8m, r.CityStateDelta); Assert.True(r.TotalDelta<0m);}    

    [Fact]
    public void AbandonedOrEmpty_NoChange()
    {
        var a = Calc(population: 0);
        Assert.Equal(0m, a.TotalDelta);
        Assert.Equal(a.StartingWealth, a.EndingWealth);
        var b = Calc(state: CityState.Abandoned);
        Assert.Equal(0m, b.TotalDelta);
        Assert.Equal(b.StartingWealth, b.EndingWealth);
    }

    [Fact]
    public void TotalDelta_Clamped()
    {
        Assert.True(Calc(state: CityState.Collapse, crime: 100, security: 0, endingFood: 0m, goodsShortage: 100m, resourcesShortage: 100m).TotalDelta >= -15m);
        Assert.True(Calc(state: CityState.Prosperous, crime: 0, security: 90, mainlandSupply: 500m, goodsProduced: 500m).TotalDelta <= 5m);
    }

    private DailyWealthFlowResult Calc(int population=420, decimal wealth=320m, int security=60, int crime=30, CityState state=CityState.Stable, decimal mainlandSupply=36m, decimal endingFood=500m, decimal populationConsumption=84m, decimal goodsProduced=8m, decimal goodsShortage=0m, decimal resourcesShortage=0m)
    {
        var city = new City("id","city",population,1000m,wealth,50,security,crime,100m,50m,state);
        return _calculator.Calculate(city,
            new DailyFoodFlowResult{StartingFood=1000m,PopulationConsumption=populationConsumption,FishingIncome=0m,HuntingIncome=0m,MainlandSupplyIncome=mainlandSupply,EventDelta=0m,TotalDelta=0m,EndingFood=endingFood},
            new GoodsCraftingProductionResult{NaturalPotential=0m,RequiredWorkers=0,AssignedWorkers=0,WorkerCoverage=0m,ExtraWorkers=0,OverstaffBonus=0m,MoodModifier=1m,SecurityModifier=1m,StateModifier=1m,ResourceCostPerGoods=1m,PotentialGoodsOutput=0m,ResourcesNeeded=0m,ResourcesAvailable=0m,ResourcesConsumed=0m,GoodsProduced=goodsProduced},
            new HouseholdConsumptionResult{GoodsShortage=goodsShortage,ResourcesShortage=resourcesShortage,HasAnyShortage=goodsShortage>0m||resourcesShortage>0m});
    }
}
