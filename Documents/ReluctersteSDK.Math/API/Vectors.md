# API：2D 向量

命名空间：`ReluctersteSDK.Math.Vectors`

| 类型 | 精度 | 构造 | 说明 |
|---|---|---|---|
| `CartesianVector2D` | `float` | `(X, Y)` | 与 MonoGame `Vector2` 的互转移到 Game 包 |
| `PolarVector2D` | `float` | `(Radius, Angle)` | 角度为弧度，构造时自动规范化 |
| `CartesianDoubleVector2D` | `double` | `(X, Y)` | 双精度 |
| `PolarDoubleVector2D` | `double` | `(Radius, Angle)` | 双精度极坐标 |
| `Vector2Int` | `int` | `(X, Y)` | 网格坐标 / 像素对齐 |

这些类型都是 `readonly struct`，所有运算返回新实例。**float 与 double 两套 API 基本对称**（只差类型后缀与转换方法名），下文以 `CartesianVector2D` 为例。

---

## `CartesianVector2D` / `CartesianDoubleVector2D`

### 字段与静态属性

| 成员 | 说明 |
|---|---|
| `X` / `Y` | `readonly` 字段（注意是**字段**，不是属性） |
| `Zero` / `One` / `UnitX` / `UnitY` | 常用常量 |

### 派生量

| 成员 | 说明 |
|---|---|
| `SqrMagnitude` | `X² + Y²`，判距离优先用它（免开方） |
| `Magnitude` | `√SqrMagnitude` |
| `AngleRad` | `Atan2(Y, X)`，范围 `[-π, π]` |
| `AngleDeg` | 同上转角度制 |
| `Normalized` | 单位向量；**零向量返回零向量**（不抛异常、不返回 NaN） |
| `PerpendicularCCW` / `PerpendicularCW` | 逆 / 顺时针 90° 垂直向量 |

### 方法

| 成员 | 返回 | 说明 |
|---|---|---|
| `Dot(other)` | 标量 | 点积 |
| `Cross(other)` | 标量 | 2D 叉积（标量，`X·oY − Y·oX`） |
| `DistanceTo(other)` | 标量 | |
| `SqrDistanceTo(other)` | 标量 | 免开方版本 |
| `AngleTo(other)` | 标量 | **有符号**夹角，`[-π, π]` |
| `Rotate(angleRad)` | 向量 | 逆时针旋转 |
| `Rotate90CCW()` / `Rotate90CW()` | 向量 | 快路径 |
| `ProjectOnto(onto)` | 向量 | 投影；`onto` 为零向量时返回 `Zero` |
| `Reflect(normal)` | 向量 | 反射，**`normal` 必须是单位向量**（不校验） |
| `ToPolar()` | 同精度的极坐标 | float 版 → `PolarVector2D`；double 版 → `PolarDoubleVector2D` |
| `ToDoubleVector2D()` | `CartesianDoubleVector2D` | **仅 float 版** |
| `ToCartesianVector2D()` | `CartesianVector2D` | **仅 double 版**；返回单精度本项目向量 |
| `ClampMagnitude(maxLength)` | 向量 | 限制长度，方向不变 |
| `MoveTowards(target, maxDelta)` | 向量 | 固定步长移动，不越过目标 |
| `RotateTowards(target, maxRadiansDelta)` | 向量 | 固定角步长旋转，保持长度 |
| `RotateAround(pivot, angleRad)` | 向量 | 绕支点旋转 |
| `WithX(x)` / `WithY(y)` | 向量 | 替换单个分量 |

### 静态工具

| 成员 | 说明 |
|---|---|
| `Lerp(a, b, t)` | `t` **clamp 到 [0,1]** |
| `LerpUnclamped(a, b, t)` | 不 clamp，可外插 |
| `Dot(a, b)` / `Cross(a, b)` | 静态转发 |
| `Distance(a, b)` / `SqrDistance(a, b)` | |
| `AngleSigned(from, to)` | 等价于 `from.AngleTo(to)` |
| `Angle(a, b)` | **无符号**夹角 `[0, π]`；任一为零向量时返回 `0` |
| `FromPolar(radius, angleRad)` | 由极坐标分量构造 |
| `FromAngle(angleRad, length = 1f)` | 按角度构造指定长度向量 |
| `Abs(v)` / `Min(a,b)` / `Max(a,b)` / `Clamp(v,min,max)` | 分量级工具 |

### 运算符与相等

支持 `+` `-`（向量间）、`*` `*`（标量，两侧）、`/`（标量）、一元 `-`、`==` `!=`。

```csharp
bool approx = a.ApproximatelyEquals(b);        // 默认 1e-6f（float）/ 1e-8（double）
```

`Equals` 与 `==` 是**精确比较**（`float` 直接比 `==`）。浮点误差场景请用 `ApproximatelyEquals`。

### MonoGame 互转

Math 包不再引用 MonoGame。Game 包提供显式扩展（`ReluctersteSDK.Game.Rendering.MonoGameInterop`）：

```csharp
Vector2 xna = cartesian.ToXna();
CartesianVector2D math = xna.ToCartesian();
```

---

## `PolarVector2D` / `PolarDoubleVector2D`

### 字段

| 成员 | 说明 |
|---|---|
| `Radius` | 半径（规范化后 ≥ 0） |
| `Angle` | 弧度，规范化到 `[0, 2π)` |

### 构造时的规范化

`PolarVector2D(radius, angle)`：

| 输入 | 结果 |
|---|---|
| `radius < 0` | `Radius = -radius`，`Angle += π` |
| `radius == 0` | `Angle = 0`（无论传入什么） |
| 任意 `angle` | 取模到 `[0, 2π)` |

因此 `new PolarVector2D(-5, π/3)` 等价于 `new PolarVector2D(5, π/3 + π)`。

### 派生量与方法

| 成员 | 说明 |
|---|---|
| `X` / `Y` | 换算出的笛卡尔分量（计算属性） |
| `SqrMagnitude` / `Magnitude` | `Radius²` / `Radius` |
| `AngleDegrees` | 角度制 |
| `Direction` | 单位方向向量（`CartesianVector2D`） |
| `ToCartesian()` / `ToVector2Cartesian()` | 转笛卡尔 |
| `Rotate(deltaAngleRad)` | 只转角度 |
| `ScaleRadius(factor)` | 只缩放半径 |
| `WithRadius(r)` / `WithAngle(a)` | 替换单个分量 |
| `AngleTo(other)` | 最短有符号角差 `[-π, π]` |
| `Dot(other)` / `Cross(other)` | 按极坐标公式计算 |
| `Lerp(a, b, t)` | 半径线性插值；**角度走最短路径**，`t` clamp 到 [0,1] |
| `LerpUnclamped(a, b, t)` | 不 clamp 版本 |
| `ShortestAngleDelta(from, to)` | 静态最短角差工具 |
| `ToPolar()` / `ToDoublePolar()` | 变精度 |

> 极坐标**没有** `ProjectOnto`、`Reflect`。

### 运算符

`+` `-` `*` `/` 一元 `-` 都存在，但 `+` / `-` 的实现是**先转笛卡尔算完再转回来**：

```csharp
var a = new PolarVector2D(1, 0);
var b = new PolarVector2D(1, MathF.PI / 2);
(a + b).ToCartesian();     // ≈ (1, 1)
```

结果会重新规范化（半径非负、角度归一）。

---

## 转换关系

```
CartesianVector2D  ⇄  PolarVector2D          （explicit）
CartesianVector2D  ⇄  CartesianDoubleVector2D（显式方法）
Vector2Int         →  CartesianVector2D       （ToCartesianVector2D）
PolarVector2D      ⇄  PolarDoubleVector2D    （显式方法）
CartesianVector2D  ⇄  MonoGame Vector2        （Game 包的显式扩展）
```

- 极坐标 ↔ 笛卡尔是 **`explicit`**，必须写 `(CartesianVector2D)p` 或 `p.ToCartesian()`；
- 精度互转只能走方法（`ToDoubleVector2D()`、`ToCartesianVector2D()` 等），没有运算符。

---

## 已知限制

- **`==` 是精确比较**：`float` 运算后用 `==` 判等很容易失败，请用 `ApproximatelyEquals`。
- **`Reflect` 不校验单位法线**：传非单位向量会得到错误结果，且不会报错。
- **极坐标原地幂等性**：`Rotate` / `WithAngle` 会重新规范化，`Angle` 永远落在 `[0, 2π)`，不要依赖"转过 3π 后角度是 3π"。
- **`X` / `Y` / `Radius` / `Angle` 是字段**，不是属性，不能用于数据绑定或 `nameof` 之外的反射场景（值类型字段，装箱后是副本）。
- **不再提供隐式 MonoGame 转换**：需要 `Vector2` 时用 Game 包的 `ToXna()`；Math 本身零 MonoGame 依赖。
- **`Vector2Int` 只提供整数除法**：`/` 是截断除法，不是浮点除法。
