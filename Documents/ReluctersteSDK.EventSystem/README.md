# ReluctersteSDK.EventSystem

线程安全的异步事件总线：发布 / 订阅、基类分发、处理器顺序、错误聚合、取消与超时，以及基于特性的自动注册。

- 目录：`ReluctersteSDK.EventSystem/`
- 包 ID：`ReluctersteSDK.EventSystem`
- 命名空间：`ReluctersteSDK.EventSystem`（接口在 `.Interfaces`）
- 依赖：`Microsoft.Extensions.DependencyInjection`
- 版本：**0.13.0.0**（相对 0.8.x 是破坏性重写，见文末）

---

## 场景一：定义事件，订阅并发布

**目标**：玩家死亡时通知几个松散耦合的地方。

```csharp
using ReluctersteSDK.EventSystem;

public sealed class PlayerDied : EventBase
{
    public string PlayerId { get; init; } = "";
}

var bus = new EventBus();

using IDisposable token = bus.Subscribe<PlayerDied>(async (e, ct) =>
{
    Console.WriteLine($"玩家 {e.PlayerId} 死亡，事件 id = {e.EventId}");
    await Task.CompletedTask;
});

EventDispatchResult result = await bus.PublishAsync(new PlayerDied { PlayerId = "p1" });
Console.WriteLine($"匹配 {result.MatchedCount} 个处理器，成功 {result.SuccessCount} 个");
```

`EventBase` 在构造时生成一次 `EventId` 与 UTC `Timestamp`，同一个实例多次读取不会变化。

---

## 场景二：处理器类 + 依赖注入

**目标**：处理器需要 Scoped 的仓储 / 上下文。

```csharp
using Microsoft.Extensions.DependencyInjection;
using ReluctersteSDK.EventSystem;
using ReluctersteSDK.EventSystem.Interfaces;

public sealed class PlayerDiedHandler : IEventHandler<PlayerDied>
{
    private readonly IPlayerRepository _repository;

    public PlayerDiedHandler(IPlayerRepository repository) => _repository = repository;

    public async Task HandleAsync(PlayerDied @event, CancellationToken cancellationToken = default)
    {
        await _repository.MarkDeadAsync(@event.PlayerId, cancellationToken);
    }
}

var services = new ServiceCollection();
services.AddScoped<IPlayerRepository, PlayerRepository>();
services.AddScoped<PlayerDiedHandler>();
using ServiceProvider provider = services.BuildServiceProvider();

var bus = new EventBus(provider);
bus.Subscribe<PlayerDied, PlayerDiedHandler>();

await bus.PublishAsync(new PlayerDied { PlayerId = "p1" });
```

每个类型处理器在每次发布时创建**独立的 DI Scope**，Scope 活到处理器执行结束；Scoped 依赖在处理器内部是安全的。

解析顺序：具体处理器类型 → `IEventHandler<TEvent>` 注册中匹配的具体实例 → `Activator.CreateInstance` 回退（可用 `EnableActivatorFallback = false` 关闭）。

---

## 场景三：订阅基类事件

**目标**：所有 `GameEvent` 都做统一日志，不必为每个派生事件重复订阅。

```csharp
public class GameEvent : EventBase { }
public sealed class PlayerDied : GameEvent { }

var bus = new EventBus();          // 默认 IncludeBaseTypes = true
bus.Subscribe<GameEvent>((e, ct) =>
{
    Console.WriteLine($"收到 {e.GetType().Name}");
    return Task.CompletedTask;
});

await bus.PublishAsync(new PlayerDied());   // ✅ 基类处理器会被调用
```

接口分发默认关闭，避免意外命中过宽的订阅：

```csharp
var bus = new EventBus(new EventBusOptions { IncludeInterfaces = true });
bus.Subscribe<IPlayerEvent>(handler);       // 只有开启后才允许订阅事件接口
```

> `IEvent` 本身不能作为订阅键；`Subscribe<IEvent>` 会直接抛 `InvalidOperationException`。

---

## 场景四：精确退订

**目标**：组件销毁时只摘掉自己那一条订阅。

```csharp
IDisposable subscription = bus.Subscribe<PlayerDied>(handler);

subscription.Dispose();   // 幂等；重复 Dispose 不会误删别人的订阅
```

每次 `Subscribe` 都是**独立订阅**，不按处理器类型或委托去重。同一个委托订阅两次就会执行两次，两个令牌互不影响。

需要批量退订时用 `Unsubscribe`，返回实际删除数量：

```csharp
int removed = bus.Unsubscribe<PlayerDied>(handler);   // 删除全部匹配项
int cleared = bus.Clear<PlayerDied>();                // 只清精确键
bus.Clear();                                          // 清整张订阅表
```

---

## 场景五：错误处理

**目标**：一个处理器失败，不影响其他处理器；发布方必须能知道失败。

默认 `ThrowOnError = true`：全部处理器执行完后，如果有错误就抛 `EventDispatchException`。

```csharp
try
{
    await bus.PublishAsync(new PlayerDied());
}
catch (EventDispatchException ex)
{
    foreach (EventHandlerError error in ex.Errors)
        Console.WriteLine($"{error.HandlerName}: {error.Exception.Message}");
}
```

不想要异常时关闭：

```csharp
var bus = new EventBus(new EventBusOptions
{
    ThrowOnError = false,
    OnError = error => logger.LogError(error.Exception, "处理器 {Name} 失败", error.HandlerName),
});

EventDispatchResult result = await bus.PublishAsync(new PlayerDied());
if (!result.IsSuccess)
{
    // result.Errors / result.FailedCount / result.CallbackErrors
}
```

`OnError` 在全部处理器结束后按稳定顺序调用；回调自身抛出的异常会进入 `Result.CallbackErrors`，不会覆盖原始处理器错误。

---

## 场景六：顺序与并发

**目标**：某些处理器必须先执行。

```csharp
bus.Subscribe<PlayerDied>(auditHandler, order: -10);
bus.Subscribe<PlayerDied>(notificationHandler, order: 0);
bus.Subscribe<PlayerDied>(cleanupHandler, order: 10);
```

- `Order` 越小越先执行；相同 `Order` 按订阅先后。
- 默认 `ExecutionMode = Parallel`：按顺序**启动**，但不保证完成顺序。
- `Sequential` 严格逐个等待；一个处理器失败不会阻止后续处理器。

```csharp
var bus = new EventBus(new EventBusOptions
{
    ExecutionMode = EventExecutionMode.Sequential,
});
```

> `Parallel` 的 `Order` 只决定启动顺序。要严格顺序就选 `Sequential`。

---

## 场景七：取消与超时

**目标**：页面关闭或退出游戏时，别让慢处理器无限挂住发布方。

```csharp
using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(2));

EventDispatchResult result = await bus.PublishAsync(new PlayerDied(), cts.Token);
if (result.IsCanceled)
    Console.WriteLine($"取消了 {result.CanceledCount} 个处理器");
```

```csharp
var bus = new EventBus(new EventBusOptions
{
    HandlerTimeout = TimeSpan.FromMilliseconds(500),
    ThrowOnError = false,
});
```

- 预取消：不调用任何处理器，直接返回 `IsCanceled = true`。
- 中途取消：处理器收到取消令牌；顺序模式不再启动后续处理器。
- 超时：包装方法用 linked CTS 通知处理器，并让发布方不再等待；**不是强制终止**。处理器如果忽略取消令牌，仍可能在后台继续运行，Scope 会等它结束后才释放。
- 处理器自己抛 `TimeoutException` 会记为普通处理器错误，`EventHandlerError.IsTimeout` 为 `false`；只有总线超时才是 `true`。

---

## 场景八：特性扫描自动注册

**目标**：一个对象上写几个方法，不想逐个 `Subscribe`。

```csharp
public sealed class PlayerNotifications
{
    [EventHandler(Order = -10)]
    private void Audit(PlayerDied @event)
        => Console.WriteLine($"审计：{@event.PlayerId}");

    [EventHandler]
    private async Task Notify(PlayerDied @event, CancellationToken cancellationToken)
        => await notificationService.SendAsync(@event.PlayerId, cancellationToken);
}

using IDisposable token = bus.SubscribeFromTarget(new PlayerNotifications());
```

合法签名：

| 参数 | 返回 |
|---|---|
| 1 个 `IEvent` 派生参数 | `void` / `Task` / `ValueTask` |
| 1 个事件参数 + `CancellationToken` | `void` / `Task` / `ValueTask` |

扫描会拒绝：`async void`、泛型方法、静态方法、`ref/out/in/params`、多个事件参数、`IEvent` 本身、未开启接口/基类分发时的接口/抽象参数、`Task<T>` / `ValueTask<T>`，以及值类型 target。

**两阶段注册**：先收集并校验全部方法，全部通过后才真正注册；任何一个非法方法都会让整次扫描失败，不会留下半套订阅。

---

## 场景九：线程安全与重入

- 订阅表使用**写时复制快照**：发布路径无锁；发布期间新增的订阅不参与本轮，发布期间被 `Dispose` 的令牌仍可能在本轮执行。
- 多个线程可以同时 `Subscribe` / `Unsubscribe` / `PublishAsync`。
- 处理器内部再次 `PublishAsync` 是安全的（基于快照），但没有递归深度保护。
- 处理器修改共享状态时必须自己同步。

> `EventBus` 建议注册为 Singleton。如果 EventBus 本身是 Scoped，它为处理器创建的是**子 Scope**，看不到父 Scope 里的 Scoped 服务。

---

## 与 0.8.x 的破坏性变更

| 变更 | 说明 |
|---|---|
| `EventBusBase` 删除 | 改用 `EventBus`；不再有 `protected` 字段与 `virtual` 扩展点 |
| `IEvent` 需要稳定属性 | `EventId` / `Timestamp` 不再是默认接口成员；继承 `EventBase` 即可 |
| `PublishAsync` 返回 `EventDispatchResult` | 可以拿到匹配数、成功/失败/取消/超时计数与错误列表 |
| 默认分发包含基类 | 0.8.x 只认运行时精确类型 |
| 默认错误会抛异常 | 0.8.x 吞掉异常只打印一行；现在默认抛 `EventDispatchException` |
| `Subscribe` 返回 `IDisposable` | 令牌精确退订；重复订阅不再去重 |
| `Unsubscribe` 返回 `int` | 明确删除数量 |

---

## API 文档

| 文档 | 内容 |
|---|---|
| [API/Interfaces.md](API/Interfaces.md) | `IEvent`、`EventBase`、`IEventHandler`、`IEventBus` |
| [API/EventBus.md](API/EventBus.md) | 构造、订阅/退订、分发算法、错误、取消、超时、DI、线程安全 |
| [API/Options.md](API/Options.md) | `EventBusOptions` 全部选项、默认值与组合语义 |
| [API/Results.md](API/Results.md) | `EventDispatchResult`、`EventHandlerError`、`EventDispatchException` |
| [API/Attributes.md](API/Attributes.md) | `[EventHandler]` 与 `SubscribeFromTarget` 的扫描规则 |

## 已知限制

- 没有事件队列 / 延迟发布 / 重试 / 死信。
- 没有弱引用订阅；忘记 `Dispose` 会一直持有处理器。
- 超时是协作式的：不能强制终止忽略取消令牌的处理器。
- 没有内置日志依赖；错误通过 `EventDispatchException`、`EventDispatchResult` 与 `OnError` 暴露。
- 不提供同步阻塞的 `Publish`；请在异步上下文中 `await PublishAsync`。
