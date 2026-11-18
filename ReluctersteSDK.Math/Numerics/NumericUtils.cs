namespace ReluctersteSDK.Math.Numerics
{
    /// <summary>
    /// 标量数值工具：常量、区间限制、角度规范化、环绕与浮点比较。
    /// </summary>
    public static class NumericUtils
    {
        /// <summary>圆周率 π。</summary>
        public const float Pi = MathF.PI;

        /// <summary>2π。</summary>
        public const float TwoPi = MathF.PI * 2f;

        /// <summary>π/2。</summary>
        public const float HalfPi = MathF.PI * 0.5f;

        /// <summary>π/4。</summary>
        public const float QuarterPi = MathF.PI * 0.25f;

        /// <summary>自然常数 e。</summary>
        public const float E = MathF.E;

        /// <summary>角度转弧度系数。</summary>
        public const float Deg2Rad = MathF.PI / 180f;

        /// <summary>弧度转角度系数。</summary>
        public const float Rad2Deg = 180f / MathF.PI;

        /// <summary>默认浮点容差。</summary>
        public const float Epsilon = 1e-6f;

        /// <summary>把值限制到 [min, max]。</summary>
        public static float Clamp(float value, float min, float max) => SystemMath.Clamp(value, min, max);

        /// <summary>把值限制到 [min, max]。</summary>
        public static int Clamp(int value, int min, int max) => SystemMath.Clamp(value, min, max);

        /// <summary>把值限制到 [0, 1]。</summary>
        public static float Clamp01(float value) => value < 0f ? 0f : (value > 1f ? 1f : value);

        /// <summary>线性插值；t 不 clamp。</summary>
        public static float Lerp(float a, float b, float t) => a + (b - a) * t;

        /// <summary>反解线性插值；a == b 时返回 0。</summary>
        public static float InverseLerp(float a, float b, float value)
            => SystemMath.Abs(b - a) < float.Epsilon ? 0f : (value - a) / (b - a);

        /// <summary>把值从输入区间映射到输出区间。</summary>
        public static float Remap(float value, float inMin, float inMax, float outMin, float outMax)
            => outMin + (value - inMin) * (outMax - outMin) / (inMax - inMin);

        /// <summary>平滑阶跃 3t²-2t³。</summary>
        public static float SmoothStep(float t)
        {
            t = Clamp01(t);
            return t * t * (3f - 2f * t);
        }

        /// <summary>更平滑阶跃 6t⁵-15t⁴+10t³。</summary>
        public static float SmootherStep(float t)
        {
            t = Clamp01(t);
            return t * t * t * (t * (t * 6f - 15f) + 10f);
        }

        /// <summary>按容差比较两个 float。</summary>
        public static bool ApproximatelyEquals(float a, float b, float epsilon = Epsilon)
            => SystemMath.Abs(a - b) < epsilon;

        /// <summary>按容差比较两个 double。</summary>
        public static bool ApproximatelyEquals(double a, double b, double epsilon = 1e-8)
            => SystemMath.Abs(a - b) < epsilon;

        /// <summary>判断 float 是否为有限数。</summary>
        public static bool IsFinite(float value) => float.IsFinite(value);

        /// <summary>返回符号：-1、0 或 1。</summary>
        public static float Sign(float value) => value > 0f ? 1f : (value < 0f ? -1f : 0f);

        /// <summary>把值按长度环绕到 [0, length)。</summary>
        public static float Repeat(float value, float length)
        {
            if (length <= 0f)
                return 0f;

            float result = value % length;
            return result < 0f ? result + length : result;
        }

        /// <summary>乒乓环绕：值在 [0, length] 之间往复。</summary>
        public static float PingPong(float value, float length)
        {
            if (length <= 0f)
                return 0f;

            float repeated = Repeat(value, length * 2f);
            return repeated <= length ? repeated : length * 2f - repeated;
        }

        /// <summary>把弧度规范到 [0, 2π)。</summary>
        public static float WrapAngle(float radians) => Repeat(radians, TwoPi);

        /// <summary>把角度规范到 [0, 360)。</summary>
        public static float WrapAngleDegrees(float degrees) => Repeat(degrees, 360f);

        /// <summary>计算两个角之间的最短有符号差（弧度，范围 [-π, π]）。</summary>
        public static float DeltaAngle(float fromRad, float toRad)
        {
            float delta = Repeat(toRad - fromRad + Pi, TwoPi) - Pi;
            return delta;
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
            float delta = DeltaAngle(currentRad, targetRad);
            if (SystemMath.Abs(delta) <= maxDeltaRad)
                return targetRad;
            return currentRad + SystemMath.Sign(delta) * maxDeltaRad;
        }

        /// <summary>把值四舍五入到指定步长。</summary>
        public static float RoundTo(float value, float step)
            => step <= 0f ? value : MathF.Round(value / step) * step;

        /// <summary>返回大于等于指定值的最小 2 的幂；0 与负数返回 1。</summary>
        public static int NextPowerOfTwo(int value)
        {
            if (value <= 1)
                return 1;

            value--;
            value |= value >> 1;
            value |= value >> 2;
            value |= value >> 4;
            value |= value >> 8;
            value |= value >> 16;
            return value + 1;
        }

        /// <summary>判断正整数是否为 2 的幂。</summary>
        public static bool IsPowerOfTwo(int value) => value > 0 && (value & (value - 1)) == 0;
    }
}
