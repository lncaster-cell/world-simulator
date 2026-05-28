using WorldSimulator.Core.Cities;

namespace WorldSimulator.Core.Trade;

internal static class TradeDemandPolicy
{
    private const decimal WeeklyFoodDaysReserve = 8m;
    private const decimal WeeklyFoodDaysTarget = 12m;
    private const decimal WeeklyGoodsPerCapitaReserve = 0.15m;
    private const decimal WeeklyGoodsPerCapitaTarget = 0.3m;
    private const decimal WeeklyResourcesPerCapitaReserve = 0.2m;
    private const decimal WeeklyResourcesPerCapitaTarget = 0.4m;

    public static decimal GetReserve(City city, TradeGoodType good)
    {
        return good switch
        {
            TradeGoodType.Food => decimal.Round(
                city.CalculateDailyFoodConsumption() * WeeklyFoodDaysReserve,
                2,
                MidpointRounding.AwayFromZero),
            TradeGoodType.Goods => decimal.Round(
                city.Population * WeeklyGoodsPerCapitaReserve,
                2,
                MidpointRounding.AwayFromZero),
            _ => decimal.Round(
                city.Population * WeeklyResourcesPerCapitaReserve,
                2,
                MidpointRounding.AwayFromZero)
        };
    }

    public static decimal GetTarget(City city, TradeGoodType good)
    {
        return good switch
        {
            TradeGoodType.Food => decimal.Round(
                city.CalculateDailyFoodConsumption() * WeeklyFoodDaysTarget,
                2,
                MidpointRounding.AwayFromZero),
            TradeGoodType.Goods => decimal.Round(
                city.Population * WeeklyGoodsPerCapitaTarget,
                2,
                MidpointRounding.AwayFromZero),
            _ => decimal.Round(
                city.Population * WeeklyResourcesPerCapitaTarget,
                2,
                MidpointRounding.AwayFromZero)
        };
    }

    public static decimal CalculateSurplus(City city, TradeGoodType good)
    {
        return decimal.Max(
            0m,
            TradeInventoryPolicy.GetStock(city, good) - GetReserve(city, good));
    }

    public static decimal CalculateDeficit(City city, TradeGoodType good)
    {
        return decimal.Max(
            0m,
            GetTarget(city, good) - TradeInventoryPolicy.GetStock(city, good));
    }
}
