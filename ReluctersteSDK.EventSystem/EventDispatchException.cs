namespace ReluctersteSDK.EventSystem
{
    /// <summary>
    /// 分发结束后存在处理器错误时抛出的聚合异常。
    /// </summary>
    public sealed class EventDispatchException : Exception
    {
        /// <summary>用一次分发的完整结果创建异常。</summary>
        /// <param name="result">分发结果。</param>
        /// <exception cref="ArgumentNullException"><paramref name="result"/> 为 <c>null</c>。</exception>
        public EventDispatchException(EventDispatchResult result)
            : base(BuildMessage(result), BuildAggregate(result))
        {
            Result = result;
        }

        /// <summary>本次分发的完整结果，包含所有处理器错误与计数。</summary>
        public EventDispatchResult Result { get; }

        /// <summary>处理器错误列表，等价于 <see cref="Result"/> 的 <see cref="EventDispatchResult.Errors"/>。</summary>
        public IReadOnlyList<EventHandlerError> Errors => Result.Errors;

        private static string BuildMessage(EventDispatchResult result)
        {
            ArgumentNullException.ThrowIfNull(result);
            return $"事件 {result.EventType.Name} 分发失败：{result.Errors.Count} 个处理器出错。";
        }

        private static AggregateException BuildAggregate(EventDispatchResult result)
        {
            ArgumentNullException.ThrowIfNull(result);
            return new AggregateException(result.Errors.Select(error => error.Exception));
        }
    }
}
