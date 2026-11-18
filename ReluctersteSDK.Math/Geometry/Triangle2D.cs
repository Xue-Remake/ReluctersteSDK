using ReluctersteSDK.Math.Vectors;

namespace ReluctersteSDK.Math.Geometry
{
    /// <summary>
    /// 2D 三角形：三个顶点 A、B、C。
    /// </summary>
    public readonly struct Triangle2D
    {
        /// <summary>顶点 A。</summary>
        public readonly CartesianVector2D A;
        /// <summary>顶点 B。</summary>
        public readonly CartesianVector2D B;
        /// <summary>顶点 C。</summary>
        public readonly CartesianVector2D C;

        /// <summary>初始化一个三角形。</summary>
        public Triangle2D(CartesianVector2D a, CartesianVector2D b, CartesianVector2D c)
        {
            A = a;
            B = b;
            C = c;
        }

        /// <summary>获取面积。</summary>
        public float Area => SystemMath.Abs((B - A).Cross(C - A)) * 0.5f;

        /// <summary>获取重心。</summary>
        public CartesianVector2D Centroid => (A + B + C) / 3f;

        /// <summary>获取轴对齐包围盒。</summary>
        public Aabb2D Bounds => new Aabb2D(
            CartesianVector2D.Min(A, CartesianVector2D.Min(B, C)),
            CartesianVector2D.Max(A, CartesianVector2D.Max(B, C)));

        /// <summary>判断点是否在三角形内（边界算包含）。</summary>
        /// <param name="point">目标点。</param>
        public bool Contains(CartesianVector2D point) => Geometry2D.PointInTriangle(A, B, C, point);

        /// <summary>计算点的重心坐标 (u, v, w)，满足 point = u*A + v*B + w*C。</summary>
        /// <param name="point">目标点。</param>
        /// <param name="u">输出：A 的权重。</param>
        /// <param name="v">输出：B 的权重。</param>
        /// <param name="w">输出：C 的权重。</param>
        public void Barycentric(CartesianVector2D point, out float u, out float v, out float w)
        {
            CartesianVector2D v0 = B - A;
            CartesianVector2D v1 = C - A;
            CartesianVector2D v2 = point - A;

            float denom = v0.Cross(v1);
            if (SystemMath.Abs(denom) < Geometry2D.Epsilon)
            {
                u = 1f;
                v = 0f;
                w = 0f;
                return;
            }

            v = v2.Cross(v1) / denom;
            w = v0.Cross(v2) / denom;
            u = 1f - v - w;
        }

        /// <summary>获取三角形上距离指定点最近的位置。</summary>
        /// <param name="point">目标点。</param>
        public CartesianVector2D ClosestPoint(CartesianVector2D point)
        {
            if (Contains(point))
                return point;

            CartesianVector2D ab = new Segment2D(A, B).ClosestPoint(point);
            CartesianVector2D bc = new Segment2D(B, C).ClosestPoint(point);
            CartesianVector2D ca = new Segment2D(C, A).ClosestPoint(point);

            float dab = point.SqrDistanceTo(ab);
            float dbc = point.SqrDistanceTo(bc);
            float dca = point.SqrDistanceTo(ca);

            if (dab <= dbc && dab <= dca) return ab;
            return dbc <= dca ? bc : ca;
        }

        /// <inheritdoc />
        public override string ToString() => $"Triangle({A}, {B}, {C})";
    }
}
