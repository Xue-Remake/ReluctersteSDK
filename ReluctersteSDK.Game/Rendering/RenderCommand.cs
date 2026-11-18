using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace ReluctersteSDK.Game.Rendering
{
    /// <summary>
    /// 一条渲染命令：渲染系统产出、渲染器消费。
    /// <para>逻辑层只负责填写数据，不直接调用 <see cref="SpriteBatch"/>。</para>
    /// </summary>
    public struct RenderCommand
    {
        /// <summary>纹理；为 null 的命令会被渲染器跳过。</summary>
        public Texture2D? Texture;

        /// <summary>纹理采样区域。</summary>
        public Rectangle Source;

        /// <summary>世界坐标位置。</summary>
        public Vector2 Position;

        /// <summary>旋转弧度。</summary>
        public float Rotation;

        /// <summary>缩放。</summary>
        public Vector2 Scale;

        /// <summary>旋转/缩放中心。</summary>
        public Vector2 Origin;

        /// <summary>着色。</summary>
        public Color Tint;

        /// <summary>精灵特效（翻转等）。</summary>
        public SpriteEffects Effects;

        /// <summary>渲染层。</summary>
        public int Layer;

        /// <summary>同层深度。</summary>
        public float Depth;

        /// <summary>来源实体的槽位索引，用于同层同深度时的稳定排序。</summary>
        public int EntityIndex;
    }
}
