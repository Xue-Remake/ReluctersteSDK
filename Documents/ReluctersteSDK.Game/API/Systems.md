# API：内置系统

命名空间：`ReluctersteSDK.Game.Systems`

| 系统 | 阶段 | 作用 |
|---|---|---|
| `MotionSystem` | Update | 把 `Velocity` / `Acceleration` / `Damping` 积分到 `Transform.LocalPosition` |
| `TransformSystem` | Update | 按拓扑序把 `Transform` 逐层累乘成 `WorldTransform` |

两者都是无状态之外的**可复用实例**（内部缓存字段复用），注册一次即可长期使用。

---

## `MotionSystem`

```csharp
public sealed class MotionSystem : ISystem
```

### 每帧步骤

对每个同时持有 `Transform` 与 `Velocity` 的实体：

1. `a = Has<Acceleration> ? Acceleration.Value : Zero`
2. **速度更新**
   - 若 `Has<Damping>` 且 `Coefficient > 0`：
     ```
     若 v.SqrMagnitude < 1e-12 → v = 0            // 防方向抖动
     v += (a − k·v) · dt                          // 一阶阻尼积分
     若 MaxSpeed > 0 且 |v| > MaxSpeed → v = v.Normalized * MaxSpeed
     ```
   - 否则：`v += a · dt`
3. **位置推进**（用更新后的速度）：`LocalPosition += v · dt`
4. **朝向**（若 `Has<Heading>` 且 `v` 非零）：`Heading.Radians = Atan2(v.Y, v.X)`

### 阻尼公式

`v_{n+1} = v_n + (a − k·v_n)·dt`

这是显式欧拉离散。稳定条件大致是 `k·dt < 2`——`k = 6`、`dt = 0.016` 没问题；`k = 500` 配 `dt = 0.05` 会震荡甚至发散。

### 零值语义

| 组件状态 | 效果 |
|---|---|
| 无 `Acceleration` | 加速度视为 0 |
| `Damping` 的 `Coefficient = 0`（含 `default`） | **无阻尼**，速度保持不变（除非有加速度） |
| `Damping` 的 `MaxSpeed <= 0` | **不限速** |
| 无 `Heading` | 不维护朝向 |

所以 `default(Damping)` 是彻底的空操作——加了等于没加。

### 依赖

- 需要 `Transform`（必需）与 `Velocity`（必需）；
- 不读 `WorldTransform`，所以**可以排在 `TransformSystem` 之前**（推荐）。

---

## `TransformSystem`

```csharp
public sealed class TransformSystem : ISystem
```

### 每帧步骤

1. **收集根**：遍历 `Transform` 存储，把"没有可用父节点"的实体列为根。
   可用父 = `Parent` 存在 && 父存活 && 父有 `Transform` && 父不是自己。
2. **DFS 拓扑序**：从根开始深度优先入栈（子节点倒序压栈以保持自然顺序），弹出即访问，得到"父先于子"的顺序。
3. **兜底**：把没被访问到的实体补进队列（有环等异常状态）。
4. **按需累乘**：只重算 `IsDirty`、快照无效、缺少 `WorldTransform`、局部字段变化、父节点变化或父节点本帧重算的实体：
   ```
   local = Transform.ToMatrix()
   if 父有 WorldTransform → local = local * parentWorld    // 行向量：子在前、父在后
   world.Add(entity, new WorldTransform { Value = local })
   transform.IsDirty = false
   ```

### 三条兜底规则

| 情况 | 处理 |
|---|---|
| 父节点已销毁 | 自己当根 |
| 父节点没有 `Transform` | 自己当根（避免整棵子树被漏算） |
| 存在环（不该发生，但被外部破坏了） | 未访问的实体补进队列，仍能算出一个矩阵 |

### 性能

- **脏标记 + 上一帧快照比对**：局部字段被直接修改也能检测到；父节点重算会带动整棵子树；
- 拓扑序缓存：只在 `StructuralVersion` / `HierarchyVersion` 变化时重建；
- 覆盖写入 `WorldTransform` **不再自增 `StructuralVersion`**（`ComponentStore.Add` 只有新建才增长版本）；
- 提供 `FullRebuild()` 强制下一帧全量重算。

### 依赖与顺序

- 需要 `Transform`（必需）；
- 可选 `Parent`、`Children`、`WorldTransform`；
- **必须排在所有修改 `LocalPosition` / `LocalRotation` / `LocalScale` 的系统之后**，否则算的是上一帧的状态。

推荐顺序：

```
输入/AI → MotionSystem → 其他逻辑 → TransformSystem → SpriteAnimationSystem
```

### 与渲染的关系

`SpriteRenderSystem` 优先读 `WorldTransform`；**没有时退回 `transform.ToMatrix()`**。所以：

- 不需要层级的简单场景可以**不注册 `TransformSystem`**，直接渲染局部矩阵；
- 但只要用了 `SetParent`，就必须注册，否则子物体不会跟随。

---

## 已知限制

- **`MotionSystem` 用显式欧拉**：`k·dt` 大时会震荡。
- **`MotionSystem` 不处理旋转驱动**：没有角速度、角加速度组件。
- **`Heading` 只在速度非零时更新**：停下后保持最后朝向（这是期望行为，但注意它不是"永远指向鼠标"）。
- **`TransformSystem` 的脏检测依赖快照比较**：每帧仍会遍历全部带 `Transform` 的实体，只是跳过矩阵运算；超大场景可考虑空间分区。
- **父节点无 `Transform` 时子节点当根**：这是兼容行为，不视为错误。
- **不支持 3D / 倾斜 / 切变**：世界矩阵固定是 `Matrix3x2`。
- **`TransformSystem` 不检测环**：只做兜底，`SetParent` 才是拦环的地方。
