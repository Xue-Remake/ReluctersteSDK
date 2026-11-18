using ReluctersteSDK.Math.Geometry;
using ReluctersteSDK.Math.Vectors;

namespace ReluctersteSDK.Math.QuickRandom
{
    /// <summary>
    /// <see cref="IRandomSource"/> 的常用扩展：浮点、布尔、向量、采样与洗牌。
    /// </summary>
    public static class RandomSourceExtensions
    {
        /// <summary>生成 [0, 1) 之间的单精度随机数。</summary>
        public static float NextFloat(this IRandomSource source)
        {
            if (source is null) throw new ArgumentNullException(nameof(source));
            return (float)source.NextDouble();
        }

        /// <summary>以概率 <paramref name="probability"/> 返回 true。</summary>
        public static bool NextBool(this IRandomSource source, double probability = 0.5)
        {
            if (source is null) throw new ArgumentNullException(nameof(source));
            if (probability < 0.0 || probability > 1.0)
                throw new ArgumentOutOfRangeException(nameof(probability), "概率必须在 [0, 1] 范围内。");

            return source.NextDouble() < probability;
        }

        /// <summary>生成 [min, max) 之间的双精度随机数。</summary>
        public static double NextDouble(this IRandomSource source, double min, double max)
        {
            if (source is null) throw new ArgumentNullException(nameof(source));
            if (max < min) throw new ArgumentOutOfRangeException(nameof(max), "上界不能小于下界。");
            return min + (max - min) * source.NextDouble();
        }

        /// <summary>生成任意方向的单位向量。</summary>
        public static CartesianVector2D NextUnitVector2(this IRandomSource source)
        {
            if (source is null) throw new ArgumentNullException(nameof(source));
            double angle = source.NextDouble() * SystemMath.PI * 2.0;
            return new CartesianVector2D((float)SystemMath.Cos(angle), (float)SystemMath.Sin(angle));
        }

        /// <summary>在圆内均匀采样一个点。</summary>
        public static CartesianVector2D NextPointInCircle(this IRandomSource source, Circle2D circle)
        {
            if (source is null) throw new ArgumentNullException(nameof(source));
            double angle = source.NextDouble() * SystemMath.PI * 2.0;
            float radius = circle.Radius * MathF.Sqrt((float)source.NextDouble());
            return circle.Center + new CartesianVector2D(
                radius * (float)SystemMath.Cos(angle),
                radius * (float)SystemMath.Sin(angle));
        }

        /// <summary>在圆周上均匀采样一个点。</summary>
        public static CartesianVector2D NextPointOnCircle(this IRandomSource source, Circle2D circle)
        {
            if (source is null) throw new ArgumentNullException(nameof(source));
            return circle.Center + source.NextUnitVector2() * circle.Radius;
        }

        /// <summary>对列表做 Fisher–Yates 原地洗牌。</summary>
        public static void Shuffle<T>(this IRandomSource source, IList<T> list)
        {
            if (source is null) throw new ArgumentNullException(nameof(source));
            if (list is null) throw new ArgumentNullException(nameof(list));

            for (int i = list.Count - 1; i > 0; i--)
            {
                int j = source.NextInt(0, i + 1);
                (list[i], list[j]) = (list[j], list[i]);
            }
        }

        /// <summary>从列表中等概率选取一项。</summary>
        public static T Pick<T>(this IRandomSource source, IReadOnlyList<T> list)
        {
            if (source is null) throw new ArgumentNullException(nameof(source));
            if (list is null) throw new ArgumentNullException(nameof(list));
            if (list.Count == 0) throw new ArgumentException("列表不能为空。", nameof(list));

            return list[source.NextInt(0, list.Count)];
        }

        /// <summary>按权重从列表中选择一项。</summary>
        /// <param name="source">随机源。</param>
        /// <param name="list">候选项。</param>
        /// <param name="weights">与候选项一一对应的非负权重。</param>
        public static T PickWeighted<T>(this IRandomSource source, IReadOnlyList<T> list, IReadOnlyList<double> weights)
        {
            if (source is null) throw new ArgumentNullException(nameof(source));
            if (list is null) throw new ArgumentNullException(nameof(list));
            if (weights is null) throw new ArgumentNullException(nameof(weights));
            if (list.Count == 0) throw new ArgumentException("列表不能为空。", nameof(list));
            if (list.Count != weights.Count)
                throw new ArgumentException("权重数量必须与候选项数量一致。", nameof(weights));

            double total = 0.0;
            for (int i = 0; i < weights.Count; i++)
            {
                if (weights[i] < 0.0 || !double.IsFinite(weights[i]))
                    throw new ArgumentOutOfRangeException(nameof(weights), "权重必须是非负有限数。");
                total += weights[i];
            }

            if (total <= 0.0)
                throw new ArgumentException("权重总和必须大于 0。", nameof(weights));

            double target = source.NextDouble() * total;
            double cumulative = 0.0;
            for (int i = 0; i < list.Count; i++)
            {
                cumulative += weights[i];
                if (target < cumulative)
                    return list[i];
            }

            return list[^1];
        }
    }
}
