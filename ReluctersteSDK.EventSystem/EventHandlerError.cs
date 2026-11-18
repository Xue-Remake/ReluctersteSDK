namespace ReluctersteSDK.EventSystem
{
    /// <summary>
    /// 单个处理器在一次分发中的失败记录。
    /// </summary>
    public sealed class EventHandlerError
    {
        /// <summary>创建一个处理器错误记录。</summary>
        /// <param name="eventType">发布事件的运行时类型。</param>
        /// <param name="subscribedEventType">订阅时使用的事件类型。</param>
        /// <param name="handlerType">处理器类型；委托订阅时为 <c>null</c>。</param>
        /// <param name="handlerName">处理器或委托的可读名称。</param>
        /// <param name="isTimeout">是否由总线超时导致。</param>
        /// <param name="exception">原始异常；超时记录中为 <see cref="TimeoutException"/>。</param>
        public EventHandlerError(
            Type eventType,
            Type subscribedEventType,
            Type? handlerType,
            string handlerName,
            bool isTimeout,
            Exception exception)
        {
            EventType = eventType ?? throw new ArgumentNullException(nameof(eventType));
            SubscribedEventType = subscribedEventType ?? throw new ArgumentNullException(nameof(subscribedEventType));
            HandlerType = handlerType;
            HandlerName = handlerName ?? throw new ArgumentNullException(nameof(handlerName));
            IsTimeout = isTimeout;
            Exception = exception ?? throw new ArgumentNullException(nameof(exception));
        }

        /// <summary>发布事件的运行时类型。</summary>
        public Type EventType { get; }

        /// <summary>订阅时使用的事件类型；基类分发时可能与 <see cref="EventType"/> 不同。</summary>
        public Type SubscribedEventType { get; }

        /// <summary>处理器类型；委托订阅时为 <c>null</c>。</summary>
        public Type? HandlerType { get; }

        /// <summary>处理器或委托的可读名称，用于日志定位。</summary>
        public string HandlerName { get; }

        /// <summary>是否由总线超时导致。</summary>
        public bool IsTimeout { get; }

        /// <summary>原始异常；超时记录中为 <see cref="TimeoutException"/>。</summary>
        public Exception Exception { get; }

        /// <inheritdoc />
        public override string ToString()
            => $"{HandlerName}: {(IsTimeout ? "超时" : Exception.GetType().Name)} - {Exception.Message}";
    }
}
