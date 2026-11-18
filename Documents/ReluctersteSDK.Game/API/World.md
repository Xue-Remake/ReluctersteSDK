# API：World / 查询 / 命令缓冲 / 系统

命名空间：`ReluctersteSDK.Game.Ecs`

- [`World`](#world)
- [`Query<T1>` … `Query<T1,T2,T3,T4>` / `Without`](#query)
- [`CommandBuffer`](#commandbuffer)
- [`ISystem` / `SystemPhase`](#isystem--systemphase)

---

## `World`

持有实体、组件存储、系统列表与命令缓冲。

### 属性

| 成员 | 类型 | 说明 |
|---|---|---|
| `Entities` | `EntityManager` | 实体分配器 |
| `EntityCount` | `int` | 存活实体数 |
| `UpdateSystems` | `IReadOnlyList<ISystem>` | Update 阶段系统（按执行顺序） |
| `RenderSystems` | `IReadOnlyList<ISystem>` | Render 阶段系统 |
| `Commands` | `CommandBuffer` | 延迟命令缓冲 |
| `StructuralVersion` / `HierarchyVersion` | `int` | 结构/层级修改版本 |
| `StatisticsEnabled` | `bool` | 是否记录调试统计 |
| `SystemTimings` | `IReadOnlyList<SystemTiming>` | 上一帧系统耗时 |

### 实体

| 方法 | 说明 |
|---|---|
| `Create()` | 创建实体，结构性版本 +1 |
| `Destroy(Entity)` | 级联清理所有组件；维护层级一致性；已销毁时**静默返回** |
| `IsAlive(Entity)` | |

`Destroy` 的层级处理（顺序固定）：

1. 从父节点的 `Children` 里摘除自己；
2. 自己的子节点**升级为根**（移除它们的 `Parent`），**不级联销毁**；
3. 遍历所有组件存储，按 `Index` 清掉数据；
4. `Entities.Destroy` 回收槽位。

### 组件

| 方法 | 说明 |
|---|---|
| `Store<T>()` | 取（必要时创建）该类型的存储；直接增删也会维护版本与事件 |
| `Add<T>(Entity, in T)` | 添加或**覆盖**；实体已销毁抛 `ArgumentException` |
| `Get<T>(Entity)` | 返回 `ref T`；失效实体抛 `ArgumentException`，缺组件抛 `KeyNotFoundException` |
| `TryGet<T>(Entity, out T)` | 失效实体返回 `false` |
| `Has<T>(Entity)` | 失效实体返回 `false`；不会创建空存储 |
| `Remove<T>(Entity)` | 确实移除返回 `true`；失效实体返回 `false` |

`Add` 同一类型两次是**覆盖**：不会产生两份，也不会自增 `StructuralVersion`。

### 查询

| 方法 | 返回 |
|---|---|
| `Query<T1>()` | `Query<T1>` |
| `Query<T1,T2>()` / `Query<T1,T2,T3>()` / `Query<T1,T2,T3,T4>()` | 交集查询 |
| `.Without<TExclude>()` | 排除同时持有指定组件的实体 |

### 单例

| 方法 | 说明 |
|---|---|
| `SetSingleton<T>(in T)` | 设置；覆盖不增长版本 |
| `GetSingleton<T>()` | 返回 `ref T` |
| `TryGetSingleton<T>(out T)` / `HasSingleton<T>()` / `RemoveSingleton<T>()` | |

### 系统

| 方法 | 说明 |
|---|---|
| `AddSystem(ISystem, SystemPhase = Update, int priority = 0, bool enabled = true)` | 按 priority 升序、同优先级按注册顺序执行 |
| `RemoveSystem(ISystem)` | 移除并调用 OnStop |
| `SetSystemEnabled` / `IsSystemEnabled` / `SetSystemPriority` / `TryGetSystemEntry` | 运行时控制 |
| `GetSystems(SystemPhase)` | 返回 `SystemEntry` 快照 |
| `Start()` / `Shutdown()` | Startup/Teardown 与 OnStart/OnStop |
| `Update(float dt)` | Update → LateUpdate |
| `Render(float dt)` | Render |
| `Clear()` | 清空实体、组件、系统、命令、单例，版本 +1 |
| `Dispose()` | 释放世界 |

**执行与回放时机**：

```
对每个系统：
    system.Execute(world, dt)
    Commands.Playback(world)        ← 每个系统后立即回放
结束后：
    Commands.Playback(world)        ← 兜底（系统列表为空、或命令是系统外写入的）
```

因此下一个系统能看到上一个系统通过命令缓冲做出的修改。

---

## `Query`

```csharp
public readonly struct Query<T1> where T1 : notnull
public readonly struct Query<T1, T2> where T1 : notnull where T2 : notnull
```

| 成员 | 说明 |
|---|---|
| `Count` | 匹配实体数量；多组件时遍历较小的存储 |
| `ForEach(RefAction<...>)` | 单/双/三/四组件交集遍历 |
| `ToEntityArray()` | 匹配实体的快照数组 |
| `Without<TExclude>()` | 排除同时持有指定组件的实体，可链式调用 |

### `ForEach` 是 `ref` 委托

```csharp
public delegate void RefAction<T1>(Entity entity, ref T1 c1) where T1 : notnull;
public delegate void RefAction<T1, T2>(Entity entity, ref T1 c1, ref T2 c2)
    where T1 : notnull where T2 : notnull;
public delegate void RefAction<T1, T2, T3>(Entity entity, ref T1 c1, ref T2 c2, ref T3 c3);
public delegate void RefAction<T1, T2, T3, T4>(Entity entity, ref T1 c1, ref T2 c2, ref T3 c3, ref T4 c4);
```

C# 的元组不能含 `ref`，所以这里不用 `foreach` 而用回调：组件以引用传入，**回调里直接改就是改存储**。

```csharp
world.Query<Transform, Velocity>().ForEach((Entity e, ref Transform t, ref Velocity v) =>
    t.LocalPosition += v.Value * 0.016f);
```

### 迭代安全

`ForEach` 在**每次回调之后**校验 `StructuralVersion`，变了就抛 `InvalidOperationException`。

这意味着「遍历时增删实体 / 组件」会立刻炸，而不是产生难查的越界。合法做法见 [CommandBuffer](#commandbuffer) 或先 `ToEntityArray()`。

### 交集驱动方向

`Query<T1,T2>.ForEach` 会用 **`Count` 较小的一方**作为驱动存储，对每个实体做一次 `TryGetDense` 判断。所以两个存储数量悬殊时性能仍然接近线性。

---

## `CommandBuffer`

把结构性修改延后到安全时机执行。内部就是 `List<Action<World>>`。

| 成员 | 说明 |
|---|---|
| `Count` / `IsEmpty` | 待执行命令数 / 是否为空 |
| `Create(Action<Entity>? onCreate = null)` | 登记"创建实体"，回调里可立刻挂组件 |
| `Destroy(Entity)` | 回放时实体已失效则跳过 |
| `Add<T>(Entity, T component)` | **值拷贝**；回放时实体已失效则跳过 |
| `Remove<T>(Entity)` | 同上 |
| `SetParent` / `RemoveParent` | 层级命令 |
| `SetSingleton<T>` / `RemoveSingleton<T>` | 单例命令 |
| `Defer(Action<World>)` | 自定义命令 |
| `Append(CommandBuffer)` | 追加另一个缓冲 |
| `Playback(World)` | 返回执行数量；异常会聚合后抛出 |
| `Clear()` | 丢弃全部待执行命令 |

### 为什么先清空再执行

`Playback` 里 `_actions.ToArray()` 后立刻 `_actions.Clear()`，再遍历快照执行。这样**命令执行过程中新登记的命令会留到下一次回放**，不会死循环，也不会漏掉。

### 一个常见陷阱

```csharp
world.Commands.Add(e, new Health { Value = 100 });   // 值拷贝
world.Commands.Add(e, new Velocity());               // 两条命令都生效
```

`Add<T>` 捕获的是**传出时的值**。若你之后又改了局部变量，回放时用的仍是旧值。

---

## `ISystem` / `SystemPhase`

```csharp
public enum SystemPhase { Startup, Update, LateUpdate, Render, Teardown }

public interface ISystem
{
    void Execute(World world, float deltaTime);
    void OnStart(World world) { }
    void OnStop(World world) { }
}
```

| 阶段 | 何时执行 | 典型系统 |
|---|---|---|
| `Startup` | `World.Start()` | 初始化 |
| `Update` | `World.Update(dt)` | 输入、AI、`MotionSystem`、`TransformSystem` |
| `LateUpdate` | `World.Update(dt)` 之后 | 相机跟随、收尾 |
| `Render` | `World.Render(dt)` | `SpriteRenderSystem` |
| `Teardown` | `World.Shutdown()` | 资源释放 |

同一阶段内 **priority 升序，相同 priority 按注册顺序**；执行使用快照数组，系统在 `Execute` 中增删系统从下一帧生效。

常见的顺序约束：

| 约束 | 原因 |
|---|---|
| `TransformSystem` 在改位置之后 | 否则算出来的是上一帧的世界矩阵 |
| `SpriteAnimationSystem` 在 `SpriteRenderSystem` 之前 | 渲染读的是 `Sprite.Source` |
| 消费 `FrameChanged` / `FinishedClip` 的系统在 `SpriteAnimationSystem` 之后 | 这两个字段每帧开头被清空 |

---

## 已知限制

- **`Query` 最多 4 个组件**：更多条件需要自己组合（用 `Has` 过滤）或手写 `Store<T>()` 循环。
- **`ForEach` 是委托调用**：每次回调一次间接跳转，比手写 `for` 慢；极热路径可自行写 `Store<T>()` + `Span` 循环。
- **排除查询只有 `Without<T>`**：没有 `Any<T>` / `OneOf<T>` 语义。
- **`CommandBuffer` 无优先级**：严格按登记顺序执行。对同一实体先 `Destroy` 再 `Add` / `Remove`，后续命令会在回放时因实体已失效而被**跳过**；`Defer` 里的自定义逻辑不会被跳过，需自己检查存活。
- **`RemoveSystem` 用引用相等**：每次 `new` 一个系统实例注册会导致旧实例删不掉。
- **没有多线程支持**：`World` 不是线程安全的。
