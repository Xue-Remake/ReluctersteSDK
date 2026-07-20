using System;
using System.Collections.Generic;
using System.Text;

namespace ReluctersteSDK.CoordinatesPackage.Vector
{
    /// <summary>
    /// 静态工具类：角度转换、向量常用计算等
    /// </summary>
    public static class Vector2DUtils
    {
        /// <summary>
        /// 角度转弧度
        /// </summary>
        /// <param name="degrees">角度值（度）</param>
        /// <returns>对应的弧度值</returns>
        public static double DegreesToRadians(double degrees) =>
            degrees * Math.PI / 180.0;

        /// <summary>
        /// 弧度转角度
        /// </summary>
        /// <param name="radians">弧度值</param>
        /// <returns>对应的角度值（度）</returns>
        public static double RadiansToDegrees(double radians) =>
            radians * 180.0 / Math.PI;

        /// <summary>
        /// 计算两个笛卡尔向量之间的欧几里得距离
        /// </summary>
        /// <param name="a">第一个向量</param>
        /// <param name="b">第二个向量</param>
        /// <returns>两点间的距离</returns>
        public static double Distance(CartesianVector a, CartesianVector b)
        {
            double dx = a.X - b.X;
            double dy = a.Y - b.Y;
            return Math.Sqrt(dx * dx + dy * dy);
        }

        /// <summary>
        /// 将笛卡尔向量归一化为单位向量
        /// </summary>
        /// <param name="v">要归一化的向量</param>
        /// <returns>长度为一的单位向量；若输入为零向量，则返回零向量</returns>
        public static CartesianVector Normalize(CartesianVector v)
        {
            double mag = v.Magnitude;
            if (mag == 0) return new CartesianVector(0, 0);
            return v / mag;
        }

        /// <summary>
        /// 将极坐标向量归一化（半径设为1，角度不变）
        /// </summary>
        /// <param name="v">要归一化的极坐标向量</param>
        /// <returns>半径为一、角度不变的极坐标向量</returns>
        public static PolarVector Normalize(PolarVector v)
        {
            return new PolarVector(1.0, v.Theta);
        }

        /// <summary>
        /// 使用角度制创建极坐标向量（内部转换为弧度）
        /// </summary>
        /// <param name="r">径向距离</param>
        /// <param name="degrees">极角（度）</param>
        /// <returns>极坐标向量</returns>
        public static PolarVector CreatePolarFromDegrees(double r, double degrees)
        {
            return new PolarVector(r, DegreesToRadians(degrees));
        }

        /// <summary>
        /// 将极坐标向量的角度以角度制返回
        /// </summary>
        /// <param name="v">极坐标向量</param>
        /// <returns>极角（度）</returns>
        public static double GetAngleInDegrees(PolarVector v)
        {
            return RadiansToDegrees(v.Theta);
        }

        /// <summary>
        /// 在两个笛卡尔向量之间进行线性插值
        /// </summary>
        /// <param name="a">起始向量</param>
        /// <param name="b">终止向量</param>
        /// <param name="t">插值系数，通常在 [0,1] 之间</param>
        /// <returns>插值结果向量</returns>
        public static CartesianVector Lerp(CartesianVector a, CartesianVector b, double t)
        {
            return new CartesianVector(
                a.X + (b.X - a.X) * t,
                a.Y + (b.Y - a.Y) * t);
        }

        /// <summary>
        /// 逆时针旋转笛卡尔向量
        /// </summary>
        /// <param name="v">原始向量</param>
        /// <param name="radians">旋转角度（弧度），正值表示逆时针</param>
        /// <returns>旋转后的向量</returns>
        public static CartesianVector Rotate(CartesianVector v, double radians)
        {
            double cos = Math.Cos(radians);
            double sin = Math.Sin(radians);
            return new CartesianVector(
                v.X * cos - v.Y * sin,
                v.X * sin + v.Y * cos);
        }
    }
}
