# API：JSON 序列化

命名空间：`ReluctersteSDK.Registry.Json`

支持 `[JsonConverter]` 特性的只有两个类型：

| 类型 | 转换器 |
|---|---|
| `ValueRegistry<TKey,TValue>` | `ValueRegistryConverter<TKey,TValue>`（经 `ValueRegistryConverterFactory` 匹配） |
| `CompositeValueRegistry<TValue>` | `CompositeValueRegistryConverter<TValue>`（经 `CompositeValueRegistryConverterFactory` 匹配） |
| `CompositeKey` | `CompositeKeyConverter` |

**`TypeRegistry` / `InstanceRegistry` / `DelegateRegistry` 没有转换器**——它们持有 `Type`、`object`、`Delegate`，本身就不是可序列化的数据。

---

## 直接序列化

特性已经挂在类型上，不需要任何配置：

```csharp
var reg = new ValueRegistry<string, int>();
reg.Register("hp", 100);
reg.Register("mp", 50);
reg.Disable("mp");

string json = JsonSerializer.Serialize(reg);
```

```json
[
  { "key": "hp", "value": 100, "enabled": true },
  { "key": "mp", "value": 50, "enabled": false }
]
```

反序列化时自动调用 `Disable` 还原启用状态：

```csharp
ValueRegistry<string, int>? back = JsonSerializer.Deserialize<ValueRegistry<string, int>>(json);
back.IsEnabled("mp");   // false
back.GetValue("hp");    // 100
```

---

## 复合键的形状

`CompositeKey` 序列化为一个 JSON 数组，条目形如：

```json
[
  { "key": [2026, "season", "autumn"], "value": "秋季规则集", "enabled": true },
  { "key": ["player", 1],              "value": "第一个玩家", "enabled": false }
]
```

属性名大小写都接受（`key` / `Key`、`value` / `Value`、`enabled` / `Enabled`）。缺少 `key` 字段会抛 `JsonException`；未知属性被跳过。

---

## `RegistryJson` 选项助手

需要把转换器统一装到自己的 `JsonSerializerOptions` 上时（比如和 DataStructures 的转换器一起用）：

| 成员 | 说明 |
|---|---|
| `RegistryJson.Default` | 预置了本模块全部转换器的静态选项（缩进） |
| `RegistryJson.CreateOptions(bool writeIndented = true)` | 新建一套选项 |
| `options.UseRegistry()` | 往已有选项上叠加，**幂等**（重复调用不会重复添加） |

```csharp
using ReluctersteSDK.Registry.Json;

var options = RegistryJson.CreateOptions(writeIndented: true);

// 或者叠加到已有 options
var myOptions = new JsonSerializerOptions().UseRegistry();
```

> `Default` 是共享静态实例。`JsonSerializerOptions` 一旦被用于序列化就会冻结（不能再改），**不要往 `Default` 上继续加转换器**，用 `CreateOptions` 自己建一份。

---

## 已知限制

### 1. 复合键的列类型会被抹平

`CompositeKey` 反序列化时把 JSON 数字还原为 `int` → `long` → `double` 中能装下的最小类型，**无法保留原始列类型**。而 `CompositeKey` 是值相等比较：

```csharp
var reg = new CompositeValueRegistry<string>();
reg.Register(2_026_000_000L, "x");            // long 列

// 序列化 → 反序列化后，该列变成 int（如果值装得下）或 long
back.GetValue(2_026_000_000L);                 // ✅ 值大，仍是 long，能命中
back.GetValue(1L);                             // ❌ 原注册时若是 int 1，这里对不上
```

**建议复合键统一使用 `string` 列**，或保证写入 / 读取两侧的数值类型一致。

### 2. 值类型必须能被 `System.Text.Json` 直接处理

转换器只是把每个 `value` 交给 `JsonSerializer.Deserialize<TValue>`。`TValue` 含接口、抽象类型、无参构造器类型时仍需要额外的多态配置。

### 3. 不保留插入顺序

条目数组的顺序来自 `ConcurrentDictionary` 的枚举顺序，**反序列化后不要假设顺序与写入时一致**。

### 4. `enabled: false` 的条目会一直留在文件里

禁用项也会被写出——这是有意为之（保证往返无损），但意味着长期 `Disable` 的条目不会自动从存档中消失，需要显式 `Remove`。
