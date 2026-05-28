namespace WorldSimulator.Core.Trade;

internal static class TradeWealthPolicy
{
    public const decimal WealthPerUnitTransferred = 0.02m;
    public const decimal MaxWealthDeltaPerTransfer = 2m;

    public static decimal CalculateAffordableAmount(decimal importerWealth)
    {
        if (importerWealth <= 0m)
        {
            return 0m;
        }

        if (importerWealth >= MaxWealthDeltaPerTransfer)
        {
            return decimal.MaxValue;
        }

        return decimal.Round(
            importerWealth / WealthPerUnitTransferred,
            2,
            MidpointRounding.ToZero);
    }

    public static decimal CalculateWealthDelta(decimal amount, decimal importerWealth)
    {
        var rawDelta = decimal.Round(
            amount * WealthPerUnitTransferred,
            2,
            MidpointRounding.AwayFromZero);

        return decimal.Min(
            importerWealth,
            decimal.Min(MaxWealthDeltaPerTransfer, rawDelta));
    }
}
