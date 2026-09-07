namespace ReluctersteSDK.MathTool.Vectors
{
    /// <summary>
    /// 表示基于 <see cref="float"/> 的 2D 极坐标向量 (Radius, Angle)。<br/>
    /// 构造函数会自动规范化：负半径转为正半径，角度限制在 [0, 2π) 范围。
    /// </summary>
    public readonly struct PolarVector2D : IEquatable<PolarVector2D>
    {
        /// <summary>极径（半径），始终保持非负。</summary>
        public readonly float Radius;

        /// <summary>极角（弧度），始终规范化至 [0, 2π) 范围。</summary>
        public readonly float Angle;

        /// <summary>
        /// 使用指定的极径与极角初始化 <see cref="PolarVector2D"/> 的新实例。
        /// </summary>
        /// <param name="radius">极径（若为负数会自动取绝对值并反转角度）。</param>
        /// <param name="angle">极角（弧度，会自动规范化至 [0, 2π)）。</param>
        public PolarVector2D(float radius, float angle)
        {
            if (radius < 0f)
            {
                radius = -radius;
                angle += (float)Math.PI;
            }

            float twoPi = 2f * (float)Math.PI;
            angle %= twoPi;
            if (angle < 0f) angle += twoPi;

            if (radius == 0f) angle = 0f;

            Radius = radius;
            Angle = angle;
        }

        /// <summary>获取极坐标零向量 (0, 0)。</summary>
        public static PolarVector2D Zero => new PolarVector2D(0f, 0f);

        /// <summary>获取极坐标单位向量 (1, 0)。</summary>
        public static PolarVector2D One => new PolarVector2D(1f, 0f);

        /// <summary>获取转换到笛卡尔坐标的 X 分量。</summary>
        public float X => Radius * (float)Math.Cos(Angle);

        /// <summary>获取转换到笛卡尔坐标的 Y 分量。</summary>
        public float Y => Radius * (float)Math.Sin(Angle);

        /// <summary>获取极径的平方。</summary>
        public float SqrMagnitude => Radius * Radius;

        /// <summary>获取模长（即极径 Radius）。</summary>
        public float Magnitude => Radius;

        /// <summary>获取极角的角度制表示 (0 到 360 度)。</summary>
        public float AngleDegrees => Angle * (180f / (float)Math.PI);

        /// <summary>获取方向单位向量（笛卡尔坐标）。</summary>
        public CartesianVector2D Direction => new CartesianVector2D((float)Math.Cos(Angle), (float)Math.Sin(Angle));

        /// <summary>转换为 <see cref="CartesianVector2D"/> 笛卡尔坐标向量。</summary>
        /// <returns>对应的笛卡尔坐标向量。</returns>
        public CartesianVector2D ToCartesian() => new CartesianVector2D(X, Y);

        /// <summary>旋转极角。</summary>
        /// <param name="deltaAngleRad">旋转的增量弧度。</param>
        /// <returns>旋转后的新极坐标向量。</returns>
        public PolarVector2D Rotate(float deltaAngleRad) => new PolarVector2D(Radius, Angle + deltaAngleRad);

        /// <summary>按系数缩放半径。</summary>
        /// <param name="factor">缩放系数。</param>
        /// <returns>缩放后的新极坐标向量。</returns>
        public PolarVector2D ScaleRadius(float factor) => new PolarVector2D(Radius * factor, Angle);

        /// <summary>返回一个替换了极径的新极坐标向量。</summary>
        /// <param name="newRadius">新的极径。</param>
        public PolarVector2D WithRadius(float newRadius) => new PolarVector2D(newRadius, Angle);

        /// <summary>返回一个替换了极角的新极坐标向量。</summary>
        /// <param name="newAngle">新的极角（弧度）。</param>
        public PolarVector2D WithAngle(float newAngle) => new PolarVector2D(Radius, newAngle);

        /// <summary>计算从当前极角指向目标极角的最短有符号弧度差（范围 [-π, π]）。</summary>
        /// <param name="other">目标极坐标向量。</param>
        /// <returns>有符号弧度差。</returns>
        public float AngleTo(PolarVector2D other)
        {
            float diff = other.Angle - Angle;
            float twoPi = 2f * (float)Math.PI;
            diff = ((diff + (float)Math.PI) % twoPi + twoPi) % twoPi - (float)Math.PI;
            return diff;
        }

        /// <summary>计算点积。</summary>
        public float Dot(PolarVector2D other) => Radius * other.Radius * (float)Math.Cos(AngleTo(other));

        /// <summary>计算 2D 叉积。</summary>
        public float Cross(PolarVector2D other) => Radius * other.Radius * (float)Math.Sin(AngleTo(other));

        /// <summary>
        /// 在两个极坐标向量之间进行插值。半径线性插值，角度沿着最短路径插值。
        /// </summary>
        /// <param name="a">起始向量。</param>
        /// <param name="b">目标向量。</param>
        /// <param name="t">插值系数 [0, 1]。</param>
        /// <returns>插值得到的极坐标向量。</returns>
        public static PolarVector2D Lerp(PolarVector2D a, PolarVector2D b, float t)
        {
            t = t < 0f ? 0f : (t > 1f ? 1f : t);
            float radius = a.Radius + (b.Radius - a.Radius) * t;
            float angle = a.Angle + a.AngleTo(b) * t;
            return new PolarVector2D(radius, angle);
        }

        /// <summary>转换为双精度极坐标向量 <see cref="PolarDoubleVector2D"/>。</summary>
        public PolarDoubleVector2D ToDoublePolar() => new PolarDoubleVector2D(Radius, Angle);

        /// <summary>转换为双精度笛卡尔坐标向量 <see cref="CartesianDoubleVector2D"/>。</summary>
        public CartesianDoubleVector2D ToDoubleCartesian() => ToCartesian().ToDoubleVector2D();

        /// <summary>极坐标加法（底层转换为笛卡尔坐标计算后转回）。</summary>
        public static PolarVector2D operator +(PolarVector2D a, PolarVector2D b)
            => (a.ToCartesian() + b.ToCartesian()).ToPolar();

        /// <summary>极坐标减法（底层转换为笛卡尔坐标计算后转回）。</summary>
        public static PolarVector2D operator -(PolarVector2D a, PolarVector2D b)
            => (a.ToCartesian() - b.ToCartesian()).ToPolar();

        /// <summary>按标量缩放半径。</summary>
        public static PolarVector2D operator *(PolarVector2D a, float s)
            => new PolarVector2D(a.Radius * s, a.Angle);

        /// <summary>按标量缩放半径。</summary>
        public static PolarVector2D operator *(float s, PolarVector2D a) => a * s;

        /// <summary>半径除以标量。</summary>
        public static PolarVector2D operator /(PolarVector2D a, float s)
            => new PolarVector2D(a.Radius / s, a.Angle);

        /// <summary>向量取反（方向旋转 π 弧度）。</summary>
        public static PolarVector2D operator -(PolarVector2D a)
            => new PolarVector2D(a.Radius, a.Angle + (float)Math.PI);

        /// <summary>显式转换极坐标为笛卡尔坐标。</summary>
        public static explicit operator CartesianVector2D(PolarVector2D polar) => polar.ToCartesian();

        /// <summary>显式转换笛卡尔坐标为极坐标。</summary>
        public static explicit operator PolarVector2D(CartesianVector2D cart) => cart.ToPolar();

        /// <summary>判断是否相等（半径与角度完全一致）。</summary>
        public bool Equals(PolarVector2D other) => Radius == other.Radius && Angle == other.Angle;

        /// <summary>判断与指定对象是否相等。</summary>
        public override bool Equals(object obj) => obj is PolarVector2D other && Equals(other);

        /// <summary>获取哈希值。</summary>
        public override int GetHashCode()
        {
            unchecked
            {
                int hash = 17;
                hash = hash * 31 + Radius.GetHashCode();
                hash = hash * 31 + Angle.GetHashCode();
                return hash;
            }
        }

        /// <summary>判断是否相等。</summary>
        public static bool operator ==(PolarVector2D a, PolarVector2D b) => a.Equals(b);

        /// <summary>判断是否不相等。</summary>
        public static bool operator !=(PolarVector2D a, PolarVector2D b) => !a.Equals(b);

        /// <summary>按指定容差判断两个极坐标向量是否近似相等。</summary>
        /// <param name="other">对比向量。</param>
        /// <param name="radiusEpsilon">半径容差。</param>
        /// <param name="angleEpsilon">角度容差（弧度）。</param>
        public bool ApproximatelyEquals(PolarVector2D other, float radiusEpsilon = 1e-6f, float angleEpsilon = 1e-6f)
            => Math.Abs(Radius - other.Radius) < radiusEpsilon &&
               Math.Abs(AngleTo(other)) < angleEpsilon;

        /// <summary>返回字符串表达。</summary>
        public override string ToString() => $"({Radius:0.###}, {Angle:0.###} rad)";
    }
}
