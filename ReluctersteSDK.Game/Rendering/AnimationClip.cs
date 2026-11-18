using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using Microsoft.Xna.Framework;

namespace ReluctersteSDK.Game.Rendering
{
    /// <summary>
    /// 动画中的一帧：源矩形 + 停留时长。
    /// </summary>
    public readonly struct AnimationFrame
    {
        /// <summary>该帧在纹理上的采样区域。</summary>
        public readonly Rectangle Source;

        /// <summary>该帧停留时长（秒），必须大于 0。</summary>
        public readonly float Duration;

        /// <summary>初始化一个动画帧。</summary>
        /// <param name="source">纹理采样区域。</param>
        /// <param name="duration">停留时长（秒）。</param>
        /// <exception cref="ArgumentOutOfRangeException">时长不是正数。</exception>
        public AnimationFrame(Rectangle source, float duration)
        {
            if (duration <= 0f || float.IsNaN(duration) || float.IsInfinity(duration))
                throw new ArgumentOutOfRangeException(nameof(duration), duration, "帧时长必须是有限的正数。");

            Source = source;
            Duration = duration;
        }
    }

    /// <summary>
    /// 一段不可变动画：命名 + 帧序列 + 是否循环。
    /// <para>Clip 本身不含播放状态，同一份 Clip 可以被多个实体共用。</para>
    /// </summary>
    public sealed class AnimationClip
    {
        private readonly AnimationFrame[] _frames;
        private readonly ReadOnlyCollection<AnimationFrame> _framesView;

        /// <summary>动画名称（在同一 <see cref="Animator"/> 内唯一）。</summary>
        public string Name { get; }

        /// <summary>是否循环播放。</summary>
        public bool Loop { get; }

        /// <summary>一整轮播放的总时长（秒）。</summary>
        public float TotalDuration { get; }

        /// <summary>帧数量。</summary>
        public int FrameCount => _frames.Length;

        /// <summary>帧序列的只读视图。</summary>
        public IReadOnlyList<AnimationFrame> Frames => _framesView;

        /// <summary>获取指定下标的帧。</summary>
        /// <param name="index">帧下标。</param>
        public AnimationFrame this[int index] => _frames[index];

        /// <summary>使用帧序列初始化动画。</summary>
        /// <param name="name">动画名称。</param>
        /// <param name="frames">帧序列，至少包含一帧。</param>
        /// <param name="loop">是否循环，默认 <c>true</c>。</param>
        /// <exception cref="ArgumentException">名称为空或帧序列为空。</exception>
        public AnimationClip(string name, IEnumerable<AnimationFrame> frames, bool loop = true)
        {
            if (string.IsNullOrWhiteSpace(name))
                throw new ArgumentException("动画名称不能为空。", nameof(name));
            if (frames is null)
                throw new ArgumentNullException(nameof(frames));

            _frames = frames.ToArray();
            if (_frames.Length == 0)
                throw new ArgumentException("动画至少需要一帧。", nameof(frames));

            float total = 0f;
            for (int i = 0; i < _frames.Length; i++)
                total += _frames[i].Duration;

            Name = name;
            Loop = loop;
            TotalDuration = total;
            _framesView = Array.AsReadOnly(_frames);
        }

        /// <summary>使用统一帧时长从一组源矩形创建动画。</summary>
        /// <param name="name">动画名称。</param>
        /// <param name="sources">帧的源矩形序列。</param>
        /// <param name="frameDuration">每帧停留时长（秒）。</param>
        /// <param name="loop">是否循环。</param>
        public AnimationClip(string name, IEnumerable<Rectangle> sources, float frameDuration, bool loop = true)
            : this(name, (sources ?? throw new ArgumentNullException(nameof(sources)))
                .Select(source => new AnimationFrame(source, frameDuration)), loop)
        {
        }
    }
}
