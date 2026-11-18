using System.Numerics;
using ReluctersteSDK.Math.Geometry;
using ReluctersteSDK.Math.Matrices;
using ReluctersteSDK.Math.Vectors;

namespace ReluctersteSDK.Game.Rendering
{
    /// <summary>
    /// 2D 摄像机：把世界坐标映射到屏幕坐标，并提供可见范围用于裁剪。
    /// </summary>
    public sealed class Camera2D
    {
        private float _zoom = 1f;
        private int _viewportWidth = 1280;
        private int _viewportHeight = 720;

        /// <summary>获取或设置摄像机中心在世界中的位置。</summary>
        public CartesianVector2D Position { get; set; } = CartesianVector2D.Zero;

        /// <summary>获取或设置摄像机旋转（弧度，正值为逆时针）。</summary>
        public float Rotation { get; set; }

        /// <summary>获取或设置缩放倍率，必须大于 0。</summary>
        public float Zoom
        {
            get => _zoom;
            set
            {
                if (float.IsFinite(value) && value > 0f)
                    _zoom = value;
            }
        }

        /// <summary>获取或设置视口宽度（像素），必须大于 0。</summary>
        public int ViewportWidth
        {
            get => _viewportWidth;
            set
            {
                if (value > 0)
                    _viewportWidth = value;
            }
        }

        /// <summary>获取或设置视口高度（像素），必须大于 0。</summary>
        public int ViewportHeight
        {
            get => _viewportHeight;
            set
            {
                if (value > 0)
                    _viewportHeight = value;
            }
        }

        /// <summary>获取世界到屏幕的变换矩阵。</summary>
        public Matrix3x2 ViewMatrix
            => Matrix3x2.CreateTranslation(-Position.X, -Position.Y)
               * Matrix3x2.CreateRotation(-Rotation)
               * Matrix3x2.CreateScale(Zoom, Zoom)
               * Matrix3x2.CreateTranslation(ViewportWidth * 0.5f, ViewportHeight * 0.5f);

        /// <summary>获取摄像机在 world 空间中覆盖的轴对齐包围盒。</summary>
        public Aabb2D ViewBounds
        {
            get
            {
                Span<CartesianVector2D> corners = stackalloc CartesianVector2D[4];
                corners[0] = ScreenToWorld(new CartesianVector2D(0, 0));
                corners[1] = ScreenToWorld(new CartesianVector2D(ViewportWidth, 0));
                corners[2] = ScreenToWorld(new CartesianVector2D(ViewportWidth, ViewportHeight));
                corners[3] = ScreenToWorld(new CartesianVector2D(0, ViewportHeight));
                return Aabb2D.FromPoints(corners);
            }
        }

        /// <summary>调整视口尺寸。</summary>
        /// <param name="width">宽度（像素）。</param>
        /// <param name="height">高度（像素）。</param>
        public void Resize(int width, int height)
        {
            ViewportWidth = width;
            ViewportHeight = height;
        }

        /// <summary>把世界坐标转换为屏幕坐标。</summary>
        /// <param name="worldPoint">世界坐标。</param>
        public CartesianVector2D WorldToScreen(CartesianVector2D worldPoint)
            => ViewMatrix.TransformPoint(worldPoint);

        /// <summary>把屏幕坐标转换为世界坐标。</summary>
        /// <param name="screenPoint">屏幕坐标。</param>
        public CartesianVector2D ScreenToWorld(CartesianVector2D screenPoint)
            => ViewMatrix.InvertOrIdentity().TransformPoint(screenPoint);
    }
}
