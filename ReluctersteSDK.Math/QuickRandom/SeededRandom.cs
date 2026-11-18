namespace ReluctersteSDK.Math.QuickRandom
{
    /// <summary>
    /// 基于 PCG32 算法的可种子化随机源。
    /// <para>同一 seed 在同一 .NET 版本及以后版本产生相同序列；非线程安全。</para>
    /// </summary>
    public sealed class SeededRandom : IRandomSource
    {
        private const ulong Multiplier = 6364136223846793005UL;
        private const ulong DefaultIncrement = 1442695040888963407UL;

        private ulong _state;
        private ulong _increment;

        /// <summary>使用 32 位种子创建随机源。</summary>
        /// <param name="seed">种子。</param>
        public SeededRandom(int seed)
            : this(unchecked((uint)seed), DefaultIncrement)
        {
        }

        /// <summary>使用 32 位种子创建随机源。</summary>
        /// <param name="seed">种子。</param>
        public SeededRandom(uint seed)
            : this(seed, DefaultIncrement)
        {
        }

        /// <summary>使用 64 位种子与流标识创建随机源。</summary>
        /// <param name="seed">种子。</param>
        /// <param name="stream">流标识；内部会强制转成奇数。</param>
        public SeededRandom(ulong seed, ulong stream)
        {
            _increment = (stream << 1) | 1UL;
            _state = 0UL;
            NextUInt32();
            _state += seed;
            NextUInt32();
        }

        /// <summary>生成下一个 32 位无符号随机数。</summary>
        public uint NextUInt32()
        {
            ulong oldState = _state;
            _state = oldState * Multiplier + _increment;

            uint xorShifted = (uint)(((oldState >> 18) ^ oldState) >> 27);
            int rotation = (int)(oldState >> 59);
            return (xorShifted >> rotation) | (xorShifted << ((-rotation) & 31));
        }

        /// <summary>生成下一个 64 位无符号随机数。</summary>
        public ulong NextUInt64()
        {
            ulong high = NextUInt32();
            return (high << 32) | NextUInt32();
        }

        /// <summary>生成 [0, 1) 之间的双精度随机数。</summary>
        public double NextDouble() => (NextUInt64() >> 11) * (1.0 / (1UL << 53));

        /// <summary>生成 [minInclusive, maxExclusive) 之间的整数。</summary>
        /// <param name="minInclusive">包含的下界。</param>
        /// <param name="maxExclusive">不包含的上界。</param>
        public int NextInt(int minInclusive, int maxExclusive)
        {
            if (maxExclusive <= minInclusive)
                throw new ArgumentOutOfRangeException(nameof(maxExclusive), "上界必须大于下界。");

            uint range = unchecked((uint)(maxExclusive - minInclusive));
            uint threshold = unchecked(0u - range) % range;

            while (true)
            {
                uint value = NextUInt32();
                if (value >= threshold)
                    return minInclusive + (int)(value % range);
            }
        }

        /// <summary>用随机字节填充缓冲区。</summary>
        /// <param name="buffer">目标缓冲区。</param>
        public void Fill(Span<byte> buffer)
        {
            uint word = 0;
            for (int i = 0; i < buffer.Length; i++)
            {
                if ((i & 3) == 0)
                    word = NextUInt32();

                buffer[i] = (byte)(word >> ((i & 3) * 8));
            }
        }

        /// <summary>生成 [0, 1) 之间的单精度随机数。</summary>
        public float NextFloat() => (float)NextDouble();

        /// <summary>以概率 <paramref name="probability"/> 返回 true。</summary>
        /// <param name="probability">返回 true 的概率，范围 [0, 1]。</param>
        public bool NextBool(double probability = 0.5)
        {
            if (probability < 0.0 || probability > 1.0)
                throw new ArgumentOutOfRangeException(nameof(probability), "概率必须在 [0, 1] 范围内。");

            return NextDouble() < probability;
        }

        /// <summary>派生一个独立但可复现的子随机源。</summary>
        public SeededRandom Fork()
            => new SeededRandom(NextUInt64(), NextUInt64() | 1UL);

        /// <summary>捕获当前状态。</summary>
        public RandomState CaptureState() => new RandomState(_state, _increment);

        /// <summary>恢复之前捕获的状态。</summary>
        /// <param name="state">状态快照。</param>
        public void RestoreState(RandomState state)
        {
            _state = state.State;
            _increment = state.Increment;
        }

        /// <summary>用新种子重置序列。</summary>
        /// <param name="seed">新种子。</param>
        public void Reseed(int seed)
        {
            _increment = (DefaultIncrement << 1) | 1UL;
            _state = 0UL;
            NextUInt32();
            _state += unchecked((uint)seed);
            NextUInt32();
        }
    }
}
