using System.Collections.ObjectModel;
using ReluctersteSDK.Math.Vectors;

namespace ReluctersteSDK.Math.Geometry
{
    /// <summary>
    /// 简单多边形（顶点按顺序给出，首尾自动闭合）。
    /// <para>支持凹多边形；自交多边形的包含判断结果未定义。</para>
    /// </summary>
    public sealed class Polygon2D
    {
        private readonly CartesianVector2D[] _points;
        private readonly ReadOnlyCollection<CartesianVector2D> _view;

        /// <summary>用有序顶点初始化多边形。</summary>
        /// <param name="points">顶点序列，至少 3 个。</param>
        /// <exception cref="ArgumentNullException">顶点序列为 null。</exception>
        /// <exception cref="ArgumentException">顶点少于 3 个。</exception>
        public Polygon2D(IEnumerable<CartesianVector2D> points)
        {
            if (points is null) throw new ArgumentNullException(nameof(points));

            _points = points.ToArray();
            if (_points.Length < 3)
                throw new ArgumentException("多边形至少需要 3 个顶点。", nameof(points));

            _view = Array.AsReadOnly(_points);
        }

        /// <summary>获取顶点只读列表。</summary>
        public IReadOnlyList<CartesianVector2D> Points => _view;

        /// <summary>获取顶点数量。</summary>
        public int Count => _points.Length;

        /// <summary>获取面积（绝对值）。</summary>
        public float Area => SystemMath.Abs(SignedArea);

        /// <summary>获取有符号面积（逆时针为正）。</summary>
        public float SignedArea
        {
            get
            {
                float sum = 0f;
                for (int i = 0; i < _points.Length; i++)
                {
                    CartesianVector2D a = _points[i];
                    CartesianVector2D b = _points[(i + 1) % _points.Length];
                    sum += a.Cross(b);
                }

                return sum * 0.5f;
            }
        }

        /// <summary>获取重心；退化多边形返回顶点平均值。</summary>
        public CartesianVector2D Centroid
        {
            get
            {
                float area = SignedArea;
                if (SystemMath.Abs(area) < Geometry2D.Epsilon)
                {
                    CartesianVector2D sum = CartesianVector2D.Zero;
                    for (int i = 0; i < _points.Length; i++)
                        sum += _points[i];
                    return sum / _points.Length;
                }

                float cx = 0f;
                float cy = 0f;
                for (int i = 0; i < _points.Length; i++)
                {
                    CartesianVector2D a = _points[i];
                    CartesianVector2D b = _points[(i + 1) % _points.Length];
                    float cross = a.Cross(b);
                    cx += (a.X + b.X) * cross;
                    cy += (a.Y + b.Y) * cross;
                }

                float factor = 1f / (6f * area);
                return new CartesianVector2D(cx * factor, cy * factor);
            }
        }

        /// <summary>获取轴对齐包围盒。</summary>
        public Aabb2D Bounds => Aabb2D.FromPoints(_points);

        /// <summary>判断点是否在多边形内（奇偶规则，边界结果未定义）。</summary>
        /// <param name="point">目标点。</param>
        public bool Contains(CartesianVector2D point) => Geometry2D.PointInPolygon(_points, point);

        /// <summary>判断多边形是否为凸多边形。</summary>
        public bool IsConvex
        {
            get
            {
                bool hasPositive = false;
                bool hasNegative = false;

                for (int i = 0; i < _points.Length; i++)
                {
                    CartesianVector2D a = _points[i];
                    CartesianVector2D b = _points[(i + 1) % _points.Length];
                    CartesianVector2D c = _points[(i + 2) % _points.Length];
                    float cross = (b - a).Cross(c - b);

                    if (cross > Geometry2D.Epsilon) hasPositive = true;
                    else if (cross < -Geometry2D.Epsilon) hasNegative = true;

                    if (hasPositive && hasNegative)
                        return false;
                }

                return true;
            }
        }

        /// <summary>枚举多边形的边。</summary>
        public IEnumerable<Segment2D> Edges()
        {
            for (int i = 0; i < _points.Length; i++)
                yield return new Segment2D(_points[i], _points[(i + 1) % _points.Length]);
        }

        /// <inheritdoc />
        public override string ToString() => $"Polygon({_points.Length} vertices, area={Area:0.###})";
    }
}
