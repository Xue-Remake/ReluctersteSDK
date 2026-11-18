using ReluctersteSDK.Math.Vectors;

namespace ReluctersteSDK.Game.Ecs
{
    /// <summary>速度组件（单位/秒）。</summary>
    public struct Velocity
    {
        /// <summary>速度向量。</summary>
        public CartesianVector2D Value;
    }

    /// <summary>加速度组件（单位/秒²）。</summary>
    public struct Acceleration
    {
        /// <summary>加速度向量。</summary>
        public CartesianVector2D Value;
    }

    /// <summary>
    /// 线性阻尼组件：阻尼加速度为 <c>-k·v</c>，可附带限速。
    /// </summary>
    public struct Damping
    {
        /// <summary>阻尼系数 k（正数，越大减速越快）；为 0 时视为无阻尼。</summary>
        public float Coefficient;

        /// <summary>最大速度；<c>&lt;= 0</c> 表示不限速。</summary>
        /// <remarks>
        /// 用 <c>&lt;= 0</c> 表示"不限速"而不是"限速为 0"，
        /// 是为了让 <c>default(Damping)</c> 成为无副作用的空组件。
        /// </remarks>
        public float MaxSpeed;
    }

    /// <summary>朝向组件：由 <c>MotionSystem</c> 根据速度方向写入（弧度）。</summary>
    public struct Heading
    {
        /// <summary>运动朝向角（弧度）。</summary>
        public float Radians;
    }
}
