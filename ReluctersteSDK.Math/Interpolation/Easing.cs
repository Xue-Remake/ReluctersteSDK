namespace ReluctersteSDK.Math.Interpolation
{
    /// <summary>
    /// 缓动函数集合。所有函数的标准输入范围是 [0, 1]。
    /// </summary>
    public static class Easing
    {
        private const float BackC1 = 1.70158f;
        private const float BackC3 = BackC1 + 1f;
        private const float ElasticC4 = (2f * MathF.PI) / 3f;
        private const float ElasticC5 = (2f * MathF.PI) / 4.5f;

        /// <summary>计算缓动值；输入会先 clamp 到 [0, 1]。</summary>
        /// <param name="mode">缓动曲线。</param>
        /// <param name="t">输入进度。</param>
        public static float Evaluate(EasingMode mode, float t)
        {
            t = t < 0f ? 0f : (t > 1f ? 1f : t);
            return EvaluateUnclamped(mode, t);
        }

        /// <summary>计算缓动值；输入不 clamp，Back/Elastic/Bounce 可能返回范围外值。</summary>
        /// <param name="mode">缓动曲线。</param>
        /// <param name="t">输入进度。</param>
        public static float EvaluateUnclamped(EasingMode mode, float t)
        {
            if (float.IsNaN(t))
                return 0f;

            switch (mode)
            {
                case EasingMode.Linear:
                    return t;
                case EasingMode.QuadIn:
                    return t * t;
                case EasingMode.QuadOut:
                    return 1f - (1f - t) * (1f - t);
                case EasingMode.QuadInOut:
                    return t < 0.5f
                        ? 2f * t * t
                        : 1f - 2f * (1f - t) * (1f - t);
                case EasingMode.CubicIn:
                    return t * t * t;
                case EasingMode.CubicOut:
                    return 1f - Pow3(1f - t);
                case EasingMode.CubicInOut:
                    return t < 0.5f
                        ? 4f * t * t * t
                        : 1f - Pow3(-2f * t + 2f) / 2f;
                case EasingMode.QuartIn:
                    return t * t * t * t;
                case EasingMode.QuartOut:
                    return 1f - Pow4(1f - t);
                case EasingMode.QuartInOut:
                    return t < 0.5f
                        ? 8f * t * t * t * t
                        : 1f - Pow4(-2f * t + 2f) / 2f;
                case EasingMode.QuintIn:
                    return t * t * t * t * t;
                case EasingMode.QuintOut:
                    return 1f - Pow5(1f - t);
                case EasingMode.QuintInOut:
                    return t < 0.5f
                        ? 16f * t * t * t * t * t
                        : 1f - Pow5(-2f * t + 2f) / 2f;
                case EasingMode.SineIn:
                    return 1f - MathF.Cos(t * MathF.PI * 0.5f);
                case EasingMode.SineOut:
                    return MathF.Sin(t * MathF.PI * 0.5f);
                case EasingMode.SineInOut:
                    return -(MathF.Cos(MathF.PI * t) - 1f) * 0.5f;
                case EasingMode.ExpoIn:
                    return t <= 0f ? 0f : MathF.Pow(2f, 10f * t - 10f);
                case EasingMode.ExpoOut:
                    return t >= 1f ? 1f : 1f - MathF.Pow(2f, -10f * t);
                case EasingMode.ExpoInOut:
                    if (t <= 0f) return 0f;
                    if (t >= 1f) return 1f;
                    return t < 0.5f
                        ? MathF.Pow(2f, 20f * t - 10f) * 0.5f
                        : (2f - MathF.Pow(2f, -20f * t + 10f)) * 0.5f;
                case EasingMode.CircIn:
                    return 1f - MathF.Sqrt(1f - t * t);
                case EasingMode.CircOut:
                    return MathF.Sqrt(1f - (t - 1f) * (t - 1f));
                case EasingMode.CircInOut:
                    return t < 0.5f
                        ? (1f - MathF.Sqrt(1f - 4f * t * t)) * 0.5f
                        : (MathF.Sqrt(1f - (-2f * t + 2f) * (-2f * t + 2f)) + 1f) * 0.5f;
                case EasingMode.BackIn:
                    return BackC3 * t * t * t - BackC1 * t * t;
                case EasingMode.BackOut:
                    return 1f + BackC3 * Pow3(t - 1f) + BackC1 * (t - 1f) * (t - 1f);
                case EasingMode.BackInOut:
                {
                    const float c2 = BackC1 * 1.525f;
                    float x = 2f * t;
                    return t < 0.5f
                        ? x * x * ((c2 + 1f) * x - c2) * 0.5f
                        : ((x - 2f) * (x - 2f) * ((c2 + 1f) * (x - 2f) + c2) + 2f) * 0.5f;
                }
                case EasingMode.ElasticIn:
                    return t <= 0f ? 0f
                        : t >= 1f ? 1f
                        : -MathF.Pow(2f, 10f * t - 10f) * MathF.Sin((t * 10f - 10.75f) * ElasticC4);
                case EasingMode.ElasticOut:
                    return t <= 0f ? 0f
                        : t >= 1f ? 1f
                        : MathF.Pow(2f, -10f * t) * MathF.Sin((t * 10f - 0.75f) * ElasticC4) + 1f;
                case EasingMode.ElasticInOut:
                    if (t <= 0f) return 0f;
                    if (t >= 1f) return 1f;
                    return t < 0.5f
                        ? -(MathF.Pow(2f, 20f * t - 10f) * MathF.Sin((20f * t - 11.125f) * ElasticC5)) * 0.5f
                        : MathF.Pow(2f, -20f * t + 10f) * MathF.Sin((20f * t - 11.125f) * ElasticC5) * 0.5f + 1f;
                case EasingMode.BounceIn:
                    return 1f - BounceOut(1f - t);
                case EasingMode.BounceOut:
                    return BounceOut(t);
                case EasingMode.BounceInOut:
                    return t < 0.5f
                        ? (1f - BounceOut(1f - 2f * t)) * 0.5f
                        : (1f + BounceOut(2f * t - 1f)) * 0.5f;
                case EasingMode.SmoothStep:
                    return SmoothStep(t);
                case EasingMode.SmootherStep:
                    return SmootherStep(t);
                default:
                    return t;
            }
        }

        /// <summary>平滑阶跃 3t²-2t³；输入会 clamp 到 [0, 1]。</summary>
        /// <param name="t">输入进度。</param>
        public static float SmoothStep(float t)
        {
            t = Clamp01(t);
            return t * t * (3f - 2f * t);
        }

        /// <summary>更平滑阶跃 6t⁵-15t⁴+10t³；输入会 clamp 到 [0, 1]。</summary>
        /// <param name="t">输入进度。</param>
        public static float SmootherStep(float t)
        {
            t = Clamp01(t);
            return t * t * t * (t * (t * 6f - 15f) + 10f);
        }

        /// <summary>反解线性插值：返回给定值在 [a, b] 中的进度；a == b 时返回 0。</summary>
        /// <param name="a">起点。</param>
        /// <param name="b">终点。</param>
        /// <param name="value">当前值。</param>
        public static float InverseLerp(float a, float b, float value)
            => SystemMath.Abs(b - a) < float.Epsilon ? 0f : Clamp01((value - a) / (b - a));

        private static float Clamp01(float value)
            => value < 0f ? 0f : (value > 1f ? 1f : value);

        private static float BounceOut(float t)
        {
            const float n1 = 7.5625f;
            const float d1 = 2.75f;

            if (t < 1f / d1)
                return n1 * t * t;

            if (t < 2f / d1)
            {
                t -= 1.5f / d1;
                return n1 * t * t + 0.75f;
            }

            if (t < 2.5f / d1)
            {
                t -= 2.25f / d1;
                return n1 * t * t + 0.9375f;
            }

            t -= 2.625f / d1;
            return n1 * t * t + 0.984375f;
        }

        private static float Pow3(float value) => value * value * value;
        private static float Pow4(float value) => value * value * value * value;
        private static float Pow5(float value) => value * value * value * value * value;
    }
}
