namespace ReluctersteSDK.Math.QuickRandom
{
    /// <summary>
    /// 基础随机数生成工具，用于生成单条随机数据。
    /// <para>所有方法都接受可选 <see cref="IRandomSource"/>；省略时使用当前线程的默认随机源。</para>
    /// </summary>
    public static class BasicRandomGen
    {
        /// <summary>
        /// 生成一个在 [min, max] 范围内的随机数，自动匹配输入值的最高小数精度，并返回指定类型 T 的值。
        /// 若 T 为整数类型，生成的浮点值会向下取整。
        /// </summary>
        /// <typeparam name="T">返回类型（值类型，如 int、float、double、decimal 等）</typeparam>
        /// <param name="min">下限（包含）</param>
        /// <param name="max">上限（包含）</param>
        /// <param name="source">可选随机源。</param>
        /// <returns>随机数</returns>
        public static T Next<T>(T min, T max, IRandomSource? source = null) where T : struct, IComparable, IConvertible
        {
            decimal dMin = Convert.ToDecimal(min);
            decimal dMax = Convert.ToDecimal(max);
            if (dMin > dMax)
                throw new ArgumentException("min 不能大于 max");

            int scale = SystemMath.Max(GetDecimalScale(dMin), GetDecimalScale(dMax));
            decimal randomValue = GenerateRandomDecimal(dMin, dMax, scale, source ?? RandomSource.Shared);
            return ConvertTo<T>(randomValue);
        }

        /// <summary>
        /// 生成包含 n 个随机数的数组，每个随机数的生成规则与 <see cref="Next{T}"/> 相同。
        /// </summary>
        /// <typeparam name="T">元素类型</typeparam>
        /// <param name="min">下限</param>
        /// <param name="max">上限</param>
        /// <param name="n">数组长度</param>
        /// <param name="source">可选随机源。</param>
        public static T[] NextArray<T>(T min, T max, int n, IRandomSource? source = null)
            where T : struct, IComparable, IConvertible
        {
            if (n < 0)
                throw new ArgumentOutOfRangeException(nameof(n), "数组长度不能为负数");

            IRandomSource rng = source ?? RandomSource.Shared;
            var result = new T[n];
            for (int i = 0; i < n; i++)
                result[i] = Next(min, max, rng);
            return result;
        }

        /// <summary>
        /// 根据概率 p 和 q (p+q=1) 生成随机数：以概率 p 返回 [min, k) 中的值，以概率 q 返回 [k, max] 中的值。
        /// 精度与输入值的最高小数位一致。若 p+q≠1 或 p、q 小于 0，返回 null。
        /// </summary>
        /// <typeparam name="T">返回类型（值类型）</typeparam>
        /// <param name="min">下限（包含）</param>
        /// <param name="max">上限（包含）</param>
        /// <param name="k">分割点</param>
        /// <param name="p">值小于 k 的概率</param>
        /// <param name="q">值大于等于 k 的概率</param>
        /// <param name="source">可选随机源。</param>
        public static T? NextProbability<T>(T min, T max, T k, double p, double q, IRandomSource? source = null)
            where T : struct, IComparable, IConvertible
        {
            if (p < 0 || q < 0 || SystemMath.Abs(p + q - 1.0) > 1e-9)
                return null;

            decimal dMin = Convert.ToDecimal(min);
            decimal dMax = Convert.ToDecimal(max);
            decimal dK = Convert.ToDecimal(k);

            if (dMin > dMax || dK < dMin || dK > dMax)
                throw new ArgumentException("min, max, k 必须满足 min ≤ k ≤ max");

            int scale = SystemMath.Max(SystemMath.Max(GetDecimalScale(dMin), GetDecimalScale(dMax)), GetDecimalScale(dK));
            IRandomSource rng = source ?? RandomSource.Shared;

            if (rng.NextDouble() < p)
            {
                decimal step = (decimal)SystemMath.Pow(10, -scale);
                decimal upper = dK - step;
                if (upper < dMin) upper = dMin;
                return ConvertTo<T>(GenerateRandomDecimal(dMin, upper, scale, rng));
            }

            return ConvertTo<T>(GenerateRandomDecimal(dK, dMax, scale, rng));
        }

        /// <summary>从列表中等概率随机选择一项。</summary>
        /// <typeparam name="T">实现了 <see cref="ICanbeRandom"/> 的类型</typeparam>
        /// <param name="list">待选择的列表</param>
        /// <param name="source">可选随机源。</param>
        public static T RandomSelectOne<T>(List<T> list, IRandomSource? source = null) where T : ICanbeRandom
        {
            if (list == null || list.Count == 0)
                throw new ArgumentException("列表不能为 null 或空");

            return list[(source ?? RandomSource.Shared).NextInt(0, list.Count)];
        }

        /// <summary>
        /// 从列表中随机选择一项，以概率 p 选中类型为 TItem 的项。
        /// 若列表中不存在 TItem 类型的项，则退化到全随机等概率。
        /// </summary>
        /// <typeparam name="TItem">目标类型，实现了 <see cref="ICanbeRandom"/></typeparam>
        /// <param name="list">待选择的列表</param>
        /// <param name="p">选中 TItem 类型项的概率，范围 [0, 1]</param>
        /// <param name="source">可选随机源。</param>
        public static ICanbeRandom RandomSelectOne<TItem>(List<ICanbeRandom> list, double p, IRandomSource? source = null)
            where TItem : ICanbeRandom
        {
            if (list == null || list.Count == 0)
                throw new ArgumentException("列表不能为 null 或空");
            if (p < 0 || p > 1)
                throw new ArgumentOutOfRangeException(nameof(p), "概率 p 必须在 [0, 1] 范围内");

            IRandomSource rng = source ?? RandomSource.Shared;
            var typedItems = list.OfType<TItem>().ToList();
            if (typedItems.Count == 0)
                return list[rng.NextInt(0, list.Count)];

            return rng.NextDouble() < p
                ? typedItems[rng.NextInt(0, typedItems.Count)]
                : list[rng.NextInt(0, list.Count)];
        }

        /// <summary>等概率返回一个随机布尔值。</summary>
        /// <param name="source">可选随机源。</param>
        public static bool NextBool(IRandomSource? source = null)
            => (source ?? RandomSource.Shared).NextInt(0, 2) == 1;

        /// <summary>
        /// 以指定概率返回指定的布尔值：以概率 <paramref name="p"/> 返回 <paramref name="b"/>，
        /// 以概率 1-p 返回 !b。
        /// </summary>
        /// <param name="b">期望返回的布尔值</param>
        /// <param name="p">返回 <paramref name="b"/> 的概率，范围 [0, 1]</param>
        /// <param name="source">可选随机源。</param>
        public static bool NextBool(bool b, double p, IRandomSource? source = null)
        {
            if (p < 0 || p > 1)
                throw new ArgumentOutOfRangeException(nameof(p), "概率 p 必须在 [0, 1] 范围内");

            return (source ?? RandomSource.Shared).NextDouble() < p ? b : !b;
        }

        /// <summary>生成 [minInclusive, maxExclusive) 之间的整数；比泛型版本更快。</summary>
        /// <param name="minInclusive">包含的下界。</param>
        /// <param name="maxExclusive">不包含的上界。</param>
        /// <param name="source">可选随机源。</param>
        public static int NextInt(int minInclusive, int maxExclusive, IRandomSource? source = null)
            => (source ?? RandomSource.Shared).NextInt(minInclusive, maxExclusive);

        /// <summary>生成 [min, max) 之间的单精度随机数。</summary>
        /// <param name="min">下界（包含）。</param>
        /// <param name="max">上界（不包含）。</param>
        /// <param name="source">可选随机源。</param>
        public static float NextFloat(float min, float max, IRandomSource? source = null)
        {
            if (max < min) throw new ArgumentOutOfRangeException(nameof(max), "上界不能小于下界。");
            return min + (max - min) * (float)(source ?? RandomSource.Shared).NextDouble();
        }

        /// <summary>生成 [min, max) 之间的双精度随机数。</summary>
        /// <param name="min">下界（包含）。</param>
        /// <param name="max">上界（不包含）。</param>
        /// <param name="source">可选随机源。</param>
        public static double NextDouble(double min, double max, IRandomSource? source = null)
        {
            if (max < min) throw new ArgumentOutOfRangeException(nameof(max), "上界不能小于下界。");
            return min + (max - min) * (source ?? RandomSource.Shared).NextDouble();
        }

        /// <summary>用指定种子重置当前线程的默认随机源，便于复现实验。</summary>
        /// <param name="seed">新种子。</param>
        public static void Reseed(int seed) => RandomSource.Reseed(seed);

        private static decimal GenerateRandomDecimal(decimal min, decimal max, int scale, IRandomSource source)
        {
            double factor = source.NextDouble();
            decimal range = max - min;
            decimal unrounded = min + range * (decimal)factor;
            decimal rounded = SystemMath.Round(unrounded, scale, MidpointRounding.AwayFromZero);
            if (rounded < min) rounded = min;
            if (rounded > max) rounded = max;
            return rounded;
        }

        private static int GetDecimalScale(decimal value)
        {
            int[] bits = decimal.GetBits(value);
            return (bits[3] >> 16) & 0x7F;
        }

        private static T ConvertTo<T>(decimal value) where T : struct, IComparable, IConvertible
        {
            Type t = typeof(T);
            if (t == typeof(int) || t == typeof(long) || t == typeof(short) || t == typeof(byte) ||
                t == typeof(uint) || t == typeof(ulong) || t == typeof(ushort) || t == typeof(sbyte))
            {
                decimal floored = SystemMath.Floor(value);
                return (T)Convert.ChangeType(floored, t);
            }

            return (T)Convert.ChangeType(value, t);
        }
    }
}
