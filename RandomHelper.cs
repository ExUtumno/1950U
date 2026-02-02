static class RH
{
    public static T Random<T>(this T[] array, Random random) => array[random.Next(array.Length)];
    public static T Random<T>(this List<T> list, Random random) => list[random.Next(list.Count)];

    public static float Uniform(this Random random, float min, float max) => min + random.NextSingle() * (max - min);
    public static float MultUniform(this Random random, float min, float max)
    {
        if (min <= 0f || max <= 0f) throw new ArgumentOutOfRangeException(nameof(min), "range must be positive for multiplicative uniform distribution");
        if (min > max) throw new Exception("wrong order");
        return min * MathF.Pow(max / min, random.NextSingle());
    }

    /*public static int Random(this double[] weights, double r)
    {
        double sum = 0;
        for (int i = 0; i < weights.Length; i++) sum += weights[i];
        double threshold = r * sum;

        double partialSum = 0;
        for (int i = 0; i < weights.Length; i++)
        {
            partialSum += weights[i];
            if (partialSum >= threshold) return i;
        }
        return 0;
    }

    public static int ArgMin(this int[] array, Random random)
    {
        int min = 1000;
        for (int i = 0; i < array.Length; i++)
        {
            int a = array[i];
            if (a >= 0 && a < min) min = array[i];
        }

        var argmins = new List<int>();
        for (int i = 0; i < array.Length; i++) if (array[i] == min) argmins.Add(i);
        return argmins.Any() ? argmins.Random(random) : -1;
    }*/
}

#if false
class LCG
{
    private const ulong MULTIPLIER = 6364136223846793005UL;
    private ulong state;
    private ulong increment;

    public readonly struct Snapshot
    {
        public Snapshot(ulong state, ulong increment)
        {
            State = state;
            Increment = increment;
        }

        public ulong State { get; }
        public ulong Increment { get; }
    }

    public LCG() : this((ulong)Environment.TickCount64) { }
    public LCG(uint seed) : this((ulong)seed) { }

    public LCG(ulong seed, ulong stream = 0x9E3779B97F4A7C15UL)
    {
        increment = (stream << 1) | 1UL;
        Initialize(seed);
    }

    public LCG(LCG other)
    {
        state = other.state;
        increment = other.increment;
    }

    private void Initialize(ulong seed)
    {
        state = 0UL;
        increment |= 1UL;
        NextUInt();
        state += seed;
        NextUInt();
    }

    public Snapshot GetState() => new(state, increment);

    public void SetState(Snapshot snapshot)
    {
        state = snapshot.State;
        increment = snapshot.Increment | 1UL;
    }

    public void Reseed(ulong seed, ulong stream = 0x9E3779B97F4A7C15UL)
    {
        increment = (stream << 1) | 1UL;
        Initialize(seed);
    }

    private static uint RotateRight(uint value, int rot)
    {
        return (value >> rot) | (value << ((-rot) & 31));
    }

    public uint NextUInt()
    {
        ulong old = state;
        state = unchecked(old * MULTIPLIER + increment);
        uint xorshifted = (uint)(((old >> 18) ^ old) >> 27);
        int rot = (int)(old >> 59);
        return RotateRight(xorshifted, rot);
    }

    public ulong NextULong() => ((ulong)NextUInt() << 32) | NextUInt();

    public int Next() => (int)(NextUInt() & 0x7FFFFFFF);

    public int Next(int maxValue)
    {
        if (maxValue <= 0) throw new ArgumentOutOfRangeException(nameof(maxValue), "MaxValue must be positive");
        return Next(0, maxValue);
    }

    public int Next(int minValue, int maxValue)
    {
        if (minValue >= maxValue) throw new ArgumentOutOfRangeException(nameof(minValue), "MinValue must be less than MaxValue");
        uint range = (uint)(maxValue - minValue);
        uint threshold = (uint)(-range) % range;
        while (true)
        {
            uint r = NextUInt();
            if (r >= threshold) return (int)(minValue + (r % range));
        }
    }

    public double NextDouble() => (NextULong() >> 11) * (1.0 / (1UL << 53));

    public float NextSingle() => (NextUInt() >> 8) * (1f / (1 << 24));

    public bool NextBool() => (NextUInt() & 1u) == 0u;

    public float Uniform(float min, float max)
    {
        if (min > max) throw new ArgumentOutOfRangeException(nameof(min), "Min cannot exceed Max");
        return min + NextSingle() * (max - min);
    }
}
#endif
