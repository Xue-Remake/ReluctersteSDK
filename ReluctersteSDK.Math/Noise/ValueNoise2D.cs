namespace ReluctersteSDK.Math.Noise
{
    /// <summary>
    /// 值噪声（Value Noise）2D：格点随机值 + 平滑双线性插值。输出范围 [0, 1]。
    /// </summary>
    public sealed class ValueNoise2D : INoise2D
    {
        private readonly int _seed;

        /// <summary>用指定种子初始化噪声。</summary>
        /// <param name="seed">种子。</param>
        public ValueNoise2D(int seed)
        {
            _seed = seed;
        }

        /// <summary>采样噪声，返回值在 [0, 1]。</summary>
        /// <param name="x">X 坐标。</param>
        /// <param name="y">Y 坐标。</param>
        public float Sample(float x, float y)
        {
            int xi = (int)MathF.Floor(x);
            int yi = (int)MathF.Floor(y);
            float xf = x - MathF.Floor(x);
            float yf = y - MathF.Floor(y);

            float u = NoiseUtils.Fade(xf);
            float v = NoiseUtils.Fade(yf);

            float v00 = Hash(xi, yi);
            float v10 = Hash(xi + 1, yi);
            float v01 = Hash(xi, yi + 1);
            float v11 = Hash(xi + 1, yi + 1);

            float bottom = NoiseUtils.Lerp(v00, v10, u);
            float top = NoiseUtils.Lerp(v01, v11, u);
            return NoiseUtils.Lerp(bottom, top, v);
        }

        /// <summary>分形叠加（fBm）。</summary>
        /// <param name="x">X 坐标。</param>
        /// <param name="y">Y 坐标。</param>
        /// <param name="octaves">叠加层数。</param>
        /// <param name="lacunarity">每层频率倍率。</param>
        /// <param name="gain">每层振幅倍率。</param>
        public float Fbm(float x, float y, int octaves = 4, float lacunarity = 2f, float gain = 0.5f)
        {
            if (octaves < 1) return 0f;

            float sum = 0f;
            float amplitude = 1f;
            float frequency = 1f;
            float maxAmplitude = 0f;

            for (int i = 0; i < octaves; i++)
            {
                sum += Sample(x * frequency, y * frequency) * amplitude;
                maxAmplitude += amplitude;
                frequency *= lacunarity;
                amplitude *= gain;
            }

            return maxAmplitude > 0f ? sum / maxAmplitude : 0f;
        }

        private float Hash(int x, int y)
            => NoiseUtils.Hash2D(x, y, _seed) / 255f;
    }
}
