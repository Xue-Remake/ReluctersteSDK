namespace ReluctersteSDK.Math.QuickRandom
{
    /// <summary>
    /// 为旧版静态随机 API 提供线程静态默认随机源。
    /// </summary>
    public static class RandomSource
    {
        [ThreadStatic]
        private static SeededRandom? _shared;

        /// <summary>获取当前线程的默认随机源。</summary>
        public static IRandomSource Shared
            => _shared ??= new SeededRandom(System.Random.Shared.Next());

        /// <summary>用指定种子重置当前线程的默认随机源。</summary>
        /// <param name="seed">新种子。</param>
        public static void Reseed(int seed)
            => _shared = new SeededRandom(seed);
    }
}
