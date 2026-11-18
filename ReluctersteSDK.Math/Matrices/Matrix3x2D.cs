using System.Numerics;
using ReluctersteSDK.Math.Vectors;

namespace ReluctersteSDK.Math.Matrices
{
    /// <summary>
    /// 双精度 2D 仿射矩阵（行向量约定），字段布局与 <see cref="Matrix3x2"/> 一致。
    /// </summary>
    public readonly struct Matrix3x2D : IEquatable<Matrix3x2D>
    {
        /// <summary>第一行第一列。</summary>
        public readonly double M11;
        /// <summary>第一行第二列。</summary>
        public readonly double M12;
        /// <summary>第二行第一列。</summary>
        public readonly double M21;
        /// <summary>第二行第二列。</summary>
        public readonly double M22;
        /// <summary>第三行第一列（X 平移）。</summary>
        public readonly double M31;
        /// <summary>第三行第二列（Y 平移）。</summary>
        public readonly double M32;

        /// <summary>初始化一个双精度 2D 仿射矩阵。</summary>
        public Matrix3x2D(double m11, double m12, double m21, double m22, double m31, double m32)
        {
            M11 = m11;
            M12 = m12;
            M21 = m21;
            M22 = m22;
            M31 = m31;
            M32 = m32;
        }

        /// <summary>获取单位矩阵。</summary>
        public static Matrix3x2D Identity => new Matrix3x2D(1, 0, 0, 1, 0, 0);

        /// <summary>获取零矩阵。</summary>
        public static Matrix3x2D Zero => default;

        /// <summary>获取矩阵行列式。</summary>
        public double Determinant => M11 * M22 - M12 * M21;

        /// <summary>创建只含平移的矩阵。</summary>
        /// <param name="position">平移分量。</param>
        public static Matrix3x2D CreateTranslation(CartesianDoubleVector2D position)
            => new Matrix3x2D(1, 0, 0, 1, position.X, position.Y);

        /// <summary>创建只含缩放的矩阵。</summary>
        /// <param name="scale">缩放分量。</param>
        public static Matrix3x2D CreateScale(CartesianDoubleVector2D scale)
            => new Matrix3x2D(scale.X, 0, 0, scale.Y, 0, 0);

        /// <summary>创建只含旋转的矩阵。</summary>
        /// <param name="rotationRad">旋转弧度（正数为逆时针）。</param>
        public static Matrix3x2D CreateRotation(double rotationRad)
        {
            double cos = SystemMath.Cos(rotationRad);
            double sin = SystemMath.Sin(rotationRad);
            return new Matrix3x2D(cos, sin, -sin, cos, 0, 0);
        }

        /// <summary>创建"先缩放、再旋转、最后平移"的组合矩阵。</summary>
        /// <param name="position">平移分量。</param>
        /// <param name="rotationRad">旋转弧度。</param>
        /// <param name="scale">缩放分量。</param>
        public static Matrix3x2D CreateTRS(CartesianDoubleVector2D position, double rotationRad, CartesianDoubleVector2D scale)
            => CreateScale(scale) * CreateRotation(rotationRad) * CreateTranslation(position);

        /// <summary>用矩阵变换一个点（包含平移）。</summary>
        /// <param name="point">源点。</param>
        public CartesianDoubleVector2D TransformPoint(CartesianDoubleVector2D point)
            => new CartesianDoubleVector2D(
                point.X * M11 + point.Y * M21 + M31,
                point.X * M12 + point.Y * M22 + M32);

        /// <summary>用矩阵变换一个方向向量（忽略平移）。</summary>
        /// <param name="vector">源向量。</param>
        public CartesianDoubleVector2D TransformVector(CartesianDoubleVector2D vector)
            => new CartesianDoubleVector2D(
                vector.X * M11 + vector.Y * M21,
                vector.X * M12 + vector.Y * M22);

        /// <summary>尝试求逆矩阵；奇异矩阵返回 <c>false</c>。</summary>
        /// <param name="inverse">输出：逆矩阵。</param>
        public bool TryInvert(out Matrix3x2D inverse)
        {
            double det = Determinant;
            if (SystemMath.Abs(det) < 1e-12)
            {
                inverse = Identity;
                return false;
            }

            double invDet = 1.0 / det;
            inverse = new Matrix3x2D(
                M22 * invDet,
                -M12 * invDet,
                -M21 * invDet,
                M11 * invDet,
                (M21 * M32 - M22 * M31) * invDet,
                (M12 * M31 - M11 * M32) * invDet);
            return true;
        }

        /// <summary>把矩阵分解为位置、旋转（弧度）与缩放。</summary>
        /// <param name="position">输出：平移分量。</param>
        /// <param name="rotationRad">输出：旋转弧度。</param>
        /// <param name="scale">输出：非负缩放分量。</param>
        public void Decompose(out CartesianDoubleVector2D position, out double rotationRad, out CartesianDoubleVector2D scale)
        {
            position = new CartesianDoubleVector2D(M31, M32);
            rotationRad = SystemMath.Atan2(M12, M11);
            scale = new CartesianDoubleVector2D(
                SystemMath.Sqrt(M11 * M11 + M12 * M12),
                SystemMath.Sqrt(M21 * M21 + M22 * M22));
        }

        /// <summary>矩阵乘法（行向量约定）。</summary>
        public static Matrix3x2D operator *(Matrix3x2D a, Matrix3x2D b)
            => new Matrix3x2D(
                a.M11 * b.M11 + a.M12 * b.M21,
                a.M11 * b.M12 + a.M12 * b.M22,
                a.M21 * b.M11 + a.M22 * b.M21,
                a.M21 * b.M12 + a.M22 * b.M22,
                a.M31 * b.M11 + a.M32 * b.M21 + b.M31,
                a.M31 * b.M12 + a.M32 * b.M22 + b.M32);

        /// <summary>转换为单精度矩阵。</summary>
        public Matrix3x2 ToFloatMatrix()
            => new Matrix3x2((float)M11, (float)M12, (float)M21, (float)M22, (float)M31, (float)M32);

        /// <summary>从单精度矩阵构造双精度矩阵。</summary>
        /// <param name="matrix">源矩阵。</param>
        public static Matrix3x2D FromFloatMatrix(Matrix3x2 matrix)
            => new Matrix3x2D(matrix.M11, matrix.M12, matrix.M21, matrix.M22, matrix.M31, matrix.M32);

        /// <summary>判断两个矩阵是否完全相等。</summary>
        public bool Equals(Matrix3x2D other)
            => M11 == other.M11 && M12 == other.M12 && M21 == other.M21
               && M22 == other.M22 && M31 == other.M31 && M32 == other.M32;

        /// <inheritdoc />
        public override bool Equals(object? obj) => obj is Matrix3x2D other && Equals(other);

        /// <inheritdoc />
        public override int GetHashCode()
        {
            unchecked
            {
                int hash = 17;
                hash = hash * 31 + M11.GetHashCode();
                hash = hash * 31 + M12.GetHashCode();
                hash = hash * 31 + M21.GetHashCode();
                hash = hash * 31 + M22.GetHashCode();
                hash = hash * 31 + M31.GetHashCode();
                hash = hash * 31 + M32.GetHashCode();
                return hash;
            }
        }

        /// <summary>判断两个矩阵是否相等。</summary>
        public static bool operator ==(Matrix3x2D a, Matrix3x2D b) => a.Equals(b);

        /// <summary>判断两个矩阵是否不相等。</summary>
        public static bool operator !=(Matrix3x2D a, Matrix3x2D b) => !a.Equals(b);

        /// <summary>按容差判断两个矩阵是否近似相等。</summary>
        public bool ApproximatelyEquals(Matrix3x2D other, double epsilon = 1e-8)
            => SystemMath.Abs(M11 - other.M11) < epsilon
               && SystemMath.Abs(M12 - other.M12) < epsilon
               && SystemMath.Abs(M21 - other.M21) < epsilon
               && SystemMath.Abs(M22 - other.M22) < epsilon
               && SystemMath.Abs(M31 - other.M31) < epsilon
               && SystemMath.Abs(M32 - other.M32) < epsilon;

        /// <inheritdoc />
        public override string ToString()
            => $"[{M11:0.###}, {M12:0.###}; {M21:0.###}, {M22:0.###}; {M31:0.###}, {M32:0.###}]";
    }
}
