namespace ReluctersteSDK.EventSystem
{
    /// <summary>
    /// 处理器执行模式。
    /// </summary>
    public enum EventExecutionMode
    {
        /// <summary>并行启动全部处理器，等待全部结束。顺序只保证启动顺序。</summary>
        Parallel = 0,

        /// <summary>按顺序逐个等待处理器结束；一个失败不会阻止后续处理器。</summary>
        Sequential = 1,
    }
}
