# ReluctersteSDK.Game

面向 **MonoGame 的 2D ECS 框架**。它把"实体、组件、系统"从概念落成一套可直接上手的运行时：实体分配、组件存储、层级变换、查询、系统调度、精灵渲染、帧动画、双缓冲事件、快照与调试统计。

- 目录：`ReluctersteSDK.Game/`
- 包 ID：`ReluctersteSDK.Game`
- 命名空间：`ReluctersteSDK.Game.Ecs` / `.Ecs.Events` / `.Ecs.Serialization` / `.Systems` / `.Rendering`
- 依赖：`ReluctersteSDK.Math`、`MonoGame.Framework.DesktopGL`
- 目标框架：`net10.0`

---

## 它解决什么问题

不做引擎，只做**一层薄而完整的 2D 游戏数据结构**：

| 你原本要手写的东西 | 本包提供 |
|---|---|
| 实体 ID 分配与回收 | `Entity`（8 字节，带复用代数）+ `EntityManager` |
| 组件容器 | Sparse Set：增删 O(1)、遍历连续、struct 零装箱 |
| 父子层级与变换累乘 | `Transform` / `WorldTransform` + `TransformSystem`（拓扑序 + 脏标记） |
| 遍历中改结构的崩溃 | `StructuralVersion` 校验 + `CommandBuffer` 延迟执行 |
| 系统顺序与生命周期 | `SystemPhase` + 优先级 + `OnStart/OnStop` |
| 帧内系统通信 | 双缓冲 `World.Events`：本帧写、下帧读，遍历中可继续写入 |
| 精灵排序与合批 | `SpriteRenderSystem`：Layer → Depth → 纹理 → 实体索引 |
| 存档 | `WorldSnapshot`：只序列化注册过的纯数据组件 |

---

## 硬约束：这是 2D 库

整个包以 **2D** 为前提，不是"暂时只做了 2D"：

- 变换只用 2D 向量 + 单轴旋转（弧度）+ 2D 缩放，世界矩阵固定为 `System.Numerics.Matrix3x2`；
- 不提供 `Quaternion`、`Matrix4x4`、3D 相机、透视投影；
- 渲染只考虑 sprite 合批，`Depth` 只用于 2D 排序，不是深度缓冲；
- 碰撞与空间划分只考虑 2D（AABB、圆、线段、多边形）。

需要 3D 请另建独立包，**不要往这里加维度开关**。

---

## 核心设计

### 1. 实体 = 槽位 + 复用代数

```csharp
readonly struct Entity { int Index; uint Generation; }
```

槽位会被回收复用。只存索引的句柄在实体销毁、槽位被新实体占用后会"幽灵指向"新实体；带上 `Generation` 后，旧句柄永远是失效的。

### 2. 组件 = Sparse Set

`ComponentStore<T>` 用 `_sparse[entityIndex] → denseIndex` 做映射，数据紧凑存放：

- 添加 / 删除 O(1)，删除采用 swap-remove；
- `Query` 遍历时直接拿 `Span<T>` 顺序访问；
- 代价是**顺序不稳定**，不要依赖 `Entities` / `Components` 的次序。

### 3. 结构修改 = 显式安全边界

创建/销毁实体、增删组件都会让 `StructuralVersion` 自增。`Query.ForEach` 在每次回调前后校验版本，遍历中改结构会**立即抛 `InvalidOperationException`**，而不是产生隐蔽的越界。

两条合法路径：`world.Commands`（每个系统执行后自动回放）或先 `ToEntityArray()` 取快照。

### 4. 层级变换 = 拓扑序 + 脏标记

`Transform` 只存局部值；`TransformSystem` 按拓扑序把父子矩阵累乘写入 `WorldTransform`。检测同时看脏标记与上一帧快照，因此直接改 `LocalPosition` 也能被发现。没有 `WorldTransform` 时渲染系统退回局部矩阵，扁平场景可以不注册该 System。

### 5. 系统 = 阶段 + 优先级

```csharp
world.AddSystem(system, SystemPhase.Update, priority: -10);
```

同一阶段内按优先级升序、相同优先级按注册顺序执行；执行使用快照数组，运行中增删系统从下一帧生效。每个系统执行完后自动回放命令缓冲，因此下一个系统能看到上一个系统的结构修改。

### 6. 事件 = 双缓冲，不是总线

`World.Events` 按类型分队列：`Raise` 写写缓冲，`Read` / `ReadList` 读读缓冲，`World.Update` 开头交换。第 N 帧登记的事件在第 N+1 帧可读；遍历读缓冲时继续 `Raise` 是安全的。

它**没有**委托订阅、异步、继承分发——那是 `ReluctersteSDK.EventSystem` 的职责，需要跨模块广播时在应用层桥接。

---

## 模块结构

```
ReluctersteSDK.Game/
├─ Ecs/                      纯 C#，不出现 Microsoft.Xna.*
│  ├─ Entity.cs              Entity 句柄
│  ├─ EntityManager.cs       槽位分配与回收
│  ├─ ComponentStore.cs      Sparse Set
│  ├─ World.cs               实体 / 组件 / 单例 / 事件
│  ├─ World.Systems.cs       系统注册与阶段调度
│  ├─ World.Stats.cs         调试统计
│  ├─ Query.cs               1~4 组件查询 + Without
│  ├─ CommandBuffer.cs       结构修改延迟执行
│  ├─ ISystem.cs             ISystem / SystemPhase
│  ├─ HierarchyExtensions.cs SetParent / RemoveParent / 祖先判断
│  ├─ Components/            Transform / Parent / Children / Velocity …
│  ├─ Events/                WorldEvents / EventQueue<T>
│  └─ Serialization/         WorldSnapshot / ComponentCodecRegistry
├─ Systems/                  MotionSystem / TransformSystem
├─ Rendering/                Sprite / SpriteRenderSystem / Camera2D / Animator
├─ EventLogger.cs            与控制台日志（与 ECS 无关）
└─ LogEventAttribute.cs
```

**分层约束**：`Ecs/` 与 `Systems/` 只引用 BCL 与 `ReluctersteSDK.Math`；`Microsoft.Xna.*` 只允许出现在 `Rendering/`。这条约束靠命名空间与代码审阅维持，编译期不强制。

---

## 能力总览

### ECS 核心

| 能力 | 入口 |
|---|---|
| 实体生命周期 | `World.Create` / `Destroy` / `IsAlive` |
| 组件读写 | `World.Add` / `Get` / `TryGet` / `Has` / `Remove` / `Store<T>` |
| 单例组件 | `World.SetSingleton` / `GetSingleton` / `TryGetSingleton` |
| 查询 | `Query<T1>` … `Query<T1,T2,T3,T4>`、`.Without<T>()`、`.Count`、`.ToEntityArray()` |
| 延迟结构修改 | `World.Commands.Create/Destroy/Add/Remove/SetParent/Defer` |
| 生命周期事件 | `EntityCreated` / `EntityDestroyed` / `ComponentAdded` / `ComponentRemoved` / `WorldCleared` |

### 层级与运动

| 能力 | 入口 |
|---|---|
| 父子关系 | `SetParent` / `RemoveParent` / `GetParent` / `GetChildren` / `IsInAncestorChain` |
| 局部/世界变换 | `Transform`、`WorldTransform`、`TransformSystem` |
| 速度/加速度/阻尼/朝向 | `Velocity`、`Acceleration`、`Damping`、`Heading` + `MotionSystem` |

### 渲染与表现

| 能力 | 入口 |
|---|---|
| 精灵 | `Sprite`（Texture / Source / Tint / Layer / Depth / Origin / Effects） |
| 渲染收集与提交 | `SpriteRenderSystem.Execute` / `Submit` |
| 摄像机 | `Camera2D`：`ViewMatrix`、`ViewBounds`、`WorldToScreen` / `ScreenToWorld` |
| 帧动画 | `AnimationClip`、`AnimationFrame`、`Animator`、`SpriteSheet`、`SpriteAnimationSystem` |

### 运行时支撑

| 能力 | 入口 |
|---|---|
| 双缓冲事件 | `World.Events.Raise` / `Read` / `ReadList` |
| 快照 | `WorldSnapshotSerializer.Capture/Restore/Save/Load` + `ComponentCodecRegistry` |
| 调试统计 | `StatisticsEnabled`、`GetStats()`、`SystemTimings`、`GetComponentStats()` |
| 控制台日志 | `EventLogger.Emit` / `LogMarked` |

---

## 典型帧循环

```
World.Update(dt)
├─ StartIfNeeded()            首次调用先跑 Start + Startup
├─ Events.Swap()              本帧可见上一帧登记的事件
├─ Update 阶段                输入 → AI → MotionSystem → 逻辑 → TransformSystem
├─ LateUpdate 阶段            相机跟随、收尾
└─ Commands.Playback()        系统外写入的命令兜底

World.Render(dt)
├─ Render 阶段                SpriteAnimationSystem → SpriteRenderSystem（收集）
└─ Commands.Playback()

spriteBatch.Begin(SpriteSortMode.Deferred, …)
renderSystem.Submit(spriteBatch)
spriteBatch.End()
```

推荐系统顺序：

```
输入/AI → MotionSystem(priority -10) → 其他逻辑 → TransformSystem(priority 10)
        → SpriteAnimationSystem → SpriteRenderSystem
```

---

## 安装与使用

```powershell
# 工程引用
dotnet add package ReluctersteSDK.Game
```

```xml
<PackageReference Include="ReluctersteSDK.Game" Version="0.13.0.0" />
```

只想做数学计算时引用 `ReluctersteSDK.Math`（零 MonoGame 依赖）；需要整套工具时引用聚合包 `ReluctersteSDK`。

**第一次使用请从 [Tutorial/01-快速开始.md](Tutorial/01-快速开始.md) 开始**——那里有一个可以直接运行的 MonoGame 最小工程。

---

## 文档导航

| 文档 | 内容 |
|---|---|
| **[Tutorial/](Tutorial/README.md)** | 按主题组织的完整用法示例，从最小工程到存档、事件、调试 |
| [API/World.md](API/World.md) | `World`、`Query`、`CommandBuffer`、`ISystem` 的成员与语义 |
| [API/Entity.md](API/Entity.md) | `Entity`、`EntityManager`、`ComponentStore<T>` |
| [API/Components.md](API/Components.md) | 内置组件字段与零值语义 |
| [API/Systems.md](API/Systems.md) | `MotionSystem` / `TransformSystem` 的每帧步骤 |
| [API/Rendering.md](API/Rendering.md) | `Sprite`、`RenderCommand`、`SpriteRenderSystem` |
| [API/Camera.md](API/Camera.md) | `Camera2D` 的坐标转换与裁剪 |
| [API/Animation.md](API/Animation.md) | 帧动画类型与 `Tick` 算法 |
| [API/Events.md](API/Events.md) | `WorldEvents` / `EventQueue<T>` 的双缓冲语义 |
| [API/Snapshots.md](API/Snapshots.md) | 快照格式、选项与恢复规则 |
| [API/Diagnostics.md](API/Diagnostics.md) | `WorldStats` / `SystemTiming` / `ComponentStoreStats` |
| [API/EventLogger.md](API/EventLogger.md) | 控制台日志特性 |
| [ECS-设计.md](ECS-设计.md) | 设计稿：分层原则、取舍与落地顺序 |

---

## 设计取舍

| 决策 | 选择 | 理由 |
|---|---|---|
| 组件存储 | Sparse Set，不用 Archetype | 增删 O(1)、遍历连续、实现约 400 行；Archetype 的查询收益在 2D 游戏规模下不足以抵消复杂度 |
| 组件形态 | `struct`，`Children` 例外 | 热路径零装箱；`Children` 需要可变列表，必须是引用类型才能让 `Query` 拿到同一份 |
| `Sprite` | 引用类型 | `Color` 的 `default` 是透明黑，struct 忘记初始化会导致精灵完全不可见 |
| 结构修改 | 命令缓冲 + 版本校验 | 把"什么时候能改结构"从口头约定变成运行时检查 |
| 系统通信 | 双缓冲事件 | 比"单列表 + 快照"少一次分配，且遍历中可继续写入 |
| 与 EventSystem 的关系 | 不依赖 | ECS 事件是帧内数据流；跨模块、异步、委托订阅交给独立总线 |
| 渲染排序 | 托管侧排序 + `Deferred` | 层序完全可控；换其它 `SpriteSortMode` 会被 SpriteBatch 重排 |
| 快照 | 白名单注册 | 避免按 JSON 类型名反射加载任意类型；运行时资源不参与 |

---

## 已知限制

- **非线程安全**：`World`、`Camera2D`、`WorldEvents` 都应在主线程使用。
- **没有空间分区**：摄像机裁剪是保守 AABB；实体很多时仍需自己做四叉树/网格。
- **没有 Job System / 并行系统**：系统串行执行。
- **`Query` 最多 4 个组件**：更多条件请用 `Has` 过滤或自行遍历 `Store<T>()`。
- **`ForEach` 是委托调用**：极热路径可改用 `Store<T>().Entities/Components` 手写循环。
- **Transform 只支持缩放/旋转/平移**：负缩放分解会丢失，翻转请用 `SpriteEffects`；不支持切变。
- **动画没有帧事件与过渡**：需要"第 3 帧触发判定"请轮询 `FrameChanged` + `CurrentFrameIndex`。
- **快照不含 `Sprite` / `Animator` / 纹理 / 系统**：只保存注册过的纯数据组件与单例。
- **事件单帧延迟**：本帧 `Raise` 的事件本帧读不到（Startup 阶段除外）。

---

## 构建与测试

```powershell
dotnet build ReluctersteSDK.Game/ReluctersteSDK.Game.csproj -p:GeneratePackageOnBuild=false
dotnet test ReluctersteSDK.Game.Test/ReluctersteSDK.Game.Test.csproj -p:GeneratePackageOnBuild=false
```

当前基线：**构建 0 错误 / 0 警告**，`ReluctersteSDK.Game.Test` **135 个用例全部通过**。
