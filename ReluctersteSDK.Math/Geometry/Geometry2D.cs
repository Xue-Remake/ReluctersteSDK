using ReluctersteSDK.Math.Vectors;

namespace ReluctersteSDK.Math.Geometry
{
    /// <summary>
    /// 2D 几何计算的静态助手：最近点、包含测试、相交测试。
    /// </summary>
    public static class Geometry2D
    {
        /// <summary>几何计算使用的容差。</summary>
        public const float Epsilon = 1e-6f;

        /// <summary>获取线段上距离指定点最近的位置。</summary>
        /// <param name="a">线段起点。</param>
        /// <param name="b">线段终点。</param>
        /// <param name="point">目标点。</param>
        public static CartesianVector2D ClosestPointOnSegment(
            CartesianVector2D a,
            CartesianVector2D b,
            CartesianVector2D point)
        {
            CartesianVector2D ab = b - a;
            float lengthSqr = ab.SqrMagnitude;
            if (lengthSqr <= Epsilon)
                return a;

            float t = (point - a).Dot(ab) / lengthSqr;
            t = t < 0f ? 0f : (t > 1f ? 1f : t);
            return a + ab * t;
        }

        /// <summary>判断点是否在三角形内（边界算包含）。</summary>
        /// <param name="a">顶点 A。</param>
        /// <param name="b">顶点 B。</param>
        /// <param name="c">顶点 C。</param>
        /// <param name="point">目标点。</param>
        public static bool PointInTriangle(
            CartesianVector2D a,
            CartesianVector2D b,
            CartesianVector2D c,
            CartesianVector2D point)
        {
            float d1 = (b - a).Cross(point - a);
            float d2 = (c - b).Cross(point - b);
            float d3 = (a - c).Cross(point - c);

            bool hasNegative = d1 < -Epsilon || d2 < -Epsilon || d3 < -Epsilon;
            bool hasPositive = d1 > Epsilon || d2 > Epsilon || d3 > Epsilon;
            return !(hasNegative && hasPositive);
        }

        /// <summary>判断点是否在多边形内（奇偶规则）。</summary>
        /// <param name="points">有序顶点。</param>
        /// <param name="point">目标点。</param>
        public static bool PointInPolygon(ReadOnlySpan<CartesianVector2D> points, CartesianVector2D point)
        {
            if (points.Length < 3)
                return false;

            bool inside = false;
            int j = points.Length - 1;

            for (int i = 0; i < points.Length; i++)
            {
                CartesianVector2D pi = points[i];
                CartesianVector2D pj = points[j];

                bool crosses = (pi.Y > point.Y) != (pj.Y > point.Y);
                if (crosses)
                {
                    float x = (pj.X - pi.X) * (point.Y - pi.Y) / (pj.Y - pi.Y) + pi.X;
                    if (point.X < x)
                        inside = !inside;
                }

                j = i;
            }

            return inside;
        }

        /// <summary>判断两条线段是否相交；相交时输出交点。</summary>
        /// <param name="a">线段 A 起点。</param>
        /// <param name="b">线段 A 终点。</param>
        /// <param name="c">线段 B 起点。</param>
        /// <param name="d">线段 B 终点。</param>
        /// <param name="point">输出：交点（共线重叠时返回一个重叠端点）。</param>
        public static bool SegmentIntersectsSegment(
            CartesianVector2D a,
            CartesianVector2D b,
            CartesianVector2D c,
            CartesianVector2D d,
            out CartesianVector2D point)
            => SegmentIntersectsSegment(new Segment2D(a, b), new Segment2D(c, d), out point);

        /// <summary>判断两条线段是否相交；相交时输出交点。</summary>
        /// <param name="first">线段 A。</param>
        /// <param name="second">线段 B。</param>
        /// <param name="point">输出：交点（共线重叠时返回一个重叠端点）。</param>
        public static bool SegmentIntersectsSegment(
            Segment2D first,
            Segment2D second,
            out CartesianVector2D point)
        {
            CartesianVector2D r = first.B - first.A;
            CartesianVector2D s = second.B - second.A;
            float denominator = r.Cross(s);
            CartesianVector2D offset = second.A - first.A;

            if (SystemMath.Abs(denominator) < Epsilon)
            {
                // 平行；只有共线且重叠时才算相交
                if (SystemMath.Abs(offset.Cross(r)) > Epsilon)
                {
                    point = default;
                    return false;
                }

                if (PointOnSegment(first.A, second))
                {
                    point = first.A;
                    return true;
                }

                if (PointOnSegment(first.B, second))
                {
                    point = first.B;
                    return true;
                }

                if (PointOnSegment(second.A, first))
                {
                    point = second.A;
                    return true;
                }

                if (PointOnSegment(second.B, first))
                {
                    point = second.B;
                    return true;
                }

                point = default;
                return false;
            }

            float t = offset.Cross(s) / denominator;
            float u = offset.Cross(r) / denominator;

            if (t >= 0f && t <= 1f && u >= 0f && u <= 1f)
            {
                point = first.A + r * t;
                return true;
            }

            point = default;
            return false;
        }

        /// <summary>判断线段是否与圆相交。</summary>
        /// <param name="segment">线段。</param>
        /// <param name="circle">圆。</param>
        public static bool SegmentIntersectsCircle(Segment2D segment, Circle2D circle)
            => segment.Intersects(circle);

        /// <summary>判断线段是否与轴对齐包围盒相交（包含端点落在盒内的情形）。</summary>
        /// <param name="segment">线段。</param>
        /// <param name="box">包围盒。</param>
        public static bool SegmentIntersectsAabb(Segment2D segment, Aabb2D box)
        {
            if (box.Contains(segment.A) || box.Contains(segment.B))
                return true;

            return ClipSegment(segment, box.Min, box.Max);
        }

        /// <summary>判断射线是否与轴对齐包围盒相交；相交时输出最近的非负距离。</summary>
        /// <param name="ray">射线。</param>
        /// <param name="box">包围盒。</param>
        /// <param name="distance">输出：从起点到交点的距离。</param>
        public static bool RayIntersectsAabb(Ray2D ray, Aabb2D box, out float distance)
        {
            float tMin = 0f;
            float tMax = float.PositiveInfinity;

            if (!UpdateSlab(ray.Origin.X, ray.Direction.X, box.Min.X, box.Max.X, ref tMin, ref tMax) ||
                !UpdateSlab(ray.Origin.Y, ray.Direction.Y, box.Min.Y, box.Max.Y, ref tMin, ref tMax) ||
                tMin > tMax)
            {
                distance = 0f;
                return false;
            }

            distance = tMin;
            return true;
        }

        /// <summary>获取点相对有向线段的方位：正数在左侧，负数在右侧，0 共线。</summary>
        /// <param name="a">线段起点。</param>
        /// <param name="b">线段终点。</param>
        /// <param name="point">目标点。</param>
        public static float Orientation(CartesianVector2D a, CartesianVector2D b, CartesianVector2D point)
            => (b - a).Cross(point - a);

        /// <summary>用一组点创建最小包围盒。</summary>
        /// <param name="points">点集。</param>
        public static Aabb2D AabbFromPoints(IEnumerable<CartesianVector2D> points) => Aabb2D.FromPoints(points);

        private static bool ClipSegment(Segment2D segment, CartesianVector2D min, CartesianVector2D max)
        {
            float tMin = 0f;
            float tMax = 1f;
            CartesianVector2D direction = segment.B - segment.A;

            return UpdateSlab(segment.A.X, direction.X, min.X, max.X, ref tMin, ref tMax)
                && UpdateSlab(segment.A.Y, direction.Y, min.Y, max.Y, ref tMin, ref tMax)
                && tMin <= tMax;
        }

        private static bool UpdateSlab(
            float origin,
            float direction,
            float min,
            float max,
            ref float tMin,
            ref float tMax)
        {
            if (SystemMath.Abs(direction) < Epsilon)
                return origin >= min && origin <= max;

            float t1 = (min - origin) / direction;
            float t2 = (max - origin) / direction;
            if (t1 > t2)
            {
                float temp = t1;
                t1 = t2;
                t2 = temp;
            }

            tMin = SystemMath.Max(tMin, t1);
            tMax = SystemMath.Min(tMax, t2);
            return tMin <= tMax;
        }

        private static bool PointOnSegment(CartesianVector2D point, Segment2D segment)
        {
            CartesianVector2D ab = segment.B - segment.A;
            CartesianVector2D ap = point - segment.A;

            if (SystemMath.Abs(ab.Cross(ap)) > Epsilon)
                return false;

            float dot = ap.Dot(ab);
            return dot >= -Epsilon && dot <= ab.SqrMagnitude + Epsilon;
        }
    }
}
