# API：实体与组件存储

命名空间：`ReluctersteSDK.Game.Ecs`

- [`Entity`](#entity)：8 字节句柄
- [`EntityManager`](#entitymanager)：槽位分配与回收
- [`ComponentStore<T>`](#componentstoret)：Sparse Set 存储
- [`IComponentStore`](#icomponentstore)：非泛型契约

---

## `Entity`

```csharp
public readonly struct Entity : IEquatable<Entity>
```

| 成员 | 类型 | 说明 |
|---|---|---|
| `Index` | `int` | 槽位下标 |
| `Generation` | `uint` | 复用代数 |
| `Null` | `static Entity` | `Index = -1`，表示"无" |
| `IsNull` | `bool` | `Index < 0` |
| `Entity(int index, uint generation)` | 构造 | |
| `Equals` / `==` / `!=` | | **Index 与 Generation 都相等**才算同一个实体 |
| `GetHashCode()` | `int` | `HashCode.Combine(Index, Generation)` |
| `ToString()` | `string` | `"Entity(3:2)"` 或 `"Entity(Null)"` |

### 为什么要 Generation

槽位会被复用。如果句柄只存 `Index`，一个已被销毁、槽位又被新实体占用的旧句柄会"复活"成另一个实体。

```csharp
Entity a = world.Create();
world.Destroy(a);
Entity b = world.Create();     // 可能复用 a.Index

a == b;                        // false —— Generation 不同
world.IsAlive(a);              // false
world.Has<Transform>(a);       // false（存储按 Index 查，需自己注意）
```

> `ComponentStore.Contains(int)` 只看 `Index`。**直接用裸 Index 查组件会误判**，始终传 `Entity`。

---

## `EntityManager`

| 成员 | 类型 | 说明 |
|---|---|---|
| `AliveCount` | `int` | 存活实体数 |
| `Capacity` | `int` | 已分配的槽位数组长度 |
| `MaxIndex` | `int` | 下一个待分配的下标（已用槽位的高水位） |
| `Create()` | `Entity` | 优先复用空闲栈里的槽位（代数 +1），否则扩容 |
| `Destroy(Entity)` | `bool` | 槽位入空闲栈、代数 +1；实体已死返回 `false` |
| `IsAlive(Entity)` | `bool` | 槽位在范围内 && 代数匹配 && `Alive` |
| `EnsureCapacity(int)` | `void` | 扩容槽位 |
| `Restore(int index, uint generation)` | `Entity` | 按原索引/代数恢复（快照用） |
| `GetAliveEntities()` | `Entity[]` | 当前存活实体快照 |
| `Clear()` | `void` | 清空全部槽位 |

`EntityManager(int initialCapacity = DefaultCapacity)` 只是预分配，之后按需翻倍扩容。

### 回收语义

`Destroy` 时：

1. 该槽位标记为不存活；
2. 代数 `+1`；
3. 槽位下标压入空闲栈。

下次 `Create` 从栈顶取，因此**销毁后立刻创建会复用同一个 Index**，这是预期行为。

---

## `ComponentStore<T>`

```csharp
public sealed class ComponentStore<T> : IComponentStore where T : notnull
```

Sparse Set：`_sparse[entityIndex] → denseIndex`，`_entities[denseIndex]` / `_components[denseIndex]` 是紧凑数组。

### 成员

| 成员 | 返回 | 说明 |
|---|---|---|
| `ComponentType` | `Type` | `typeof(T)` |
| `Count` | `int` | 组件数量 = 紧凑数组有效长度 |
| `Entities` | `ReadOnlySpan<Entity>` | 紧凑数组视图（与 `Components` **同序**） |
| `Components` | `Span<T>` | 组件紧凑数组视图 |
| `Add(Entity, in T)` | `bool` | 新建返回 `true`，覆盖返回 `false`；失效实体抛 `ArgumentException` |
| `Get(Entity)` | `ref T` | 不存在抛 `KeyNotFoundException` |
| `TryGet(Entity, out T)` | `bool` | |
| `Remove(Entity)` | `bool` | 按 `Entity` 移除 |
| `Remove(int entityIndex)` | `bool` | 绕过存活检查与回调，仅供 `World.Destroy` 内部使用 |
| `Contains(int entityIndex)` | `bool` | 只查 `Index`，不看代数 |
| `TryGetDense(int entityIndex, out int denseIndex)` | `bool` | 取紧凑下标，供 `Query` 交集用 |
| `Clear()` | `void` | |

由 `World.Store<T>()` 创建时，存储会注入实体存活校验与结构变化回调；直接调用 `Store<T>().Add/Remove` 也会维护 `StructuralVersion` 并触发生命周期事件。独立 `new ComponentStore<T>()` 没有这些回调。

### Swap-Remove

`Remove` 不保持顺序：把**最后一个元素**搬到被删位置，再收缩长度。

```csharp
// 删除 dense[2]，长度 5
// 前： [A][B][C][D][E]
// 后： [A][B][E][D]
//                    ↑ 原 E 位置失效
```

因此：

- **遍历期间的 `denseIndex` 在删除后全部失效**；
- `Entities` / `Components` 的顺序**不稳定**，不要依赖它；
- 想要稳定顺序，请在渲染层自己排序（`SpriteRenderSystem` 就是这么做的，用 `EntityIndex` 兜底）。

### 为什么不放 `Dictionary<Entity, T>`

- 紧凑数组对缓存友好；
- 遍历时拿 `Span` 直接顺序访问，无哈希开销；
- `Query<T1,T2>` 靠 `TryGetDense` 做 O(1) 交集判断。

代价就是上面那条：顺序不稳定。

---

## `IComponentStore`

非泛型契约，只给 `World` 做级联清理用：

```csharp
public interface IComponentStore
{
    Type ComponentType { get; }
    int Count { get; }
    bool Contains(int entityIndex);
    bool Remove(int entityIndex);
    void Clear();
}
```

业务代码**不需要**面向这个接口，直接用 `ComponentStore<T>` 或 `world.Add/Get/Remove`。

---

## 已知限制

- **`Contains(int)` / `Remove(int)` 只看 Index**：绕过代数是内部优化，公开使用容易踩复用陷阱。
- **`Entity` 是 8 字节**：`int` + `uint`，可以安全放进 `Dictionary` 或数组。
- **没有实体版本的批量创建**：一次一个，需要批量请循环或用 `CommandBuffer`。
- **`Entities` / `Components` 是 `Span`**：不能跨 `await` 保存，也不能存进字段。
- **组件必须是 `notnull`**：`string`、类类型都行；`Nullable<T>` 结构体不行。
