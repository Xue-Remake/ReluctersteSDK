# API：结果与异常模型

命名空间：`ReluctersteSDK.EventSystem`

---

## `EventDispatchResult`

一次发布的完整统计结果，不可变。

| 属性 | 语义 |
|---|---|
| `EventId` | 发布事件的稳定标识 |
| `EventType` | 发布事件的运行时类型 |
| `MatchedCount` | 本次分发匹配到的订阅总数 |
| `ExecutedCount` | 实际开始执行的处理器数量；顺序模式下可能小于 `MatchedCount` |
| `SuccessCount` | 成功完成的处理器数量 |
| `FailedCount` | 抛出异常的处理器数量 |
| `CanceledCount` | 因取消提前结束的处理器数量 |
| `TimedOutCount` | 因超时被放弃等待的处理器数量 |
| `Errors` | `EventHandlerError` 列表，按订阅顺序稳定排列 |
| `CallbackErrors` | `OnError` 回调自身抛出的异常 |
| `IsCanceled` | 是否因取消没有执行完 |
| `IsTimedOut` | 是否出现处理器超时 |
| `IsSuccess` | `FailedCount == 0 && !IsCanceled && !IsTimedOut` |

`IsSuccess` **不**受 `CallbackErrors` 影响：日志回调失败不等于事件分发失败。

---

## `EventHandlerError`

| 属性 | 语义 |
|---|---|
| `EventType` | 发布事件的运行时类型 |
| `SubscribedEventType` | 订阅时使用的事件类型；基类分发时可能与 `EventType` 不同 |
| `HandlerType` | 处理器类型；委托订阅时为 `null` |
| `HandlerName` | 处理器或委托的可读名称 |
| `IsTimeout` | 是否由总线超时导致 |
| `Exception` | 原始异常；超时记录中为 `TimeoutException` |

```csharp
foreach (EventHandlerError error in result.Errors)
{
    Console.WriteLine($"{error.HandlerName} ({error.SubscribedEventType.Name})"
        + $"{(error.IsTimeout ? " 超时" : "")}: {error.Exception.Message}");
}
```

---

## `EventDispatchException`

`ThrowOnError = true` 且存在处理器错误时抛出。

| 成员 | 语义 |
|---|---|
| `Result` | 完整分发结果 |
| `Errors` | 等价于 `Result.Errors` |
| `InnerException` | `AggregateException`，包含全部处理器异常 |
| `Message` | `事件 X 分发失败：N 个处理器出错。` |

```csharp
try
{
    await bus.PublishAsync(evt);
}
catch (EventDispatchException ex)
{
    foreach (EventHandlerError error in ex.Errors)
        logger.LogError(error.Exception, "处理器 {Name} 失败", error.HandlerName);
}
```

取消本身**不**抛 `OperationCanceledException`；只有处理器错误会触发 `EventDispatchException`。取消与错误同时存在时，异常仍会抛出，`Result.IsCanceled` 保留取消状态。

---

## 超时记录

总线超时产生的错误：

```csharp
new EventHandlerError(
    eventType,
    subscribedEventType,
    handlerType,
    handlerName,
    isTimeout: true,
    new TimeoutException("处理器 X 在 00:00:00.5000000 内未完成。"));
```

处理器自己抛出的 `TimeoutException` 记录为 `IsTimeout = false`，属于普通处理器错误。
