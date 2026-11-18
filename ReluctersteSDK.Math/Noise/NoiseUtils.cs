using ReluctersteSDK.Math.Vectors;

namespace ReluctersteSDK.Math.Noise
{
    /// <summary>噪声实现共用的插值与梯度工具。</summary>
    public static class NoiseUtils
    {
        /// <summary>Perlin 五次平滑曲线 6t⁵-15t⁴+10t³。</summary>
        public static float Fade(float t) => t * t * t * (t * (t * 6f - 15f) + 10f);

        /// <summary>线性插值。</summary>
        public static float Lerp(float a, float b, float t) => a + (b - a) * t;

        /// <summary>根据哈希值取八个方向之一。</summary>
        public static CartesianVector2D Gradient(int hash, float x, float y)
        {
            switch (hash & 7)
            {
                case 0: return new CartesianVector2D(x, y);
                case 1: return new CartesianVector2D(-x, y);
                case 2: return new CartesianVector2D(x, -y);
                case 3: return new CartesianVector2D(-x, -y);
                case 4: return new CartesianVector2D(x, 0f);
                case 5: return new CartesianVector2D(-x, 0f);
                case 6: return new CartesianVector2D(0f, y);
                default: return new CartesianVector2D(0f, -y);
            }
        }

        /// <summary>计算梯度与偏移的点积（八个方向）。</summary>
        public static float GradDot(int hash, float x, float y)
        {
            switch (hash & 7)
            {
                case 0: return x + y;
                case 1: return -x + y;
                case 2: return x - y;
                case 3: return -x - y;
                case 4: return x;
                case 5: return -x;
                case 6: return y;
                default: return -y;
            }
        }

        /// <summary>把两个整数坐标哈希为 [0, 255]。</summary>
        public static int Hash2D(int x, int y, int seed)
        {
            unchecked
            {
                int hash = x * 374761393 + y * 668265263 + seed * 1442695041;
                hash = (hash ^ (hash >> 13)) * 1274126177;
                return (hash ^ (hash >> 16)) & 0xFF;
            }
        }
    }
}
