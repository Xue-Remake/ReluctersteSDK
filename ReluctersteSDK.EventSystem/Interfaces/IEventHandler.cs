namespace ReluctersteSDK.EventSystem.Interfaces
{
    /// <summary>
    /// 事件处理器接口。实现类通过依赖注入或公共无参构造函数创建。
    /// </summary>
    /// <typeparam name="TEvent">处理器关注的事件类型。</typeparam>
    /// <remarks>
    /// 泛型参数是逆变的：<c>IEventHandler&lt;BaseEvent&gt;</c> 可以当作
    /// <c>IEventHandler&lt;DerivedEvent&gt;</c> 使用。
    /// </remarks>
    public interface IEventHandler<in TEvent> where TEvent : IEvent
    {
        /// <summary>处理事件。</summary>
        /// <param name="event">事件实例。</param>
        /// <param name="cancellationToken">发布方传入的取消令牌；处理器应尽量协作取消。</param>
        /// <returns>表示处理完成的异步任务。</returns>
        Task HandleAsync(TEvent @event, CancellationToken cancellationToken = default);
    }
}
