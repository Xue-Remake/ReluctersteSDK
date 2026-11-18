using ReluctersteSDK.Math.QuickRandom;

namespace ReluctersteSDK.Math.Noise
{
    /// <summary>
    /// 经典 Perlin 噪声（2D）。同一 seed 与坐标产生完全相同的结果。
    /// </summary>
    public sealed class PerlinNoise2D : INoise2D
    {
        private readonly int[] _permutation = new int[512];

        /// <summary>用指定种子初始化噪声。</summary>
        /// <param name="seed">种子。</param>
        public PerlinNoise2D(int seed)
        {
            int[] values = new int[256];
            for (int i = 0; i < values.Length; i++)
                values[i] = i;

            var rng = new SeededRandom(seed);
            for (int i = values.Length - 1; i > 0; i--)
            {
                int j = rng.NextInt(0, i + 1);
                (values[i], values[j]) = (values[j], values[i]);
            }

            for (int i = 0; i < _permutation.Length; i++)
                _permutation[i] = values[i & 255];
        }

        /// <summary>采样噪声，返回值大致在 [-1, 1]。</summary>
        /// <param name="x">X 坐标。</param>
        /// <param name="y">Y 坐标。</param>
        public float Sample(float x, float y)
        {
            int xi = (int)MathF.Floor(x) & 255;
            int yi = (int)MathF.Floor(y) & 255;
            float xf = x - MathF.Floor(x);
            float yf = y - MathF.Floor(y);

            float u = NoiseUtils.Fade(xf);
            float v = NoiseUtils.Fade(yf);

            int aa = _permutation[_permutation[xi] + yi];
            int ab = _permutation[_permutation[xi] + yi + 1];
            int ba = _permutation[_permutation[xi + 1] + yi];
            int bb = _permutation[_permutation[xi + 1] + yi + 1];

            float x1 = NoiseUtils.Lerp(
                NoiseUtils.GradDot(aa, xf, yf),
                NoiseUtils.GradDot(ba, xf - 1f, yf),
                u);
            float x2 = NoiseUtils.Lerp(
                NoiseUtils.GradDot(ab, xf, yf - 1f),
                NoiseUtils.GradDot(bb, xf - 1f, yf - 1f),
                u);

            return NoiseUtils.Lerp(x1, x2, v) * 0.7071f;
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

        /// <summary>脊状分形：把噪声取绝对值后翻转，适合山脉/裂纹。</summary>
        /// <param name="x">X 坐标。</param>
        /// <param name="y">Y 坐标。</param>
        /// <param name="octaves">叠加层数。</param>
        /// <param name="lacunarity">每层频率倍率。</param>
        /// <param name="gain">每层振幅倍率。</param>
        public float Ridged(float x, float y, int octaves = 4, float lacunarity = 2f, float gain = 0.5f)
        {
            if (octaves < 1) return 0f;

            float sum = 0f;
            float amplitude = 1f;
            float frequency = 1f;
            float maxAmplitude = 0f;

            for (int i = 0; i < octaves; i++)
            {
                float value = 1f - MathF.Abs(Sample(x * frequency, y * frequency));
                sum += value * value * amplitude;
                maxAmplitude += amplitude;
                frequency *= lacunarity;
                amplitude *= gain;
            }

            return maxAmplitude > 0f ? sum / maxAmplitude : 0f;
        }
    }
}
