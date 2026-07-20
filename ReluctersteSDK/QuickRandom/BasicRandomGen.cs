using System;
using System.Collections.Generic;
using System.Text;

namespace ReluctersteSDK.QuickRandom
{
    public static class BasicRandomGen
    {
        // ========== ThreadLocal 保证线程安全 ==========
        private static int _seed = Environment.TickCount;
        private static readonly ThreadLocal<Random> Rng = new ThreadLocal<Random>(() =>
        {
            int seed = Interlocked.Increment(ref _seed);
            return new Random(seed);
        });

        /// <summary>
        /// 生成一个在 [min, max] 范围内的随机数，自动匹配输入值的最高小数精度，并返回指定类型 T 的值。
        /// 若 T 为整数类型，生成的浮点值会向下取整。
        /// </summary>
        /// <typeparam name="T">返回类型（值类型，如 int、float、double、decimal 等）</typeparam>
        /// <param name="min">下限（包含）</param>
        /// <param name="max">上限（包含）</param>
        /// <returns>随机数</returns>
        public static T Next<T>(T min, T max) where T : struct, IComparable, IConvertible
        {
            decimal dMin = Convert.ToDecimal(min);
            decimal dMax = Convert.ToDecimal(max);
            if (dMin > dMax)
                throw new ArgumentException("min 不能大于 max");

            int scale = Math.Max(GetDecimalScale(dMin), GetDecimalScale(dMax));
            decimal randomValue = GenerateRandomDecimal(dMin, dMax, scale);
            return ConvertTo<T>(randomValue);
        }

        /// <summary>
        /// 生成包含 n 个随机数的数组，每个随机数的生成规则与 <see cref="Next{T}"/> 相同。
        /// </summary>
        /// <typeparam name="T">元素类型</typeparam>
        /// <param name="min">下限</param>
        /// <param name="max">上限</param>
        /// <param name="n">数组长度</param>
        /// <returns>随机数数组</returns>
        public static T[] NextArray<T>(T min, T max, int n) where T : struct, IComparable, IConvertible
        {
            if (n < 0)
                throw new ArgumentOutOfRangeException(nameof(n), "数组长度不能为负数");
            var result = new T[n];
            for (int i = 0; i < n; i++)
                result[i] = Next(min, max);
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
        /// <returns>随机数，非法输入时返回 null</returns>
        public static T? NextProbability<T>(T min, T max, T k, double p, double q)
            where T : struct, IComparable, IConvertible
        {
            if (p < 0 || q < 0 || Math.Abs(p + q - 1.0) > 1e-9)
                return null;

            decimal dMin = Convert.ToDecimal(min);
            decimal dMax = Convert.ToDecimal(max);
            decimal dK = Convert.ToDecimal(k);

            if (dMin > dMax || dK < dMin || dK > dMax)
                throw new ArgumentException("min, max, k 必须满足 min ≤ k ≤ max");

            int scale = Math.Max(Math.Max(GetDecimalScale(dMin), GetDecimalScale(dMax)), GetDecimalScale(dK));
            double r = Rng.Value.NextDouble();

            if (r < p)
            {
                decimal step = (decimal)Math.Pow(10, -scale);
                decimal upper = dK - step;
                if (upper < dMin) upper = dMin;
                return ConvertTo<T>(GenerateRandomDecimal(dMin, upper, scale));
            }
            else
            {
                return ConvertTo<T>(GenerateRandomDecimal(dK, dMax, scale));
            }
        }

        /// <summary>
        /// 从列表中等概率随机选择一项（方法1）。
        /// </summary>
        /// <typeparam name="T">实现了 <see cref="CanbeRandom"/> 的类型</typeparam>
        /// <param name="list">待选择的列表</param>
        /// <returns>随机选中的项</returns>
        public static T RandomSelectOne<T>(List<T> list) where T : ICanbeRandom
        {
            if (list == null || list.Count == 0)
                throw new ArgumentException("列表不能为 null 或空");
            return list[Rng.Value.Next(list.Count)];
        }

        /// <summary>
        /// 从列表中随机选择一项，以概率 p 选中类型为 TItem 的项（方法2）。
        /// 若列表中不存在 TItem 类型的项，则退化到方法1（全随机等概率）。
        /// </summary>
        /// <typeparam name="TItem">目标类型，实现了 <see cref="CanbeRandom"/></typeparam>
        /// <param name="list">待选择的列表</param>
        /// <param name="p">选中 TItem 类型项的概率，范围 [0, 1]</param>
        /// <returns>随机选中的项</returns>
        public static ICanbeRandom RandomSelectOne<TItem>(List<ICanbeRandom> list, double p)
            where TItem : ICanbeRandom
        {
            if (list == null || list.Count == 0)
                throw new ArgumentException("列表不能为 null 或空");
            if (p < 0 || p > 1)
                throw new ArgumentOutOfRangeException(nameof(p), "概率 p 必须在 [0, 1] 范围内");

            var typedItems = list.OfType<TItem>().ToList();
            if (typedItems.Count == 0)
                return list[Rng.Value.Next(list.Count)];

            if (Rng.Value.NextDouble() < p)
                return typedItems[Rng.Value.Next(typedItems.Count)];
            else
                return list[Rng.Value.Next(list.Count)];
        }

        /// <summary>
        /// 等概率返回一个随机布尔值（true 或 false 各 50%）。
        /// </summary>
        /// <remarks>基于 <see cref="Next{T}"/> 生成随机数；不保证加密强随机性。</remarks>
        /// <returns>等概率返回 true 或 false。</returns>
        public static bool NextBool()
        {
            int ran = BasicRandomGen.Next(0, 2);
            if (ran % 2 == 0) return false;
            else return true;
        }

        /// <summary>
        /// 以指定概率返回指定的布尔值：以概率 <paramref name="p"/> 返回 <paramref name="b"/>，
        /// 以概率 1-p 返回 !b。
        /// </summary>
        /// <param name="b">期望返回的布尔值</param>
        /// <param name="p">返回 <paramref name="b"/> 的概率，范围 [0, 1]</param>
        /// <returns>以概率 p 返回 b，否则返回 !b</returns>
        public static bool NextBool(bool b, double p)
        {
            if (p < 0 || p > 1)
                throw new ArgumentOutOfRangeException(nameof(p), "概率 p 必须在 [0, 1] 范围内");
            return Rng.Value.NextDouble() < p ? b : !b;
        }

        // ---------- 内部辅助方法 ----------
        private static decimal GenerateRandomDecimal(decimal min, decimal max, int scale)
        {
            double factor = Rng.Value.NextDouble();
            decimal range = max - min;
            decimal unrounded = min + range * (decimal)factor;
            decimal rounded = Math.Round(unrounded, scale, MidpointRounding.AwayFromZero);
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
                decimal floored = Math.Floor(value);
                return (T)Convert.ChangeType(floored, t);
            }
            return (T)Convert.ChangeType(value, t);
        }
    }
}
