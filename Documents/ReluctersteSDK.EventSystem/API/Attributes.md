# API：EventHandlerAttribute 与 SubscribeFromTarget

命名空间：`ReluctersteSDK.EventSystem`

---

## `EventHandlerAttribute`

```csharp
[AttributeUsage(AttributeTargets.Method, AllowMultiple = false, Inherited = true)]
public sealed class EventHandlerAttribute : Attribute
{
    public int Order { get; set; } = int.MinValue;
}
```

| 属性 | 语义 |
|---|---|
| `Order` | 执行顺序；越小越先。`int.MinValue` 表示未指定，使用 `SubscribeFromTarget` 的 `order` 参数 |

`Order` 使用 `set` 而不是 `init`，保证特性命名参数 `[EventHandler(Order = -10)]` 在所有编译器版本下都能绑定。

---

## `SubscribeFromTarget`

```csharp
IDisposable SubscribeFromTarget(object target, int order = 0);
```

扫描 `target.GetType()` 上所有带 `[EventHandler]` 的方法，批量注册并返回组合令牌。

```csharp
using IDisposable token = bus.SubscribeFromTarget(new PlayerNotifications(), order: 0);
```

- `target` 为 `null` → `ArgumentNullException`。
- `target` 是值类型 → `ArgumentException`（方法内修改无法回写原值）。
- 没有匹配方法 → 返回 no-op 令牌，不报错。
- 组合令牌释放幂等，逐个释放内部订阅。

---

## 合法签名

| 参数 | 返回 |
|---|---|
| 1 个 `IEvent` 派生参数 | `void` |
| 1 个 `IEvent` 派生参数 | `Task` |
| 1 个 `IEvent` 派生参数 | `ValueTask` |
| 1 个事件参数 + `CancellationToken` | `void` / `Task` / `ValueTask` |

```csharp
[EventHandler]
private void Handle(PlayerDied @event) { }

[EventHandler]
private async Task Handle(PlayerDied @event, CancellationToken cancellationToken)
    => await repository.SaveAsync(@event, cancellationToken);

[EventHandler]
private ValueTask Handle(PlayerDied @event, CancellationToken cancellationToken)
    => cache.PutAsync(@event, cancellationToken);
```

---

## 非法签名

以下情况在扫描阶段直接抛 `InvalidOperationException`，**不会**注册任何订阅：

| 情况 | 原因 |
|---|---|
| `async void` | 异常无法被总线捕获 |
| 泛型方法 | 无法确定事件类型 |
| 静态方法 | `SubscribeFromTarget` 绑定实例 |
| `ref` / `out` / `in` / `params` | `MethodInfo.Invoke` 无法安全传参 |
| 参数数量 <1 或 >2 | 无法匹配事件/令牌 |
| 第一个参数不实现 `IEvent` | 不是事件 |
| 第一个参数是 `IEvent` 本身 | 标记接口不是分发键 |
| 接口参数 + `IncludeInterfaces=false` | 永远收不到事件 |
| 抽象参数 + `IncludeBaseTypes=false` | 永远收不到事件 |
| `Task<T>` / `ValueTask<T>` | 返回值会被忽略，容易误用 |
| 第二个参数不是 `CancellationToken` | 参数语义不明确 |

---

## 继承、重载与隐藏

- 方法按元数据令牌排序，注册顺序稳定。
- `override` 方法没有重新标注特性时，`inherit: true` 会找到基类方法上的特性。
- `new` 隐藏方法会同时返回基类与派生方法；两者都带特性时会注册两次。需要唯一行为时请显式改写方法名。
- 基类的 **private** 方法不会被 `GetMethods` 返回；只支持当前类型的 private 方法，以及继承的 public/protected/protected internal 方法。

---

## 执行与异常

- 同步方法通过 `MethodInfo.Invoke` 调用；`TargetInvocationException` 会解包成原始异常。
- 异步方法的异常由返回的 Task 携带，由总线统一聚合。
- 方法抛出的异常记录为 `EventHandlerError`，`HandlerType` 为目标对象类型，`HandlerName` 为 `类型名.方法名`。
- 组合令牌释放后，所有方法订阅一并移除。
