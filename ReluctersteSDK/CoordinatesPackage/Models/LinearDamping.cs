using ReluctersteSDK.CoordinatesPackage.Vector;
using System;
using System.Collections.Generic;
using System.Text;

namespace ReluctersteSDK.CoordinatesPackage.Models
{
    /// <summary>
    /// 线性阻尼模型
    /// </summary>
    public class LinearDamping : PolarSpeedPoint2D
    {
        /// <summary>
        /// 最大允许速度（极径）。
        /// 实际速度会被限制在此值以内。
        /// </summary>
        public double MaxSpeed { get; set; }
        /// <summary>
        /// 线性阻尼系数（正数）。越大则阻尼越强，物体减速越快，
        /// 且在驱动加速度下能达到的平衡速度越低。
        /// 推荐取值在 0 到 1 之间，避免过度振荡。
        /// </summary>
        public double DampingCoefficient { get; set; }
        /// <summary>
        /// 构造线性阻尼模型
        /// </summary>
        /// <param name="speed">初始速度</param>
        /// <param name="acceleration">初始加速度</param>
        /// <param name="maxSpeed">速度最大值</param>
        /// <param name="dampingCoefficient">阻尼系数k，-a=kv </param>
        public LinearDamping(PolarVector speed, PolarVector acceleration, double maxSpeed, double dampingCoefficient)
        {
            Speed = speed;
            Acceleration = acceleration;
            MaxSpeed = maxSpeed;
            DampingCoefficient = dampingCoefficient;
        }
        protected override void accelerate()
        {
            const double epsilon = 1e-6;
            if (Speed.R < epsilon) Speed.ReturnToZero();
            PolarVector SumAccelerate = Acceleration - new PolarVector(DampingCoefficient * Speed.R, Speed.Theta);
            Speed += SumAccelerate;
            if (Speed.R > MaxSpeed) Speed.R = MaxSpeed;
        }
    }
}
