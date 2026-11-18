using System.Numerics;
using ReluctersteSDK.Math.Vectors;

namespace ReluctersteSDK.Math.Matrices
{
    /// <summary>
    /// 围绕 <see cref="Matrix3x2"/> 的 2D 仿射矩阵助手，提供与 <see cref="CartesianVector2D"/> 互操作的便捷入口。
    /// </summary>
    public static class Matrix3x2Extensions
    {
        /// <summary>创建只含平移的矩阵。</summary>
        /// <param name="position">平移分量。</param>
        public static Matrix3x2 CreateTranslation(CartesianVector2D position)
            => Matrix3x2.CreateTranslation(position.X, position.Y);

        /// <summary>创建只含缩放的矩阵。</summary>
        /// <param name="scale">缩放分量。</param>
        public static Matrix3x2 CreateScale(CartesianVector2D scale)
            => Matrix3x2.CreateScale(scale.X, scale.Y);

        /// <summary>创建只含旋转的矩阵。</summary>
        /// <param name="rotationRad">旋转弧度（正数为逆时针）。</param>
        public static Matrix3x2 CreateRotation(float rotationRad)
            => Matrix3x2.CreateRotation(rotationRad);

        /// <summary>创建"先缩放、再旋转、最后平移"的组合矩阵。</summary>
        /// <param name="position">平移分量。</param>
        /// <param name="rotationRad">旋转弧度。</param>
        /// <param name="scale">缩放分量。</param>
        public static Matrix3x2 CreateTRS(CartesianVector2D position, float rotationRad, CartesianVector2D scale)
            => Matrix3x2.CreateScale(scale.X, scale.Y)
               * Matrix3x2.CreateRotation(rotationRad)
               * Matrix3x2.CreateTranslation(position.X, position.Y);

        /// <summary>把矩阵分解为位置、旋转（弧度）与缩放。</summary>
        /// <param name="matrix">待分解矩阵。</param>
        /// <param name="position">输出：平移分量。</param>
        /// <param name="rotationRad">输出：旋转弧度。</param>
        /// <param name="scale">输出：非负缩放分量。</param>
        public static void Decompose(
            this Matrix3x2 matrix,
            out CartesianVector2D position,
            out float rotationRad,
            out CartesianVector2D scale)
        {
            Transform2DMath.Decompose(matrix, out Vector2 p, out rotationRad, out Vector2 s);
            position = new CartesianVector2D(p.X, p.Y);
            scale = new CartesianVector2D(s.X, s.Y);
        }

        /// <summary>用矩阵变换一个点（包含平移）。</summary>
        /// <param name="matrix">变换矩阵。</param>
        /// <param name="point">源点。</param>
        public static CartesianVector2D TransformPoint(this Matrix3x2 matrix, CartesianVector2D point)
        {
            Vector2 result = Vector2.Transform(new Vector2(point.X, point.Y), matrix);
            return new CartesianVector2D(result.X, result.Y);
        }

        /// <summary>用矩阵变换一个方向向量（忽略平移）。</summary>
        /// <param name="matrix">变换矩阵。</param>
        /// <param name="vector">源向量。</param>
        public static CartesianVector2D TransformVector(this Matrix3x2 matrix, CartesianVector2D vector)
        {
            Vector2 result = Vector2.TransformNormal(new Vector2(vector.X, vector.Y), matrix);
            return new CartesianVector2D(result.X, result.Y);
        }

        /// <summary>尝试求逆矩阵；奇异矩阵返回 <c>false</c>。</summary>
        /// <param name="matrix">源矩阵。</param>
        /// <param name="inverse">输出：逆矩阵。</param>
        public static bool TryInvert(this Matrix3x2 matrix, out Matrix3x2 inverse)
        {
            float det = matrix.M11 * matrix.M22 - matrix.M12 * matrix.M21;
            if (MathF.Abs(det) < 1e-8f)
            {
                inverse = Matrix3x2.Identity;
                return false;
            }

            Matrix3x2.Invert(matrix, out inverse);
            return true;
        }

        /// <summary>求逆矩阵；奇异矩阵返回单位矩阵。</summary>
        /// <param name="matrix">源矩阵。</param>
        public static Matrix3x2 InvertOrIdentity(this Matrix3x2 matrix)
            => matrix.TryInvert(out Matrix3x2 inverse) ? inverse : Matrix3x2.Identity;

        /// <summary>按分量线性插值两个矩阵。</summary>
        /// <param name="a">起始矩阵。</param>
        /// <param name="b">目标矩阵。</param>
        /// <param name="t">插值系数（不 clamp）。</param>
        public static Matrix3x2 Lerp(Matrix3x2 a, Matrix3x2 b, float t)
            => new Matrix3x2(
                a.M11 + (b.M11 - a.M11) * t,
                a.M12 + (b.M12 - a.M12) * t,
                a.M21 + (b.M21 - a.M21) * t,
                a.M22 + (b.M22 - a.M22) * t,
                a.M31 + (b.M31 - a.M31) * t,
                a.M32 + (b.M32 - a.M32) * t);

        /// <summary>按容差判断两个矩阵是否近似相等。</summary>
        /// <param name="a">矩阵 A。</param>
        /// <param name="b">矩阵 B。</param>
        /// <param name="epsilon">分量容差。</param>
        public static bool ApproximatelyEquals(this Matrix3x2 a, Matrix3x2 b, float epsilon = 1e-6f)
            => MathF.Abs(a.M11 - b.M11) < epsilon
               && MathF.Abs(a.M12 - b.M12) < epsilon
               && MathF.Abs(a.M21 - b.M21) < epsilon
               && MathF.Abs(a.M22 - b.M22) < epsilon
               && MathF.Abs(a.M31 - b.M31) < epsilon
               && MathF.Abs(a.M32 - b.M32) < epsilon;
    }
}
