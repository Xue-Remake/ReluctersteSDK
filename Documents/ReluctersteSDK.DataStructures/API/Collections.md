# API：集合类型

命名空间：`ReluctersteSDK.DataStructures.Structures`

- [`BiDictionary<TKey, TValue>`](#bidictionarytkey-tvalue)：双向映射
- [`ListDictionary<TKey, TValue>`](#listdictionarytkey-tvalue)：保序 + 线程安全

---

## `BiDictionary<TKey, TValue>`

键与值都唯一，可从任一侧反查。

```csharp
public class BiDictionary<TKey, TValue> : IEnumerable<KeyValuePair<TKey, TValue>>
    where TKey : notnull
    where TValue : notnull
```

内部维护正向（`TKey → TValue`）与反向（`TValue → TKey`）两张 `Dictionary`。

### 构造

| 构造 | 说明 |
|---|---|
| `BiDictionary()` | 默认比较器 |
| `BiDictionary(IEqualityComparer<TKey>? keyComparer, IEqualityComparer<TValue>? valueComparer)` | 自定义比较器 |
| `BiDictionary(IEnumerable<KeyValuePair<TKey,TValue>> collection, IEqualityComparer<TKey>? = null, IEqualityComparer<TValue>? = null)` | 逐条 `Add`；**遇重复会抛异常** |
| `BiDictionary(IDictionary<TKey,TValue> dictionary, IEqualityComparer<TKey>? = null, IEqualityComparer<TValue>? = null)` | 同上 |

### 成员

| 成员 | 返回 | 说明 |
|---|---|---|
| `Count` | `int` | 条目数 |
| `Keys` | `IEnumerable<TKey>` | 正向键（顺序 = 正向字典枚举顺序） |
| `Values` | `IEnumerable<TValue>` | 值集合（实现上是 `_reverse.Keys`） |
| `this[TKey key]` | `TValue` | **读**等价于 `GetValue`（缺失抛 `KeyNotFoundException`）；**写**等价于 `AddOrUpdate` |
| `Add(TKey, TValue)` | `void` | 键或值重复 → `ArgumentException` |
| `Add(ValueTuple<TKey,TValue>)` / `Add(KeyValuePair<TKey,TValue>)` | `void` | 便于集合初始化器 |
| `AddOrUpdate(TKey, TValue)` | `void` | 先解除冲突的旧映射，再写入 |
| `GetValue(TKey)` | `TValue` | 缺失抛 `KeyNotFoundException` |
| `GetKey(TValue)` | `TKey` | 缺失抛 `KeyNotFoundException` |
| `TryGetValue(TKey, out TValue)` | `bool` | |
| `TryGetKey(TValue, out TKey)` | `bool` | |
| `ContainsKey(TKey)` / `ContainsValue(TValue)` | `bool` | |
| `RemoveByKey(TKey)` | `bool` | 同时删反向映射 |
| `RemoveByValue(TValue)` | `bool` | 同时删正向映射 |
| `Clear()` | `void` | |
| `GetEnumerator()` | `KeyValuePair<TKey,TValue>` | 遍历**正向**字典 |

### 排序

`Sort` 只重排**正向字典**的枚举顺序，不影响映射关系，也不影响反向。

| 重载 | 说明 |
|---|---|
| `Sort()` | 按 `TKey` 默认比较 |
| `Sort(Comparison<KeyValuePair<TKey,TValue>>)` | 自定义比较 |
| `Sort(IComparer<KeyValuePair<TKey,TValue>>)` | 自定义比较器 |
| `Sort<TSortKey>(Func<KeyValuePair<TKey,TValue>, TSortKey> keySelector, bool descending = false)` | 键选择器 |

```csharp
bi.Sort();
bi.Sort((a, b) => a.Value.Length.CompareTo(b.Value.Length));
bi.Sort(kv => kv.Value, descending: true);
```

### 已知限制

- **`AddOrUpdate` 有中间态**：先删后加，不是原子操作；并发写同一张表需要外部加锁。
- **`Sort` 改变的是枚举顺序**，`Keys` / `Values` 随之变化，但 `Count` / 查找不受影响。
- **`Values` 实际是反向字典的键集**，因此它的枚举顺序与 `Keys` **不一致**。
- **键/值必须非 null**（`notnull` 约束）。`string` 传 null 会在运行时抛异常。
- **相等性依赖比较器**：注册后修改键对象状态会导致查找失败。

---

## `ListDictionary<TKey, TValue>`

基于 `List<KeyValuePair<>>` 的保序键值集合，**线程安全**，支持按下标访问，不实现 `IDictionary`。

```csharp
public class ListDictionary<TKey, TValue>
    : IEnumerable<KeyValuePair<TKey, TValue>>, IDisposable
```

### 构造

| 构造 | 说明 |
|---|---|
| `ListDictionary()` | 默认键比较器 |
| `ListDictionary(IEqualityComparer<TKey> keyComparer)` | 自定义键比较器（null 回退到默认） |

### 成员

| 成员 | 返回 | 说明 |
|---|---|---|
| `Count` | `int` | 加读锁 |
| `this[int index]` | `KeyValuePair<TKey,TValue>` | 按下标读写。读越界抛 `ArgumentOutOfRangeException`；**写**时若新键与其他位置重复抛 `ArgumentException` |
| `GetValue(TKey)` | `TValue` | 缺失抛 `KeyNotFoundException` |
| `TryGetValue(TKey, out TValue)` | `bool` | |
| `SetValue(TKey, TValue)` | `void` | 有则改（顺序不变），无则追加 |
| `ContainsKey(TKey)` | `bool` | |
| `GetKeys()` | `List<TKey>` | 快照 |
| `GetVals()` | `List<TValue>` | 快照 |
| `Add(TKey, TValue)` | `void` | 追加到末尾；键重复抛 `ArgumentException` |
| `Add(ValueTuple<TKey,TValue>)` / `Add(KeyValuePair<TKey,TValue>)` | `void` | |
| `Remove(TKey)` | `bool` | |
| `RemoveAt(int index)` | `void` | 越界抛 `ArgumentOutOfRangeException` |
| `RemoveWhere(Func<KeyValuePair<TKey,TValue>, bool>)` | `int` | 返回删除条数 |
| `FirstOrDefault(Func<KeyValuePair<TKey,TValue>, bool>)` | `KeyValuePair<...>?` | 无匹配返回 `null` |
| `Where(Func<KeyValuePair<TKey,TValue>, bool>)` | `IEnumerable<...>` | **在锁内求值并物化**，返回的不是惰性流 |
| `Any(Func<KeyValuePair<TKey,TValue>, bool>)` | `bool` | |
| `Clear()` | `void` | |
| `Sort()` / `Sort(Comparison<...>)` / `Sort(IComparer<...>)` / `Sort<TSortKey>(...)` | `void` | 与 `BiDictionary.Sort` 同名同义 |
| `Dispose()` | `void` | 释放内部 `ReaderWriterLockSlim` |

### 并发模型

- 所有读写走 `ReaderWriterLockSlim`（`LockRecursionPolicy.NoRecursion`）。
- **枚举返回调用时刻的快照**：遍历期间其他线程的修改不影响本次循环。
- 因为枚举是快照，`Count` 与枚举结果**可能不一致**——这是预期行为。
- **不可重入**：在锁内再调用同一实例的加锁方法会抛 `LockRecursionException`。因此 `Where` 的谓词里不要再调本表的方法。

### 性能

内部是 `List` + 线性扫描：

| 操作 | 复杂度 |
|---|---|
| 按下标访问 | O(1) |
| `Add`（追加） | O(1) |
| 按键查找 / `Add` 去重 / `Remove` | O(N) |
| `Sort` | O(N log N) |

元素多（> 100）且不需要顺序时请用 `Dictionary`。

### 已知限制

- **不实现 `IDictionary`**：不能直接传给需要字典参数的 API。
- **`GetKeys()` / `GetVals()` 是快照**，大表频繁调用会产生分配。
- **一次 `Dispose` 后再用会抛 `ObjectDisposedException`**。
- **不支持反向映射**：一个值可以被多个键持有。
- **`RemoveWhere` 在写锁内执行谓词**，谓词耗时会阻塞所有读写。
