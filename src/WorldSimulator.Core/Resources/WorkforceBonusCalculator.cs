namespace WorldSimulator.Core.Resources;

public static class WorkforceBonusCalculator
{
    public static decimal CalculateOverstaffBonus(
        decimal naturalPotential,
        decimal overstaffBonusCap,
        int extraWorkers,
        int requiredWorkers)
    {
        if (extraWorkers <= 0 || requiredWorkers <= 0)
        {
            return 0m;
        }

        var overstaffRatio = (decimal)extraWorkers / requiredWorkers;
        return naturalPotential * overstaffBonusCap * (1m - (1m / (1m + overstaffRatio)));
    }
}
