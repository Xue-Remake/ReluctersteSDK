using ReluctersteSDK.Math.Vectors;

namespace ReluctersteSDK.Math.Geometry
{
    /// <summary>
    /// 2D 圆：中心点与半径。
    /// </summary>
    public readonly struct Circle2D
    {
        /// <summary>圆心。</summary>
        public readonly CartesianVector2D Center;

        /// <summary>半径（非负）。</summary>
        public readonly float Radius;

        /// <summary>初始化一个圆。</summary>
        /// <param name="center">圆心。</param>
        /// <param name="radius">半径，必须是非负有限数。</param>
        /// <exception cref="ArgumentOutOfRangeException">半径为负数或非有限数。</exception>
        public Circle2D(CartesianVector2D center, float radius)
        {
            if (radius < 0f || !float.IsFinite(radius))
                throw new ArgumentOutOfRangeException(nameof(radius), radius, "半径必须是非负有限数。");

            Center = center;
            Radius = radius;
        }

        /// <summary>获取面积。</summary>
        public float Area => MathF.PI * Radius * Radius;

        /// <summary>判断点是否在圆内（边界算包含）。</summary>
        /// <param name="point">目标点。</param>
        public bool Contains(CartesianVector2D point)
            => Center.SqrDistanceTo(point) <= Radius * Radius;

        /// <summary>判断另一个圆是否完全包含在本圆内。</summary>
        /// <param name="other">另一个圆。</param>
        public bool Contains(Circle2D other)
            => Center.DistanceTo(other.Center) + other.Radius <= Radius;

        /// <summary>判断两个圆是否相交（相切算相交）。</summary>
        /// <param name="other">另一个圆。</param>
        public bool Intersects(Circle2D other)
        {
            float sum = Radius + other.Radius;
            return Center.SqrDistanceTo(other.Center) <= sum * sum;
        }

        /// <summary>判断圆是否与轴对齐包围盒相交。</summary>
        /// <param name="box">包围盒。</param>
        public bool Intersects(Aabb2D box)
        {
            CartesianVector2D closest = box.ClosestPoint(Center);
            return Center.SqrDistanceTo(closest) <= Radius * Radius;
        }

        /// <summary>获取圆周上距离指定点最近的位置；点在圆心时返回正右方圆周点。</summary>
        /// <param name="point">目标点。</param>
        public CartesianVector2D ClosestPoint(CartesianVector2D point)
        {
            CartesianVector2D direction = point - Center;
            if (direction.SqrMagnitude == 0f)
                return Center + new CartesianVector2D(Radius, 0f);

            return Center + direction.Normalized * Radius;
        }

        /// <summary>计算点到圆周的距离；点在圆内时返回 0。</summary>
        /// <param name="point">目标点。</param>
        public float DistanceTo(CartesianVector2D point)
            => SystemMath.Max(0f, Center.DistanceTo(point) - Radius);

        /// <summary>按指定增量扩张半径。</summary>
        /// <param name="amount">扩张量；负数表示收缩，结果不小于 0。</param>
        public Circle2D Inflate(float amount)
            => new Circle2D(Center, SystemMath.Max(0f, Radius + amount));

        /// <inheritdoc />
        public override string ToString() => $"Circle({Center}, r={Radius:0.###})";
    }
}
