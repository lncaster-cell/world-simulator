using WorldSimulator.Core.Cities;

namespace WorldSimulator.Core.Trade;

internal static class TradeInventoryPolicy
{
    public static decimal GetStock(City city, TradeGoodType good)
    {
        return good switch
        {
            TradeGoodType.Food => city.Food,
            TradeGoodType.Goods => city.Goods,
            _ => city.Resources
        };
    }

    public static void AddStock(City city, TradeGoodType good, decimal amount)
    {
        if (good == TradeGoodType.Food)
        {
            city.Food += amount;
        }
        else if (good == TradeGoodType.Goods)
        {
            city.Goods += amount;
        }
        else
        {
            city.Resources += amount;
        }
    }

    public static void RemoveStock(City city, TradeGoodType good, decimal amount)
    {
        if (good == TradeGoodType.Food)
        {
            city.Food -= amount;
        }
        else if (good == TradeGoodType.Goods)
        {
            city.Goods -= amount;
        }
        else
        {
            city.Resources -= amount;
        }
    }
}
