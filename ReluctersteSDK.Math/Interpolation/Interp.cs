using ReluctersteSDK.Math.Vectors;

namespace ReluctersteSDK.Math.Interpolation
{
    /// <summary>
    /// 插值助手：标量、向量、角度与曲线插值。
    /// </summary>
    public static class Interp
    {
        /// <summary>线性插值；t 会 clamp 到 [0, 1]。</summary>
        public static float Lerp(float a, float b, float t)
        {
            t = t < 0f ? 0f : (t > 1f ? 1f : t);
            return a + (b - a) * t;
        }

        /// <summary>未限制范围的线性插值。</summary>
        public static float LerpUnclamped(float a, float b, float t) => a + (b - a) * t;

        /// <summary>按最短路径插值两个角度（弧度）；t 会 clamp 到 [0, 1]。</summary>
        public static float LerpAngle(float aRad, float bRad, float t)
        {
            t = t < 0f ? 0f : (t > 1f ? 1f : t);
            float delta = Numerics.NumericUtils.DeltaAngle(aRad, bRad);
            return aRad + delta * t;
        }

        /// <summary>按最短路径插值两个角度（角度制）；t 会 clamp 到 [0, 1]。</summary>
        public static float LerpAngleDegrees(float aDeg, float bDeg, float t)
        {
            t = t < 0f ? 0f : (t > 1f ? 1f : t);
            float delta = Numerics.NumericUtils.WrapAngleDegrees(bDeg - aDeg);
            return aDeg + delta * t;
        }

        /// <summary>以固定步长把当前值移向目标值。</summary>
        public static float MoveTowards(float current, float target, float maxDelta)
        {
            float delta = target - current;
            if (SystemMath.Abs(delta) <= maxDelta)
                return target;
            return current + SystemMath.Sign(delta) * maxDelta;
        }

        /// <summary>以固定角步长把当前角度移向目标角度（最短路径）。</summary>
        public static float MoveTowardsAngle(float currentRad, float targetRad, float maxDeltaRad)
        {
            float delta = Numerics.NumericUtils.DeltaAngle(currentRad, targetRad);
            if (SystemMath.Abs(delta) <= maxDeltaRad)
                return targetRad;
            return currentRad + SystemMath.Sign(delta) * maxDeltaRad;
        }

        /// <summary>Hermite 曲线插值。</summary>
        public static float Hermite(float p0, float m0, float p1, float m1, float t)
        {
            float t2 = t * t;
            float t3 = t2 * t;
            return (2f * t3 - 3f * t2 + 1f) * p0
                 + (t3 - 2f * t2 + t) * m0
                 + (-2f * t3 + 3f * t2) * p1
                 + (t3 - t2) * m1;
        }

        /// <summary>Catmull-Rom 样条插值（p1 到 p2 之间）。</summary>
        public static float CatmullRom(float p0, float p1, float p2, float p3, float t)
        {
            float t2 = t * t;
            float t3 = t2 * t;
            return 0.5f * (
                2f * p1
                + (-p0 + p2) * t
                + (2f * p0 - 5f * p1 + 4f * p2 - p3) * t2
                + (-p0 + 3f * p1 - 3f * p2 + p3) * t3);
        }

        /// <summary>向量线性插值；t 会 clamp 到 [0, 1]。</summary>
        public static CartesianVector2D Lerp(CartesianVector2D a, CartesianVector2D b, float t)
            => CartesianVector2D.Lerp(a, b, t);

        /// <summary>双精度向量线性插值；t 会 clamp 到 [0, 1]。</summary>
        public static CartesianDoubleVector2D Lerp(CartesianDoubleVector2D a, CartesianDoubleVector2D b, double t)
            => CartesianDoubleVector2D.Lerp(a, b, t);
    }
}
