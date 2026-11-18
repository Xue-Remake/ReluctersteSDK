# API：插值与缓动

命名空间：`ReluctersteSDK.Math.Interpolation`

## `EasingMode`

`Linear`、`QuadIn/Out/InOut`、`Cubic...`、`Quart...`、`Quint...`、`Sine...`、`Expo...`、`Circ...`、`Back...`、`Elastic...`、`Bounce...`、`SmoothStep`、`SmootherStep`。

## `Easing`

| 方法 | 说明 |
|---|---|
| `Evaluate(mode, t)` | 先把 `t` clamp 到 `[0,1]`，再计算 |
| `EvaluateUnclamped(mode, t)` | 不 clamp；Back/Elastic/Bounce 可能越界 |
| `SmoothStep(t)` | `3t²-2t³` |
| `SmootherStep(t)` | `6t⁵-15t⁴+10t³` |
| `InverseLerp(a, b, value)` | 反解线性插值并 clamp 到 `[0,1]` |

```csharp
float v = Easing.Evaluate(EasingMode.CubicInOut, 0.5f);
```

## `Interp`

| 方法 | 说明 |
|---|---|
| `Lerp(a,b,t)` / `LerpUnclamped` | 标量 |
| `LerpAngle(aRad,bRad,t)` / `LerpAngleDegrees` | 最短路径 |
| `MoveTowards` / `MoveTowardsAngle` | 固定步长 |
| `Hermite(p0,m0,p1,m1,t)` | Hermite 曲线 |
| `CatmullRom(p0,p1,p2,p3,t)` | Catmull-Rom 样条 |
| `Lerp(CartesianVector2D...)` / `Lerp(CartesianDoubleVector2D...)` | 向量 |

## 已知限制

- 缓动函数是纯函数，不提供带状态的 Tween/动画对象。
- `Evaluate` 会 clamp；需要过冲效果时用 `EvaluateUnclamped`。
- `LerpAngle` 依赖 `NumericUtils.DeltaAngle` 的最短路径约定。
