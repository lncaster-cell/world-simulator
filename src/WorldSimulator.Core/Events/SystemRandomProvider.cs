namespace WorldSimulator.Core.Events;

public sealed class SystemRandomProvider : IRandomProvider
{
    private readonly Random _random;

    public SystemRandomProvider()
        : this(new Random())
    {
    }

    public SystemRandomProvider(Random random)
    {
        _random = random ?? throw new ArgumentNullException(nameof(random));
    }

    public double NextDouble() => _random.NextDouble();

    public int NextInt(int maxExclusive) => _random.Next(maxExclusive);
}
