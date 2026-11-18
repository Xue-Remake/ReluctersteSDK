using System.Numerics;

namespace ReluctersteSDK.Math.Matrices
{
    /// <summary>
    /// 2D 矩阵与"位置/旋转/缩放"之间的互转（纯 <see cref="System.Numerics"/>，不依赖 MonoGame）。
    /// </summary>
    public static class Transform2DMath
    {
        /// <summary>
        /// 把 2D 世界矩阵分解为位置、旋转（弧度）与缩放。
        /// </summary>
        /// <param name="matrix">待分解的矩阵（行向量约定）。</param>
        /// <param name="position">输出：平移分量。</param>
        /// <param name="rotation">输出：旋转弧度，范围 [-π, π]。</param>
        /// <param name="scale">输出：非负缩放分量。</param>
        /// <remarks>
        /// 只支持"缩放 + 旋转 + 平移"构成的矩阵；带切变或负缩放的矩阵无法无损分解，
        /// 负缩放请改用 <c>SpriteEffects.FlipHorizontally</c> 表达。
        /// </remarks>
        public static void Decompose(Matrix3x2 matrix, out Vector2 position, out float rotation, out Vector2 scale)
        {
            position = new Vector2(matrix.M31, matrix.M32);
            rotation = MathF.Atan2(matrix.M12, matrix.M11);
            scale = new Vector2(
                MathF.Sqrt(matrix.M11 * matrix.M11 + matrix.M12 * matrix.M12),
                MathF.Sqrt(matrix.M21 * matrix.M21 + matrix.M22 * matrix.M22));
        }

        /// <summary>
        /// 由位置、旋转（弧度）与缩放组合出 2D 矩阵（先缩放、再旋转、最后平移）。
        /// </summary>
        /// <param name="position">平移分量。</param>
        /// <param name="rotation">旋转弧度。</param>
        /// <param name="scale">缩放分量。</param>
        public static Matrix3x2 Compose(Vector2 position, float rotation, Vector2 scale)
        {
            float cos = MathF.Cos(rotation);
            float sin = MathF.Sin(rotation);

            return new Matrix3x2(
                scale.X * cos, scale.X * sin,
                -scale.Y * sin, scale.Y * cos,
                position.X, position.Y);
        }
    }
}
