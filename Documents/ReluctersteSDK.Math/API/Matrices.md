# API：2D 矩阵与随机矩阵

命名空间：`ReluctersteSDK.Math.Matrices`（矩阵） / `ReluctersteSDK.Math.QuickRandom`（随机矩阵）

---

## `Matrix3x2Extensions`

float 精度复用 BCL 的 `System.Numerics.Matrix3x2`，扩展类提供与 `CartesianVector2D` 互操作：

| 方法 | 说明 |
|---|---|
| `CreateTranslation(CartesianVector2D)` | 只含平移 |
| `CreateScale(CartesianVector2D)` | 只含缩放 |
| `CreateRotation(float)` | 只含旋转 |
| `CreateTRS(pos, rotRad, scale)` | 先缩放、再旋转、最后平移 |
| `Decompose(this Matrix3x2, out pos, out rot, out scale)` | 分解为非负缩放 + 旋转 |
| `TransformPoint(this Matrix3x2, CartesianVector2D)` | 含平移 |
| `TransformVector(this Matrix3x2, CartesianVector2D)` | 忽略平移 |
| `TryInvert(this Matrix3x2, out Matrix3x2)` | 奇异矩阵返回 `false` |
| `InvertOrIdentity(this Matrix3x2)` | 奇异矩阵返回单位矩阵 |
| `Lerp(a, b, t)` | 分量插值 |
| `ApproximatelyEquals(a, b, epsilon)` | 分量容差比较 |

```csharp
Matrix3x2 m = Matrix3x2Extensions.CreateTRS(position, rotation, scale);
CartesianVector2D p = m.TransformPoint(point);
```

---

## `Matrix3x2D`

双精度 2D 仿射矩阵，字段布局与 `Matrix3x2` 一致：

`M11 M12 M21 M22 M31 M32`、`Identity`、`Zero`、`Determinant`、
`CreateTranslation/Scale/Rotation/TRS`、`TransformPoint/TransformVector`、
`TryInvert`、`Decompose`、`operator *`、`ToFloatMatrix/FromFloatMatrix`、`ApproximatelyEquals`。

```csharp
var m = Matrix3x2D.CreateTRS(
    new CartesianDoubleVector2D(10, 20),
    0.5,
    new CartesianDoubleVector2D(2, 3));
var p = m.TransformPoint(CartesianDoubleVector2D.Zero);
```

---

## `Transform2DMath`

与 `System.Numerics.Vector2` 的 Compose/Decompose，签名保持兼容：

```csharp
Transform2DMath.Decompose(matrix, out Vector2 position, out float rotation, out Vector2 scale);
Matrix3x2 matrix = Transform2DMath.Compose(position, rotation, scale);
```

> 只支持“缩放 + 旋转 + 平移”；切变或负缩放无法无损分解。

---

## 随机矩阵

`MatrixRandomGen`（返回 `T[,]`）与 `JaggedMatrixRandomGen`（返回 `T[][]`）API 一一对应：

| 方法 | 说明 |
|---|---|
| `NextMatrix<T>(rows, cols, min, max)` / `NextJaggedMatrix<T>(...)` | 均匀随机矩阵 |
| `NextNDArray<T>(min, max, params int[] dimensions)` | 任意维数组 |
| `NextIdentityMatrix<T>(n)` / `NextIdentityMatrix<T>(n, diagMin, diagMax)` | 单位阵 / 对角线随机 |
| `NextAdjacencyMatrix(n, p, directed, allowSelfLoops)` | 邻接矩阵；无向模式对称 |
| `NextBernoulliMatrix(rows, cols, p)` | 0/1 伯努利掩码 |

随机源默认使用 `RandomSource.Shared`；需要复现时通过 `BasicRandomGen.Reseed(seed)` 或 `SeededRandom`。

---

## 已知限制

- `Matrix3x2Extensions.CreateTRS` 使用行向量约定（与 `System.Numerics` 一致）。
- `Decompose` 不处理切变与负缩放；负缩放请用 `SpriteEffects` 表达。
- 随机矩阵的精度规则继承 `BasicRandomGen.Next<T>`；整数上界存在轻微舍入偏差。
- `NextIdentityMatrix<T>(int n)` 的 `T` 无法从参数推断，必须显式写 `<int>`。
