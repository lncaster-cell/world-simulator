namespace WorldSimulator.Core.Resources;

public sealed class WeeklyCrimeFlowResult
{
    public required int StartingCrime { get; init; }

    public required int FoodPressure { get; init; }
    public required int GoodsShortagePressure { get; init; }
    public required int ResourcesShortagePressure { get; init; }
    public required int MoodPressure { get; init; }
    public required int SecurityPressure { get; init; }
    public required int CityStatePressure { get; init; }

    public required int MentalityPressure { get; init; }
    public required int LawPressure { get; init; }
    public required int GlobalEventsPressure { get; init; }

    public required int SecurityReduction { get; init; }
    public required int FutureOrderMeasuresReduction { get; init; }

    public required int RawDelta { get; init; }
    public required int ClampedDelta { get; init; }
    public required int EndingCrime { get; init; }

    public bool Changed => StartingCrime != EndingCrime;
}
