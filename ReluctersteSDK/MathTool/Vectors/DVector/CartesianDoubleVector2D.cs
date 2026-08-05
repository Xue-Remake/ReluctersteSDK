using System.Runtime.CompilerServices;

namespace ReluctersteSDK.MathTool.Vectors
{
    public class CartesianDoubleVector2D
    {
        /// <summary>X 轴分量。</summary>
        public readonly double X;

        /// <summary>Y 轴分量。</summary>
        public readonly double Y;

        /// <summary>
        /// 使用指定的 X 和 Y 分量初始化 <see cref="CartesianDoubleVector2D"/> 的新实例。
        /// </summary>
        /// <param name="x">X 轴分量。</param>
        /// <param name="y">Y 轴分量。</param>
        public CartesianDoubleVector2D(double x, double y)
        {
            X = x;
            Y = y;
        }

        /// <summary>获取双精度零向量 (0, 0)。</summary>
        public static CartesianDoubleVector2D Zero => new CartesianDoubleVector2D(0, 0);

        /// <summary>获取双精度全一向量 (1, 1)。</summary>
        public static CartesianDoubleVector2D One => new CartesianDoubleVector2D(1, 1);

        /// <summary>获取双精度 X 轴单位向量 (1, 0)。</summary>
        public static CartesianDoubleVector2D UnitX => new CartesianDoubleVector2D(1, 0);

        /// <summary>获取双精度 Y 轴单位向量 (0, 1)。</summary>
        public static CartesianDoubleVector2D UnitY => new CartesianDoubleVector2D(0, 1);

        /// <summary>获取向量长度的平方。</summary>
        public double SqrMagnitude => X * X + Y * Y;

        /// <summary>获取向量的模长。</summary>
        public double Magnitude => Math.Sqrt(SqrMagnitude);

        /// <summary>获取向量的角度（弧度，范围 [-π, π]）。</summary>
        public double AngleRad => Math.Atan2(Y, X);

        /// <summary>获取向量的角度（角度制，范围 [-180, 180]）。</summary>
        public double AngleDeg => AngleRad * (180.0 / Math.PI);

        /// <summary>
        /// 获取单位化后的向量（方向相同且长度为 1）。若模长为 0 则返回零向量。
        /// </summary>
        public CartesianDoubleVector2D Normalized
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get
            {
                double sqr = SqrMagnitude;
                if (sqr > 0)
                {
                    double inv = 1.0 / Math.Sqrt(sqr);
                    return new CartesianDoubleVector2D(X * inv, Y * inv);
                }
                return Zero;
            }
        }

        /// <summary>获取逆时针旋转 90 度的垂直向量 (-Y, X)。</summary>
        public CartesianDoubleVector2D PerpendicularCCW => new CartesianDoubleVector2D(-Y, X);

        /// <summary>获取顺时针旋转 90 度的垂直向量 (Y, -X)。</summary>
        public CartesianDoubleVector2D PerpendicularCW => new CartesianDoubleVector2D(Y, -X);

        /// <summary>计算与另一个双精度向量的点积。</summary>
        public double Dot(CartesianDoubleVector2D other) => X * other.X + Y * other.Y;

        /// <summary>计算与另一个双精度向量的 2D 叉积。</summary>
        public double Cross(CartesianDoubleVector2D other) => X * other.Y - Y * other.X;

        /// <summary>计算与另一个点之间的欧氏距离。</summary>
        public double DistanceTo(CartesianDoubleVector2D other)
        {
            double dx = X - other.X;
            double dy = Y - other.Y;
            return Math.Sqrt(dx * dx + dy * dy);
        }

        /// <summary>计算与另一个点之间欧氏距离的平方。</summary>
        public double SqrDistanceTo(CartesianDoubleVector2D other)
        {
            double dx = X - other.X;
            double dy = Y - other.Y;
            return dx * dx + dy * dy;
        }

        /// <summary>计算从当前向量指向另一个向量的有符号夹角（弧度，范围 [-π, π]）。</summary>
        public double AngleTo(CartesianDoubleVector2D other) => Math.Atan2(Cross(other), Dot(other));

        /// <summary>将当前向量按指定的弧度旋转。</summary>
        /// <param name="angleRad">旋转弧度。</param>
        public CartesianDoubleVector2D Rotate(double angleRad)
        {
            double cos = Math.Cos(angleRad);
            double sin = Math.Sin(angleRad);
            return new CartesianDoubleVector2D(X * cos - Y * sin, X * sin + Y * cos);
        }

        /// <summary>逆时针旋转 90 度。</summary>
        public CartesianDoubleVector2D Rotate90CCW() => new CartesianDoubleVector2D(-Y, X);

        /// <summary>顺时针旋转 90 度。</summary>
        public CartesianDoubleVector2D Rotate90CW() => new CartesianDoubleVector2D(Y, -X);

        /// <summary>计算当前向量在目标向量上的投影向量。</summary>
        public CartesianDoubleVector2D ProjectOnto(CartesianDoubleVector2D onto)
        {
            double d = onto.Dot(onto);
            if (d == 0) return Zero;
            return onto * (Dot(onto) / d);
        }

        /// <summary>根据表面法线计算反射向量。</summary>
        public CartesianDoubleVector2D Reflect(CartesianDoubleVector2D normal)
            => this - normal * (2 * Dot(normal));

        /// <summary>转换为双精度极坐标向量 <see cref="PolarDoubleVector2D"/>。</summary>
        public PolarDoubleVector2D ToPolar() => new PolarDoubleVector2D(Magnitude, AngleRad);

        /// <summary>截断转换为单精度笛卡尔向量 <see cref="CartesianVector2D"/>。</summary>
        public CartesianVector2D ToVector2() => new CartesianVector2D((float)X, (float)Y);

        /// <summary>在两个向量之间进行双精度线性插值 (Clamp t 在 [0, 1])。</summary>
        public static CartesianDoubleVector2D Lerp(CartesianDoubleVector2D a, CartesianDoubleVector2D b, double t)
        {
            t = t < 0 ? 0 : (t > 1 ? 1 : t);
            return LerpUnclamped(a, b, t);
        }

        /// <summary>在两个向量之间进行未限制范围的双精度线性插值。</summary>
        public static CartesianDoubleVector2D LerpUnclamped(CartesianDoubleVector2D a, CartesianDoubleVector2D b, double t)
            => new CartesianDoubleVector2D(a.X + (b.X - a.X) * t, a.Y + (b.Y - a.Y) * t);

        /// <summary>计算两个向量的点积。</summary>
        public static double Dot(CartesianDoubleVector2D a, CartesianDoubleVector2D b) => a.Dot(b);

        /// <summary>计算两个向量的 2D 叉积。</summary>
        public static double Cross(CartesianDoubleVector2D a, CartesianDoubleVector2D b) => a.Cross(b);

        /// <summary>计算两点之间的距离。</summary>
        public static double Distance(CartesianDoubleVector2D a, CartesianDoubleVector2D b) => a.DistanceTo(b);

        /// <summary>计算两点之间距离的平方。</summary>
        public static double SqrDistance(CartesianDoubleVector2D a, CartesianDoubleVector2D b) => a.SqrDistanceTo(b);

        /// <summary>计算从向量 <paramref name="from"/> 到向量 <paramref name="to"/> 的有符号夹角（弧度）。</summary>
        public static double AngleSigned(CartesianDoubleVector2D from, CartesianDoubleVector2D to) => from.AngleTo(to);

        /// <summary>计算两个向量之间的无符号最小夹角（弧度，范围 [0, π]）。</summary>
        public static double Angle(CartesianDoubleVector2D a, CartesianDoubleVector2D b)
        {
            double dot = Dot(a, b);
            double denom = a.Magnitude * b.Magnitude;
            if (denom == 0) return 0;
            double cos = dot / denom;
            cos = cos < -1 ? -1 : (cos > 1 ? 1 : cos);
            return Math.Acos(cos);
        }

        /// <summary>根据极坐标参数（半径和角度）构造双精度笛卡尔向量。</summary>
        public static CartesianDoubleVector2D FromPolar(double radius, double angleRad)
            => new CartesianDoubleVector2D(radius * Math.Cos(angleRad), radius * Math.Sin(angleRad));

        /// <summary>向量相加。</summary>
        public static CartesianDoubleVector2D operator +(CartesianDoubleVector2D a, CartesianDoubleVector2D b)
            => new CartesianDoubleVector2D(a.X + b.X, a.Y + b.Y);

        /// <summary>向量相减。</summary>
        public static CartesianDoubleVector2D operator -(CartesianDoubleVector2D a, CartesianDoubleVector2D b)
            => new CartesianDoubleVector2D(a.X - b.X, a.Y - b.Y);

        /// <summary>向量乘以标量。</summary>
        public static CartesianDoubleVector2D operator *(CartesianDoubleVector2D a, double s)
            => new CartesianDoubleVector2D(a.X * s, a.Y * s);

        /// <summary>标量乘以向量。</summary>
        public static CartesianDoubleVector2D operator *(double s, CartesianDoubleVector2D a) => a * s;

        /// <summary>向量除以标量。</summary>
        public static CartesianDoubleVector2D operator /(CartesianDoubleVector2D a, double s)
            => new CartesianDoubleVector2D(a.X / s, a.Y / s);

        /// <summary>向量取反。</summary>
        public static CartesianDoubleVector2D operator -(CartesianDoubleVector2D a)
            => new CartesianDoubleVector2D(-a.X, -a.Y);

        /// <summary>判断是否完全相等。</summary>
        public bool Equals(CartesianDoubleVector2D other) => X == other.X && Y == other.Y;

        /// <summary>判断与指定对象是否相等。</summary>
        public override bool Equals(object obj) => obj is CartesianDoubleVector2D other && Equals(other);

        /// <summary>获取哈希值。</summary>
        public override int GetHashCode()
        {
            unchecked
            {
                int hash = 17;
                hash = hash * 31 + X.GetHashCode();
                hash = hash * 31 + Y.GetHashCode();
                return hash;
            }
        }

        /// <summary>判断是否相等。</summary>
        public static bool operator ==(CartesianDoubleVector2D a, CartesianDoubleVector2D b) => a.Equals(b);

        /// <summary>判断是否不相等。</summary>
        public static bool operator !=(CartesianDoubleVector2D a, CartesianDoubleVector2D b) => !a.Equals(b);

        /// <summary>按指定容差判断两个向量是否近似相等。</summary>
        public bool ApproximatelyEquals(CartesianDoubleVector2D other, double epsilon = 1e-8)
            => Math.Abs(X - other.X) < epsilon && Math.Abs(Y - other.Y) < epsilon;

        /// <summary>返回字符串表达。</summary>
        public override string ToString() => $"({X:0.###}, {Y:0.###})";
    }
}
