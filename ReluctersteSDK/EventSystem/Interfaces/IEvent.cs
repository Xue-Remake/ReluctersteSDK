using System;
using System.Collections.Generic;
using System.Data;
using System.Text;

namespace ReluctersteSDK.EventSystem.Interfaces
{
    /// <summary>
    /// 事件标记接口（所有事件必须实现此接口）
    /// </summary>
    public interface IEvent
    {
        Guid EventId => Guid.NewGuid();
        DateTime Timestamp => DateTime.UtcNow;
    }
}
