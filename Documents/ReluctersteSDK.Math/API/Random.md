# API：随机数

命名空间：`ReluctersteSDK.Math.QuickRandom`

- [`IRandomSource` / `SeededRandom`](#可复现随机)：可种子化的 PCG32 随机源
- [`DistributionSampler`](#distributionsampler)：绑定随机源的分布采样器
- [`BasicRandomGen`](#basicrandomgen)：均匀随机、概率区间、随机挑选
- [`DistributionRandomGen`](#distributionrandomgen)：18 种概率分布 + 批量版本
- [`RandomSourceExtensions`](#randomsourceextensions)：洗牌、采样、随机向量
- [`ICanbeRandom`](#icanberandom)：标记接口

静态生成器默认使用当前线程的 `RandomSource.Shared`，**非加密级随机**；需要复现时用 `SeededRandom` 或 `DistributionSampler`。

---

## 可复现随机

```csharp
using ReluctersteSDK.Math.QuickRandom;

var rng = new SeededRandom(seed: 12345);

uint a = rng.NextUInt32();
double b = rng.NextDouble();
int c = rng.NextInt(0, 10);           // [0, 10)

// 派生独立子流
SeededRandom fork = rng.Fork();

// 保存/恢复序列
RandomState state = rng.CaptureState();
uint next = rng.NextUInt32();
rng.RestoreState(state);
// 再次 NextUInt32() == next

BasicRandomGen.Reseed(42);            // 重置当前线程默认源
```

`SeededRandom` 自实现 PCG32，保证同一 seed 在不同 .NET 版本上序列一致。`RandomState` 保存 `State` + `Increment`。

### `DistributionSampler`

```csharp
var sampler = new DistributionSampler(new SeededRandom(42));
double normal = sampler.NextNormal(100, 15);
int poisson = sampler.NextPoisson(4.0);
double[] batch = sampler.NextNormalArray(1000);
```

正态分布的备用值缓存属于**实例**，同一实例 + 同一初始状态可完全复现。

### `RandomSourceExtensions`

`NextFloat`、`NextBool(p)`、`NextDouble(min,max)`、`NextUnitVector2`、`NextPointInCircle/OnCircle`、`Shuffle<T>`、`Pick<T>`、`PickWeighted<T>`。

---

## `BasicRandomGen`

| 方法 | 返回 | 说明 |
|---|---|---|
| `Next<T>(T min, T max)` | `T` | `T : struct, IComparable, IConvertible` |
| `NextArray<T>(T min, T max, int n)` | `T[]` | `n < 0` 抛 `ArgumentOutOfRangeException` |
| `NextProbability<T>(T min, T max, T k, double p, double q)` | `T?` | 以 `p` 概率落在 `[min, k)`，以 `q` 概率落在 `[k, max]` |
| `RandomSelectOne<T>(List<T> list)` | `T` | 等概率，`T : ICanbeRandom` |
| `RandomSelectOne<TItem>(List<ICanbeRandom> list, double p)` | `ICanbeRandom` | 以 `p` 优先选 `TItem` 类型 |
| `NextBool()` | `bool` | 50 / 50 |
| `NextBool(bool b, double p)` | `bool` | 以 `p` 返回 `b`，否则 `!b` |
| `NextInt(int min, int max, IRandomSource? source = null)` | `int` | 右开区间，比泛型版本快 |
| `NextFloat(float min, float max, IRandomSource? source = null)` | `float` | 右开区间 |
| `NextDouble(double min, double max, IRandomSource? source = null)` | `double` | 右开区间 |
| `Reseed(int seed)` | `void` | 重置当前线程默认随机源 |

所有方法都接受可选 `IRandomSource`；省略时使用 `RandomSource.Shared`。

### 精度规则

`Next` 走 `decimal`：

1. `scale` = `min` 与 `max` 的小数位数取较大者；
2. 在 `[min, max]` 上生成 `decimal`，按 `scale` 用 `MidpointRounding.AwayFromZero` 舍入；
3. 结果 clamp 回 `[min, max]`；
4. **目标是整型时再 `Math.Floor` 一次**。

因此：

```csharp
BasicRandomGen.Next(1, 100)      // 整数：大多是 1..99；只有舍入到 100 时才出现 100（概率明显偏低）
BasicRandomGen.Next(0f, 1f)      // 浮点：小数位 0，只会得到 0 或 1
BasicRandomGen.Next(0f, 0.1f)    // 小数位 1，得到 0.0 ~ 0.1 的一位小数
```

**精度跟输入的小数位走**。想要更多小数位就传带更多位数的边界值。

### 异常

| 情况 | 行为 |
|---|---|
| `min > max` | 抛 `ArgumentException` |
| `NextProbability` 的 `p`/`q` 为负，或 `p + q ≠ 1`（容差 1e-9） | **返回 `null`** |
| `NextProbability` 的 `k` 不在 `[min, max]` | 抛 `ArgumentException` |
| `RandomSelectOne` 列表为 `null` / 空 | 抛 `ArgumentException` |
| `NextBool(b, p)` 的 `p` 不在 `[0,1]` | 抛 `ArgumentOutOfRangeException` |

### `NextProbability` 的区间划分

```csharp
int v = BasicRandomGen.NextProbability(0, 100, 50, 0.7, 0.3) ?? 0;
```

- 以 `p` 概率：`[min, k - step]`，其中 `step = 10^-scale`（**不含 `k`**）
- 以 `q` 概率：`[k, max]`（**含 `k`**）

`k` 落在第二段。第二段为空（`k` 等于 `max`）时仍可正常返回 `max`。

### `RandomSelectOne<TItem>(list, p)`

列表元素是接口 `ICanbeRandom` 时，用这个重载按**类型**加权：

```csharp
List<ICanbeRandom> pool = new() { new Slime(), new Bat(), new Dragon() };
var picked = BasicRandomGen.RandomSelectOne<Dragon>(pool, 0.3);   // 30% 优先选龙
```

- 列表里没有 `TItem` 类型的元素时，**退化成等概率全表随机**（不抛异常）；
- `p` 超出 `[0,1]` 抛 `ArgumentOutOfRangeException`。

---

## `DistributionRandomGen`

全部静态方法，参数为标准数学符号。

### 连续分布

| 方法 | 参数 |
|---|---|
| `NextNormal()` | 标准正态 N(0,1) |
| `NextNormal(double mu, double sigma)` | 正态 |
| `NextLogNormal(double mu, double sigma)` | 对数正态 |
| `NextExponential(double lambda)` | 指数，`lambda` 为速率 |
| `NextGamma(double alpha, double theta)` | Gamma |
| `NextBeta(double alpha, double beta)` | Beta |
| `NextChiSquared(int k)` | 卡方 |
| `NextStudentT(int k)` | t 分布 |
| `NextCauchy(double x0, double gamma)` | 柯西 |
| `NextLaplace(double mu, double beta)` | 拉普拉斯 |
| `NextWeibull(double lambda, double k)` | 威布尔 |
| `NextPareto(double xm, double alpha)` | 帕累托 |
| `NextTriangular(double a, double b, double c)` | 三角分布，`c` 为众数 |
| `NextUniform(double a, double b)` | 均匀 |

### 离散分布

| 方法 | 返回 | 参数 |
|---|---|---|
| `NextBernoulli(double p)` | `int` | 0 / 1 |
| `NextBinomial(int n, double p)` | `int` | 二项 |
| `NextPoisson(double lambda)` | `int` | 泊松 |
| `NextGeometric(double p)` | `int` | 几何 |
| `NextDirichlet(params double[] alphas)` | `double[]` | 各分量和为 1 |

### 批量

`NextNormalArray(n)`、`NextNormalArray(mu, sigma, n)`、`NextExponentialArray(lambda, n)`、`NextGammaArray(alpha, theta, n)`、`NextBetaArray(alpha, beta, n)`、`NextBinomialArray(nTrial, p, n)`、`NextPoissonArray(lambda, n)`。

### 实现要点

- **正态采样用 Marsaglia 极坐标法**：一次生成两个值，第二个缓存起来留到下次用。静态 API 的缓存是线程静态的；`DistributionSampler` 的缓存属于实例。
- 参数非法（如 `sigma <= 0`、`alpha <= 0`）时由底层数学运算产生 `NaN` / `∞`，**不做参数校验**。

---

## `ICanbeRandom`

```csharp
public interface ICanbeRandom { }
```

纯标记接口，无成员。实现它才能用 `RandomSelectOne<T>(List<T>)` 重载。

---

## 已知限制

- **静态 API 默认不可复现**：默认线程源由系统随机种子初始化；确定性模拟请用 `SeededRandom` / `DistributionSampler`，或先 `Reseed(seed)`。
- **`Next<T>` 走 `decimal` + `Convert.ChangeType`**，性能一般。每帧上万次的热路径请写专用版本。
- **上界语义与 .NET 不同**：`BasicRandomGen.Next(1, 100)` 的两端都"包含"，但受舍入与取整影响，上界出现概率偏低；`.NET Random.Next(1, 100)` 是右开。
- **分布方法不校验参数**：非法参数会静默产生 `NaN` / 无穷大。
- **正态采样有跨调用状态**：静态 API 按线程独立缓存；`DistributionSampler` 按实例缓存。重排调用顺序会改变后续序列。
- **`NextBool()` 内部调用 `Next(0, 2)`**，走的是 decimal 路径，比直接用 `Random.Next(2)` 慢。
