using System;
using System.Collections.Generic;
using System.Text;

namespace ReluctersteSDK.CoordinatesPackage.Vector
{
    /// <summary>
    /// 笛卡尔坐标向量 (x, y)
    /// </summary>
    public class CartesianVector
    {
        /// <summary>
        /// X 坐标分量
        /// </summary>
        public double X { get; set; }

        /// <summary>
        /// Y 坐标分量
        /// </summary>
        public double Y { get; set; }

        /// <summary>
        /// 创建笛卡尔坐标向量
        /// </summary>
        /// <param name="x">X 分量</param>
        /// <param name="y">Y 分量</param>
        public CartesianVector(double x, double y)
        {
            X = x;
            Y = y;
        }

        /// <summary>
        /// 获取向量的模（长度）
        /// </summary>
        public double Magnitude => Math.Sqrt(X * X + Y * Y);

        /// <summary>
        /// 获取向量的极角（弧度），取值范围 (-π, π]
        /// </summary>
        public double Angle => Math.Atan2(Y, X);

        /// <summary>
        /// 将笛卡尔向量显式转换为极坐标向量
        /// </summary>
        /// <param name="cart">要转换的笛卡尔向量</param>
        /// <returns>对应的极坐标向量</returns>
        public static explicit operator PolarVector(CartesianVector cart) =>
            new PolarVector(cart.Magnitude, cart.Angle);

        // ----- 算术运算符 -----

        /// <summary>
        /// 向量加法
        /// </summary>
        /// <param name="a">第一个向量</param>
        /// <param name="b">第二个向量</param>
        /// <returns>和向量</returns>
        public static CartesianVector operator +(CartesianVector a, CartesianVector b) =>
            new CartesianVector(a.X + b.X, a.Y + b.Y);

        /// <summary>
        /// 向量减法
        /// </summary>
        /// <param name="a">被减向量</param>
        /// <param name="b">减向量</param>
        /// <returns>差向量</returns>
        public static CartesianVector operator -(CartesianVector a, CartesianVector b) =>
            new CartesianVector(a.X - b.X, a.Y - b.Y);

        /// <summary>
        /// 标量乘法（向量 × 标量）
        /// </summary>
        /// <param name="a">向量</param>
        /// <param name="scalar">标量</param>
        /// <returns>缩放后的向量</returns>
        public static CartesianVector operator *(CartesianVector a, double scalar) =>
            new CartesianVector(a.X * scalar, a.Y * scalar);

        /// <summary>
        /// 标量乘法（标量 × 向量），交换律
        /// </summary>
        /// <param name="scalar">标量</param>
        /// <param name="a">向量</param>
        /// <returns>缩放后的向量</returns>
        public static CartesianVector operator *(double scalar, CartesianVector a) =>
            a * scalar;

        /// <summary>
        /// 向量除以标量
        /// </summary>
        /// <param name="a">向量</param>
        /// <param name="scalar">除数（非零）</param>
        /// <returns>缩放后的向量</returns>
        /// <exception cref="DivideByZeroException">当 scalar 为零时抛出</exception>
        public static CartesianVector operator /(CartesianVector a, double scalar) =>
            new CartesianVector(a.X / scalar, a.Y / scalar);

        /// <summary>
        /// 计算两个向量的点积（内积）
        /// </summary>
        /// <param name="a">第一个向量</param>
        /// <param name="b">第二个向量</param>
        /// <returns>点积值</returns>
        public static double Dot(CartesianVector a, CartesianVector b) =>
            a.X * b.X + a.Y * b.Y;

        /// <summary>
        /// 计算二维叉积（返回标量，等价于 |a||b|sinθ）
        /// </summary>
        /// <param name="a">第一个向量</param>
        /// <param name="b">第二个向量</param>
        /// <returns>叉积标量值</returns>
        public static double Cross(CartesianVector a, CartesianVector b) =>
            a.X * b.Y - a.Y * b.X;

        /// <summary>
        /// 返回向量的字符串表示形式
        /// </summary>
        public override string ToString() => $"({X}, {Y})";

        /// <summary>
        /// 判断当前向量是否与指定对象相等
        /// </summary>
        /// <param name="obj">要比较的对象</param>
        /// <returns>如果 obj 是 CartesianVector 且 X、Y 分量均相等，则为 true；否则为 false</returns>
        public override bool Equals(object obj) =>
            obj is CartesianVector v && X == v.X && Y == v.Y;

        /// <summary>
        /// 获取哈希码
        /// </summary>
        public override int GetHashCode() => HashCode.Combine(X, Y);
    }
}
