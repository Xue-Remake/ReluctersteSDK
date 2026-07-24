namespace ReluctersteSDK.CoordinatesPackage.Vector
{
    /// <summary>
    /// 极坐标向量 (r, θ)，θ 以弧度为单位
    /// </summary>
    public class PolarVector
    {
        /// <summary>
        /// 径向距离（半径）
        /// </summary>
        public double R { get; set; }

        /// <summary>
        /// 极角（弧度）
        /// </summary>
        public double Theta { get; set; }

        /// <summary>
        /// 创建极坐标向量
        /// </summary>
        /// <param name="r">径向距离</param>
        /// <param name="theta">极角（弧度）</param>
        public PolarVector(double r, double theta)
        {
            R = r;
            Theta = theta;
        }

        /// <summary>
        /// 将极坐标向量显式转换为笛卡尔向量
        /// </summary>
        /// <param name="polar">极坐标向量</param>
        /// <returns>对应的笛卡尔向量</returns>
        public static explicit operator CartesianVector(PolarVector polar) =>
            new CartesianVector(polar.R * Math.Cos(polar.Theta),
                               polar.R * Math.Sin(polar.Theta));

        // ----- 算术运算符 -----

        /// <summary>
        /// 极坐标向量加法：先转换为笛卡尔坐标计算，再转回极坐标
        /// </summary>
        /// <param name="a">第一个向量</param>
        /// <param name="b">第二个向量</param>
        /// <returns>和向量（极坐标）</returns>
        public static PolarVector operator +(PolarVector a, PolarVector b)
        {
            CartesianVector ca = (CartesianVector)a;
            CartesianVector cb = (CartesianVector)b;
            return (PolarVector)(ca + cb);
        }

        /// <summary>
        /// 极坐标向量减法：先转换为笛卡尔坐标计算，再转回极坐标
        /// </summary>
        /// <param name="a">被减向量</param>
        /// <param name="b">减向量</param>
        /// <returns>差向量（极坐标）</returns>
        public static PolarVector operator -(PolarVector a, PolarVector b)
        {
            CartesianVector ca = (CartesianVector)a;
            CartesianVector cb = (CartesianVector)b;
            return (PolarVector)(ca - cb);
        }

        /// <summary>
        /// 标量乘法：半径缩放，角度不变
        /// </summary>
        /// <param name="a">极坐标向量</param>
        /// <param name="scalar">标量</param>
        /// <returns>缩放后的向量</returns>
        public static PolarVector operator *(PolarVector a, double scalar) =>
            new PolarVector(a.R * scalar, a.Theta);

        /// <summary>
        /// 标量乘法（标量 × 向量），交换律
        /// </summary>
        /// <param name="scalar">标量</param>
        /// <param name="a">极坐标向量</param>
        /// <returns>缩放后的向量</returns>
        public static PolarVector operator *(double scalar, PolarVector a) =>
            a * scalar;

        /// <summary>
        /// 向量除以标量（半径缩放）
        /// </summary>
        /// <param name="a">极坐标向量</param>
        /// <param name="scalar">除数（非零）</param>
        /// <returns>缩放后的向量</returns>
        /// <exception cref="DivideByZeroException">当 scalar 为零时抛出</exception>
        public static PolarVector operator /(PolarVector a, double scalar) =>
            new PolarVector(a.R / scalar, a.Theta);

        /// <summary>
        /// 计算两个极坐标向量的点积（通过转换为笛卡尔坐标实现）
        /// </summary>
        /// <param name="a">第一个向量</param>
        /// <param name="b">第二个向量</param>
        /// <returns>点积值</returns>
        public static double Dot(PolarVector a, PolarVector b) =>
            CartesianVector.Dot((CartesianVector)a, (CartesianVector)b);

        /// <summary>
        /// 计算两个极坐标向量的二维叉积（通过转换为笛卡尔坐标实现）
        /// </summary>
        /// <param name="a">第一个向量</param>
        /// <param name="b">第二个向量</param>
        /// <returns>叉积标量值</returns>
        public static double Cross(PolarVector a, PolarVector b) =>
            CartesianVector.Cross((CartesianVector)a, (CartesianVector)b);

        /// <summary>
        /// 返回向量的字符串表示形式
        /// </summary>
        public override string ToString() => $"R={R}, θ={Theta} rad";

        /// <summary>
        /// 判断当前向量是否与指定对象相等
        /// </summary>
        /// <param name="obj">要比较的对象</param>
        /// <returns>如果 obj 是 PolarVector 且 R、Theta 均相等，则为 true；否则为 false</returns>
        public override bool Equals(object obj) =>
            obj is PolarVector v && R == v.R && Theta == v.Theta;

        /// <summary>
        /// 获取哈希码
        /// </summary>
        public override int GetHashCode() => HashCode.Combine(R, Theta);
        /// <summary>
        /// 归零
        /// </summary>
        public void ReturnToZero()
        {
            R = 0;
            Theta = 0;
        }
    }
}
