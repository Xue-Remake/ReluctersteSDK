namespace ReluctersteSDK.EventSystem
{
    /// <summary>
    /// 事件总线选项。所有属性都是 <c>init</c>，构造 <see cref="EventBus"/> 时复制使用。
    /// </summary>
    public sealed class EventBusOptions
    {
        /// <summary>
        /// 是否把派生事件分发给订阅了基类事件的处理器。默认 <c>true</c>。
        /// </summary>
        public bool IncludeBaseTypes { get; init; } = true;

        /// <summary>
        /// 是否把事件分发给订阅了其事件接口的处理器。默认 <c>false</c>；
        /// 开启后 <see cref="Interfaces.IEvent"/> 本身仍会被排除。
        /// </summary>
        public bool IncludeInterfaces { get; init; }

        /// <summary>处理器执行模式。默认并行。</summary>
        public EventExecutionMode ExecutionMode { get; init; } = EventExecutionMode.Parallel;

        /// <summary>
        /// 存在处理器错误时是否在全部处理器执行完后抛出 <see cref="EventDispatchException"/>。
        /// 默认 <c>true</c>；设为 <c>false</c> 时只能通过 <see cref="EventDispatchResult"/> 检查失败。
        /// </summary>
        public bool ThrowOnError { get; init; } = true;

        /// <summary>
        /// 单个处理器的超时时间。为 <c>null</c> 时不限时。
        /// 超时表示“发布方不再等待”，不是强制终止处理器；处理器应协作取消。
        /// </summary>
        public TimeSpan? HandlerTimeout { get; init; }

        /// <summary>
        /// 类型处理器无法从 DI 解析时，是否回退到 <see cref="Activator.CreateInstance(Type)"/>。
        /// 默认 <c>true</c>。回退实例若实现 <see cref="IDisposable"/> 或 <see cref="IAsyncDisposable"/>，由总线释放。
        /// </summary>
        public bool EnableActivatorFallback { get; init; } = true;

        /// <summary>
        /// 错误回调。全部处理器结束后按稳定顺序调用；回调自身抛出的异常会收集到
        /// <see cref="EventDispatchResult.CallbackErrors"/>，不会覆盖原始错误。
        /// </summary>
        public Action<EventHandlerError>? OnError { get; init; }

        /// <summary>校验选项组合；非法时抛出异常。</summary>
        /// <exception cref="ArgumentOutOfRangeException"><see cref="HandlerTimeout"/> 不是正数或超出定时器上限。</exception>
        internal void Validate()
        {
            if (HandlerTimeout is not { } timeout)
                return;

            if (timeout <= TimeSpan.Zero)
                throw new ArgumentOutOfRangeException(nameof(HandlerTimeout), timeout, "处理器超时时间必须大于零。");

            if (timeout.TotalMilliseconds > int.MaxValue)
                throw new ArgumentOutOfRangeException(nameof(HandlerTimeout), timeout, "处理器超时时间不能超过 int.MaxValue 毫秒。");
        }
    }
}
