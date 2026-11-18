namespace ReluctersteSDK.EventSystem.Interfaces
{
    /// <summary>
    /// 事件总线接口：负责订阅、退订、发布与结果聚合。
    /// </summary>
    public interface IEventBus
    {
        /// <summary>订阅一个由依赖注入或反射创建的处理器类型。</summary>
        /// <typeparam name="TEvent">事件类型。</typeparam>
        /// <typeparam name="THandler">处理器类型。</typeparam>
        /// <param name="order">执行顺序；数值越小越先执行，相同值按订阅先后。</param>
        /// <returns>用于精确退订的令牌；释放后只移除本次订阅。</returns>
        IDisposable Subscribe<TEvent, THandler>(int order = 0)
            where TEvent : IEvent
            where THandler : IEventHandler<TEvent>;

        /// <summary>订阅一个已有的处理器实例；总线不会释放该实例。</summary>
        /// <typeparam name="TEvent">事件类型。</typeparam>
        /// <param name="handler">处理器实例。</param>
        /// <param name="order">执行顺序；数值越小越先执行。</param>
        /// <returns>用于精确退订的令牌。</returns>
        IDisposable Subscribe<TEvent>(IEventHandler<TEvent> handler, int order = 0)
            where TEvent : IEvent;

        /// <summary>订阅一个异步委托。</summary>
        /// <typeparam name="TEvent">事件类型。</typeparam>
        /// <param name="handler">异步回调。</param>
        /// <param name="order">执行顺序；数值越小越先执行。</param>
        /// <returns>用于精确退订的令牌。</returns>
        IDisposable Subscribe<TEvent>(Func<TEvent, CancellationToken, Task> handler, int order = 0)
            where TEvent : IEvent;

        /// <summary>订阅一个单参数异步委托。</summary>
        /// <typeparam name="TEvent">事件类型。</typeparam>
        /// <param name="handler">异步回调。</param>
        /// <param name="order">执行顺序；数值越小越先执行。</param>
        /// <returns>用于精确退订的令牌。</returns>
        IDisposable SubscribeAsync<TEvent>(Func<TEvent, Task> handler, int order = 0)
            where TEvent : IEvent;

        /// <summary>订阅一个同步委托。拒绝 <c>async void</c> 方法。</summary>
        /// <typeparam name="TEvent">事件类型。</typeparam>
        /// <param name="handler">同步回调。</param>
        /// <param name="order">执行顺序；数值越小越先执行。</param>
        /// <returns>用于精确退订的令牌。</returns>
        IDisposable SubscribeSync<TEvent>(Action<TEvent> handler, int order = 0)
            where TEvent : IEvent;

        /// <summary>扫描目标对象上带 <see cref="EventHandlerAttribute"/> 的方法并批量订阅。</summary>
        /// <param name="target">方法所属实例；不能是值类型。</param>
        /// <param name="order">特性未指定 Order 时使用的默认顺序。</param>
        /// <returns>组合令牌；释放后移除本次扫描注册的全部订阅。</returns>
        IDisposable SubscribeFromTarget(object target, int order = 0);

        /// <summary>移除指定事件类型下所有该处理器类型的订阅。</summary>
        /// <typeparam name="TEvent">事件类型。</typeparam>
        /// <typeparam name="THandler">处理器类型。</typeparam>
        /// <returns>实际移除的订阅数量。</returns>
        int Unsubscribe<TEvent, THandler>()
            where TEvent : IEvent
            where THandler : IEventHandler<TEvent>;

        /// <summary>移除指定事件类型下所有该实例的订阅。</summary>
        /// <typeparam name="TEvent">事件类型。</typeparam>
        /// <param name="handler">处理器实例。</param>
        /// <returns>实际移除的订阅数量。</returns>
        int Unsubscribe<TEvent>(IEventHandler<TEvent> handler)
            where TEvent : IEvent;

        /// <summary>移除指定事件类型下所有该委托的订阅。</summary>
        /// <typeparam name="TEvent">事件类型。</typeparam>
        /// <param name="handler">委托。</param>
        /// <returns>实际移除的订阅数量。</returns>
        int Unsubscribe<TEvent>(Func<TEvent, CancellationToken, Task> handler)
            where TEvent : IEvent;

        /// <summary>移除指定事件类型下所有该单参数异步委托的订阅。</summary>
        /// <typeparam name="TEvent">事件类型。</typeparam>
        /// <param name="handler">委托。</param>
        /// <returns>实际移除的订阅数量。</returns>
        int UnsubscribeAsync<TEvent>(Func<TEvent, Task> handler)
            where TEvent : IEvent;

        /// <summary>移除指定事件类型下所有该同步委托的订阅。</summary>
        /// <typeparam name="TEvent">事件类型。</typeparam>
        /// <param name="handler">委托。</param>
        /// <returns>实际移除的订阅数量。</returns>
        int UnsubscribeSync<TEvent>(Action<TEvent> handler)
            where TEvent : IEvent;

        /// <summary>清空指定事件类型的订阅；只影响精确键，不影响基类或接口键。</summary>
        /// <typeparam name="TEvent">事件类型。</typeparam>
        /// <returns>实际移除的订阅数量。</returns>
        int Clear<TEvent>()
            where TEvent : IEvent;

        /// <summary>清空全部订阅。</summary>
        void Clear();

        /// <summary>发布事件并等待所有匹配的处理器执行完毕。</summary>
        /// <typeparam name="TEvent">事件类型；分发以运行时类型为准。</typeparam>
        /// <param name="event">事件实例。</param>
        /// <param name="cancellationToken">取消令牌。</param>
        /// <returns>本次分发的统计结果。</returns>
        /// <exception cref="EventDispatchException">存在处理器错误且 <see cref="EventBusOptions.ThrowOnError"/> 为 <c>true</c>。</exception>
        Task<EventDispatchResult> PublishAsync<TEvent>(TEvent @event, CancellationToken cancellationToken = default)
            where TEvent : IEvent;
    }
}
