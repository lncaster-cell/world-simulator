namespace WorldSimulator.Core.Events;

public interface IRandomProvider
{
    double NextDouble();

    int NextInt(int maxExclusive);
}
