# API：噪声

命名空间：`ReluctersteSDK.Math.Noise`

## `INoise2D`

```csharp
float Sample(float x, float y);
```

## `PerlinNoise2D`

| 成员 | 说明 |
|---|---|
| `PerlinNoise2D(int seed)` | 置换表由 `SeededRandom` 确定性生成 |
| `Sample(x,y)` | 返回值大致在 `[-1,1]` |
| `Fbm(x,y,octaves=4,lacunarity=2,gain=0.5)` | 分形叠加，结果按振幅归一化 |
| `Ridged(x,y,...)` | 脊状分形，适合山脉/裂纹 |

## `ValueNoise2D`

`ValueNoise2D(int seed)`、`Sample(x,y)` 返回 `[0,1]`、`Fbm(...)`。

## `NoiseUtils`

`Fade`、`Lerp`、`GradDot`、`Hash2D`。

## 已知限制

- 同 seed 同坐标结果完全一致；不同 seed 使用不同置换表。
- 不提供 Simplex / Worley 噪声。
- Perlin 输出为近似范围，未做严格归一化。
