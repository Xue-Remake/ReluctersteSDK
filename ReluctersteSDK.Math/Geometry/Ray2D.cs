using ReluctersteSDK.Math.Vectors;

namespace ReluctersteSDK.Math.Geometry
{
    /// <summary>
    /// 2D 射线：起点与单位方向。
    /// </summary>
    public readonly struct Ray2D
    {
        /// <summary>起点。</summary>
        public readonly CartesianVector2D Origin;

        /// <summary>单位方向向量。</summary>
        public readonly CartesianVector2D Direction;

        /// <summary>初始化一条射线；方向会自动归一化。</summary>
        /// <param name="origin">起点。</param>
        /// <param name="direction">方向，不能是零向量。</param>
        /// <exception cref="ArgumentException">方向是零向量或包含非有限分量。</exception>
        public Ray2D(CartesianVector2D origin, CartesianVector2D direction)
        {
            if (direction.SqrMagnitude == 0f || !float.IsFinite(direction.X) || !float.IsFinite(direction.Y))
                throw new ArgumentException("射线方向必须是非零有限向量。", nameof(direction));

            Origin = origin;
            Direction = direction.Normalized;
        }

        /// <summary>获取射线上距离起点指定距离的点。</summary>
        /// <param name="distance">沿方向的距离；负数会得到反向点。</param>
        public CartesianVector2D GetPoint(float distance) => Origin + Direction * distance;

        /// <summary>判断射线是否与圆相交；相交时输出最近的非负距离。</summary>
        /// <param name="circle">圆。</param>
        /// <param name="distance">输出：从起点到交点的距离。</param>
        public bool Intersects(Circle2D circle, out float distance)
        {
            CartesianVector2D offset = Origin - circle.Center;
            float b = offset.Dot(Direction);
            float c = offset.SqrMagnitude - circle.Radius * circle.Radius;
            float discriminant = b * b - c;

            if (discriminant < 0f)
            {
                distance = 0f;
                return false;
            }

            float sqrt = MathF.Sqrt(discriminant);
            float near = -b - sqrt;
            float far = -b + sqrt;

            if (near >= 0f)
            {
                distance = near;
                return true;
            }

            if (far >= 0f)
            {
                distance = far;
                return true;
            }

            distance = 0f;
            return false;
        }

        /// <summary>判断射线是否与轴对齐包围盒相交；相交时输出最近的非负距离。</summary>
        /// <param name="box">包围盒。</param>
        /// <param name="distance">输出：从起点到交点的距离。</param>
        public bool Intersects(Aabb2D box, out float distance)
            => Geometry2D.RayIntersectsAabb(this, box, out distance);

        /// <summary>判断射线是否与线段相交；相交时输出从起点到交点的距离。</summary>
        /// <param name="segment">线段。</param>
        /// <param name="distance">输出：从起点到交点的距离。</param>
        public bool Intersects(Segment2D segment, out float distance)
        {
            CartesianVector2D segmentDirection = segment.B - segment.A;
            float denominator = Direction.Cross(segmentDirection);

            if (SystemMath.Abs(denominator) < Geometry2D.Epsilon)
            {
                distance = 0f;
                return false;
            }

            CartesianVector2D offset = segment.A - Origin;
            float t = offset.Cross(segmentDirection) / denominator;
            float u = offset.Cross(Direction) / denominator;

            if (t >= 0f && u >= 0f && u <= 1f)
            {
                distance = t;
                return true;
            }

            distance = 0f;
            return false;
        }

        /// <inheritdoc />
        public override string ToString() => $"Ray({Origin}, {Direction})";
    }
}
