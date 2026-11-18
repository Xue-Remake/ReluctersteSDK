# API：内置组件

命名空间：`ReluctersteSDK.Game.Ecs`（`Transform2DMath` 在 `ReluctersteSDK.Math.Matrices`）

| 组件 | 字段 | 用途 |
|---|---|---|
| `Transform` | `LocalPosition` / `LocalRotation` / `LocalScale` / `IsDirty` | 局部变换 |
| `WorldTransform` | `Value`（`Matrix3x2`） | 世界矩阵，由 `TransformSystem` 写入 |
| `Parent` | `Value`（`Entity`） | 父节点 |
| `Children` | `Items`（`IReadOnlyList<Entity>`） | 子节点只读列表（**引用类型**） |
| `Velocity` | `Value` | 线速度 |
| `Acceleration` | `Value` | 加速度 |
| `Damping` | `Coefficient` / `MaxSpeed` | 阻尼与限速 |
| `Heading` | `Radians` | 朝向 |

全部是 `struct`（`Children` 除外）。

---

## `Transform`

```csharp
public struct Transform
{
    public CartesianVector2D LocalPosition;
    public float LocalRotation;              // 弧度，正值逆时针
    public CartesianVector2D LocalScale;
    public bool IsDirty;                     // 提示 TransformSystem 重算

    public static Transform Identity { get; }
    public Matrix3x2 ToMatrix();
}
```

### `ToMatrix()`

先缩放、再旋转、最后平移，**行向量约定**：

```csharp
Matrix3x2(
    scale.X * cos,  scale.X * sin,      // M11, M12
   -scale.Y * sin,  scale.Y * cos,      // M21, M22
    LocalPosition.X, LocalPosition.Y);  // M31, M32
```

### 零缩放的坑

`default(Transform)` 的 `LocalScale` 是 `(0, 0)`，直接算矩阵会让物体**彻底不可见**。

`ToMatrix()` 的处理是：`SqrMagnitude > 0 ? LocalScale : CartesianVector2D.One`。也就是说**零缩放按 1 处理**，而不是真的缩到 0。

想真正隐藏物体请改写 `Sprite`（移除组件或换纹理），不要指望零缩放。

### 为什么是 `struct`

ECS 的热路径要求零装箱、连续存放。代价是 `default(Transform)` 不等于 `Transform.Identity`——**新实体请显式 `world.Add(e, Transform.Identity)`**。

---

## `WorldTransform`

```csharp
public struct WorldTransform { public Matrix3x2 Value; }
```

由 `TransformSystem` 按脏标记/快照比对重算并覆盖写入。业务代码只读不写。

```csharp
Vector2 worldPosition = Vector2.Transform(Vector2.Zero, world.Get<WorldTransform>(child).Value);
```

> 只加 `Transform` 不加 `TransformSystem` 时，这个组件**不存在**——`SpriteRenderSystem` 会自动退回用 `transform.ToMatrix()`（扁平场景）。

---

## `Parent` / `Children`

```csharp
public struct Parent { public Entity Value; }

public sealed class Children
{
    public IReadOnlyList<Entity> Items { get; }
    public int Count { get; }
}
```

- `Parent.Value == Entity.Null` 表示无父；
- `Children` 是**引用类型**：必须是 class，否则 `Query` 拿到的 `ref Children` 与存储里的会是两份；
- `Items` 是只读视图，**不能手工修改**；用 `SetParent` / `RemoveParent` / `Destroy` 维护双向一致性。

层级操作在 `HierarchyExtensions`（`ReluctersteSDK.Game.Ecs` 命名空间）：

| 扩展方法 | 返回 | 说明 |
|---|---|---|
| `world.SetParent(Entity child, Entity parent)` | `bool` | 双向维护；自环 / 成环 / 二者不存活 → `false` |
| `world.RemoveParent(Entity child)` | `bool` | 从父的 `Children` 摘除并移除 `Parent` |
| `world.GetParent(Entity child)` | `Entity` | 无父返回 `Entity.Null` |
| `world.GetChildren(Entity parent)` | `IReadOnlyList<Entity>` | 无子返回空列表 |
| `world.IsInAncestorChain(Entity node, Entity candidate)` | `bool` | `candidate` 是否为 `node` 的祖先 |

`SetParent` 的语义：

1. 校验两者存活、`child != parent`、不成环；
2. 若 `child` 已有父，先从旧父的列表摘除；
3. 写入 `child.Parent`，把 `child` 追加到 `parent.Children.Items`。

**一个实体只能有一个父**（单父约束）。`Children` 的顺序是"最后挂上的在最后"。

---

## 运动组件

```csharp
public struct Velocity     { public CartesianVector2D Value; }
public struct Acceleration { public CartesianVector2D Value; }
public struct Damping      { public float Coefficient; public float MaxSpeed; }
public struct Heading      { public float Radians; }
```

这些组件本身**没有任何行为**——只存数据，由 `MotionSystem` 读取。

| 字段 | 约定 |
|---|---|
| `Damping.Coefficient` | `<= 0` 表示**无阻尼**（`default(Damping)` 是空操作） |
| `Damping.MaxSpeed` | `<= 0` 表示**不限速** |
| `Heading.Radians` | `MotionSystem` 在速度非零时写成 `Atan2(vy, vx)` |

`MotionSystem` 只处理**同时持有 `Transform` 与 `Velocity`** 的实体；`Acceleration` / `Damping` / `Heading` 都是可选的。

---

## `Transform2DMath`（已迁到 `ReluctersteSDK.Math.Matrices`）

```csharp
public static void Decompose(Matrix3x2 m, out Vector2 position, out float rotation, out Vector2 scale);
public static Matrix3x2 Compose(Vector2 position, float rotation, Vector2 scale);
```

纯 `System.Numerics` 实现，不依赖 MonoGame（`Vector2` 是 `System.Numerics.Vector2`，**不是** `Microsoft.Xna.Framework.Vector2`）。

### `Decompose` 的算法

```csharp
position = (M31, M32)
rotation = Atan2(M12, M11)                    // 范围 [-π, π]
scale    = (√(M11² + M12²), √(M21² + M22²))   // 恒为非负
```

### 无损条件

只能处理"缩放 + 旋转 + 平移"构成的矩阵：

- **负缩放会丢失**：`(-1, 1)` 分解出来是 `(1, 1)`，水平翻转请用 `SpriteEffects.FlipHorizontally`；
- **切变无法表达**：`M11·M22 − M12·M21` 为负的矩阵会被错误分解。

`Transform.ToMatrix()` 生成的矩阵天然满足无损条件（除非传入负的 `LocalScale`）。

---

## 已知限制

- **`default(Transform)` 与 `Transform.Identity` 不等价**：前者缩放为 0（被当 1 处理），后者显式为一。
- **零缩放无法隐藏物体**：`ToMatrix()` 把零缩放当 1。
- **不支持负缩放 / 切变**：见上。
- **单父约束**：一个实体只能有一个父。
- **`Children.Items` 是只读视图**：不能直接增删；内部修改走 `SetParent/RemoveParent`。
- **组件都是 `struct`**：`world.Get<T>` 返回引用，但 `world.TryGet(e, out T)` 拿到的是**副本**，改了不落回存储。
- **`Damping` / `Heading` 的零值语义偏"空操作"**：默认构造出来什么都不做，这既是便利也是陷阱——忘记设置 `Coefficient` 会以为有阻尼。
