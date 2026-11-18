using ReluctersteSDK.EventSystem.Interfaces;

namespace ReluctersteSDK.EventSystem
{
    /// <summary>
    /// 事件基类：在构造时生成一次稳定的 <see cref="EventId"/> 与 <see cref="Timestamp"/>。
    /// </summary>
    /// <remarks>
    /// 使用 <c>init</c> 访问器，测试或重放场景可以在对象初始化器中指定固定值，
    /// 例如 <c>new PlayerDied { EventId = knownId, Timestamp = knownTime }</c>。
    /// </remarks>
    public abstract class EventBase : IEvent
    {
        /// <inheritdoc />
        public Guid EventId { get; init; } = Guid.NewGuid();

        /// <inheritdoc />
        public DateTime Timestamp { get; init; } = DateTime.UtcNow;
    }
}
