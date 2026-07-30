using System;
using System.Collections.Generic;
using System.Net.Http.Headers;
using System.Text;

namespace ReluctersteSDK.MathTool.QuickRandom
{
    /// <summary>
    /// 数学分布随机数生成器 —— 基于标准伪随机算法实现常见概率分布的采样。
    /// </summary>
    /// <remarks>
    /// <para>所有方法均通过 <see cref="ThreadLocal{Random}"/> 保证线程安全，但不保证加密级随机性。</para>
    /// </remarks>
    public static class DistributionRandomGen
    {
        // ========== ThreadLocal 保证线程安全 ==========
        private static int _seed = Environment.TickCount;
        private static readonly ThreadLocal<Random> Rng = new ThreadLocal<Random>(() =>
        {
            int seed = Interlocked.Increment(ref _seed);
            return new Random(seed);
        });

        // ---------- 正态分布的备用值缓存（Box-Muller 每次产生两个正态值） ----------
        private static readonly ThreadLocal<double?> CachedNormal = new ThreadLocal<double?>(() => null);

        #region 连续型分布
        /// <summary>
        /// 标准正态分布 N(0, 1)。
        /// 使用 Marsaglia 极坐标法（Box-Muller 变体），避免三角函数调用。
        /// </summary>
        /// <returns>服从 N(0,1) 的随机值</returns>
        public static double NextNormal()
        {
            if (CachedNormal.Value.HasValue)
            {
                double cached = CachedNormal.Value.Value;
                CachedNormal.Value = null;
                return cached;
            }

            double u1, u2, s;
            do
            {
                u1 = 2.0 * Rng.Value.NextDouble() - 1.0;
                u2 = 2.0 * Rng.Value.NextDouble() - 1.0;
                s = u1 * u1 + u2 * u2;
            } while (s >= 1.0 || s <= 0.0);

            double factor = Math.Sqrt(-2.0 * Math.Log(s) / s);
            CachedNormal.Value = u2 * factor;
            return u1 * factor;
        }

        /// <summary>
        /// 正态分布 N(μ, σ²)。
        /// </summary>
        /// <param name="mu">均值 μ</param>
        /// <param name="sigma">标准差 σ，必须 ≥ 0</param>
        /// <returns>服从 N(μ, σ²) 的随机值</returns>
        public static double NextNormal(double mu, double sigma)
        {
            if (sigma < 0)
                throw new ArgumentOutOfRangeException(nameof(sigma), "标准差 σ 不能为负数");
            return mu + sigma * NextNormal();
        }

        /// <summary>
        /// 对数正态分布 LogNormal(μ, σ)。
        /// 若 X ~ N(μ, σ)，则 Y = exp(X) ~ LogNormal(μ, σ)。
        /// </summary>
        /// <param name="mu">对数均值 μ</param>
        /// <param name="sigma">对数标准差 σ，必须 ≥ 0</param>
        /// <returns>服从 LogNormal(μ, σ) 的随机值（恒正）</returns>
        public static double NextLogNormal(double mu, double sigma)
        {
            if (sigma < 0)
                throw new ArgumentOutOfRangeException(nameof(sigma), "σ 不能为负数");
            return Math.Exp(NextNormal(mu, sigma));
        }

        /// <summary>
        /// 指数分布 Exp(λ)，概率密度 f(x) = λ·e^(-λx)，x ≥ 0。
        /// 使用逆变换法：X = -ln(U) / λ。
        /// </summary>
        /// <param name="lambda">速率参数 λ，必须 &gt; 0</param>
        /// <returns>服从 Exp(λ) 的随机值（≥ 0）</returns>
        public static double NextExponential(double lambda)
        {
            if (lambda <= 0)
                throw new ArgumentOutOfRangeException(nameof(lambda), "速率参数 λ 必须 > 0");
            // 使用 1-U 避免 log(0)
            return -Math.Log(1.0 - Rng.Value.NextDouble()) / lambda;
        }

        /// <summary>
        /// 伽马分布 Gamma(α, θ)，形状参数 α、尺度参数 θ。
        /// α ≥ 1 时使用 Marsaglia-Tsang (2000) 法；α &lt; 1 时利用 Gamma(α+1, θ)·U^(1/α)。
        /// </summary>
        /// <param name="alpha">形状参数 α，必须 &gt; 0</param>
        /// <param name="theta">尺度参数 θ，必须 &gt; 0</param>
        /// <returns>服从 Gamma(α, θ) 的随机值（≥ 0）</returns>
        public static double NextGamma(double alpha, double theta)
        {
            if (alpha <= 0)
                throw new ArgumentOutOfRangeException(nameof(alpha), "形状参数 α 必须 > 0");
            if (theta <= 0)
                throw new ArgumentOutOfRangeException(nameof(theta), "尺度参数 θ 必须 > 0");

            if (alpha < 1.0)
            {
                // Gamma(α, θ) = Gamma(α+1, θ) * U^(1/α)
                double u = Rng.Value.NextDouble();
                return NextGamma(alpha + 1.0, theta) * Math.Pow(u, 1.0 / alpha);
            }

            // Marsaglia-Tsang (2000)
            double d = alpha - 1.0 / 3.0;
            double c = 1.0 / Math.Sqrt(9.0 * d);

            while (true)
            {
                double z = NextNormal();
                double v = (1.0 + c * z);
                v = v * v * v; // v = (1 + c*z)^3

                if (v <= 0)
                    continue;

                double u = Rng.Value.NextDouble();
                double z4 = z * z * z * z;

                // 快速接受条件
                if (u < 1.0 - 0.0331 * z4)
                    return theta * d * v;

                // 慢速接受条件
                if (Math.Log(u) < 0.5 * z * z + d * (1.0 - v + Math.Log(v)))
                    return theta * d * v;
            }
        }

        /// <summary>
        /// 贝塔分布 Beta(α, β)。
        /// 若 X ~ Gamma(α, 1), Y ~ Gamma(β, 1)，则 X/(X+Y) ~ Beta(α, β)。
        /// </summary>
        /// <param name="alpha">形状参数 α，必须 &gt; 0</param>
        /// <param name="beta">形状参数 β，必须 &gt; 0</param>
        /// <returns>服从 Beta(α, β) 的随机值（[0, 1]）</returns>
        public static double NextBeta(double alpha, double beta)
        {
            if (alpha <= 0)
                throw new ArgumentOutOfRangeException(nameof(alpha), "α 必须 > 0");
            if (beta <= 0)
                throw new ArgumentOutOfRangeException(nameof(beta), "β 必须 > 0");

            double x = NextGamma(alpha, 1.0);
            double y = NextGamma(beta, 1.0);
            return x / (x + y);
        }

        /// <summary>
        /// 卡方分布 χ²(k)，即 Gamma(k/2, 2)。
        /// </summary>
        /// <param name="k">自由度 k，必须 ≥ 1</param>
        /// <returns>服从 χ²(k) 的随机值（≥ 0）</returns>
        public static double NextChiSquared(int k)
        {
            if (k < 1)
                throw new ArgumentOutOfRangeException(nameof(k), "自由度 k 必须 ≥ 1");
            return NextGamma(k / 2.0, 2.0);
        }

        /// <summary>
        /// Student t 分布 t(k)。
        /// 若 Z ~ N(0,1), V ~ χ²(k)，则 Z / sqrt(V/k) ~ t(k)。
        /// </summary>
        /// <param name="k">自由度 k，必须 ≥ 1</param>
        /// <returns>服从 t(k) 的随机值</returns>
        public static double NextStudentT(int k)
        {
            if (k < 1)
                throw new ArgumentOutOfRangeException(nameof(k), "自由度 k 必须 ≥ 1");
            double z = NextNormal();
            double chi = NextChiSquared(k);
            return z / Math.Sqrt(chi / k);
        }

        /// <summary>
        /// 柯西分布 Cauchy(x₀, γ)，概率密度 f(x) = 1/[πγ(1+((x-x₀)/γ)²)]。
        /// 使用逆变换法：X = x₀ + γ·tan(π·(U-0.5))。
        /// </summary>
        /// <param name="x0">位置参数 x₀</param>
        /// <param name="gamma">尺度参数 γ，必须 &gt; 0</param>
        /// <returns>服从 Cauchy(x₀, γ) 的随机值</returns>
        public static double NextCauchy(double x0, double gamma)
        {
            if (gamma <= 0)
                throw new ArgumentOutOfRangeException(nameof(gamma), "尺度参数 γ 必须 > 0");
            double u = Rng.Value.NextDouble();
            return x0 + gamma * Math.Tan(Math.PI * (u - 0.5));
        }

        /// <summary>
        /// 拉普拉斯分布 Laplace(μ, β)，概率密度 f(x) = 1/(2β)·exp(-|x-μ|/β)。
        /// 使用逆变换法。
        /// </summary>
        /// <param name="mu">位置参数 μ</param>
        /// <param name="beta">尺度参数 β，必须 &gt; 0</param>
        /// <returns>服从 Laplace(μ, β) 的随机值</returns>
        public static double NextLaplace(double mu, double beta)
        {
            if (beta <= 0)
                throw new ArgumentOutOfRangeException(nameof(beta), "尺度参数 β 必须 > 0");
            double u = Rng.Value.NextDouble() - 0.5;
            return mu - beta * Math.Sign(u) * Math.Log(1.0 - 2.0 * Math.Abs(u));
        }

        /// <summary>
        /// 威布尔分布 Weibull(λ, k)，概率密度 f(x) = (k/λ)(x/λ)^(k-1)·exp(-(x/λ)^k)，x ≥ 0。
        /// 使用逆变换法：X = λ·(-ln(U))^(1/k)。
        /// </summary>
        /// <param name="lambda">尺度参数 λ，必须 &gt; 0</param>
        /// <param name="k">形状参数 k，必须 &gt; 0</param>
        /// <returns>服从 Weibull(λ, k) 的随机值（≥ 0）</returns>
        public static double NextWeibull(double lambda, double k)
        {
            if (lambda <= 0)
                throw new ArgumentOutOfRangeException(nameof(lambda), "尺度参数 λ 必须 > 0");
            if (k <= 0)
                throw new ArgumentOutOfRangeException(nameof(k), "形状参数 k 必须 > 0");
            double u = Rng.Value.NextDouble();
            return lambda * Math.Pow(-Math.Log(1.0 - u), 1.0 / k);
        }

        /// <summary>
        /// 帕累托分布 Pareto(xₘ, α)，概率密度 f(x) = α·xₘ^α / x^(α+1)，x ≥ xₘ。
        /// 使用逆变换法：X = xₘ / U^(1/α)。
        /// </summary>
        /// <param name="xm">尺度参数（最小值）xₘ，必须 &gt; 0</param>
        /// <param name="alpha">形状参数 α，必须 &gt; 0</param>
        /// <returns>服从 Pareto(xₘ, α) 的随机值（≥ xₘ）</returns>
        public static double NextPareto(double xm, double alpha)
        {
            if (xm <= 0)
                throw new ArgumentOutOfRangeException(nameof(xm), "尺度参数 xₘ 必须 > 0");
            if (alpha <= 0)
                throw new ArgumentOutOfRangeException(nameof(alpha), "形状参数 α 必须 > 0");
            double u = Rng.Value.NextDouble();
            return xm / Math.Pow(u, 1.0 / alpha);
        }

        /// <summary>
        /// 三角分布 Triangular(a, b, c)，a ≤ c ≤ b。
        /// </summary>
        /// <param name="a">下限 a</param>
        /// <param name="b">上限 b，必须 ≥ a</param>
        /// <param name="c">众数 c，必须在 [a, b] 内</param>
        /// <returns>服从 Triangular(a, b, c) 的随机值</returns>
        public static double NextTriangular(double a, double b, double c)
        {
            if (a > b)
                throw new ArgumentException("a 不能大于 b");
            if (c < a || c > b)
                throw new ArgumentOutOfRangeException(nameof(c), "众数 c 必须在 [a, b] 范围内");

            double u = Rng.Value.NextDouble();
            double fc = (c - a) / (b - a); // 累积概率在 c 处的值

            if (u < fc)
                return a + Math.Sqrt((b - a) * (c - a) * u);
            else
                return b - Math.Sqrt((b - a) * (b - c) * (1.0 - u));
        }

        /// <summary>
        /// [a, b) 上的连续均匀分布。
        /// </summary>
        /// <param name="a">下限 a（包含）</param>
        /// <param name="b">上限 b（不包含），必须 ≥ a</param>
        /// <returns>服从 U(a, b) 的随机值</returns>
        public static double NextUniform(double a, double b)
        {
            if (a > b)
                throw new ArgumentException("a 不能大于 b");
            return a + (b - a) * Rng.Value.NextDouble();
        }
        #endregion

        #region 离散型分布
        /// <summary>
        /// 伯努利分布 Bernoulli(p)。
        /// 以概率 p 返回 1，以概率 1-p 返回 0。
        /// </summary>
        /// <param name="p">成功概率，范围 [0, 1]</param>
        /// <returns>0 或 1</returns>
        public static int NextBernoulli(double p)
        {
            if (p < 0 || p > 1)
                throw new ArgumentOutOfRangeException(nameof(p), "概率 p 必须在 [0, 1] 范围内");
            return Rng.Value.NextDouble() < p ? 1 : 0;
        }

        /// <summary>
        /// 二项分布 Binomial(n, p) —— n 次独立伯努利试验的成功次数。
        /// 小 n 时直接求和；大 n 时用正态近似并限幅。
        /// </summary>
        /// <param name="n">试验次数 n，必须 ≥ 0</param>
        /// <param name="p">每次成功概率，范围 [0, 1]</param>
        /// <returns>成功次数（0 ≤ 返回值 ≤ n）</returns>
        public static int NextBinomial(int n, double p)
        {
            if (n < 0)
                throw new ArgumentOutOfRangeException(nameof(n), "试验次数 n 不能为负数");
            if (p < 0 || p > 1)
                throw new ArgumentOutOfRangeException(nameof(p), "概率 p 必须在 [0, 1] 范围内");

            if (n == 0 || p == 0) return 0;
            if (p == 1) return n;

            // 小 n 或极端 p：直接求和伯努利
            if (n <= 30 || n * p < 5 || n * (1 - p) < 5)
            {
                int count = 0;
                for (int i = 0; i < n; i++)
                    if (Rng.Value.NextDouble() < p)
                        count++;
                return count;
            }

            // 大 n：正态近似 N(np, np(1-p))
            double mu = n * p;
            double sigma = Math.Sqrt(n * p * (1.0 - p));
            int result = (int)Math.Round(NextNormal(mu, sigma), MidpointRounding.AwayFromZero);
            return Math.Max(0, Math.Min(n, result));
        }

        /// <summary>
        /// 泊松分布 Poisson(λ) —— 单位时间内随机事件的发生次数。
        /// λ ≤ 30 时使用 Knuth 法；λ &gt; 30 时使用正态近似。
        /// </summary>
        /// <param name="lambda">均值 λ，必须 ≥ 0</param>
        /// <returns>事件次数（≥ 0）</returns>
        public static int NextPoisson(double lambda)
        {
            if (lambda < 0)
                throw new ArgumentOutOfRangeException(nameof(lambda), "λ 不能为负数");
            if (lambda == 0) return 0;

            // 小 λ：Knuth 法
            if (lambda <= 30)
            {
                double L = Math.Exp(-lambda);
                int k = 0;
                double p = 1.0;
                do
                {
                    k++;
                    p *= Rng.Value.NextDouble();
                } while (p > L);
                return k - 1;
            }

            // 大 λ：正态近似 N(λ, λ)
            int result = (int)Math.Round(NextNormal(lambda, Math.Sqrt(lambda)), MidpointRounding.AwayFromZero);
            return Math.Max(0, result);
        }

        /// <summary>
        /// 几何分布 Geometric(p) —— 首次成功所需的伯努利试验次数（支持 1 到 ∞）。
        /// 使用逆变换法：X = ⌊ln(U) / ln(1-p)⌋ + 1。
        /// </summary>
        /// <param name="p">每次试验的成功概率，范围 (0, 1]</param>
        /// <returns>首次成功时的试验次数（≥ 1）</returns>
        public static int NextGeometric(double p)
        {
            if (p <= 0 || p > 1)
                throw new ArgumentOutOfRangeException(nameof(p), "概率 p 必须在 (0, 1] 范围内");
            double u = Rng.Value.NextDouble();
            return (int)Math.Floor(Math.Log(u) / Math.Log(1.0 - p)) + 1;
        }
        #endregion

        #region 多元分布
        /// <summary>
        /// 狄利克雷分布 Dirichlet(α₁, …, αₖ)。
        /// 若 Xᵢ ~ Gamma(αᵢ, 1) i.i.d.，则 X/ΣX ~ Dirichlet(α)。
        /// </summary>
        /// <param name="alphas">浓度参数数组，每个 αᵢ 必须 &gt; 0</param>
        /// <returns>长度为 k 的随机向量，各分量 &gt; 0 且 ∑ = 1</returns>
        public static double[] NextDirichlet(params double[] alphas)
        {
            if (alphas == null || alphas.Length < 2)
                throw new ArgumentException("alphas 至少需要 2 个元素");
            if (alphas.Any(a => a <= 0))
                throw new ArgumentOutOfRangeException(nameof(alphas), "所有 αᵢ 必须 > 0");

            int k = alphas.Length;
            double[] samples = new double[k];
            double sum = 0;

            for (int i = 0; i < k; i++)
            {
                samples[i] = NextGamma(alphas[i], 1.0);
                sum += samples[i];
            }

            for (int i = 0; i < k; i++)
                samples[i] /= sum;

            return samples;
        }
        #endregion

        #region 批量生成
        /// <summary>
        /// 生成 n 个独立同分布 N(0, 1) 随机数。
        /// </summary>
        public static double[] NextNormalArray(int n)
        {
            if (n < 0) throw new ArgumentOutOfRangeException(nameof(n));
            var result = new double[n];
            for (int i = 0; i < n; i++) result[i] = NextNormal();
            return result;
        }

        /// <summary>
        /// 生成 n 个独立同分布 N(μ, σ²) 随机数。
        /// </summary>
        public static double[] NextNormalArray(double mu, double sigma, int n)
        {
            if (n < 0) throw new ArgumentOutOfRangeException(nameof(n));
            if (sigma < 0) throw new ArgumentOutOfRangeException(nameof(sigma));
            var result = new double[n];
            for (int i = 0; i < n; i++) result[i] = NextNormal(mu, sigma);
            return result;
        }

        /// <summary>
        /// 生成 n 个独立同分布 Exp(λ) 随机数。
        /// </summary>
        public static double[] NextExponentialArray(double lambda, int n)
        {
            if (n < 0) throw new ArgumentOutOfRangeException(nameof(n));
            if (lambda <= 0) throw new ArgumentOutOfRangeException(nameof(lambda));
            var result = new double[n];
            for (int i = 0; i < n; i++) result[i] = NextExponential(lambda);
            return result;
        }

        /// <summary>
        /// 生成 n 个独立同分布 Gamma(α, θ) 随机数。
        /// </summary>
        public static double[] NextGammaArray(double alpha, double theta, int n)
        {
            if (n < 0) throw new ArgumentOutOfRangeException(nameof(n));
            if (alpha <= 0) throw new ArgumentOutOfRangeException(nameof(alpha));
            if (theta <= 0) throw new ArgumentOutOfRangeException(nameof(theta));
            var result = new double[n];
            for (int i = 0; i < n; i++) result[i] = NextGamma(alpha, theta);
            return result;
        }

        /// <summary>
        /// 生成 n 个独立同分布 Beta(α, β) 随机数。
        /// </summary>
        public static double[] NextBetaArray(double alpha, double beta, int n)
        {
            if (n < 0) throw new ArgumentOutOfRangeException(nameof(n));
            if (alpha <= 0) throw new ArgumentOutOfRangeException(nameof(alpha));
            if (beta <= 0) throw new ArgumentOutOfRangeException(nameof(beta));
            var result = new double[n];
            for (int i = 0; i < n; i++) result[i] = NextBeta(alpha, beta);
            return result;
        }

        /// <summary>
        /// 生成 n 个独立同分布 Binomial(nTrial, p) 随机数。
        /// </summary>
        public static int[] NextBinomialArray(int nTrial, double p, int n)
        {
            if (n < 0) throw new ArgumentOutOfRangeException(nameof(n));
            if (nTrial < 0) throw new ArgumentOutOfRangeException(nameof(nTrial));
            if (p < 0 || p > 1) throw new ArgumentOutOfRangeException(nameof(p));
            var result = new int[n];
            for (int i = 0; i < n; i++) result[i] = NextBinomial(nTrial, p);
            return result;
        }

        /// <summary>
        /// 生成 n 个独立同分布 Poisson(λ) 随机数。
        /// </summary>
        public static int[] NextPoissonArray(double lambda, int n)
        {
            if (n < 0) throw new ArgumentOutOfRangeException(nameof(n));
            if (lambda < 0) throw new ArgumentOutOfRangeException(nameof(lambda));
            var result = new int[n];
            for (int i = 0; i < n; i++) result[i] = NextPoisson(lambda);
            return result;
        }
        #endregion
    }
}
