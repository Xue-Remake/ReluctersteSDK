# API：JSON 序列化

命名空间：`ReluctersteSDK.DataStructures.Json`

| 类型 | 转换器 |
|---|---|
| `BiDictionary<TKey,TValue>` | `BiDictionaryConverter<TKey,TValue>`（经 `BiDictionaryConverterFactory` 匹配） |
| `ListDictionary<TKey,TValue>` | `ListDictionaryConverter<TKey,TValue>`（经 `ListDictionaryConverterFactory` 匹配） |

`Graph<TVertex,TEdge>`、`NodeBase` 与各 `IXNode` 实现**没有转换器**——它们持有引用关系，直接落盘需要自己设计 id 映射。

---

## 格式

统一为键值对象数组：

```json
[ { "key": "a", "value": 1 } ]
```

```csharp
using System.Text.Json;
using ReluctersteSDK.DataStructures.Structures;

var bi = new BiDictionary<string, int> { { "a", 1 } };
string json = JsonSerializer.Serialize(bi);
```

属性名大小写都接受（`key` / `Key`、`value` / `Value`）。缺少任一字段会抛 `JsonException`。

---

## 反序列化差异

两个类型的往返行为**不完全一样**：

| 场景 | `BiDictionary` | `ListDictionary` |
|---|---|---|
| 条目顺序 | 不保证（正向字典） | **严格保持 JSON 中的顺序** |
| 重复键 | 用 `AddOrUpdate` 覆盖，**不抛异常** | 用 `SetValue` 覆盖（后者胜），**不抛异常** |
| 重复值 | 用 `AddOrUpdate` 覆盖（反向映射冲突时解除旧键） | 允许（值不要求唯一） |
| null 键 / 值 | 抛 `JsonException`（受 `notnull` 约束） | 键为 null 抛 `JsonException`；值允许 null |

> `BiDictionary` 反序列化"重复键不报错"是有意为之——它走 `AddOrUpdate`，这样外部系统导出的、含有冗余条目的表也能导入。
> 需要严格校验就自己先解析成数组再逐条 `Add`。

---

## `DataStructuresJson`

| 成员 | 说明 |
|---|---|
| `Default` | 预置本模块全部转换器的静态选项 |
| `CreateOptions(bool writeIndented = true)` | 新建一套选项 |
| `options.UseDataStructures()` | 往已有选项上叠加，**幂等** |

```csharp
using ReluctersteSDK.DataStructures.Json;

var options = DataStructuresJson.CreateOptions(writeIndented: true);

// 或叠加到已有 options（比如和 Registry 的转换器一起装）
var myOptions = new JsonSerializerOptions().UseDataStructures();
```

> 类型上的 `[JsonConverter]` 特性已经应付绝大多数场景；`DataStructuresJson` 主要用于"多个模块的转换器要装进同一份 options"的场合。
> `Default` 是共享静态实例，**不要往它上面继续加转换器**——选项一旦用于序列化就会冻结。

---

## 已知限制

- **不支持 `Dictionary` 以外的键类型**：键是复杂对象时依赖其 `ToString` / JSON 表示，往返后不保证能还原成同一个键实例。
- **不保留比较器**：自定义 `IEqualityComparer` 不会写进 JSON，反序列化后回到默认比较器。
- **`ListDictionary` 反序列化后是**新建实例，原来那个（若有）不会合并。
- **没有版本字段**：格式变化后旧存档无法自动识别。
