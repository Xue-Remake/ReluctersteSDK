using Microsoft.Xna.Framework;
using System.Runtime.CompilerServices;

namespace ReluctersteSDK.MathTool.Vectors
{
    /// <summary>
    /// 表示基于 <see cref="float"/> 的 2D 笛卡尔坐标向量 (X, Y)。<br/>
    /// 不可变 (readonly struct)，适用于高性能游戏逻辑。
    /// </summary>
    public readonly struct CartesianVector2D : IEquatable<CartesianVector2D>
    {
        /// <summary>X 轴分量。</summary>
        public readonly float X;

        /// <summary>Y 轴分量。</summary>
        public readonly float Y;

        /// <summary>
        /// 使用指定的 X 和 Y 分量初始化 <see cref="CartesianVector2D"/> 的新实例。
        /// </summary>
        /// <param name="x">X 轴分量。</param>
        /// <param name="y">Y 轴分量。</param>
        public CartesianVector2D(float x, float y)
        {
            X = x;
            Y = y;
        }

        /// <summary>获取零向量 (0, 0)。</summary>
        public static CartesianVector2D Zero => new CartesianVector2D(0f, 0f);

        /// <summary>获取全一向量 (1, 1)。</summary>
        public static CartesianVector2D One => new CartesianVector2D(1f, 1f);

        /// <summary>获取 X 轴单位向量 (1, 0)。</summary>
        public static CartesianVector2D UnitX => new CartesianVector2D(1f, 0f);

        /// <summary>获取 Y 轴单位向量 (0, 1)。</summary>
        public static CartesianVector2D UnitY => new CartesianVector2D(0f, 1f);

        /// <summary>获取向量长度的平方（避免开方计算，性能更好）。</summary>
        public float SqrMagnitude => X * X + Y * Y;

        /// <summary>获取向量的模长（模/长度）。</summary>
        public float Magnitude => (float)Math.Sqrt(SqrMagnitude);

        /// <summary>获取向量的角度（弧度，范围 [-π, π]）。</summary>
        public float AngleRad => (float)Math.Atan2(Y, X);

        /// <summary>获取向量的角度（角度制，范围 [-180, 180]）。</summary>
        public float AngleDeg => AngleRad * (180f / (float)Math.PI);

        /// <summary>
        /// 获取单位化后的向量（方向相同且长度为 1）。若模长为 0 则返回零向量。
        /// </summary>
        public CartesianVector2D Normalized
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get
            {
                float sqr = SqrMagnitude;
                if (sqr > 0f)
                {
                    float inv = 1f / (float)Math.Sqrt(sqr);
                    return new CartesianVector2D(X * inv, Y * inv);
                }
                return Zero;
            }
        }

        /// <summary>获取逆时针旋转 90 度的垂直向量 (-Y, X)。</summary>
        public CartesianVector2D PerpendicularCCW => new CartesianVector2D(-Y, X);

        /// <summary>获取顺时针旋转 90 度的垂直向量 (Y, -X)。</summary>
        public CartesianVector2D PerpendicularCW => new CartesianVector2D(Y, -X);

        /// <summary>计算与另一个向量的点积 (Dot Product)。</summary>
        /// <param name="other">另一个向量。</param>
        /// <returns>点积标量。</returns>
        public float Dot(CartesianVector2D other) => X * other.X + Y * other.Y;

        /// <summary>计算与另一个向量的 2D 叉积 (Cross Product)。</summary>
        /// <param name="other">另一个向量。</param>
        /// <returns>叉积标量（Z 轴分量的大小）。正数代表 other 在当前向量逆时针方向。</returns>
        public float Cross(CartesianVector2D other) => X * other.Y - Y * other.X;

        /// <summary>计算与另一个点之间的欧氏距离。</summary>
        /// <param name="other">目标点。</param>
        /// <returns>距离。</returns>
        public float DistanceTo(CartesianVector2D other)
        {
            float dx = X - other.X;
            float dy = Y - other.Y;
            return (float)Math.Sqrt(dx * dx + dy * dy);
        }

        /// <summary>计算与另一个点之间欧氏距离的平方（避免开方计算）。</summary>
        /// <param name="other">目标点。</param>
        /// <returns>距离平方。</returns>
        public float SqrDistanceTo(CartesianVector2D other)
        {
            float dx = X - other.X;
            float dy = Y - other.Y;
            return dx * dx + dy * dy;
        }

        /// <summary>计算从当前向量指向另一个向量的有符号夹角（弧度，范围 [-π, π]）。</summary>
        /// <param name="other">目标向量。</param>
        /// <returns>有符号弧度。</returns>
        public float AngleTo(CartesianVector2D other) => (float)Math.Atan2(Cross(other), Dot(other));

        /// <summary>将当前向量按指定的弧度旋转。</summary>
        /// <param name="angleRad">旋转弧度（正数为逆时针）。</param>
        /// <returns>旋转后的新向量。</returns>
        public CartesianVector2D Rotate(float angleRad)
        {
            float cos = (float)Math.Cos(angleRad);
            float sin = (float)Math.Sin(angleRad);
            return new CartesianVector2D(X * cos - Y * sin, X * sin + Y * cos);
        }

        /// <summary>将向量逆时针旋转 90 度。</summary>
        /// <returns>旋转后的新向量。</returns>
        public CartesianVector2D Rotate90CCW() => new CartesianVector2D(-Y, X);

        /// <summary>将向量顺时针旋转 90 度。</summary>
        /// <returns>旋转后的新向量。</returns>
        public CartesianVector2D Rotate90CW() => new CartesianVector2D(Y, -X);

        /// <summary>计算当前向量在目标向量方向上的投影向量。</summary>
        /// <param name="onto">被投影的目标向量。</param>
        /// <returns>投影向量。</returns>
        public CartesianVector2D ProjectOnto(CartesianVector2D onto)
        {
            float d = onto.Dot(onto);
            if (d == 0f) return Zero;
            return onto * (Dot(onto) / d);
        }

        /// <summary>根据表面法线计算反射向量。</summary>
        /// <param name="normal">表面法线（必须是单位向量）。</param>
        /// <returns>反射后的向量。</returns>
        public CartesianVector2D Reflect(CartesianVector2D normal)
            => this - normal * (2f * Dot(normal));

        /// <summary>转换为 <see cref="PolarVector2D"/> 极坐标向量。</summary>
        /// <returns>对应的极坐标向量。</returns>
        public PolarVector2D ToPolar() => new PolarVector2D(Magnitude, AngleRad);

        /// <summary>转换为高精度双精度笛卡尔向量 <see cref="CartesianDoubleVector2D"/>。</summary>
        /// <returns>对应的双精度笛卡尔向量。</returns>
        public CartesianDoubleVector2D ToDoubleVector2D() => new CartesianDoubleVector2D(X, Y);

        /// <summary>在两个向量之间进行线性插值 (Clamp t 在 [0, 1])。</summary>
        /// <param name="a">起始向量。</param>
        /// <param name="b">终点向量。</param>
        /// <param name="t">插值系数 [0, 1]。</param>
        /// <returns>插值结果。</returns>
        public static CartesianVector2D Lerp(CartesianVector2D a, CartesianVector2D b, float t)
        {
            t = t < 0f ? 0f : (t > 1f ? 1f : t);
            return LerpUnclamped(a, b, t);
        }

        /// <summary>在两个向量之间进行未限制范围的线性插值。</summary>
        /// <param name="a">起始向量。</param>
        /// <param name="b">终点向量。</param>
        /// <param name="t">插值系数。</param>
        /// <returns>插值结果。</returns>
        public static CartesianVector2D LerpUnclamped(CartesianVector2D a, CartesianVector2D b, float t)
            => new CartesianVector2D(a.X + (b.X - a.X) * t, a.Y + (b.Y - a.Y) * t);

        /// <summary>计算两个向量的点积。</summary>
        public static float Dot(CartesianVector2D a, CartesianVector2D b) => a.Dot(b);

        /// <summary>计算两个向量的 2D 叉积。</summary>
        public static float Cross(CartesianVector2D a, CartesianVector2D b) => a.Cross(b);

        /// <summary>计算两点之间的距离。</summary>
        public static float Distance(CartesianVector2D a, CartesianVector2D b) => a.DistanceTo(b);

        /// <summary>计算两点之间距离的平方。</summary>
        public static float SqrDistance(CartesianVector2D a, CartesianVector2D b) => a.SqrDistanceTo(b);

        /// <summary>计算从向量 <paramref name="from"/> 到向量 <paramref name="to"/> 的有符号夹角（弧度）。</summary>
        public static float AngleSigned(CartesianVector2D from, CartesianVector2D to) => from.AngleTo(to);

        /// <summary>计算两个向量之间的无符号最小夹角（弧度，范围 [0, π]）。</summary>
        /// <param name="a">向量 A。</param>
        /// <param name="b">向量 B。</param>
        /// <returns>弧度角。</returns>
        public static float Angle(CartesianVector2D a, CartesianVector2D b)
        {
            float dot = Dot(a, b);
            float denom = a.Magnitude * b.Magnitude;
            if (denom == 0f) return 0f;
            float cos = dot / denom;
            cos = cos < -1f ? -1f : (cos > 1f ? 1f : cos);
            return (float)Math.Acos(cos);
        }

        /// <summary>根据极坐标参数（半径和角度）构造笛卡尔向量。</summary>
        /// <param name="radius">半径。</param>
        /// <param name="angleRad">弧度角。</param>
        /// <returns>笛卡尔坐标向量。</returns>
        public static CartesianVector2D FromPolar(float radius, float angleRad)
            => new CartesianVector2D(radius * (float)Math.Cos(angleRad), radius * (float)Math.Sin(angleRad));

        /// <summary>向量相加。</summary>
        public static CartesianVector2D operator +(CartesianVector2D a, CartesianVector2D b)
            => new CartesianVector2D(a.X + b.X, a.Y + b.Y);

        /// <summary>向量相减。</summary>
        public static CartesianVector2D operator -(CartesianVector2D a, CartesianVector2D b)
            => new CartesianVector2D(a.X - b.X, a.Y - b.Y);

        /// <summary>向量乘以标量。</summary>
        public static CartesianVector2D operator *(CartesianVector2D a, float s)
            => new CartesianVector2D(a.X * s, a.Y * s);

        /// <summary>标量乘以向量。</summary>
        public static CartesianVector2D operator *(float s, CartesianVector2D a) => a * s;

        /// <summary>向量除以标量。</summary>
        public static CartesianVector2D operator /(CartesianVector2D a, float s)
            => new CartesianVector2D(a.X / s, a.Y / s);

        /// <summary>向量取反。</summary>
        public static CartesianVector2D operator -(CartesianVector2D a)
            => new CartesianVector2D(-a.X, -a.Y);

        /// <summary>判断与另一个向量是否完全相等。</summary>
        public bool Equals(CartesianVector2D other) => X == other.X && Y == other.Y;

        /// <summary>判断与指定对象是否完全相等。</summary>
        public override bool Equals(object obj) => obj is CartesianVector2D other && Equals(other);

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

        /// <summary>判断两个向量是否相等。</summary>
        public static bool operator ==(CartesianVector2D a, CartesianVector2D b) => a.Equals(b);

        /// <summary>判断两个向量是否不相等。</summary>
        public static bool operator !=(CartesianVector2D a, CartesianVector2D b) => !a.Equals(b);

        /// <summary>按指定容差判断两个向量是否近似相等。</summary>
        /// <param name="other">对比向量。</param>
        /// <param name="epsilon">容差阈值。</param>
        /// <returns>如果各分量差值均小于 <paramref name="epsilon"/> 则返回 true。</returns>
        public bool ApproximatelyEquals(CartesianVector2D other, float epsilon = 1e-6f)
            => Math.Abs(X - other.X) < epsilon && Math.Abs(Y - other.Y) < epsilon;

        /// <summary>返回表示当前向量的字符串格式。</summary>
        public override string ToString() => $"({X:0.###}, {Y:0.###})";


        public static implicit operator Vector2(CartesianVector2D cartesianVector2D)
        {
            return new Vector2(cartesianVector2D.X, cartesianVector2D.Y);
        }
        public static implicit operator CartesianVector2D(Vector2 vector2)
        {
            return new CartesianVector2D(vector2.X, vector2.Y);
        }
    }
}
