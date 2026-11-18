# API：配置节注册表与依赖注入

命名空间：`ReluctersteSDK.Config.Hosting`

---

## `ConfigRegistry`

```csharp
public sealed class ConfigRegistry
{
    public event EventHandler<ConfigChangedEventArgs>? Changed;

    public IReadOnlyCollection<string> SectionNames { get; }

    public void Register<T>(IConfigStore<T> store, string? sectionName = null)
        where T : class, IConfig, new();

    public bool TryGetStore<T>(out IConfigStore<T> store) where T : class, IConfig, new();
    public IConfigStore<T> GetStore<T>() where T : class, IConfig, new();

    public bool TryGetConfigType(string sectionName, out Type configType);
}
```

| 成员 | 说明 |
|---|---|
| `Register` | 注册配置节；类型重复或节名重复都会抛 `InvalidOperationException` |
| `SectionNames` | 已注册的节名快照 |
| `TryGetStore` / `GetStore` | 按类型取用；未注册时 `GetStore` 抛异常 |
| `TryGetConfigType` | 按节名反查配置类型 |
| `Changed` | 保存成功后触发 |

### 节名

`sectionName` 为空时使用 `typeof(T).Name`。节名在注册表内唯一，与类型一一对应。

### 事件语义

```csharp
registry.Changed += (_, e) =>
    Console.WriteLine($"{e.Section}: {e.Written} 条, {e.Mode}");
```

| 字段 | 说明 |
|---|---|
| `Section` | 配置节名 |
| `ConfigType` | 配置类型 |
| `Written` | 本次写入条数 |
| `Mode` | `SaveMode` |

**只在保存成功且 `Written > 0` 时触发。** 失败不会触发。

事件由注册表返回的包装存储触发。因此：

```csharp
IConfigStore<GameSetting> store = registry.GetStore<GameSetting>();
await store.SaveAsync(setting);      // ✅ 会通知

await rawStore.SaveAsync(setting);   // ❌ 绕过注册表，不会通知
```

### 线程

注册阶段在锁内完成，可安全并发调用 `Register`；注册完成后的读取无锁竞争。
事件在调用 `SaveAsync` 的线程上同步触发，处理器里不要做长时间阻塞。

---

## `ConfigChangedEventArgs`

```csharp
public sealed class ConfigChangedEventArgs : EventArgs
{
    public string Section { get; }
    public Type ConfigType { get; }
    public int Written { get; }
    public SaveMode Mode { get; }
}
```

---

## 依赖注入扩展

```csharp
public static IServiceCollection AddReluctersteSDKConfig(
    this IServiceCollection services,
    Action<ConfigRegistry>? configure = null);
```

```csharp
services.AddReluctersteSDKConfig(registry =>
{
    registry.Register(new SqliteConfigStore<GameSetting>(db), "game");
    registry.Register(new JsonFileConfigStore<AudioSetting>(audioPath, s => s.Key), "audio");
});
```

注册结果：

- `ConfigRegistry` 以**单例**注册。
- `configure` 在注册时立即执行，异常会直接中止启动（这是刻意的：配置注册失败属于启动错误）。

本模块只引用 `Microsoft.Extensions.DependencyInjection.Abstractions`，不包含容器实现。

---

## 已知限制

- **不跨进程**：`Changed` 只反映本进程内的写入，外部直接改配置文件不会触发。
- **不做热重载**：没有 `FileSystemWatcher`，需要重载请重新构造存储。
- **不替代 EventSystem**：需要跨模块/异步分发时，用 5 行代码桥接即可：

```csharp
registry.Changed += (_, e) => _ = eventBus.PublishAsync(new ConfigChangedEvent(e.Section));
```
