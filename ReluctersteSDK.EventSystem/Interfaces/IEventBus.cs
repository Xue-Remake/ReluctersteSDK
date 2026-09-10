namespace ReluctersteSDK.EventSystem.Interfaces
{
    /// <summary>
    /// 事件总线接口
    /// </summary>
    public interface IEventBus
    {
        /// <summary>基于类型的订阅</summary>
        void Subscribe<TEvent, THandler>()
            where TEvent : IEvent
            where THandler : IEventHandler<TEvent>;
        /// <summary>取消类型订阅</summary>
        void Unsubscribe<TEvent, THandler>()
            where TEvent : IEvent
            where THandler : IEventHandler<TEvent>;
        /// <summary>基于委托的即时订阅</summary>
        void Subscribe<TEvent>(Func<TEvent, CancellationToken, Task> handler)
            where TEvent : IEvent;
        /// <summary>取消基于委托的即时订阅</summary>
        void Unsubscribe<TEvent>(Func<TEvent, CancellationToken, Task> handler)
            where TEvent : IEvent;
        /// <summary>发布事件</summary>
        Task PublishAsync<TEvent>(TEvent @event, CancellationToken cancellationToken = default)
            where TEvent : IEvent;
    }
}
