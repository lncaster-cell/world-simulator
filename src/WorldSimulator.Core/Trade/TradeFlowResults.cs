namespace WorldSimulator.Core.Trade;

public sealed record TradeTransferResult(
    string ExporterCityId,
    string ImporterCityId,
    string CaravanId,
    TradeGoodType GoodType,
    decimal AmountTransferred,
    decimal ExporterWealthDelta,
    decimal ImporterWealthDelta);

public sealed record SettlementTradeFlowResult(
    string SettlementId,
    decimal FoodExported,
    decimal FoodImported,
    decimal GoodsExported,
    decimal GoodsImported,
    decimal ResourcesExported,
    decimal ResourcesImported,
    decimal WealthDelta);

public sealed record WorldTradeFlowResult(
    IReadOnlyList<TradeTransferResult> Transfers,
    IReadOnlyDictionary<string, SettlementTradeFlowResult> SettlementResults,
    decimal TotalFoodMoved,
    decimal TotalGoodsMoved,
    decimal TotalResourcesMoved,
    decimal TotalExporterWealthGain,
    decimal TotalImporterWealthCost);
