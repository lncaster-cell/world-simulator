namespace WorldSimulator.Core.Events;

public sealed class CityEventGenerator
{
    public const double DailyEventChance = 0.2;
    public const int MaxActiveEventsForGeneration = 3;

    private readonly IRandomProvider _randomProvider;

    private static readonly WeightedPreset[] Presets =
    [
        new("fire", 15, CityEventPresets.CreateFire),
        new("disease", 20, CityEventPresets.CreateDisease),
        new("rat_infestation", 20, CityEventPresets.CreateRatInfestation),
        new("artists_performance", 25, CityEventPresets.CreateArtistsPerformance),
        new("port_storm", 20, CityEventPresets.CreatePortStorm)
    ];

    public CityEventGenerator(IRandomProvider randomProvider)
    {
        _randomProvider = randomProvider ?? throw new ArgumentNullException(nameof(randomProvider));
    }

    public CityEventGenerationResult TryGenerate(int currentDay, IReadOnlyCollection<CityEvent> activeEvents)
    {
        ArgumentNullException.ThrowIfNull(activeEvents);

        var activeIds = activeEvents.Select(e => e.Id).ToHashSet(StringComparer.Ordinal);
        var available = Presets.Where(p => !activeIds.Contains(p.Id)).ToArray();

        if (available.Length == 0)
        {
            return CityEventGenerationResult.NotGenerated(CityEventGenerationReason.NoAvailableEventTypes);
        }

        if (activeEvents.Count >= MaxActiveEventsForGeneration)
        {
            return CityEventGenerationResult.NotGenerated(CityEventGenerationReason.MaxActiveEventsReached);
        }

        if (_randomProvider.NextDouble() >= DailyEventChance)
        {
            return CityEventGenerationResult.NotGenerated(CityEventGenerationReason.ChanceMissed);
        }

        var totalWeight = available.Sum(x => x.Weight);
        var roll = _randomProvider.NextInt(totalWeight);

        foreach (var candidate in available)
        {
            if (roll < candidate.Weight)
            {
                return CityEventGenerationResult.Generated(candidate.Factory(currentDay));
            }

            roll -= candidate.Weight;
        }

        return CityEventGenerationResult.Generated(available[^1].Factory(currentDay));
    }

    private sealed record WeightedPreset(string Id, int Weight, Func<int, CityEvent> Factory);
}
