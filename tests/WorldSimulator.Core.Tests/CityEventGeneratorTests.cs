using WorldSimulator.Core.Events;
using Xunit;

namespace WorldSimulator.Core.Tests;

public sealed class CityEventGeneratorTests
{
    [Fact]
    public void DoesNotGenerate_WhenChanceRollMisses()
    {
        var generator = new CityEventGenerator(new FakeRandomProvider(nextDouble: 0.5, nextInt: 0));

        var result = generator.TryGenerate(currentDay: 4, activeEvents: Array.Empty<CityEvent>());

        Assert.False(result.WasGenerated);
        Assert.Null(result.Event);
        Assert.Equal(CityEventGenerationReason.ChanceMissed, result.Reason);
    }

    [Fact]
    public void GeneratesEvent_WhenChanceRollSucceeds()
    {
        var generator = new CityEventGenerator(new FakeRandomProvider(nextDouble: 0.05, nextInt: 0));

        var result = generator.TryGenerate(currentDay: 4, activeEvents: Array.Empty<CityEvent>());

        Assert.True(result.WasGenerated);
        Assert.NotNull(result.Event);
        Assert.Equal(CityEventGenerationReason.Generated, result.Reason);
    }

    [Fact]
    public void DoesNotGenerate_WhenActiveEventsCountReachedMax()
    {
        var generator = new CityEventGenerator(new FakeRandomProvider(nextDouble: 0.0, nextInt: 0));
        var active = new[]
        {
            CityEventPresets.CreateFire(1),
            CityEventPresets.CreateDisease(1),
            CityEventPresets.CreateRatInfestation(1)
        };

        var result = generator.TryGenerate(currentDay: 4, activeEvents: active);

        Assert.False(result.WasGenerated);
        Assert.Equal(CityEventGenerationReason.MaxActiveEventsReached, result.Reason);
    }

    [Fact]
    public void DoesNotGenerateDuplicateActiveEventId()
    {
        var generator = new CityEventGenerator(new FakeRandomProvider(nextDouble: 0.0, nextInt: 0));
        var active = new[] { CityEventPresets.CreateFire(1) };

        var result = generator.TryGenerate(currentDay: 4, activeEvents: active);

        Assert.True(result.WasGenerated);
        Assert.NotNull(result.Event);
        Assert.NotEqual("fire", result.Event!.Id);
    }

    [Fact]
    public void DoesNotGenerate_WhenAllEventTypesAreActive()
    {
        var generator = new CityEventGenerator(new FakeRandomProvider(nextDouble: 0.0, nextInt: 0));
        var active = new[]
        {
            CityEventPresets.CreateFire(1),
            CityEventPresets.CreateDisease(1),
            CityEventPresets.CreateRatInfestation(1),
            CityEventPresets.CreateArtistsPerformance(1),
            CityEventPresets.CreatePortStorm(1)
        };

        var result = generator.TryGenerate(currentDay: 4, activeEvents: active);

        Assert.False(result.WasGenerated);
        Assert.Equal(CityEventGenerationReason.NoAvailableEventTypes, result.Reason);
    }

    [Fact]
    public void GeneratedEvent_HasStartedDayEqualToCurrentDay()
    {
        var generator = new CityEventGenerator(new FakeRandomProvider(nextDouble: 0.0, nextInt: 0));

        var result = generator.TryGenerate(currentDay: 7, activeEvents: Array.Empty<CityEvent>());

        Assert.NotNull(result.Event);
        Assert.Equal(7, result.Event!.StartedDay);
    }

    [Fact]
    public void WeightedSelection_CanGenerateExpectedPresetUsingFakeRandom()
    {
        var generator = new CityEventGenerator(new FakeRandomProvider(nextDouble: 0.0, nextInt: 95));

        var result = generator.TryGenerate(currentDay: 3, activeEvents: Array.Empty<CityEvent>());

        Assert.NotNull(result.Event);
        Assert.Equal("port_storm", result.Event!.Id);
    }

    [Fact]
    public void MaxOneEventPerCall()
    {
        var generator = new CityEventGenerator(new FakeRandomProvider(nextDouble: 0.0, nextInt: 30));

        var result = generator.TryGenerate(currentDay: 2, activeEvents: Array.Empty<CityEvent>());

        Assert.True(result.WasGenerated);
        Assert.NotNull(result.Event);
    }

    private sealed class FakeRandomProvider : IRandomProvider
    {
        private readonly double _nextDouble;
        private readonly int _nextInt;

        public FakeRandomProvider(double nextDouble, int nextInt)
        {
            _nextDouble = nextDouble;
            _nextInt = nextInt;
        }

        public double NextDouble() => _nextDouble;

        public int NextInt(int maxExclusive)
        {
            if (_nextInt >= maxExclusive)
            {
                throw new ArgumentOutOfRangeException(nameof(maxExclusive));
            }

            return _nextInt;
        }
    }
}
