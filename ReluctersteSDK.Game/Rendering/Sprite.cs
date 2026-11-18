using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace ReluctersteSDK.Game.Rendering
{
    /// <summary>
    /// 精灵渲染组件。
    /// </summary>
    /// <remarks>
    /// 设计为**引用类型**而非结构体：<see cref="Color"/> 的 <c>default</c> 是透明黑，
    /// 若用 <c>struct</c>，忘记初始化 <see cref="Tint"/> 就会导致精灵完全不可见。
    /// 用构造函数强制传入纹理并给出合理默认值，可以彻底消除这个陷阱。
    /// </remarks>
    public sealed class Sprite
    {
        /// <summary>纹理。</summary>
        public Texture2D Texture;

        /// <summary>纹理采样区域；默认取整张纹理。</summary>
        public Rectangle Source;

        /// <summary>着色，默认不透明白色。</summary>
        public Color Tint = Color.White;

        /// <summary>渲染层；数值小的先画。</summary>
        public int Layer;

        /// <summary>同层内的深度；数值小的先画。</summary>
        public float Depth;

        /// <summary>旋转/缩放的中心点（像素单位），默认左上角。</summary>
        public Vector2 Origin;

        /// <summary>翻转等精灵特效。</summary>
        public SpriteEffects Effects = SpriteEffects.None;

        /// <summary>使用整张纹理初始化精灵。</summary>
        /// <param name="texture">要绘制的纹理。</param>
        /// <exception cref="ArgumentNullException">纹理为 null。</exception>
        public Sprite(Texture2D texture)
        {
            Texture = texture ?? throw new ArgumentNullException(nameof(texture));
            Source = new Rectangle(0, 0, texture.Width, texture.Height);
        }

        /// <summary>使用指定的采样区域初始化精灵。</summary>
        /// <param name="texture">要绘制的纹理。</param>
        /// <param name="source">纹理采样区域。</param>
        public Sprite(Texture2D texture, Rectangle source)
            : this(texture)
        {
            Source = source;
        }
    }
}
