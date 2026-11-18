namespace ReluctersteSDK.Math.Interpolation
{
    /// <summary>常用缓动曲线。</summary>
    public enum EasingMode
    {
        /// <summary>线性。</summary>
        Linear,
        /// <summary>二次缓入。</summary>
        QuadIn,
        /// <summary>二次缓出。</summary>
        QuadOut,
        /// <summary>二次缓入缓出。</summary>
        QuadInOut,
        /// <summary>三次缓入。</summary>
        CubicIn,
        /// <summary>三次缓出。</summary>
        CubicOut,
        /// <summary>三次缓入缓出。</summary>
        CubicInOut,
        /// <summary>四次缓入。</summary>
        QuartIn,
        /// <summary>四次缓出。</summary>
        QuartOut,
        /// <summary>四次缓入缓出。</summary>
        QuartInOut,
        /// <summary>五次缓入。</summary>
        QuintIn,
        /// <summary>五次缓出。</summary>
        QuintOut,
        /// <summary>五次缓入缓出。</summary>
        QuintInOut,
        /// <summary>正弦缓入。</summary>
        SineIn,
        /// <summary>正弦缓出。</summary>
        SineOut,
        /// <summary>正弦缓入缓出。</summary>
        SineInOut,
        /// <summary>指数缓入。</summary>
        ExpoIn,
        /// <summary>指数缓出。</summary>
        ExpoOut,
        /// <summary>指数缓入缓出。</summary>
        ExpoInOut,
        /// <summary>圆形缓入。</summary>
        CircIn,
        /// <summary>圆形缓出。</summary>
        CircOut,
        /// <summary>圆形缓入缓出。</summary>
        CircInOut,
        /// <summary>回退缓入（会先向反方向移动）。</summary>
        BackIn,
        /// <summary>回退缓出（会冲过终点后回弹）。</summary>
        BackOut,
        /// <summary>回退缓入缓出。</summary>
        BackInOut,
        /// <summary>弹性缓入。</summary>
        ElasticIn,
        /// <summary>弹性缓出。</summary>
        ElasticOut,
        /// <summary>弹性缓入缓出。</summary>
        ElasticInOut,
        /// <summary>弹跳缓入。</summary>
        BounceIn,
        /// <summary>弹跳缓出。</summary>
        BounceOut,
        /// <summary>弹跳缓入缓出。</summary>
        BounceInOut,
        /// <summary>平滑阶跃（3t²-2t³）。</summary>
        SmoothStep,
        /// <summary>更平滑阶跃（6t⁵-15t⁴+10t³）。</summary>
        SmootherStep
    }
}
