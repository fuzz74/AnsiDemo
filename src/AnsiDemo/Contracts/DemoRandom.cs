namespace AnsiDemo.Contracts;

/// <summary>Deterministic xorshift32 generator; a seed of 0 is replaced by 1.</summary>
public sealed class DemoRandom(uint seed)
{
    private uint _state = seed == 0 ? 1u : seed;

    public uint NextUInt()
    {
        uint x = _state;
        unchecked
        {
            x ^= x << 13;
            x ^= x >> 17;
            x ^= x << 5;
        }
        _state = x;
        return x;
    }

    /// <summary>Uniform in [0, 1).</summary>
    public double NextDouble() => NextUInt() / 4294967296.0;

    /// <summary>Uniform in [0, maxExclusive).</summary>
    public int Next(int maxExclusive)
    {
        if (maxExclusive < 0)
        {
            throw new ArgumentException($"maxExclusive must not be negative, got {maxExclusive}.", nameof(maxExclusive));
        }
        return (int)(NextDouble() * maxExclusive);
    }

    /// <summary>Stateless hash of a coordinate pair.</summary>
    public static uint Hash(int x, int y)
    {
        unchecked
        {
            uint h = (uint)x * 374761393u ^ (uint)y * 668265263u;
            h ^= h >> 13;
            h *= 1274126177u;
            h ^= h >> 16;
            return h;
        }
    }
}
