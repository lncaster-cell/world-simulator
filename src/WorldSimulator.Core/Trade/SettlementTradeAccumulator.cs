namespace WorldSimulator.Core.Trade;

internal sealed class SettlementTradeAccumulator
{
    public decimal FoodExported { get; private set; }

    public decimal FoodImported { get; private set; }

    public decimal GoodsExported { get; private set; }

    public decimal GoodsImported { get; private set; }

    public decimal ResourcesExported { get; private set; }

    public decimal ResourcesImported { get; private set; }

    public decimal WealthDelta { get; private set; }

    public void RegisterExport(TradeGoodType good, decimal amount, decimal wealthDelta)
    {
        WealthDelta += wealthDelta;

        if (good == TradeGoodType.Food)
        {
            FoodExported += amount;
        }
        else if (good == TradeGoodType.Goods)
        {
            GoodsExported += amount;
        }
        else
        {
            ResourcesExported += amount;
        }
    }

    public void RegisterImport(TradeGoodType good, decimal amount, decimal wealthDelta)
    {
        WealthDelta += wealthDelta;

        if (good == TradeGoodType.Food)
        {
            FoodImported += amount;
        }
        else if (good == TradeGoodType.Goods)
        {
            GoodsImported += amount;
        }
        else
        {
            ResourcesImported += amount;
        }
    }

    public SettlementTradeFlowResult ToResult(string settlementId)
    {
        return new SettlementTradeFlowResult(
            settlementId,
            FoodExported,
            FoodImported,
            GoodsExported,
            GoodsImported,
            ResourcesExported,
            ResourcesImported,
            WealthDelta);
    }
}
