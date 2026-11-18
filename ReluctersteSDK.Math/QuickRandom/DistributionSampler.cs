namespace ReluctersteSDK.Math.QuickRandom
{
    /// <summary>
    /// 绑定到指定 <see cref="IRandomSource"/> 的概率分布采样器。
    /// <para>同一实例 + 同一初始随机状态可完全复现；正态分布的备用值缓存属于实例。</para>
    /// </summary>
    public sealed class DistributionSampler
    {
        private readonly IRandomSource _source;
        private double? _cachedNormal;

        /// <summary>使用指定随机源创建采样器。</summary>
        /// <param name="source">随机源。</param>
        public DistributionSampler(IRandomSource source)
        {
            _source = source ?? throw new ArgumentNullException(nameof(source));
        }

        /// <summary>获取底层随机源。</summary>
        public IRandomSource Source => _source;

        /// <summary>标准正态分布 N(0, 1)。</summary>
        public double NextNormal()
        {
            if (_cachedNormal.HasValue)
            {
                double cached = _cachedNormal.Value;
                _cachedNormal = null;
                return cached;
            }

            double u1, u2, s;
            do
            {
                u1 = 2.0 * _source.NextDouble() - 1.0;
                u2 = 2.0 * _source.NextDouble() - 1.0;
                s = u1 * u1 + u2 * u2;
            } while (s >= 1.0 || s <= 0.0);

            double factor = SystemMath.Sqrt(-2.0 * SystemMath.Log(s) / s);
            _cachedNormal = u2 * factor;
            return u1 * factor;
        }

        /// <summary>正态分布 N(μ, σ²)。</summary>
        public double NextNormal(double mu, double sigma)
        {
            if (sigma < 0)
                throw new ArgumentOutOfRangeException(nameof(sigma), "标准差 σ 不能为负数");
            return mu + sigma * NextNormal();
        }

        /// <summary>对数正态分布 LogNormal(μ, σ)。</summary>
        public double NextLogNormal(double mu, double sigma)
        {
            if (sigma < 0)
                throw new ArgumentOutOfRangeException(nameof(sigma), "σ 不能为负数");
            return SystemMath.Exp(NextNormal(mu, sigma));
        }

        /// <summary>指数分布 Exp(λ)。</summary>
        public double NextExponential(double lambda)
        {
            if (lambda <= 0)
                throw new ArgumentOutOfRangeException(nameof(lambda), "速率参数 λ 必须 > 0");
            return -SystemMath.Log(1.0 - _source.NextDouble()) / lambda;
        }

        /// <summary>伽马分布 Gamma(α, θ)。</summary>
        public double NextGamma(double alpha, double theta)
        {
            if (alpha <= 0)
                throw new ArgumentOutOfRangeException(nameof(alpha), "形状参数 α 必须 > 0");
            if (theta <= 0)
                throw new ArgumentOutOfRangeException(nameof(theta), "尺度参数 θ 必须 > 0");

            if (alpha < 1.0)
            {
                double u = _source.NextDouble();
                return NextGamma(alpha + 1.0, theta) * SystemMath.Pow(u, 1.0 / alpha);
            }

            double d = alpha - 1.0 / 3.0;
            double c = 1.0 / SystemMath.Sqrt(9.0 * d);

            while (true)
            {
                double z = NextNormal();
                double v = 1.0 + c * z;
                v = v * v * v;

                if (v <= 0)
                    continue;

                double u = _source.NextDouble();
                double z4 = z * z * z * z;

                if (u < 1.0 - 0.0331 * z4)
                    return theta * d * v;

                if (SystemMath.Log(u) < 0.5 * z * z + d * (1.0 - v + SystemMath.Log(v)))
                    return theta * d * v;
            }
        }

        /// <summary>贝塔分布 Beta(α, β)。</summary>
        public double NextBeta(double alpha, double beta)
        {
            if (alpha <= 0)
                throw new ArgumentOutOfRangeException(nameof(alpha), "α 必须 > 0");
            if (beta <= 0)
                throw new ArgumentOutOfRangeException(nameof(beta), "β 必须 > 0");

            double x = NextGamma(alpha, 1.0);
            double y = NextGamma(beta, 1.0);
            return x / (x + y);
        }

        /// <summary>卡方分布 χ²(k)。</summary>
        public double NextChiSquared(int k)
        {
            if (k < 1)
                throw new ArgumentOutOfRangeException(nameof(k), "自由度 k 必须 ≥ 1");
            return NextGamma(k / 2.0, 2.0);
        }

        /// <summary>Student t 分布 t(k)。</summary>
        public double NextStudentT(int k)
        {
            if (k < 1)
                throw new ArgumentOutOfRangeException(nameof(k), "自由度 k 必须 ≥ 1");
            double z = NextNormal();
            double chi = NextChiSquared(k);
            return z / SystemMath.Sqrt(chi / k);
        }

        /// <summary>柯西分布 Cauchy(x₀, γ)。</summary>
        public double NextCauchy(double x0, double gamma)
        {
            if (gamma <= 0)
                throw new ArgumentOutOfRangeException(nameof(gamma), "尺度参数 γ 必须 > 0");
            double u = _source.NextDouble();
            return x0 + gamma * SystemMath.Tan(SystemMath.PI * (u - 0.5));
        }

        /// <summary>拉普拉斯分布 Laplace(μ, β)。</summary>
        public double NextLaplace(double mu, double beta)
        {
            if (beta <= 0)
                throw new ArgumentOutOfRangeException(nameof(beta), "尺度参数 β 必须 > 0");
            double u = _source.NextDouble() - 0.5;
            return mu - beta * SystemMath.Sign(u) * SystemMath.Log(1.0 - 2.0 * SystemMath.Abs(u));
        }

        /// <summary>威布尔分布 Weibull(λ, k)。</summary>
        public double NextWeibull(double lambda, double k)
        {
            if (lambda <= 0)
                throw new ArgumentOutOfRangeException(nameof(lambda), "尺度参数 λ 必须 > 0");
            if (k <= 0)
                throw new ArgumentOutOfRangeException(nameof(k), "形状参数 k 必须 > 0");
            double u = _source.NextDouble();
            return lambda * SystemMath.Pow(-SystemMath.Log(1.0 - u), 1.0 / k);
        }

        /// <summary>帕累托分布 Pareto(xₘ, α)。</summary>
        public double NextPareto(double xm, double alpha)
        {
            if (xm <= 0)
                throw new ArgumentOutOfRangeException(nameof(xm), "尺度参数 xₘ 必须 > 0");
            if (alpha <= 0)
                throw new ArgumentOutOfRangeException(nameof(alpha), "形状参数 α 必须 > 0");
            double u = _source.NextDouble();
            return xm / SystemMath.Pow(u, 1.0 / alpha);
        }

        /// <summary>三角分布 Triangular(a, b, c)。</summary>
        public double NextTriangular(double a, double b, double c)
        {
            if (a > b)
                throw new ArgumentException("a 不能大于 b");
            if (c < a || c > b)
                throw new ArgumentOutOfRangeException(nameof(c), "众数 c 必须在 [a, b] 范围内");

            double u = _source.NextDouble();
            double fc = (c - a) / (b - a);
            return u < fc
                ? a + SystemMath.Sqrt((b - a) * (c - a) * u)
                : b - SystemMath.Sqrt((b - a) * (b - c) * (1.0 - u));
        }

        /// <summary>[a, b) 上的连续均匀分布。</summary>
        public double NextUniform(double a, double b)
        {
            if (a > b)
                throw new ArgumentException("a 不能大于 b");
            return a + (b - a) * _source.NextDouble();
        }

        /// <summary>伯努利分布 Bernoulli(p)。</summary>
        public int NextBernoulli(double p)
        {
            if (p < 0 || p > 1)
                throw new ArgumentOutOfRangeException(nameof(p), "概率 p 必须在 [0, 1] 范围内");
            return _source.NextDouble() < p ? 1 : 0;
        }

        /// <summary>二项分布 Binomial(n, p)。</summary>
        public int NextBinomial(int n, double p)
        {
            if (n < 0)
                throw new ArgumentOutOfRangeException(nameof(n), "试验次数 n 不能为负数");
            if (p < 0 || p > 1)
                throw new ArgumentOutOfRangeException(nameof(p), "概率 p 必须在 [0, 1] 范围内");

            if (n == 0 || p == 0) return 0;
            if (p == 1) return n;

            if (n <= 30 || n * p < 5 || n * (1 - p) < 5)
            {
                int count = 0;
                for (int i = 0; i < n; i++)
                    if (_source.NextDouble() < p)
                        count++;
                return count;
            }

            double mu = n * p;
            double sigma = SystemMath.Sqrt(n * p * (1.0 - p));
            int result = (int)SystemMath.Round(NextNormal(mu, sigma), MidpointRounding.AwayFromZero);
            return SystemMath.Max(0, SystemMath.Min(n, result));
        }

        /// <summary>泊松分布 Poisson(λ)。</summary>
        public int NextPoisson(double lambda)
        {
            if (lambda < 0)
                throw new ArgumentOutOfRangeException(nameof(lambda), "λ 不能为负数");
            if (lambda == 0) return 0;

            if (lambda <= 30)
            {
                double l = SystemMath.Exp(-lambda);
                int k = 0;
                double p = 1.0;
                do
                {
                    k++;
                    p *= _source.NextDouble();
                } while (p > l);
                return k - 1;
            }

            int result = (int)SystemMath.Round(NextNormal(lambda, SystemMath.Sqrt(lambda)), MidpointRounding.AwayFromZero);
            return SystemMath.Max(0, result);
        }

        /// <summary>几何分布 Geometric(p)。</summary>
        public int NextGeometric(double p)
        {
            if (p <= 0 || p > 1)
                throw new ArgumentOutOfRangeException(nameof(p), "概率 p 必须在 (0, 1] 范围内");
            double u = _source.NextDouble();
            return (int)SystemMath.Floor(SystemMath.Log(u) / SystemMath.Log(1.0 - p)) + 1;
        }

        /// <summary>狄利克雷分布 Dirichlet(α₁, …, αₖ)。</summary>
        public double[] NextDirichlet(params double[] alphas)
        {
            if (alphas == null || alphas.Length < 2)
                throw new ArgumentException("alphas 至少需要 2 个元素");
            if (alphas.Any(a => a <= 0))
                throw new ArgumentOutOfRangeException(nameof(alphas), "所有 αᵢ 必须 > 0");

            int k = alphas.Length;
            double[] samples = new double[k];
            double sum = 0.0;

            for (int i = 0; i < k; i++)
            {
                samples[i] = NextGamma(alphas[i], 1.0);
                sum += samples[i];
            }

            for (int i = 0; i < k; i++)
                samples[i] /= sum;

            return samples;
        }

        /// <summary>生成 n 个独立同分布 N(0, 1) 随机数。</summary>
        public double[] NextNormalArray(int n)
        {
            if (n < 0) throw new ArgumentOutOfRangeException(nameof(n));
            var result = new double[n];
            for (int i = 0; i < n; i++) result[i] = NextNormal();
            return result;
        }

        /// <summary>生成 n 个独立同分布 N(μ, σ²) 随机数。</summary>
        public double[] NextNormalArray(double mu, double sigma, int n)
        {
            if (n < 0) throw new ArgumentOutOfRangeException(nameof(n));
            if (sigma < 0) throw new ArgumentOutOfRangeException(nameof(sigma));
            var result = new double[n];
            for (int i = 0; i < n; i++) result[i] = NextNormal(mu, sigma);
            return result;
        }

        /// <summary>生成 n 个独立同分布 Exp(λ) 随机数。</summary>
        public double[] NextExponentialArray(double lambda, int n)
        {
            if (n < 0) throw new ArgumentOutOfRangeException(nameof(n));
            if (lambda <= 0) throw new ArgumentOutOfRangeException(nameof(lambda));
            var result = new double[n];
            for (int i = 0; i < n; i++) result[i] = NextExponential(lambda);
            return result;
        }

        /// <summary>生成 n 个独立同分布 Gamma(α, θ) 随机数。</summary>
        public double[] NextGammaArray(double alpha, double theta, int n)
        {
            if (n < 0) throw new ArgumentOutOfRangeException(nameof(n));
            if (alpha <= 0) throw new ArgumentOutOfRangeException(nameof(alpha));
            if (theta <= 0) throw new ArgumentOutOfRangeException(nameof(theta));
            var result = new double[n];
            for (int i = 0; i < n; i++) result[i] = NextGamma(alpha, theta);
            return result;
        }

        /// <summary>生成 n 个独立同分布 Beta(α, β) 随机数。</summary>
        public double[] NextBetaArray(double alpha, double beta, int n)
        {
            if (n < 0) throw new ArgumentOutOfRangeException(nameof(n));
            if (alpha <= 0) throw new ArgumentOutOfRangeException(nameof(alpha));
            if (beta <= 0) throw new ArgumentOutOfRangeException(nameof(beta));
            var result = new double[n];
            for (int i = 0; i < n; i++) result[i] = NextBeta(alpha, beta);
            return result;
        }

        /// <summary>生成 n 个独立同分布 Binomial(nTrial, p) 随机数。</summary>
        public int[] NextBinomialArray(int nTrial, double p, int n)
        {
            if (n < 0) throw new ArgumentOutOfRangeException(nameof(n));
            if (nTrial < 0) throw new ArgumentOutOfRangeException(nameof(nTrial));
            if (p < 0 || p > 1) throw new ArgumentOutOfRangeException(nameof(p));
            var result = new int[n];
            for (int i = 0; i < n; i++) result[i] = NextBinomial(nTrial, p);
            return result;
        }

        /// <summary>生成 n 个独立同分布 Poisson(λ) 随机数。</summary>
        public int[] NextPoissonArray(double lambda, int n)
        {
            if (n < 0) throw new ArgumentOutOfRangeException(nameof(n));
            if (lambda < 0) throw new ArgumentOutOfRangeException(nameof(lambda));
            var result = new int[n];
            for (int i = 0; i < n; i++) result[i] = NextPoisson(lambda);
            return result;
        }
    }
}
