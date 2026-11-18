using ReluctersteSDK.Math.Vectors;

namespace ReluctersteSDK.Math.Geometry
{
    /// <summary>
    /// 2D 线段：由起点 A 与终点 B 定义。
    /// </summary>
    public readonly struct Segment2D
    {
        /// <summary>起点。</summary>
        public readonly CartesianVector2D A;

        /// <summary>终点。</summary>
        public readonly CartesianVector2D B;

        /// <summary>初始化一条线段。</summary>
        /// <param name="a">起点。</param>
        /// <param name="b">终点。</param>
        public Segment2D(CartesianVector2D a, CartesianVector2D b)
        {
            A = a;
            B = b;
        }

        /// <summary>获取长度。</summary>
        public float Length => A.DistanceTo(B);

        /// <summary>获取长度平方。</summary>
        public float SqrLength => A.SqrDistanceTo(B);

        /// <summary>获取方向单位向量；零长线段返回零向量。</summary>
        public CartesianVector2D Direction => (B - A).Normalized;

        /// <summary>获取线段上参数 <paramref name="t"/> 处的点（t=0 为 A，t=1 为 B）。</summary>
        /// <param name="t">参数，不限制范围。</param>
        public CartesianVector2D GetPoint(float t) => A + (B - A) * t;

        /// <summary>获取线段上距离指定点最近的位置。</summary>
        /// <param name="point">目标点。</param>
        public CartesianVector2D ClosestPoint(CartesianVector2D point)
            => Geometry2D.ClosestPointOnSegment(A, B, point);

        /// <summary>计算点到线段的最短距离。</summary>
        /// <param name="point">目标点。</param>
        public float DistanceTo(CartesianVector2D point)
            => point.DistanceTo(ClosestPoint(point));

        /// <summary>判断两条线段是否相交；相交时输出交点。</summary>
        /// <param name="other">另一条线段。</param>
        /// <param name="point">输出：交点（共线重叠时返回一个重叠端点）。</param>
        public bool Intersects(Segment2D other, out CartesianVector2D point)
            => Geometry2D.SegmentIntersectsSegment(this, other, out point);

        /// <summary>判断线段是否与圆相交（相切算相交）。</summary>
        /// <param name="circle">圆。</param>
        public bool Intersects(Circle2D circle)
            => ClosestPoint(circle.Center).SqrDistanceTo(circle.Center) <= circle.Radius * circle.Radius;

        /// <summary>判断线段是否与轴对齐包围盒相交。</summary>
        /// <param name="box">包围盒。</param>
        public bool Intersects(Aabb2D box) => Geometry2D.SegmentIntersectsAabb(this, box);

        /// <inheritdoc />
        public override string ToString() => $"[{A} -> {B}]";
    }
}
