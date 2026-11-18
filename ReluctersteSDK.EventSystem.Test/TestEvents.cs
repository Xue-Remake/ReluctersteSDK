using ReluctersteSDK.EventSystem.Interfaces;

namespace ReluctersteSDK.EventSystem.Test;

/// <summary>所有测试事件与处理器共用的基础类型。</summary>
public class BaseTestEvent : EventBase
{
    public int Value { get; init; }
}

/// <summary>一级派生事件。</summary>
public class DerivedTestEvent : BaseTestEvent
{
}

/// <summary>二级派生事件，用于多级基类链测试。</summary>
public sealed class LeafTestEvent : DerivedTestEvent
{
}

/// <summary>与 BaseTestEvent 无继承关系的独立事件。</summary>
public sealed class OtherTestEvent : EventBase
{
}

/// <summary>嵌套发布使用的事件。</summary>
public sealed class InnerTestEvent : EventBase
{
}

/// <summary>抽象事件，用于分发开关测试。</summary>
public abstract class AbstractTestEvent : EventBase
{
}

/// <summary>事件接口，用于接口分发测试。</summary>
public interface IMarkedTestEvent : IEvent
{
}

/// <summary>实现 IMarkedTestEvent 的具体事件。</summary>
public sealed class MarkedTestEvent : EventBase, IMarkedTestEvent
{
}

/// <summary>值类型事件，验证 struct 事件也能分发。</summary>
public readonly struct StructTestEvent : IEvent
{
    public Guid EventId { get; init; }
    public DateTime Timestamp { get; init; }
}

/// <summary>无操作处理器，供类型订阅测试使用。</summary>
public sealed class NoOpHandler : IEventHandler<OtherTestEvent>
{
    public Task HandleAsync(OtherTestEvent @event, CancellationToken cancellationToken = default)
        => Task.CompletedTask;
}
