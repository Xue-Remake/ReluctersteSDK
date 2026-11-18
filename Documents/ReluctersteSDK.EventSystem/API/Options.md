# API：EventBusOptions

命名空间：`ReluctersteSDK.EventSystem`

```csharp
public sealed class EventBusOptions
{
    public bool IncludeBaseTypes { get; init; } = true;
    public bool IncludeInterfaces { get; init; }
    public EventExecutionMode ExecutionMode { get; init; } = EventExecutionMode.Parallel;
    public bool ThrowOnError { get; init; } = true;
    public TimeSpan? HandlerTimeout { get; init; }
    public bool EnableActivatorFallback { get; init; } = true;
    public Action<EventHandlerError>? OnError { get; init; }
}
```

所有属性都是 `init`，构造 `EventBus` 时复制使用；运行期不能修改。

| 选项 | 默认 | 语义 |
|---|---|---|
| `IncludeBaseTypes` | `true` | 派生事件分发给订阅了基类的处理器；关闭后只认精确类型 |
| `IncludeInterfaces` | `false` | 是否允许订阅事件接口并接收实现类事件；`IEvent` 始终排除 |
| `ExecutionMode` | `Parallel` | `Parallel` 并行启动后等待全部；`Sequential` 严格逐个等待 |
| `ThrowOnError` | `true` | 全部处理器结束后有错误时抛 `EventDispatchException` |
| `HandlerTimeout` | `null` | 单个处理器超时；`null` 表示不限时 |
| `EnableActivatorFallback` | `true` | DI 解析失败时是否用 `Activator.CreateInstance` 创建处理器 |
| `OnError` | `null` | 错误观察回调；全部处理器结束后按稳定顺序调用 |

## 校验

构造 `EventBus` 时调用内部 `Validate()`：

- `HandlerTimeout <= TimeSpan.Zero` → `ArgumentOutOfRangeException`。
- `HandlerTimeout.TotalMilliseconds > int.MaxValue` → `ArgumentOutOfRangeException`。

## 组合语义

| 组合 | 行为 |
|---|---|
| `IncludeBaseTypes = false` | 订阅抽象事件直接抛 `InvalidOperationException`；订阅具体基类仍然合法，但只接收精确发布 |
| `IncludeInterfaces = false` | 订阅事件接口直接抛 `InvalidOperationException`；特性扫描遇到接口参数也会失败 |
| `ExecutionMode = Sequential` + `HandlerTimeout` | 某个处理器超时后停止启动后续处理器，`IsTimedOut = true` |
| `ThrowOnError = false` | 错误只出现在 `EventDispatchResult.Errors`；`OnError` 仍会调用 |
| `EnableActivatorFallback = false` | 未在 DI 注册的处理器记录为 `EventHandlerError`，不会静默跳过 |
| `OnError` 自身抛异常 | 进入 `Result.CallbackErrors`，不覆盖原始错误，也不会递归回调 |

## 示例

```csharp
var options = new EventBusOptions
{
    IncludeBaseTypes = true,
    IncludeInterfaces = false,
    ExecutionMode = EventExecutionMode.Sequential,
    ThrowOnError = false,
    HandlerTimeout = TimeSpan.FromMilliseconds(500),
    OnError = error => logger.LogError(error.Exception, "{Handler} 失败", error.HandlerName),
};

var bus = new EventBus(serviceProvider, options);
```
