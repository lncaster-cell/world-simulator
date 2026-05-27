using WorldSimulator.Core.Cities;

namespace WorldSimulator.Core.Resources;

public sealed class DailyWealthFlowCalculator
{
    public DailyWealthFlowResult Calculate(City city, DailyFoodFlowResult foodFlow, GoodsCraftingProductionResult goodsCrafting, HouseholdConsumptionResult householdConsumption)
    {
        ArgumentNullException.ThrowIfNull(city);
        ArgumentNullException.ThrowIfNull(foodFlow);
        ArgumentNullException.ThrowIfNull(goodsCrafting);
        ArgumentNullException.ThrowIfNull(householdConsumption);

        if (city.Population <= 0 || city.CityState == CityState.Abandoned)
        {
            return BuildRounded(city.Wealth, 0m, 0m, 0m, 0m, 0m, 0m, 0m, 0m, 0m);
        }

        var portTradeBonus = Math.Min(foodFlow.MainlandSupplyIncome * 0.03m, 2m);
        var goodsProductionBonus = Math.Min(goodsCrafting.GoodsProduced * 0.12m, 2m);
        var consumptionCoverageBonus = householdConsumption.HasAnyShortage ? 0m : 0.5m;

        var foodDays = foodFlow.PopulationConsumption <= 0m ? 999m : foodFlow.EndingFood / foodFlow.PopulationConsumption;
        var foodShortagePenalty = foodFlow.EndingFood <= 0m
            ? -5m
            : foodDays < 2m
                ? -3m
                : foodDays < 5m
                    ? -1m
                    : 0m;

        var goodsShortagePenalty = Math.Max(householdConsumption.GoodsShortage * -0.20m, -5m);
        var resourcesShortagePenalty = Math.Max(householdConsumption.ResourcesShortage * -0.25m, -3m);

        var securityModifierDelta = city.Security >= 70 ? 0.5m : city.Security >= 40 ? 0m : city.Security >= 20 ? -1m : -2m;
        var crimePenalty = city.Crime >= 70 ? -3m : city.Crime >= 50 ? -2m : city.Crime >= 30 ? -1m : 0m;
        var cityStateDelta = city.CityState switch
        {
            CityState.Prosperous => 1.5m,
            CityState.Stable => 0.75m,
            CityState.Stagnation => 0m,
            CityState.Recovery => 0.5m,
            CityState.EconomicDecline => -1m,
            CityState.FoodShortage => -1.5m,
            CityState.Famine => -4m,
            CityState.CrimeProblem => -2m,
            CityState.Unrest => -4m,
            CityState.Collapse => -8m,
            CityState.Abandoned => 0m,
            _ => 0m
        };

        return BuildRounded(city.Wealth, portTradeBonus, goodsProductionBonus, consumptionCoverageBonus, foodShortagePenalty, goodsShortagePenalty, resourcesShortagePenalty, securityModifierDelta, crimePenalty, cityStateDelta);
    }

    private static DailyWealthFlowResult BuildRounded(decimal startingWealth, decimal portTradeBonus, decimal goodsProductionBonus, decimal consumptionCoverageBonus, decimal foodShortagePenalty, decimal goodsShortagePenalty, decimal resourcesShortagePenalty, decimal securityModifierDelta, decimal crimePenalty, decimal cityStateDelta)
    {
        var roundedPortTradeBonus = decimal.Round(portTradeBonus, 2);
        var roundedGoodsProductionBonus = decimal.Round(goodsProductionBonus, 2);
        var roundedConsumptionCoverageBonus = decimal.Round(consumptionCoverageBonus, 2);
        var roundedFoodShortagePenalty = decimal.Round(foodShortagePenalty, 2);
        var roundedGoodsShortagePenalty = decimal.Round(goodsShortagePenalty, 2);
        var roundedResourcesShortagePenalty = decimal.Round(resourcesShortagePenalty, 2);
        var roundedSecurityModifierDelta = decimal.Round(securityModifierDelta, 2);
        var roundedCrimePenalty = decimal.Round(crimePenalty, 2);
        var roundedCityStateDelta = decimal.Round(cityStateDelta, 2);

        var totalDelta = roundedPortTradeBonus + roundedGoodsProductionBonus + roundedConsumptionCoverageBonus + roundedFoodShortagePenalty + roundedGoodsShortagePenalty + roundedResourcesShortagePenalty + roundedSecurityModifierDelta + roundedCrimePenalty + roundedCityStateDelta;
        var roundedTotalDelta = decimal.Round(Math.Clamp(totalDelta, -15m, 5m), 2);
        var endingWealth = decimal.Round(Math.Max(0m, startingWealth + roundedTotalDelta), 2);

        return new DailyWealthFlowResult
        {
            StartingWealth = decimal.Round(startingWealth, 2),
            PortTradeBonus = roundedPortTradeBonus,
            GoodsProductionBonus = roundedGoodsProductionBonus,
            ConsumptionCoverageBonus = roundedConsumptionCoverageBonus,
            FoodShortagePenalty = roundedFoodShortagePenalty,
            GoodsShortagePenalty = roundedGoodsShortagePenalty,
            ResourcesShortagePenalty = roundedResourcesShortagePenalty,
            SecurityModifierDelta = roundedSecurityModifierDelta,
            CrimePenalty = roundedCrimePenalty,
            CityStateDelta = roundedCityStateDelta,
            TotalDelta = roundedTotalDelta,
            EndingWealth = endingWealth
        };
    }
}
