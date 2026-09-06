using System;
using System.Collections.Generic;
using System.Text;

namespace ReluctersteSDK.EventSystem.Interfaces
{
    /// <summary>
    /// 事件处理器接口
    /// </summary>
    public interface IEventHandler<in TEvent> where TEvent : IEvent
    {
        Task HandleAsync(TEvent @event, CancellationToken cancellationToken = default);
    }
}
