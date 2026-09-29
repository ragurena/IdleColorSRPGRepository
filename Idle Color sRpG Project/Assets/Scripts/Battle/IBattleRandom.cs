using System;

public interface IBattleRandom
{
    int NextInt(int minInclusive, int maxExclusive);
    double NextDouble();
}

public sealed class SystemBattleRandom : IBattleRandom
{
    readonly Random _random;

    public SystemBattleRandom()
    {
        _random = new Random();
    }

    public SystemBattleRandom(int seed)
    {
        _random = new Random(seed);
    }

    public int NextInt(int minInclusive, int maxExclusive)
    {
        if (maxExclusive <= minInclusive)
            return minInclusive;
        return _random.Next(minInclusive, maxExclusive);
    }

    public double NextDouble()
    {
        return _random.NextDouble();
    }
}
