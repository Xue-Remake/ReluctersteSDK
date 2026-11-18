namespace ReluctersteSDK.Math.QuickRandom
{
    /// <summary>
    /// PCG32 随机源的内部状态快照，可用于保存与恢复随机序列。
    /// </summary>
    public readonly struct RandomState
    {
        /// <summary>当前内部状态。</summary>
        public ulong State { get; }

        /// <summary>流增量（始终为奇数）。</summary>
        public ulong Increment { get; }

        /// <summary>初始化一个状态快照。</summary>
        /// <param name="state">内部状态。</param>
        /// <param name="increment">流增量。</param>
        public RandomState(ulong state, ulong increment)
        {
            State = state;
            Increment = increment;
        }
    }
}
