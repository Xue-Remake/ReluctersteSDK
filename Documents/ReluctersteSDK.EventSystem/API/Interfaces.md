# API：接口定义

命名空间：`ReluctersteSDK.EventSystem.Interfaces`

---

## `IEvent`

```csharp
public interface IEvent
{
    Guid EventId { get; }
    DateTime Timestamp { get; }
}
```

| 成员 | 语义 |
|---|---|
| `EventId` | 事件的唯一标识；同一实例多次读取必须返回相同值 |
| `Timestamp` | 事件创建时间；约定为 UTC |

两个属性都是**只读且必须稳定**。0.8.x 的默认接口成员每次读取都会生成新值，已在 0.9.0.0 移除。

需要省事时继承 `ReluctersteSDK.EventSystem.EventBase`：

```csharp
public abstract class EventBase : IEvent
{
    public Guid EventId { get; init; } = Guid.NewGuid();
    public DateTime Timestamp { get; init; } = DateTime.UtcNow;
}
```

`init` 访问器允许在对象初始化器中指定固定值，便于测试与重放：

```csharp
var evt = new PlayerDied { EventId = knownId, Timestamp = knownTime };
```

`EventBase` 是普通抽象类，不是 record；class 与 record 都可以继承。

---

## `IEventHandler<in TEvent>`

```csharp
public interface IEventHandler<in TEvent> where TEvent : IEvent
{
    Task HandleAsync(TEvent @event, CancellationToken cancellationToken = default);
}
```

- `in` 表示逆变：`IEventHandler<BaseEvent>` 可以当作 `IEventHandler<DerivedEvent>` 使用。
- 类型处理器由 DI 或 `Activator.CreateInstance` 创建；实例订阅与委托订阅直接使用调用方提供的对象。
- 处理器内部应尽量协作取消：把 `cancellationToken` 传给下游 IO / `Task.Delay`。

---

## `IEventBus`

```csharp
public interface IEventBus
{
    IDisposable Subscribe<TEvent, THandler>(int order = 0)
        where TEvent : IEvent where THandler : IEventHandler<TEvent>;

    IDisposable Subscribe<TEvent>(IEventHandler<TEvent> handler, int order = 0)
        where TEvent : IEvent;

    IDisposable Subscribe<TEvent>(Func<TEvent, CancellationToken, Task> handler, int order = 0)
        where TEvent : IEvent;

    IDisposable SubscribeAsync<TEvent>(Func<TEvent, Task> handler, int order = 0)
        where TEvent : IEvent;

    IDisposable SubscribeSync<TEvent>(Action<TEvent> handler, int order = 0)
        where TEvent : IEvent;

    IDisposable SubscribeFromTarget(object target, int order = 0);

    int Unsubscribe<TEvent, THandler>()
        where TEvent : IEvent where THandler : IEventHandler<TEvent>;

    int Unsubscribe<TEvent>(IEventHandler<TEvent> handler) where TEvent : IEvent;
    int Unsubscribe<TEvent>(Func<TEvent, CancellationToken, Task> handler) where TEvent : IEvent;
    int UnsubscribeAsync<TEvent>(Func<TEvent, Task> handler) where TEvent : IEvent;
    int UnsubscribeSync<TEvent>(Action<TEvent> handler) where TEvent : IEvent;

    int Clear<TEvent>() where TEvent : IEvent;
    void Clear();

    Task<EventDispatchResult> PublishAsync<TEvent>(TEvent @event, CancellationToken cancellationToken = default)
        where TEvent : IEvent;
}
```

### 订阅重载怎么选

| 场景 | 用法 |
|---|---|
| 处理器类，走 DI / 反射 | `Subscribe<TEvent, THandler>()` |
| 已有处理器实例 | `Subscribe<TEvent>(IEventHandler<TEvent>)` |
| 双参数异步回调 | `Subscribe<TEvent>(Func<TEvent, CancellationToken, Task>)` |
| 单参数异步回调 | `SubscribeAsync<TEvent>(Func<TEvent, Task>)` |
| 同步回调 | `SubscribeSync<TEvent>(Action<TEvent>)` |
| 特性扫描 | `SubscribeFromTarget(object)` |

**为什么不提供同名 `Action<TEvent>` 重载**：`async e => ...` 在 `Action` 与 `Func<..., Task>` 之间可能产生重载歧义，最坏会绑定成 `async void`，异常直接冲到进程，绕过错误聚合。同步/单参数异步使用不同方法名可以彻底避免。

`SubscribeSync` 会检查委托方法，遇到 `async void` 直接抛 `ArgumentException`。

### 退订语义

- `Subscribe` 返回的 `IDisposable`：**精确删除**本次订阅；释放幂等；重复订阅同一委托时两个令牌互不影响。
- `Unsubscribe`：删除该事件类型下**全部匹配项**，返回删除数量。
- `Clear<TEvent>()`：只删除精确键，不影响基类/接口键。
- `Clear()`：清空整张订阅表。

### 泛型参数与运行时类型

```csharp
bus.PublishAsync(new PlayerDied());               // TEvent 推断为 PlayerDied
bus.PublishAsync<GameEvent>(new PlayerDied());    // 显式指定基类
```

分发始终以 `@event.GetType()`（运行时类型）为起点；显式 `TEvent` 只是编译期约束，不会改变分发目标。

---

## 已知限制

- `IEventBus` 没有 `Count` / 枚举订阅的接口；需要统计时用 `EventDispatchResult.MatchedCount`。
- 没有非泛型 `Publish(object)`；发布必须使用编译期事件类型。
- 接口事件必须开启 `EventBusOptions.IncludeInterfaces` 才能订阅；`IEvent` 本身永远不能作为订阅键。
