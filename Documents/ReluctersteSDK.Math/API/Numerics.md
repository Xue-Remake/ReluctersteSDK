# API：数值工具

命名空间：`ReluctersteSDK.Math.Numerics`

## `NumericUtils`

### 常量

`Pi`、`TwoPi`、`HalfPi`、`QuarterPi`、`E`、`Deg2Rad`、`Rad2Deg`、`Epsilon`。

### 方法

| 方法 | 说明 |
|---|---|
| `Clamp(value,min,max)` | float / int |
| `Clamp01(value)` | 限制到 `[0,1]` |
| `Lerp` / `InverseLerp` / `Remap` | 线性插值与区间映射 |
| `SmoothStep` / `SmootherStep` | 阶跃平滑 |
| `ApproximatelyEquals` | float / double 容差比较 |
| `IsFinite` | 有限数判断 |
| `Sign` | -1 / 0 / 1 |
| `Repeat` / `PingPong` | 环绕 |
| `WrapAngle` / `WrapAngleDegrees` | 规范到 `[0,2π)` / `[0,360)` |
| `DeltaAngle` | 最短有符号角差 `[-π,π]` |
| `MoveTowards` / `MoveTowardsAngle` | 固定步长 |
| `RoundTo(value, step)` | 按步长四舍五入 |
| `NextPowerOfTwo` / `IsPowerOfTwo` | 2 的幂工具 |

## 已知限制

- `Remap` 在 `inMin == inMax` 时会产生除零；调用方需自行保证输入区间非退化。
- `DeltaAngle` 使用 float 取模，极端大角度可能存在精度损失。
