# API：控制台事件日志

命名空间：`ReluctersteSDK.Game`

| 类型 | 说明 |
|---|---|
| `EventLogger` | 静态类：带时间戳与彩色标签的控制台输出 |
| `LogEventAttribute` | 标记特性：给事件类型绑定一个日志标签 |

**与 ECS 无关**，是本模块里唯一不依赖 `World` 的代码。适合调试期观察事件流，不适合生产日志。

---

## `LogEventAttribute`

```csharp
[AttributeUsage(AttributeTargets.Field | AttributeTargets.Class | AttributeTargets.Struct,
                Inherited = false)]
public sealed class LogEventAttribute : Attribute
{
    public string Tag { get; }
    public LogEventAttribute(string tag = "EVENT");
}
```

标在**事件类 / 结构体 / 字段**上。标签就是输出时方括号里的内容。

> `Inherited = false`：派生类**不会**继承基类的标签，`Emit` 读的是精确类型。

---

## `EventLogger`

### `Emit<T>(T eventData, string message)`

```csharp
public static void Emit<T>(T eventData, string message)
```

1. 读 `typeof(T)` 上的 `LogEventAttribute`；
2. **没有该特性时什么都不做**（静默忽略）；
3. 有的话按 `[HH:mm:ss.fff] [Tag] message` 格式输出，标签部分用青色。

```csharp
[LogEvent("COMBAT")]
public class EnemyKilledEvent
{
    public string EnemyId { get; init; } = "";
}

EventLogger.Emit(new EnemyKilledEvent(), "僵尸被杀");
// [12:34:56.789] [COMBAT] 僵尸被杀
```

> 注意是 `typeof(T)`，即**编译期推断的静态类型**。把派生类实例赋给基类变量再 `Emit`，读的是基类的特性。

### `LogMarked(string tag, string message, ConsoleColor color = ConsoleColor.Yellow)`

```csharp
public static void LogMarked(string tag, string message, ConsoleColor color = ConsoleColor.Yellow)
```

不需要任何类型，直接输出指定标签与颜色。

```csharp
EventLogger.LogMarked("SAVE", "存档写入完成", ConsoleColor.Green);
// [12:34:56.789] [SAVE] 存档写入完成
```

---

## 实现方式

就是 `Console.ForegroundColor` + `Console.Write` + `Console.ResetColor()` + `Console.WriteLine`。

- 时间戳用 `DateTime.Now`（本地时间），格式 `HH:mm:ss.fff`；
- 颜色只作用于标签与方括号，消息正文用默认色；
- 没有锁、没有缓冲、没有分级。

---

## 已知限制

- **只适合调试**：直接写 `Console`，被重定向、被 UI 接管时不可用。
- **没有日志级别**：不能按 `Debug` / `Info` / `Error` 过滤。
- **`Emit` 对无特性类型完全静默**：调试时"为什么没输出"最常见的原因就是忘了标 `[LogEvent]`。
- **`Attribute.GetCustomAttribute` 每次调用都走反射**：热路径（每帧上千条）会明显拖慢。
- **不是线程安全的**：多线程同时改 `Console.ForegroundColor` 会让颜色串位。
- **`DateTime.Now` 有系统时间跳变风险**：不适合做性能计时，只适合"发生在什么时候"。
