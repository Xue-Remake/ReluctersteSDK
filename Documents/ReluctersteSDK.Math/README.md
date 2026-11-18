# ReluctersteSDK.Math

零 MonoGame 依赖的 **2D 实用数学库**：向量、2D 矩阵、几何体、插值与缓动、数值工具、可种子随机数、噪声。

- 目录：`ReluctersteSDK.Math/`
- 包 ID：`ReluctersteSDK.Math`
- 命名空间：`ReluctersteSDK.Math.Vectors` / `.Matrices` / `.Geometry` / `.Interpolation` / `.Numerics` / `.QuickRandom` / `.Noise`
- 依赖：**无**（仅 BCL）

> `ReluctersteSDK.Math` 会与 `System.Math` 同名。库内部统一使用 `SystemMath` 别名；使用方在 `ReluctersteSDK.*` 命名空间内写代码时，请用 `System.Math` / `MathF`，不要裸写 `Math.`。

---

## 场景一：2D 向量

```csharp
using ReluctersteSDK.Math.Vectors;

var pos = new CartesianVector2D(3, 4);
var vel = new CartesianVector2D(1, 0);

pos.Magnitude;                  // 5
pos.SqrMagnitude;               // 25
pos.Dot(vel);                   // 3
pos.Rotate(MathF.PI / 2);       // 逆时针 90°
pos.ClampMagnitude(2f);         // 长度限制到 2
pos.MoveTowards(target, 0.5f);  // 固定步长移动
```

- 所有向量都是 `readonly struct`，运算返回新实例。
- `Lerp` 会 clamp `t` 到 `[0,1]`；外插用 `LerpUnclamped`。
- float / double × 笛卡尔 / 极坐标四种表示；极坐标构造时自动规范化。
- `Vector2Int` 用于网格坐标与像素对齐。

```csharp
using ReluctersteSDK.Math.Vectors;

Vector2Int cell = Vector2Int.FromCartesian(new CartesianVector2D(3.6f, -2.4f));
int manhattan = cell.LengthManhattan;   // 6
```

---

## 场景二：2D 矩阵与变换

```csharp
using System.Numerics;
using ReluctersteSDK.Math.Matrices;
using ReluctersteSDK.Math.Vectors;

Matrix3x2 matrix = Matrix3x2Extensions.CreateTRS(
    new CartesianVector2D(10, 20),
    rotationRad: 0.75f,
    new CartesianVector2D(2, 3));

matrix.Decompose(out CartesianVector2D position, out float rotation, out CartesianVector2D scale);
CartesianVector2D point = matrix.TransformPoint(new CartesianVector2D(1, 0));
```

- float 版本复用 BCL 的 `System.Numerics.Matrix3x2`。
- 需要双精度时用 `Matrix3x2D`；支持乘法、求逆、分解与 float 互转。
- `Transform2DMath` 提供与 `Vector2` 的 Compose/Decompose。

---

## 场景三：2D 几何体

```csharp
using ReluctersteSDK.Math.Geometry;
using ReluctersteSDK.Math.Vectors;

var box = new Aabb2D(0, 0, 100, 50);
var circle = new Circle2D(new CartesianVector2D(20, 20), 10f);

bool hit = box.Intersects(circle);
CartesianVector2D closest = box.ClosestPoint(new CartesianVector2D(120, 25));

var segment = new Segment2D(new CartesianVector2D(0, 0), new CartesianVector2D(100, 100));
bool cross = segment.Intersects(new Segment2D(new CartesianVector2D(0, 100), new CartesianVector2D(100, 0)), out var point);

var ray = new Ray2D(CartesianVector2D.Zero, CartesianVector2D.UnitX);
bool rayHit = ray.Intersects(circle, out float distance);

var polygon = new Polygon2D(new[]
{
    new CartesianVector2D(0, 0),
    new CartesianVector2D(10, 0),
    new CartesianVector2D(10, 10),
    new CartesianVector2D(0, 10)
});
bool inside = polygon.Contains(new CartesianVector2D(5, 5));
```

提供 `Aabb2D`、`Circle2D`、`Segment2D`、`Ray2D`、`Triangle2D`、`Polygon2D` 与 `Geometry2D` 静态助手。退化输入返回 `false`/零值，不抛异常。

---

## 场景四：插值、缓动、数值工具

```csharp
using ReluctersteSDK.Math.Interpolation;
using ReluctersteSDK.Math.Numerics;

float t = Interp.Lerp(0f, 1f, 0.3f);
float angle = Interp.LerpAngle(0f, MathF.PI * 1.9f, 0.5f);   // 最短路径
float eased = Easing.Evaluate(EasingMode.CubicInOut, t);

float wrapped = NumericUtils.WrapAngle(MathF.PI * 2.5f);
float delta = NumericUtils.DeltaAngle(0f, MathF.PI / 2);
```

- `EasingMode` 覆盖 Linear / Quad / Cubic / Quart / Quint / Sine / Expo / Circ / Back / Elastic / Bounce / SmoothStep。
- `Interp` 提供标量、向量、角度、Hermite、Catmull-Rom。
- `NumericUtils` 提供 Clamp、Remap、Repeat、PingPong、角度规范化、2 的幂等。

---

## 场景五：可复现随机

```csharp
using ReluctersteSDK.Math.QuickRandom;

var rng = new SeededRandom(seed: 12345);

int roll = rng.NextInt(1, 7);          // [1, 7)
double uniform = rng.NextDouble();
float angle = rng.NextFloat() * MathF.PI * 2f;

// 绑定到分布采样器，序列可完全复现
var sampler = new DistributionSampler(rng);
double normal = sampler.NextNormal(mu: 100, sigma: 15);
int loot = sampler.NextPoisson(4.0);

// 洗牌 / 按权重选择
rng.Shuffle(list);
var picked = rng.PickWeighted(items, weights);

// 需要暂停/恢复序列时
RandomState state = rng.CaptureState();
rng.RestoreState(state);
```

`SeededRandom` 使用自实现 **PCG32**，同一 seed 在不同 .NET 版本上产生相同序列。默认静态 API（`BasicRandomGen` / `DistributionRandomGen`）使用当前线程的 `RandomSource.Shared`，可用 `BasicRandomGen.Reseed(seed)` 重置。

---

## 场景六：噪声

```csharp
using ReluctersteSDK.Math.Noise;

var perlin = new PerlinNoise2D(seed: 42);
float height = perlin.Sample(x: 12.3f, y: -4.5f);          // 约 [-1, 1]
float terrain = perlin.Fbm(x: 12.3f, y: -4.5f, octaves: 5);

var value = new ValueNoise2D(seed: 42);
float mask = value.Sample(12.3f, -4.5f);                   // [0, 1]
```

同一 seed 与坐标产生完全一致的结果；置换表由 `SeededRandom` 生成。

---

## 场景七：和 MonoGame 互转

Math 库不再引用 MonoGame。Game 包提供显式扩展：

```csharp
using Microsoft.Xna.Framework;
using ReluctersteSDK.Game.Rendering;
using ReluctersteSDK.Math.Vectors;

CartesianVector2D mathVector = xnaVector.ToCartesian();
Vector2 xna = mathVector.ToXna();
```

---

## API 文档

| 文档 | 内容 |
|---|---|
| [API/Vectors.md](API/Vectors.md) | 四种向量类型、`Vector2Int`、运算、转换、精度差异 |
| [API/Matrices.md](API/Matrices.md) | `Matrix3x2Extensions`、`Matrix3x2D`、`Transform2DMath` |
| [API/Geometry.md](API/Geometry.md) | `Aabb2D`、`Circle2D`、`Segment2D`、`Ray2D`、`Triangle2D`、`Polygon2D`、`Geometry2D` |
| [API/Interpolation.md](API/Interpolation.md) | `EasingMode`、`Easing`、`Interp` |
| [API/Numerics.md](API/Numerics.md) | `NumericUtils` |
| [API/Random.md](API/Random.md) | `IRandomSource`、`SeededRandom`、`RandomState`、`DistributionSampler`、静态生成器 |
| [API/Noise.md](API/Noise.md) | `INoise2D`、`PerlinNoise2D`、`ValueNoise2D`、`NoiseUtils` |
