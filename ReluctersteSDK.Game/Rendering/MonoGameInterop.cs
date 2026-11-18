using Microsoft.Xna.Framework;
using ReluctersteSDK.Math.Vectors;

namespace ReluctersteSDK.Game.Rendering
{
    /// <summary>
    /// <see cref="ReluctersteSDK.Math"/> 向量与 MonoGame <see cref="Vector2"/> 的显式互转。
    /// <para>
    /// Math 库本身零 MonoGame 依赖，因此不再提供隐式转换；需要与渲染层交互时调用这里的扩展方法。
    /// </para>
    /// </summary>
    public static class MonoGameInterop
    {
        /// <summary>把 <see cref="CartesianVector2D"/> 转为 MonoGame <see cref="Vector2"/>。</summary>
        /// <param name="vector">源向量。</param>
        public static Vector2 ToXna(this CartesianVector2D vector)
            => new Vector2(vector.X, vector.Y);

        /// <summary>把 MonoGame <see cref="Vector2"/> 转为 <see cref="CartesianVector2D"/>。</summary>
        /// <param name="vector">源向量。</param>
        public static CartesianVector2D ToCartesian(this Vector2 vector)
            => new CartesianVector2D(vector.X, vector.Y);
    }
}
