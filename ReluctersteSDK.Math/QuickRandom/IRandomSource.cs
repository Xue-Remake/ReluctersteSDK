namespace ReluctersteSDK.Math.QuickRandom
{
    /// <summary>
    /// 可替换、可种子化的随机源契约。
    /// <para>实现应保证：同一初始状态产生同一序列；不要求线程安全。</para>
    /// </summary>
    public interface IRandomSource
    {
        /// <summary>生成下一个 32 位无符号随机数。</summary>
        uint NextUInt32();

        /// <summary>生成下一个 64 位无符号随机数。</summary>
        ulong NextUInt64();

        /// <summary>生成 [0, 1) 之间的双精度随机数。</summary>
        double NextDouble();

        /// <summary>生成 [minInclusive, maxExclusive) 之间的整数。</summary>
        /// <param name="minInclusive">包含的下界。</param>
        /// <param name="maxExclusive">不包含的上界。</param>
        int NextInt(int minInclusive, int maxExclusive);

        /// <summary>用随机字节填充缓冲区。</summary>
        /// <param name="buffer">目标缓冲区。</param>
        void Fill(Span<byte> buffer);
    }
}
