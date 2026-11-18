# ECS 设计（ReluctersteSDK.Game）

> 状态：**v1.0 — 重构完成（阶段 1~6 全部落地）**
> `ReluctersteSDK.Game` 现为纯 ECS 结构：`Ecs/`（核心 + 组件）、`Systems/`（Transform / Motion）、`Rendering/`（Sprite / 渲染 / 帧动画）。
> 旧 `Scene2D` / `SceneNode2D` / `ContainerNode2D` / `ConsoleGame` / `Motion/*` **已全部删除**。
> 配套 `ReluctersteSDK.Game.Test` **119 个单测**全部通过。
> 0.13.0.0 已补：Transform 脏标记、3~4 组件查询与 `Without`、单例、生命周期事件、摄像机裁剪、快照、调试统计。
> 后续可做：空间分区、基准测试、Archetype 存储。
>
> 用法与 API 参考见 [README.md](README.md) 与 [API/](API/)。

## 0. 已确认的决策

| 决策项 | 结论 | 理由 |
|---|---|---|
| 组件存储形态 | **Sparse Set** | 增删 O(1)、遍历连续、struct 零装箱；约 400 行可实现；性能/复杂度最平衡 |
| 工程结构 | **合并进 `ReluctersteSDK.Game`**，用 `Ecs/` 文件夹隔离 | 少一个工程、发布简单；分层靠约定 + 命名空间约束 |
| 旧场景树 | **直接删除** `Scene2D` / `SceneNode2D` / `ContainerNode2D` | 确认无外部使用方；不留历史包袱 |
| 父子层级 | **需要**，用组件实现 | 解决现有实现"变换不累乘"的根本缺陷 |

## 1. 目标与非目标

### 适用范围（硬约束）

本库**仅面向 2D 游戏**，所有设计以此为前提：

- 变换只用 2D 向量 + 单轴旋转（弧度）+ 2D 缩放，世界矩阵用 `System.Numerics.Matrix3x2`；
- **不引入** `Quaternion`、`Matrix4x4`、3D 相机、透视投影；
- 渲染只考虑 sprite 合批，不做网格 / 材质 / 深度缓冲（`Depth` 仅用于 2D 排序）；
- 碰撞与空间划分只考虑 2D（AABB、圆、线段、四叉树）。

超出 2D 的需求不在本库范围内——需要 3D 时另建独立包，**不要往这里加维度开关**。

### 目标

1. **数据与行为分离**：组件只存数据，系统只写行为，新增玩法不改已有类。
2. **更新顺序可控**：系统顺序显式声明，不依赖反射或遍历顺序。
3. **实体生命周期明确**：创建、销毁有唯一入口，销毁后旧引用立即可检测。
4. **批量遍历零装箱**：struct 组件连续存储，遍历无分配。
5. **ECS 核心不依赖 MonoGame**：`Ecs/` 目录下不出现任何 `Microsoft.Xna.*`，可无头测试。

### 非目标（明确不做，避免过度设计）

- ❌ Archetype / chunk 存储
- ❌ 多线程 Job System、并行 System
- ❌ 网络同步、回滚
- ❌ 实体内存序列化（后续单独立项）
- ❌ 运行时热重载组件类型

## 2. 目录结构

```
ReluctersteSDK.Game/
├─ Ecs/                         ← 纯 C#，禁止 using Microsoft.Xna.*
│  ├─ Entity.cs
│  ├─ EntityManager.cs
│  ├─ IComponentStore.cs
│  ├─ ComponentStore.cs         ← SparseSet<T> 的实现载体
│  ├─ World.cs
│  ├─ Query.cs
│  ├─ CommandBuffer.cs
│  ├─ ISystem.cs
│  ├─ HierarchyExtensions.cs
│  └─ Components/
│     ├─ Transform.cs           ← Transform / WorldTransform
│     ├─ Hierarchy.cs           ← Parent / Children
│     └─ Motion.cs              ← Velocity / Acceleration / Damping / Heading
├─ Systems/                     ← 纯逻辑系统（可用 Math，不用 MonoGame）
│  ├─ TransformSystem.cs
│  └─ MotionSystem.cs
├─ Rendering/                   ← MonoGame 适配
│  ├─ Camera2D.cs               ← 2D 摄像机与裁剪
│  ├─ Sprite.cs
│  ├─ RenderCommand.cs
│  └─ SpriteRenderSystem.cs
├─ EventLogger.cs               ← 保留，与 ECS 无关
└─ LogEventAttribute.cs
```

**分层约束**：`Ecs/` 与 `Systems/` 只能引用 `System.*` 与 `ReluctersteSDK.Math`；`Microsoft.Xna.*` 只允许出现在 `Rendering/`。
（编译期无法强制，靠 code review + 可选的自定义 Roslyn Analyzer 后续补。）

## 3. 核心类型

### 3.1 Entity

```csharp
public readonly struct Entity : IEquatable<Entity>
{
    public readonly int Index;        // 实体槽位
    public readonly uint Generation;  // 复用代数

    public bool IsNull => Index < 0;

    public bool Equals(Entity other)
        => Index == other.Index && Generation == other.Generation;

    public override int GetHashCode() => HashCode.Combine(Index, Generation);
}
```

- 8 字节，可安全塞进组件、字典、列表。
- **必须带 `Generation`**：裸 `int` 在实体回收后会"幽灵指向"新实体，这类 bug 极难排查。

### 3.2 EntityManager

```csharp
public sealed class EntityManager
{
    private struct Record { public uint Generation; public bool Alive; }

    private Record[] _records;        // 按 Index 索引
    private readonly Stack<int> _freeIndices;
    private int _nextIndex;

    public int AliveCount { get; }

    public Entity Create();
    public bool Destroy(Entity entity);   // 校验 Generation，过期句柄返回 false
    public bool IsAlive(Entity entity);
    public ReadOnlySpan<int> AliveIndices { get; }  // 供系统全量遍历
}
```

规则：
- 创建优先复用 `_freeIndices`，复用时代数 `+1`；
- `Destroy` 校验 `_records[i].Generation == entity.Generation`，防止双重销毁；
- 销毁时 **World 负责同步清理所有组件 store**（`EntityManager` 只管自己的表）。

### 3.3 IComponentStore / ComponentStore&lt;T&gt;

```csharp
public interface IComponentStore
{
    int Count { get; }
    void Remove(int entityIndex);   // 按实体索引移除（供实体销毁时级联调用）
    bool Contains(int entityIndex);
    void Clear();
}

public sealed class ComponentStore<T> : IComponentStore where T : struct
{
    private int[] _sparse;      // entityIndex -> denseIndex，-1 表示没有
    private Entity[] _entities; // dense：实体句柄
    private T[] _components;    // dense：组件数据
    private int _count;
}
```

Sparse Set 的三条不变量：

```
0 <= denseIndex < _count
_sparse[_entities[i].Index] == i
_components[i] 属于 _entities[i]
```

- `Add`：sparse 没记录时追加到 dense 末尾；有记录时**覆盖**。
- `Remove`：**swap-remove**——把 dense 末尾元素搬到被删位置，同步修正 `sparse`。
- 遍历：直接扫 `_components[0.._count)`，连续内存。
- 扩容：`_sparse` 按需倍增（初始 256），`_components` 翻倍。

> ⚠️ `_sparse` 的大小跟随**最大实体索引**，不是实体数量。长时间运行且实体索引只增不减时会膨胀。
> 缓解：`EntityManager` 复用索引；极端情况下可换分段稀疏数组（paged sparse），当前不做。

### 3.4 World

```csharp
public sealed class World
{
    public EntityManager Entities { get; }

    // 实体
    public Entity Create();
    public void Destroy(Entity entity);      // 级联清理所有组件

    // 组件
    public void Add<T>(Entity e, in T component) where T : struct;
    public ref T Get<T>(Entity e) where T : struct;
    public bool TryGet<T>(Entity e, out T component) where T : struct;
    public bool Has<T>(Entity e) where T : struct;
    public bool Remove<T>(Entity e) where T : struct;

    // 单例组件（每类型最多一个）
    public void SetSingleton<T>(in T value) where T : struct;
    public ref T GetSingleton<T>() where T : notnull;

    // 查询
    public Query<T1> Query<T1>() where T1 : notnull;
    public Query<T1,T2> Query<T1,T2>() where T1,T2 : notnull;
    public Query<T1,T2,T3> Query<T1,T2,T3>();
    public Query<T1,T2,T3,T4> Query<T1,T2,T3,T4>();
    // 每个 Query 都支持 .Without<TExclude>()

    // 系统
    public void AddSystem(ISystem system, SystemPhase phase = SystemPhase.Update,
                          int priority = 0, bool enabled = true);
    public void Update(float deltaTime);   // 执行 Update + LateUpdate，并逐个 flush 命令缓冲
    public void Render(float deltaTime);   // 执行 Render 阶段

    // 迭代安全
    internal int StructuralVersion { get; }   // 结构性修改时自增
}
```

组件存储用 `Dictionary<Type, IComponentStore>` 按类型索引；首次访问某类型时惰性创建 store。

### 3.5 Query

遍历方式定为 **`ForEach` + ref 委托**，而不是 `foreach`：

```csharp
public delegate void RefAction<T1>(Entity e, ref T1 c1) where T1 : struct;
public delegate void RefAction<T1,T2>(Entity e, ref T1 c1, ref T2 c2) where T1,T2 : struct;

world.Query<Position, Velocity>().ForEach((Entity e, ref Position p, ref Velocity v) =>
{
    p.Value += v.Value * dt;
});
```

**为什么不用 `foreach`**：
C# 无法在 `Current` 里同时返回 `Entity` 和 `ref T`（元组不能含 `ref`）。硬做只能让调用方手动 `while(q.MoveNext()) { ref var p = ref q.Get1(); }`，可读性差。
`ForEach` + `ref` 委托既给到零拷贝写入，又保持可读。委托本身在无捕获时会被编译器缓存成静态实例，不产生每帧分配。

补充接口：

```csharp
public void ForEach(RefAction<...> action);
public Entity[] ToEntityArray();   // 需要结构性修改时先取快照
```

查询算法：
1. 取所有参与类型 store 中 **`Count` 最小**的那个作为驱动；
2. 扫它的 dense 数组，逐个用其余 store 的 `Contains(entityIndex)` 过滤；
3. 命中就取出各组件的 `ref` 调用委托。

复杂度 O(n·k)，k 为组件类型数（≤4），对千级实体足够。

### 3.6 迭代安全

- `World` 维护 `StructuralVersion`，`Create/Destroy/Add/Remove` 都会自增。
- `ForEach` 进入时记录版本，**每次回调后校验**；不一致立即抛 `InvalidOperationException`，
  消息里直接指明"遍历中发生了结构性修改，请改用 CommandBuffer 或 ToEntityArray()"。
- 不提供"静默容错"——迭代中改结构一定会漏数据或重复访问。

### 3.7 CommandBuffer

```csharp
public sealed class CommandBuffer
{
    public void Create(Action<Entity>? onCreate = null);
    public void Destroy(Entity entity);
    public void Add<T>(Entity entity, in T component) where T : struct;
    public void Remove<T>(Entity entity) where T : struct;

    public void Playback(World world);   // 按记录顺序执行，执行后可复用
}
```

- **每个系统执行完后立刻 flush**（而不是帧末），这样下一个系统能看到上一个系统的修改，语义最直观。
- 帧末再 flush 一次兜底。

### 3.8 ISystem

```csharp
public enum SystemPhase { Update, Render }

public interface ISystem
{
    void Execute(World world, float deltaTime);
}
```

- `World` 内部维护两个有序 `List<ISystem>`，按注册顺序执行。
- **不做特性扫描自动注册**——系统顺序是游戏逻辑的一等公民，反射顺序不可控。
- 需要"在某个系统之前插入"时，用显式的 `InsertSystem(index, system)`，而不是猜。

## 4. 组件设计

### 4.1 Transform（局部变换）

```csharp
public struct Transform
{
    public CartesianVector2D LocalPosition;
    public float LocalRotation;          // 弧度
    public CartesianVector2D LocalScale;
}
```

用 `ReluctersteSDK.Math` 的向量而非 `Vector2`，保证 `Ecs/` 不依赖 MonoGame。

### 4.2 层级（Parent / Children）

```csharp
public struct Parent { public Entity Value; }

public sealed class Children            // 引用类型：内部可变，对外只读
{
    public IReadOnlyList<Entity> Items { get; }
    public int Count { get; }
    // 内部 Add/Remove/Clear 仅供 HierarchyExtensions 与 World.Destroy 使用
}
```

为什么 `Children` 是类：Sparse Set 要求 `T : struct`，但动态子节点集合必须可增长。
解决方案：`ComponentStore<T>` 的泛型约束放宽为 `where T : notnull`，`Children` 作为引用类型照样存进 `T[]`（存引用，不涉及装箱）。

> 约束改为 `notnull` 后，struct 与 class 组件都能用，只是 struct 走连续内存、class 存指针。

### 4.3 WorldTransform（计算结果）

```csharp
public struct WorldTransform { public Matrix3x2 Value; }
```

`System.Numerics.Matrix3x2` 不依赖 MonoGame，可直接用。

### 4.4 Velocity / Acceleration

```csharp
public struct Velocity     { public CartesianVector2D Value; }
public struct Acceleration { public CartesianVector2D Value; }
public struct Damping      { public float Coefficient; public float MaxSpeed; }
public struct Heading      { public float Radians; }
```

替代原有的 `CartesianSpeedPoint2D` / `CartesianLinearDamping`（**已删除**），逻辑下沉到 `MotionSystem`。

> 实现约定：`Damping.Coefficient == 0` 视为无阻尼，`MaxSpeed <= 0` 视为不限速，
> 这样 `default(Damping)` 是不产生任何副作用（而不是把速度掐成 0）。

## 5. 父子层级的计算

### 拓扑顺序

`TransformSystem` 每帧执行：

1. 用 `Transform` 存储与 `Parent` 组件找出**根实体**（父不存在 / 已销毁 / 无 `Transform` / 自环）；
2. 从根出发深度优先，只压入带 `Transform` 的子节点，得到拓扑序；
3. 按该顺序计算（脏实体才重算）：

```csharp
worldTransform = localMatrix * parentWorldMatrix;   // Matrix3x2 乘法顺序
```

### 脏标记（0.13.0.0 已实现：快照比对 + IsDirty）

v1 采用**每帧全量重算**，实现简单、绝对正确。
0.13.0.0 起改为：`TransformSystem` 保存上一帧快照，比较 `LocalPosition/LocalRotation/LocalScale/Parent`；
`IsDirty`、快照无效、缺少 `WorldTransform`、父节点本帧重算都会触发重算。拓扑序只在
`StructuralVersion` / `HierarchyVersion` 变化时重建。

### 一致性维护

- `SetParent(child, parent)` / `RemoveParent(child)` 必须同时维护两侧的 `Parent` 与 `Children`，抽成 `HierarchyExtensions` 静态方法，禁止手改。
- 销毁实体时，`World.Destroy` 先把它从父的 `Children` 移除，再处理子节点（默认：子节点**保留**并升级为根，不级联销毁；级联销毁 `DestroyHierarchy` 尚未实现）。

## 6. 渲染

### 数据流

```
SpriteRenderSystem
  └─ 遍历 (WorldTransform, Sprite) → 生成 RenderCommand 列表
Renderer（MonoGame 层）
  └─ 按 Layer → Depth → Texture 排序，SpriteBatch 批量提交
```

```csharp
public struct Sprite
{
    public Texture2D Texture;      // 引用类型，放在 Rendering/ 下
    public Rectangle Source;
    public Color Tint;
    public int Layer;
    public float Depth;
}
```

> **实现时的修正**：`Sprite` 最终做成了 **class** 而不是 struct。
> 原因是 `Color` 的 `default` 是透明黑，用 struct 时忘记初始化 `Tint` 会让精灵彻底不可见。
> 改成 class + 构造函数强制传入纹理，并给出 `Tint = Color.White`、`Source = 整张纹理` 的默认值，从根上消除这个陷阱。

public struct RenderCommand
{
    public Texture2D Texture;
    public Rectangle Source;
    public Vector2 Position;
    public float Rotation;
    public Vector2 Scale;
    public Color Tint;
    public int Layer;
    public float Depth;
}
```

### 渲染顺序规则

1. `Layer` 升序（背景 0 → 角色 10 → UI 100）；
2. 同层按 `Depth` 升序；
3. 同层同深度按 `Entity.Index` 稳定排序（保证帧间一致）。

排序用 `List<RenderCommand>.Sort` 配合静态比较器，避免每帧 LINQ 分配。

## 7. 帧循环

```csharp
// Game.Update
world.Update((float)gameTime.ElapsedGameTime.TotalSeconds);

// Game.Draw
world.Render((float)gameTime.ElapsedGameTime.TotalSeconds);
spriteBatch.Begin(samplerState: SamplerState.PointClamp);
renderer.Submit(world.GetRenderCommands());   // 内部按规则排序并合批
spriteBatch.End();
```

`World.Update` 内部：

```
foreach (system in updateSystems)       // 按 priority 排序，跳过 disabled
{
    system.Execute(world, dt);
    commandBuffer.Playback(world);      // 每系统结束立即生效
}
// 然后执行 LateUpdate 阶段，最后再兜底 Playback 一次
```

## 8. 性能预期

| 场景 | 目标 |
|---|---|
| 遍历 1000 个 `(Transform, Velocity)` | < 0.05 ms |
| 创建/销毁 1000 个实体 | < 1 ms（不含组件分配） |
| `Add<T>` / `Remove<T>` | O(1)，无 GC |
| 单帧总分配 | 稳态下 **0 B**（`Children` 的 List 复用、RenderCommand 列表复用） |

> 需要在 `ReluctersteSDK.Game.Test` 里用 `BenchmarkDotNet` 或简单的 `Stopwatch` 用例守住。

## 9. 测试计划

| 用例 | 验证点 |
|---|---|
| Entity 回收 | 销毁后旧句柄 `IsAlive == false`，新实体拿到复用 Index 但 Generation 不同 |
| SparseSet 增删 | swap-remove 后，被搬移实体的 `sparse` 索引正确 |
| 组件覆盖 | 同一实体 `Add` 两次，`Count` 不变、值被覆盖 |
| Query 交集 | 只有同时持有全部组件的实体被遍历；顺序与 dense 一致 |
| 迭代安全 | `ForEach` 里 `Add` 抛 `InvalidOperationException` |
| CommandBuffer | 帧内 Destroy 后，后续系统看不到该实体 |
| TransformSystem | 三级父子链的世界坐标正确；旋转 + 缩放组合正确 |
| 层级一致性 | `SetParent` 后父子两侧都更新；销毁父节点后子节点变根 |

## 10. 落地顺序

| 阶段 | 内容 | 预估 | 状态 |
|---|---|---|---|
| 1 | `Ecs/` 核心：Entity / EntityManager / ComponentStore / World | 400~500 行 | ✅ 已完成 |
| 2 | Query（1~2 组件）+ CommandBuffer + 迭代安全 | 150~200 行 | ✅ 已完成 |
| 3 | ISystem 调度 + TransformSystem + 层级组件 | 200 行 | ✅ 已完成 |
| 4 | Rendering 层：Sprite / RenderCommand / SpriteRenderSystem | 200 行 | ✅ 已完成 |
| 5 | Motion 迁移为组件 + MotionSystem | 100 行 | ✅ 已完成 |
| 6 | 删除 `Scene2D` / `SceneNode2D` / `ContainerNode2D` / `ConsoleGame`，更新示例与文档 | — | ✅ 已完成 |
| 7 | 帧动画：`AnimationClip` / `Animator` / `SpriteSheet` / `SpriteAnimationSystem` | 300 行 | ✅ 已完成 |

> **0.13.0.0 更新**：`Query` 已实现 1~4 组件与 `Without` 排除；以下为 v1 的原始说明——
> 当前所有系统（TransformSystem、MotionSystem、SpriteRenderSystem、SpriteAnimationSystem）最多只需要 2 个组件。

## 11. 风险与对策

| 风险 | 影响 | 对策 |
|---|---|---|
| 合并工程后 `Ecs/` 混入 MonoGame 引用 | 分层失效、无法无头测试 | code review；后续加 Roslyn Analyzer 检查 using |
| `_sparse` 数组随实体索引膨胀 | 内存浪费 | 依赖 EntityManager 复用索引；必要时改 paged sparse |
| `ForEach` 委托可能被捕获 | 每帧分配 | 文档明确"禁止捕获"；提供静态 lambda 示例 |
| 父子层级每帧遍历 | 大树时 CPU 高 | 已加脏标记与拓扑缓存；仍会遍历全部 Transform |
| 改动破坏现有 Game 模块使用者 | 编译失败 | 已确认无外部使用方；版本号 +1 并在 CHANGELOG 标注破坏性变更 |

## 12. 待补充

- [ ] `IComponentStore` 是否支持 `T : class`（当前设计：放宽到 `notnull`）
- [ ] 单例组件的实现细节（专用 store 还是保留实体）
- [ ] 实体/组件快照与恢复（存档）—— 明确列为非目标，但接口留出空间
- [ ] 是否提供 `World.Dispose()` 回收所有数组
