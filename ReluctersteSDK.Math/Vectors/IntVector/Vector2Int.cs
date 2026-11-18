namespace ReluctersteSDK.Math.Vectors
{
    /// <summary>
    /// 表示基于 <see cref="int"/> 的 2D 整数坐标向量 (X, Y)，适合网格坐标、瓦片索引与像素对齐。
    /// </summary>
    public readonly struct Vector2Int : IEquatable<Vector2Int>
    {
        /// <summary>X 轴分量。</summary>
        public readonly int X;

        /// <summary>Y 轴分量。</summary>
        public readonly int Y;

        /// <summary>使用指定的 X 和 Y 分量初始化 <see cref="Vector2Int"/>。</summary>
        /// <param name="x">X 轴分量。</param>
        /// <param name="y">Y 轴分量。</param>
        public Vector2Int(int x, int y)
        {
            X = x;
            Y = y;
        }

        /// <summary>获取零向量 (0, 0)。</summary>
        public static Vector2Int Zero => new Vector2Int(0, 0);

        /// <summary>获取全一向量 (1, 1)。</summary>
        public static Vector2Int One => new Vector2Int(1, 1);

        /// <summary>获取 X 轴单位向量 (1, 0)。</summary>
        public static Vector2Int UnitX => new Vector2Int(1, 0);

        /// <summary>获取 Y 轴单位向量 (0, 1)。</summary>
        public static Vector2Int UnitY => new Vector2Int(0, 1);

        /// <summary>获取曼哈顿距离（各分量绝对值之和）。</summary>
        public int LengthManhattan => SystemMath.Abs(X) + SystemMath.Abs(Y);

        /// <summary>获取欧氏距离的平方。</summary>
        public int SqrMagnitude => X * X + Y * Y;

        /// <summary>转换为浮点笛卡尔向量。</summary>
        public CartesianVector2D ToCartesianVector2D() => new CartesianVector2D(X, Y);

        /// <summary>从浮点笛卡尔向量构造整数向量。</summary>
        /// <param name="value">源向量。</param>
        /// <param name="rounding">取整方式，默认四舍五入。</param>
        public static Vector2Int FromCartesian(CartesianVector2D value, MidpointRounding rounding = MidpointRounding.AwayFromZero)
            => new Vector2Int(
                (int)SystemMath.Round(value.X, rounding),
                (int)SystemMath.Round(value.Y, rounding));

        /// <summary>向量相加。</summary>
        public static Vector2Int operator +(Vector2Int a, Vector2Int b) => new Vector2Int(a.X + b.X, a.Y + b.Y);

        /// <summary>向量相减。</summary>
        public static Vector2Int operator -(Vector2Int a, Vector2Int b) => new Vector2Int(a.X - b.X, a.Y - b.Y);

        /// <summary>向量乘以整数标量。</summary>
        public static Vector2Int operator *(Vector2Int a, int s) => new Vector2Int(a.X * s, a.Y * s);

        /// <summary>整数标量乘以向量。</summary>
        public static Vector2Int operator *(int s, Vector2Int a) => a * s;

        /// <summary>向量除以整数标量（整数除法）。</summary>
        public static Vector2Int operator /(Vector2Int a, int s) => new Vector2Int(a.X / s, a.Y / s);

        /// <summary>向量取反。</summary>
        public static Vector2Int operator -(Vector2Int a) => new Vector2Int(-a.X, -a.Y);

        /// <summary>判断两个向量是否完全相等。</summary>
        public bool Equals(Vector2Int other) => X == other.X && Y == other.Y;

        /// <inheritdoc />
        public override bool Equals(object? obj) => obj is Vector2Int other && Equals(other);

        /// <inheritdoc />
        public override int GetHashCode()
        {
            unchecked
            {
                return (X * 397) ^ Y;
            }
        }

        /// <summary>判断两个向量是否相等。</summary>
        public static bool operator ==(Vector2Int a, Vector2Int b) => a.Equals(b);

        /// <summary>判断两个向量是否不相等。</summary>
        public static bool operator !=(Vector2Int a, Vector2Int b) => !a.Equals(b);

        /// <summary>返回表示当前向量的字符串格式。</summary>
        public override string ToString() => $"({X}, {Y})";
    }
}
