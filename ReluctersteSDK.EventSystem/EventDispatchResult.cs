namespace ReluctersteSDK.EventSystem
{
    /// <summary>
    /// 一次事件分发的统计结果。不可变，可安全地跨线程读取。
    /// </summary>
    public sealed class EventDispatchResult
    {
        internal EventDispatchResult(
            Guid eventId,
            Type eventType,
            int matchedCount,
            int executedCount,
            int successCount,
            int failedCount,
            int canceledCount,
            int timedOutCount,
            IReadOnlyList<EventHandlerError> errors,
            IReadOnlyList<Exception> callbackErrors,
            bool isCanceled,
            bool isTimedOut)
        {
            EventId = eventId;
            EventType = eventType;
            MatchedCount = matchedCount;
            ExecutedCount = executedCount;
            SuccessCount = successCount;
            FailedCount = failedCount;
            CanceledCount = canceledCount;
            TimedOutCount = timedOutCount;
            Errors = errors;
            CallbackErrors = callbackErrors;
            IsCanceled = isCanceled;
            IsTimedOut = isTimedOut;
        }

        /// <summary>发布事件的稳定标识。</summary>
        public Guid EventId { get; }

        /// <summary>发布事件的运行时类型。</summary>
        public Type EventType { get; }

        /// <summary>本次分发匹配到的订阅总数。</summary>
        public int MatchedCount { get; }

        /// <summary>实际开始执行的处理器数量；顺序模式下可能小于 <see cref="MatchedCount"/>。</summary>
        public int ExecutedCount { get; }

        /// <summary>成功完成的处理器数量。</summary>
        public int SuccessCount { get; }

        /// <summary>抛出异常的处理器数量。</summary>
        public int FailedCount { get; }

        /// <summary>因取消而提前结束的处理器数量。</summary>
        public int CanceledCount { get; }

        /// <summary>因超时被放弃等待的处理器数量。</summary>
        public int TimedOutCount { get; }

        /// <summary>处理器错误列表，按订阅顺序稳定排列。</summary>
        public IReadOnlyList<EventHandlerError> Errors { get; }

        /// <summary><see cref="EventBusOptions.OnError"/> 回调自身抛出的异常。</summary>
        public IReadOnlyList<Exception> CallbackErrors { get; }

        /// <summary>本次分发是否因取消而没有执行完。</summary>
        public bool IsCanceled { get; }

        /// <summary>本次分发是否出现处理器超时。</summary>
        public bool IsTimedOut { get; }

        /// <summary>没有处理器错误、没有取消、没有超时时为 <c>true</c>。</summary>
        public bool IsSuccess => FailedCount == 0 && !IsCanceled && !IsTimedOut;
    }
}
