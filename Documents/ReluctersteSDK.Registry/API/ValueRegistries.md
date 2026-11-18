# API：键值注册表

命名空间：`ReluctersteSDK.Registry`

- [统一语义](#统一语义)（五种注册表共享，先看这个）
- [`ValueRegistry<TKey, TValue>`](#valueregistrytkey-tvalue)
- [`CompositeKey`](#compositekey)
- [`CompositeValueRegistry<TValue>`](#compositevalueregistrytvalue)
- [共同限制](#共同限制)

---

## 统一语义

`ValueRegistry` / `CompositeValueRegistry` / `TypeRegistry` / `InstanceRegistry` / `DelegateRegistry`（前四者实现标记接口 `IRegistry`）共享同一套启停规则，务必记住：

| 规则 | 说明 |
|---|---|
| **禁用 ≠ 删除** | 条目的值仍保存在内部字典里，只是所有读取接口会过滤掉它 |
| `GetValue` 对禁用条目 | 返回 `default` |
| `TryGetValue` 对禁用条目 | 返回 `false` |
| `Query` 对禁用条目 | 不包含在结果里 |
| `Keys()` / `Values()` | 只返回**已启用**的条目 |
| `Exists(key)` | 查**物理存在**，无视启用状态 |
| `IsEnabled(key)` | 查**是否启用**（不存在时返回 `false`） |
| 重复 `Register` 同一个键 | **覆盖值，并保留原有启用状态** |

`IRegistry` 是空标记接口，只用于"这东西是注册表"的泛化约束。

```csharp
var registry = new ValueRegistry<string, int>();

registry.Register("hp", 100);
registry.Disable("hp");

registry.GetValue("hp");     // 0
registry.Exists("hp");       // true
registry.IsEnabled("hp");    // false

registry.Enable("hp");
registry.GetValue("hp");     // 100
```

> `Enable` / `Disable` / `Remove` 在键不存在时返回 `false`，不抛异常。

---

## `ValueRegistry<TKey, TValue>`

单键 → 单值。键为 `TKey`，值可为 `null`（`TValue?` 语义）。

### 成员

| 成员 | 返回 | 说明 |
|---|---|---|
| `Register(TKey key, TValue value)` | `void` | 注册或覆盖 |
| `this[TKey key]` | `TValue?` | 读等价于 `GetValue`；写等价于 `Register` |
| `GetValue(TKey key)` | `TValue?` | 不存在或被禁用 → `default` |
| `GetValue(Func<TValue,bool> predicate)` | `TValue?` | 返回**第一个**匹配项，顺序不保证 |
| `TryGetValue(TKey key, out TValue value)` | `bool` | 不存在或被禁用 → `false` |
| `TryGetValue(Func<TValue,bool> predicate, out IList<TValue> values)` | `bool` | 收集**全部**匹配项 |
| `Query(Func<TValue,bool> predicate)` | `IList<TValue>` | 按值过滤 |
| `Query(Func<TKey,TValue,bool> predicate)` | `IList<TValue>` | 按「键 + 值」过滤 |
| `Keys()` | `ICollection<TKey>` | 已启用的键 |
| `Values()` | `ICollection<TValue>` | 已启用的值 |
| `Enable(TKey key)` | `bool` | 键不存在 → `false` |
| `Disable(TKey key)` | `bool` | 键不存在 → `false` |
| `Remove(TKey key)` | `bool` | 物理删除，无视启停 |
| `Exists(TKey key)` | `bool` | 物理存在 |
| `IsEnabled(TKey key)` | `bool` | 存在且启用 |
| `Clear()` | `void` | 清空全部条目 |

### 查询是快照

`Query` / `Keys` / `Values` 内部先 `ToArray()` 再返回，**遍历期间被其他线程修改不会抛异常**，但结果可能滞后于当前状态。

```csharp
var reg = new ValueRegistry<string, string>();
reg.Register("greet", "hello");
reg.Register("hello", "world");

reg.Query(s => s.Length > 4);              // ["hello", "world"]
reg.Query((k, v) => k.StartsWith("g"));    // ["hello"]

reg.TryGetValue(v => v.Contains('o'), out var hits);   // 全部匹配项
```

---

## `CompositeKey`

`readonly struct`，按列依次比较的联合主键，列数不限。

### 成员

| 成员 | 说明 |
|---|---|
| `CompositeKey(params object?[] keys)` | 构造时**复制**入参数组，之后外部修改入参不影响已建键 |
| `Length` | 列数 |
| `this[int index]` | 取第 N 列 |
| `Keys` | `IReadOnlyList<object?>` |
| `Equals(CompositeKey)` / `==` | 逐列 `Equals` 比较 |
| `GetHashCode()` | 构造时预计算并缓存 |
| `ToString()` | 形如 `[2026, season, autumn]` |
| `static From(params object?[] keys)` | 显式构造（等价于 `new`） |

### 隐式转换

| 来源 | 说明 |
|---|---|
| `object?[]` | `CompositeKey k = new object?[] { 2026, "autumn" };` |
| `(object?, object?)` | 二元组 |
| `(object?, object?, object?)` | 三元组 |
| `(object?, object?, object?, object?)` | 四元组 |

```csharp
index[("player", 4)] = "第四个玩家";      // 二元组隐式转 CompositeKey
```

> 只提供到四元组；更多列请用 `CompositeKey.From(...)` 或 `new CompositeKey(...)`。

### 相等性警告

`CompositeKey` 是**值相等**比较。`1`（`int`）与 `1L`（`long`）不是同一列值，`Equals` 返回 `false`。

反序列化会把 JSON 数字统一还原为 `int` → `long` → `double` 中能装下的最小类型，因此写入时用 `long` 的键在往返后可能变成 `int` 而对不上。**建议复合键统一用 `string` 列。**

---

## `CompositeValueRegistry<TValue>`

多列联合主键 → 值。**不实现 `IRegistry`。**

### 成员

| 成员 | 返回 | 说明 |
|---|---|---|
| `Register(CompositeKey key, TValue value)` | `void` | |
| `Register<T1,T2>(T1 k1, T2 k2, TValue value)` | `void` | 强类型重载，1~4 列 |
| `this[CompositeKey key]` | `TValue?` | 读等价于 `GetValue`；写等价于 `Register` |
| `GetValue(CompositeKey key)` | `TValue?` | |
| `GetValue<T1,T2>(T1 k1, T2 k2)` | `TValue?` | |
| `GetValue(Func<TValue,bool> predicate)` | `TValue?` | 第一个匹配项 |
| `TryGetValue(CompositeKey key, out TValue value)` | `bool` | |
| `TryGetValue<T1,T2>(T1 k1, T2 k2, out TValue value)` | `bool` | |
| `TryGetValue(Func<TValue,bool>, out IList<TValue>)` | `bool` | 全部匹配项 |
| `Query(Func<TValue,bool>)` | `IList<TValue>` | |
| `Query(Func<CompositeKey,TValue,bool>)` | `IList<TValue>` | 谓词同时拿到键与值 |
| `GetValuesByPrefix(params object[] prefixKeys)` | `IList<TValue>` | 只匹配前 N 列 |
| `Keys()` / `Values()` | `ICollection<...>` | 已启用条目 |
| `Enable` / `Disable` / `Remove` / `Exists` / `IsEnabled` | `bool` | 均有 1~3 列强类型重载 |
| `Clear()` | `void` | |

### 前缀查询

`GetValuesByPrefix("player")` 匹配所有首列为 `"player"` 的条目；`GetValuesByPrefix(2026, "season")` 匹配前两列。传入的列数不能超过键的长度，否则不匹配任何条目。

```csharp
var index = new CompositeValueRegistry<string>();

index.Register("player", 1, "第一个玩家的名字");
index.Register("player", 2, "第二个玩家的名字");
index.Register(2026, "season", "autumn", "秋季节气");

index.GetValue("player", 1);                  // "第一个玩家的名字"
index.GetValuesByPrefix("player");            // 两条
index.GetValuesByPrefix(2026, "season");      // "秋季节气"
index.GetValuesByPrefix(2026, "winter");      // 空
```

### 重载顺序

`Register` 的重载固定为 **Key 在前、Value 在最后**：

```csharp
index.Register(k1, k2, value);       // ✅
index.Register(value, k1, k2);       // ❌ 编译不过（除非类型刚好兼容，会静默写错）
```

因为列类型是泛型参数、值也是泛型，编译器无法帮你分辨"最后那个是不是值"。写错且类型兼容时不会报错——这是本类型最容易踩的坑。

---

## 共同限制

- **没有枚举与批量写**：只能逐条 `Register`，没有 `AddRange` / 事务语义。
- **没有容量上限**：注册表会一直增长，需要回收请显式 `Remove` 或 `Clear`。
- **`Query` 顺序不保证**：底层是 `ConcurrentDictionary`，不要依赖返回顺序。
- **`CompositeValueRegistry` 无 `IRegistry`**：写"接受任意注册表"的泛化代码时要排除它。
- **键/值相等依赖默认比较器**：传入自定义 `Equals`/`GetHashCode` 的类型时注意可变性——注册后修改键对象会破坏查找。
