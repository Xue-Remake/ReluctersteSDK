namespace ReluctersteSDK.EventSystem.Interfaces
{
    /// <summary>
    /// 事件标记契约。所有事件都必须提供稳定的标识与创建时间。
    /// </summary>
    /// <remarks>
    /// 这两个成员在同一个事件实例的生命周期内必须保持不变。需要省事时直接继承
    /// <see cref="ReluctersteSDK.EventSystem.EventBase"/>，它会自动生成一次标识与时间戳。
    /// </remarks>
    public interface IEvent
    {
        /// <summary>事件的唯一标识；同一个实例多次读取必须返回相同值。</summary>
        Guid EventId { get; }

        /// <summary>事件创建时的 UTC 时间戳；同一个实例多次读取必须返回相同值。</summary>
        DateTime Timestamp { get; }
    }
}
