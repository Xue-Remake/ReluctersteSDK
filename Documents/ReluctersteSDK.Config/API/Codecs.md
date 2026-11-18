# API：编解码器

命名空间：`ReluctersteSDK.Config.Codecs`

---

## `IConfigCodec<T>`

```csharp
public interface IConfigCodec<T> where T : class, IConfig
{
    string TypeName { get; }
    object? GetKey(T value);
    void Write(Utf8JsonWriter writer, T value);
    T Read(JsonElement element);
}
```

| 成员 | 说明 |
|---|---|
| `TypeName` | 稳定的类型名，用于诊断与多态配置 |
| `GetKey` | 主键；替代构造时传入的 `keySelector` |
| `Write` | 把单个配置写进 writer（由调用方负责数组边界） |
| `Read` | 从 JSON 元素还原配置；失败应抛 `JsonException` |

实现方可自行决定存储格式，`JsonFileConfigStore<T>` 只负责遍历与落盘。

---

## `JsonConfigCodec<T>`

```csharp
public sealed class JsonConfigCodec<T> : IConfigCodec<T>
{
    public JsonConfigCodec(
        JsonTypeInfo<T> typeInfo,
        Func<T, object?> keySelector,
        string? typeName = null);

    public static JsonConfigCodec<T> FromContext(
        JsonSerializerContext context,
        Func<T, object?> keySelector,
        string? typeName = null);
}
```

### 源生成用法

```csharp
[JsonSerializable(typeof(GameSetting))]
[JsonSourceGenerationOptions(
    WriteIndented = true,
    PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase)]
internal partial class GameConfigJson : JsonSerializerContext
{
}

var codec = JsonConfigCodec<GameSetting>.FromContext(GameConfigJson.Default, s => s.Key);
var store = new JsonFileConfigStore<GameSetting>(path, codec);
```

注意：源生成上下文类型**及其所有包含类型都必须声明为 `partial`**，否则会报 `SYSLIB1032` / `CS0534`。

### 异常

| 场景 | 行为 |
|---|---|
| `FromContext` 的上下文没有该类型元数据 | 抛 `InvalidOperationException`，**不会**静默退回反射 |
| 反序列化结果为 `null` | 抛 `JsonException` |
| `writer` 或 `value` 为 null | 抛 `ArgumentNullException` |

### 与 `JsonSerializerOptions` 路径的取舍

| | `JsonSerializerOptions` | `IConfigCodec<T>` |
|---|---|---|
| 元数据 | 运行时反射 | 编译期生成 |
| 裁剪 / AOT | 需要额外配置 | 原生支持 |
| 类型白名单 | 无 | 只有上下文里声明过的类型 |
| 上手成本 | 低 | 需要写上下文声明 |

两条路径共用同一个 `JsonFileConfigStore<T>`，可以按配置类型分别选择。
