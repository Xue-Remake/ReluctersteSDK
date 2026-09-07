namespace ReluctersteSDK.EventSystem.Interfaces
{
    /// <summary>
    /// 事件标记接口（所有事件必须实现此接口）
    /// </summary>
    public interface IEvent
    {
        /// <summary>事件的唯一标识符</summary>
        Guid EventId => Guid.NewGuid();
        /// <summary>事件触发的 UTC 时间戳</summary>
        DateTime Timestamp => DateTime.UtcNow;
    }
}
