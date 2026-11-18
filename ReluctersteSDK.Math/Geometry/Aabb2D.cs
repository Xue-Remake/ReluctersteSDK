using System.Numerics;
using ReluctersteSDK.Math.Matrices;
using ReluctersteSDK.Math.Vectors;

namespace ReluctersteSDK.Math.Geometry
{
    /// <summary>
    /// 轴对齐包围盒（Axis-Aligned Bounding Box），由最小点和最大点定义。
    /// </summary>
    public readonly struct Aabb2D
    {
        /// <summary>最小点（各分量较小值）。</summary>
        public readonly CartesianVector2D Min;

        /// <summary>最大点（各分量较大值）。</summary>
        public readonly CartesianVector2D Max;

        /// <summary>用最小点和最大点初始化包围盒；顺序会自动归一化。</summary>
        /// <param name="min">一个角点。</param>
        /// <param name="max">另一个角点。</param>
        public Aabb2D(CartesianVector2D min, CartesianVector2D max)
        {
            Min = CartesianVector2D.Min(min, max);
            Max = CartesianVector2D.Max(min, max);
        }

        /// <summary>用左下角与尺寸初始化包围盒。</summary>
        /// <param name="x">左下角 X。</param>
        /// <param name="y">左下角 Y。</param>
        /// <param name="width">宽度。</param>
        /// <param name="height">高度。</param>
        public Aabb2D(float x, float y, float width, float height)
            : this(new CartesianVector2D(x, y), new CartesianVector2D(x + width, y + height))
        {
        }

        /// <summary>获取中心点。</summary>
        public CartesianVector2D Center => (Min + Max) * 0.5f;

        /// <summary>获取尺寸（宽高）。</summary>
        public CartesianVector2D Size => Max - Min;

        /// <summary>获取半尺寸（中心到边的距离）。</summary>
        public CartesianVector2D Extents => Size * 0.5f;

        /// <summary>获取面积。</summary>
        public float Area
        {
            get
            {
                CartesianVector2D size = Size;
                return SystemMath.Max(0f, size.X) * SystemMath.Max(0f, size.Y);
            }
        }

        /// <summary>判断是否为空（任一分量上 Min 大于 Max）。</summary>
        public bool IsEmpty => Min.X > Max.X || Min.Y > Max.Y;

        /// <summary>用中心点和尺寸创建包围盒。</summary>
        public static Aabb2D FromCenterSize(CartesianVector2D center, CartesianVector2D size)
        {
            CartesianVector2D half = CartesianVector2D.Abs(size) * 0.5f;
            return new Aabb2D(center - half, center + half);
        }

        /// <summary>用一组点创建最小包围盒；空集合返回默认空盒。</summary>
        /// <param name="points">点集。</param>
        public static Aabb2D FromPoints(ReadOnlySpan<CartesianVector2D> points)
        {
            if (points.Length == 0)
                return default;

            CartesianVector2D min = points[0];
            CartesianVector2D max = points[0];
            for (int i = 1; i < points.Length; i++)
            {
                min = CartesianVector2D.Min(min, points[i]);
                max = CartesianVector2D.Max(max, points[i]);
            }

            return new Aabb2D(min, max);
        }

        /// <summary>用一组点创建最小包围盒。</summary>
        /// <param name="points">点集。</param>
        public static Aabb2D FromPoints(IEnumerable<CartesianVector2D> points)
        {
            if (points is null) throw new ArgumentNullException(nameof(points));

            using IEnumerator<CartesianVector2D> enumerator = points.GetEnumerator();
            if (!enumerator.MoveNext())
                return default;

            CartesianVector2D min = enumerator.Current;
            CartesianVector2D max = enumerator.Current;
            while (enumerator.MoveNext())
            {
                min = CartesianVector2D.Min(min, enumerator.Current);
                max = CartesianVector2D.Max(max, enumerator.Current);
            }

            return new Aabb2D(min, max);
        }

        /// <summary>判断点是否在盒内（边界算包含）。</summary>
        /// <param name="point">目标点。</param>
        public bool Contains(CartesianVector2D point)
            => point.X >= Min.X && point.X <= Max.X && point.Y >= Min.Y && point.Y <= Max.Y;

        /// <summary>判断另一个包围盒是否完全包含在本盒内。</summary>
        /// <param name="other">另一个包围盒。</param>
        public bool Contains(Aabb2D other)
            => other.Min.X >= Min.X && other.Max.X <= Max.X
               && other.Min.Y >= Min.Y && other.Max.Y <= Max.Y;

        /// <summary>判断两个包围盒是否相交（接触边界算相交）。</summary>
        /// <param name="other">另一个包围盒。</param>
        public bool Intersects(Aabb2D other)
            => Min.X <= other.Max.X && Max.X >= other.Min.X
               && Min.Y <= other.Max.Y && Max.Y >= other.Min.Y;

        /// <summary>判断包围盒是否与圆相交。</summary>
        /// <param name="circle">圆。</param>
        public bool Intersects(Circle2D circle) => circle.Intersects(this);

        /// <summary>求两个包围盒的交集；不相交时返回空盒。</summary>
        /// <param name="other">另一个包围盒。</param>
        public Aabb2D Intersection(Aabb2D other)
        {
            CartesianVector2D min = CartesianVector2D.Max(Min, other.Min);
            CartesianVector2D max = CartesianVector2D.Min(Max, other.Max);
            return min.X > max.X || min.Y > max.Y ? default : new Aabb2D(min, max);
        }

        /// <summary>求包含两个包围盒的最小包围盒。</summary>
        /// <param name="other">另一个包围盒。</param>
        public Aabb2D Union(Aabb2D other)
            => new Aabb2D(CartesianVector2D.Min(Min, other.Min), CartesianVector2D.Max(Max, other.Max));

        /// <summary>向四周扩张指定距离。</summary>
        /// <param name="amount">扩张量；负数表示收缩。</param>
        public Aabb2D Inflate(float amount)
            => new Aabb2D(Min - new CartesianVector2D(amount, amount), Max + new CartesianVector2D(amount, amount));

        /// <summary>按分量扩张/收缩。</summary>
        /// <param name="amount">各方向扩张量。</param>
        public Aabb2D Expand(CartesianVector2D amount)
            => new Aabb2D(Min - amount, Max + amount);

        /// <summary>获取盒内距离指定点最近的位置。</summary>
        /// <param name="point">目标点。</param>
        public CartesianVector2D ClosestPoint(CartesianVector2D point)
            => CartesianVector2D.Clamp(point, Min, Max);

        /// <summary>用矩阵变换包围盒，返回变换后四个角点的轴对齐包围盒。</summary>
        /// <param name="matrix">变换矩阵。</param>
        public Aabb2D Transform(Matrix3x2 matrix)
        {
            Span<CartesianVector2D> corners = stackalloc CartesianVector2D[4];
            corners[0] = new CartesianVector2D(Min.X, Min.Y);
            corners[1] = new CartesianVector2D(Max.X, Min.Y);
            corners[2] = new CartesianVector2D(Max.X, Max.Y);
            corners[3] = new CartesianVector2D(Min.X, Max.Y);

            for (int i = 0; i < corners.Length; i++)
                corners[i] = matrix.TransformPoint(corners[i]);

            return FromPoints(corners);
        }

        /// <inheritdoc />
        public override string ToString() => $"[{Min} .. {Max}]";
    }
}
