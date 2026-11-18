# API：EventBus

命名空间：`ReluctersteSDK.EventSystem`

```csharp
public sealed class EventBus : IEventBus
{
    public EventBus(IServiceProvider? serviceProvider = null, EventBusOptions? options = null);
    public EventBus(EventBusOptions options);
}
```

`EventBus` 是 `sealed` 的。0.8.x 的 `EventBusBase` 及其 `protected` 字段、`virtual` 扩展点已删除；自定义行为通过 `EventBusOptions.OnError` 与结果对象完成。

---

## 构造

| 构造 | 说明 |
|---|---|
| `new EventBus()` | 无 DI、默认选项 |
| `new EventBus(options)` | 无 DI、指定选项 |
| `new EventBus(provider)` | 使用 DI 容器创建类型处理器 |
| `new EventBus(provider, options)` | 同时指定 DI 与选项 |

构造时校验 `EventBusOptions.HandlerTimeout`；非法值抛 `ArgumentOutOfRangeException`。

---

## 订阅表与快照

- 内部是 `Dictionary<Type, Subscription[]>`。
- 写操作（订阅 / 退订 / Clear）在 `lock` 内复制整张字典后原子替换。
- 发布用 `Volatile.Read` 取整表快照，发布路径无锁。
- 发布期间新增的订阅不参与本轮；发布期间被 `Dispose` 的令牌仍可能在本轮执行。
- 每次订阅是独立记录，不按处理器类型/委托去重。

---

## 分发流程

`PublishAsync`：

1. 空事件抛 `ArgumentNullException`；取 `@event.GetType()` 作为运行时类型。
2. 按选项计算分发键：精确类型 → 基类链（`IncludeBaseTypes`，排除 `object` / `ValueType`）→ 事件接口（`IncludeInterfaces`，排除 `IEvent`）。
3. 从快照中取出所有匹配订阅，按 `Order` 升序、再按注册 `Sequence` 升序排序。
4. 若 `cancellationToken` 已取消：不调用处理器，返回 `IsCanceled = true`。
5. `Parallel`：按排序顺序启动全部处理器，`Task.WhenAll` 等待结束。
   `Sequential`：逐个 `await`；取消或超时后不再启动后续处理器。
6. 汇总 `SuccessCount` / `FailedCount` / `CanceledCount` / `TimedOutCount` 与错误列表。
7. 按稳定顺序调用 `OnError`；回调异常进入 `CallbackErrors`。
8. `ThrowOnError = true` 且存在处理器错误时抛 `EventDispatchException`；否则返回 `EventDispatchResult`。

### 基类分发

订阅 `BaseEvent` 的处理器能收到 `DerivedEvent`。调用时按**订阅时的泛型 `TEvent`** 强转，因此 `HandleAsync(BaseEvent)` 可以正常接收派生实例。

> 同一处理器如果同时订阅了基类和派生类型，发布派生事件时会**执行两次**。这是“每次订阅独立”的直接结果，不是 bug。

### 接口分发

默认关闭。开启后，`Subscribe<IPlayerEvent>` 可以收到所有实现该接口的事件；`IEvent` 本身始终排除。

---

## 错误处理

- 单个处理器异常不会阻止其他处理器；记录为 `EventHandlerError`。
- 处理器同步抛出、返回 `null` Task、DI 解析失败、类型不匹配，都记录为错误。
- `ThrowOnError = true`（默认）时，全部处理器结束后抛 `EventDispatchException`；`exception.Result` 保留完整结果。
- `OnError` 只做观察，不参与控制流；它抛出的异常进入 `Result.CallbackErrors`。

---

## 取消

| 场景 | 结果 |
|---|---|
| 发布前 token 已取消 | 不调用处理器；`IsCanceled = true`，`ExecutedCount = 0` |
| 处理器抛出的 `OperationCanceledException` 关联外部 token | 计为取消，不算错误 |
| 处理器用私有 token 抛 OCE，外部 token 未取消 | 计为处理器错误 |
| 顺序模式下取消 | 不再启动后续处理器 |
| 并行模式下取消 | 等待已启动的处理器结束；不提前返回 |

取消**不默认抛 `OperationCanceledException`**；调用方检查 `Result.IsCanceled`。取消与处理器错误同时存在时，`ThrowOnError = true` 仍抛聚合异常，`Result.IsCanceled` 保留取消状态。

---

## 超时

`EventBusOptions.HandlerTimeout`：

- 每个处理器创建 linked CTS，并在到期时通知处理器。
- 包装方法自己持有 DI Scope 与 CTS，在处理器任务结束后释放。
- 发布方用 `Task.WaitAsync(timeout, token)` 限时等待；超时后**不再等待**，但处理器可能继续运行。
- 超时记录为 `EventHandlerError.IsTimeout = true`，异常为 `TimeoutException`。
- 顺序模式下某个处理器超时后停止启动后续处理器。
- 处理器自己抛出的 `TimeoutException` 记为普通错误（`IsTimeout = false`）。

> 处理器如果**同步阻塞**（`Thread.Sleep`、锁等待、同步 IO），超时无法中断它；超时只对返回 Task 之后的等待生效。

---

## DI 与生命周期

类型处理器每次发布创建一个 `AsyncServiceScope`：

1. `provider.GetService(handlerType)`；
2. 失败则 `provider.GetServices(typeof(IEventHandler<TEvent>))`，找 `handlerType.IsInstanceOfType` 的实例；
3. 仍失败且 `EnableActivatorFallback = true` 时 `Activator.CreateInstance`；
4. 全部失败记录 `EventHandlerError`。

- DI 创建的实例由 Scope 释放。
- Activator 创建的 `IDisposable` / `IAsyncDisposable` 由总线释放。
- 实例订阅、委托订阅不会释放用户对象。
- EventBus 建议 Singleton。Scoped EventBus 为处理器创建的是子 Scope，看不到父 Scope 的 Scoped 服务。

---

## 线程安全与重入

- 订阅 / 退订 / 发布可并发调用；写操作串行化，读操作走快照。
- 处理器内部再次 `PublishAsync` 是安全的；没有递归深度保护。
- 处理器修改共享状态时需要自己同步。
- 发布路径默认不切换线程上下文（内部没有 `ConfigureAwait(false)` 的公开路径），顺序模式的后续处理器会回到调用方上下文；后台释放任务使用 `ConfigureAwait(false)`。

---

## 已知限制

- 无事件队列、延迟发布、重试、死信。
- 无弱引用订阅；忘记 `Dispose` 会一直持有处理器。
- 无同步阻塞 `Publish`；请 `await PublishAsync`。
- 无内置日志依赖；错误通过异常、结果与 `OnError` 暴露。
- 无中间件 / 过滤器；需要横切逻辑请写在处理器基类或 `OnError` 里。
