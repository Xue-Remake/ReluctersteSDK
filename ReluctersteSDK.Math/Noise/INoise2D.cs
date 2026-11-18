namespace ReluctersteSDK.Math.Noise
{
    /// <summary>2D 噪声源契约。</summary>
    public interface INoise2D
    {
        /// <summary>采样指定坐标的噪声值。</summary>
        /// <param name="x">X 坐标。</param>
        /// <param name="y">Y 坐标。</param>
        float Sample(float x, float y);
    }
}
