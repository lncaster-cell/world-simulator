namespace WorldSimulator.Core.Workforce;

public sealed class WorkforceReallocationSettings
{
    public int MinimumDailyReallocationWorkers { get; init; } = 2;
    public decimal MaximumDailyReallocationShare { get; init; } = 0.08m;

    public int CalculateDailyReallocationLimit(int totalWorkers)
    {
        if (totalWorkers <= 0)
        {
            return 0;
        }

        var shareLimit = (int)decimal.Ceiling(totalWorkers * Math.Clamp(MaximumDailyReallocationShare, 0m, 1m));
        return Math.Min(totalWorkers, Math.Max(MinimumDailyReallocationWorkers, shareLimit));
    }
}
