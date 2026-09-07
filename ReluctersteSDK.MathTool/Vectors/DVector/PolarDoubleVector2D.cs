namespace ReluctersteSDK.MathTool.Vectors
{
    /// <summary>
    /// 表示基于 <see cref="double"/> 的 2D 双精度极坐标向量 (Radius, Angle)。<br/>
    /// 构造函数会自动规范化：负半径转为正半径，角度限制在 [0, 2π) 范围。
    /// </summary>
    public readonly struct PolarDoubleVector2D : IEquatable<PolarDoubleVector2D>
    {
        /// <summary>极径（半径），始终保持非负。</summary>
        public readonly double Radius;

        /// <summary>极角（弧度），始终规范化至 [0, 2π) 范围。</summary>
        public readonly double Angle;

        /// <summary>
        /// 使用指定的极径与极角初始化 <see cref="PolarDoubleVector2D"/> 的新实例。
        /// </summary>
        /// <param name="radius">极径。</param>
        /// <param name="angle">极角（弧度）。</param>
        public PolarDoubleVector2D(double radius, double angle)
        {
            if (radius < 0)
            {
                radius = -radius;
                angle += Math.PI;
            }

            double twoPi = 2.0 * Math.PI;
            angle %= twoPi;
            if (angle < 0) angle += twoPi;

            if (radius == 0) angle = 0;

            Radius = radius;
            Angle = angle;
        }

        /// <summary>获取双精度极坐标零向量 (0, 0)。</summary>
        public static PolarDoubleVector2D Zero => new PolarDoubleVector2D(0, 0);

        /// <summary>获取双精度极坐标单位向量 (1, 0)。</summary>
        public static PolarDoubleVector2D One => new PolarDoubleVector2D(1, 0);

        /// <summary>获取转换到笛卡尔坐标的 X 分量。</summary>
        public double X => Radius * Math.Cos(Angle);

        /// <summary>获取转换到笛卡尔坐标的 Y 分量。</summary>
        public double Y => Radius * Math.Sin(Angle);

        /// <summary>获取极径的平方。</summary>
        public double SqrMagnitude => Radius * Radius;

        /// <summary>获取模长（即极径 Radius）。</summary>
        public double Magnitude => Radius;

        /// <summary>获取极角的角度制表示 (0 到 360 度)。</summary>
        public double AngleDegrees => Angle * (180.0 / Math.PI);

        /// <summary>获取方向单位向量（双精度笛卡尔坐标）。</summary>
        public CartesianDoubleVector2D Direction => new CartesianDoubleVector2D(Math.Cos(Angle), Math.Sin(Angle));

        /// <summary>转换为 <see cref="CartesianDoubleVector2D"/> 双精度笛卡尔坐标向量。</summary>
        public CartesianDoubleVector2D ToCartesian() => new CartesianDoubleVector2D(X, Y);

        /// <summary>旋转极角。</summary>
        /// <param name="deltaAngleRad">增量弧度。</param>
        public PolarDoubleVector2D Rotate(double deltaAngleRad) => new PolarDoubleVector2D(Radius, Angle + deltaAngleRad);

        /// <summary>按系数缩放半径。</summary>
        public PolarDoubleVector2D ScaleRadius(double factor) => new PolarDoubleVector2D(Radius * factor, Angle);

        /// <summary>返回一个替换了极径的新极坐标向量。</summary>
        public PolarDoubleVector2D WithRadius(double newRadius) => new PolarDoubleVector2D(newRadius, Angle);

        /// <summary>返回一个替换了极角的新极坐标向量。</summary>
        public PolarDoubleVector2D WithAngle(double newAngle) => new PolarDoubleVector2D(Radius, newAngle);

        /// <summary>计算从当前极角指向目标极角的最短有符号弧度差（范围 [-π, π]）。</summary>
        public double AngleTo(PolarDoubleVector2D other)
        {
            double diff = other.Angle - Angle;
            double twoPi = 2.0 * Math.PI;
            diff = ((diff + Math.PI) % twoPi + twoPi) % twoPi - Math.PI;
            return diff;
        }

        /// <summary>计算点积。</summary>
        public double Dot(PolarDoubleVector2D other) => Radius * other.Radius * Math.Cos(AngleTo(other));

        /// <summary>计算 2D 叉积。</summary>
        public double Cross(PolarDoubleVector2D other) => Radius * other.Radius * Math.Sin(AngleTo(other));

        /// <summary>
        /// 在两个极坐标向量之间进行双精度插值。半径线性插值，角度沿着最短路径插值。
        /// </summary>
        public static PolarDoubleVector2D Lerp(PolarDoubleVector2D a, PolarDoubleVector2D b, double t)
        {
            t = t < 0 ? 0 : (t > 1 ? 1 : t);
            double radius = a.Radius + (b.Radius - a.Radius) * t;
            double angle = a.Angle + a.AngleTo(b) * t;
            return new PolarDoubleVector2D(radius, angle);
        }

        /// <summary>转换为单精度极坐标向量 <see cref="PolarVector2D"/>。</summary>
        public PolarVector2D ToPolar() => new PolarVector2D((float)Radius, (float)Angle);

        /// <summary>转换为单精度笛卡尔坐标向量 <see cref="CartesianVector2D"/>。</summary>
        public CartesianVector2D ToVector2Cartesian() => new CartesianVector2D((float)X, (float)Y);

        /// <summary>极坐标加法。</summary>
        public static PolarDoubleVector2D operator +(PolarDoubleVector2D a, PolarDoubleVector2D b)
            => (a.ToCartesian() + b.ToCartesian()).ToPolar();

        /// <summary>极坐标减法。</summary>
        public static PolarDoubleVector2D operator -(PolarDoubleVector2D a, PolarDoubleVector2D b)
            => (a.ToCartesian() - b.ToCartesian()).ToPolar();

        /// <summary>按标量缩放半径。</summary>
        public static PolarDoubleVector2D operator *(PolarDoubleVector2D a, double s)
            => new PolarDoubleVector2D(a.Radius * s, a.Angle);

        /// <summary>按标量缩放半径。</summary>
        public static PolarDoubleVector2D operator *(double s, PolarDoubleVector2D a) => a * s;

        /// <summary>半径除以标量。</summary>
        public static PolarDoubleVector2D operator /(PolarDoubleVector2D a, double s)
            => new PolarDoubleVector2D(a.Radius / s, a.Angle);

        /// <summary>向量取反（旋转 π 弧度）。</summary>
        public static PolarDoubleVector2D operator -(PolarDoubleVector2D a)
            => new PolarDoubleVector2D(a.Radius, a.Angle + Math.PI);

        /// <summary>显式转换极坐标为笛卡尔坐标。</summary>
        public static explicit operator CartesianDoubleVector2D(PolarDoubleVector2D polar) => polar.ToCartesian();

        /// <summary>显式转换笛卡尔坐标为极坐标。</summary>
        public static explicit operator PolarDoubleVector2D(CartesianDoubleVector2D cart) => cart.ToPolar();

        /// <summary>判断是否相等。</summary>
        public bool Equals(PolarDoubleVector2D other) => Radius == other.Radius && Angle == other.Angle;

        /// <summary>判断与指定对象是否相等。</summary>
        public override bool Equals(object obj) => obj is PolarDoubleVector2D other && Equals(other);

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
        public static bool operator ==(PolarDoubleVector2D a, PolarDoubleVector2D b) => a.Equals(b);

        /// <summary>判断是否不相等。</summary>
        public static bool operator !=(PolarDoubleVector2D a, PolarDoubleVector2D b) => !a.Equals(b);

        /// <summary>按指定容差判断两个双精度极坐标向量是否近似相等。</summary>
        public bool ApproximatelyEquals(PolarDoubleVector2D other, double radiusEpsilon = 1e-8, double angleEpsilon = 1e-8)
            => Math.Abs(Radius - other.Radius) < radiusEpsilon &&
               Math.Abs(AngleTo(other)) < angleEpsilon;

        /// <summary>返回字符串表达。</summary>
        public override string ToString() => $"({Radius:0.###}, {Angle:0.###} rad)";
    }
}
