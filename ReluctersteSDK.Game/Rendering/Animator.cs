using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;

namespace ReluctersteSDK.Game.Rendering
{
    /// <summary>
    /// 动画播放器组件：持有一组命名动画与当前播放状态。
    /// </summary>
    /// <remarks>
    /// <para>播放状态由 <see cref="Tick"/> 推进，通常由 <see cref="SpriteAnimationSystem"/> 调用。</para>
    /// <para>对外暴露的是**状态字段**而不是回调事件：<see cref="FrameChanged"/> 与
    /// <see cref="FinishedClip"/> 只在调用 <see cref="Tick"/> 的那一帧内有效，下一帧会被清空。
    /// 因此消费它们的系统必须排在 <see cref="SpriteAnimationSystem"/> 之后。</para>
    /// </remarks>
    public sealed class Animator
    {
        private readonly Dictionary<string, AnimationClip> _clips =
            new Dictionary<string, AnimationClip>(StringComparer.Ordinal);

        private int _frameIndex;
        private float _frameTime;

        /// <summary>使用一组动画初始化播放器。</summary>
        /// <param name="clips">初始动画集合；同名后者覆盖前者。</param>
        public Animator(params AnimationClip[] clips)
        {
            if (clips is null) return;

            for (int i = 0; i < clips.Length; i++)
            {
                if (clips[i] is not null)
                    _clips[clips[i].Name] = clips[i];
            }
        }

        /// <summary>获取当前动画；未播放过任何动画时为 <c>null</c>。</summary>
        public AnimationClip? CurrentClip { get; private set; }

        /// <summary>获取当前帧下标。</summary>
        public int CurrentFrameIndex => _frameIndex;

        /// <summary>获取当前帧已停留的时长（秒）。</summary>
        public float CurrentFrameTime => _frameTime;

        /// <summary>获取是否正在播放。</summary>
        public bool IsPlaying { get; private set; }

        private float _speed = 1f;

        /// <summary>获取或设置播放速度倍率（1 = 正常速度）。非正数、NaN 与 Infinity 会被忽略。</summary>
        public float Speed
        {
            get => _speed;
            set
            {
                if (float.IsFinite(value) && value > 0f)
                    _speed = value;
            }
        }

        /// <summary>获取本帧刚刚播放结束的**非循环**动画名；没有则为 <c>null</c>。</summary>
        public string? FinishedClip { get; private set; }

        /// <summary>获取本帧内帧下标是否发生过变化。</summary>
        public bool FrameChanged { get; private set; }

        /// <summary>获取已注册的动画数量。</summary>
        public int ClipCount => _clips.Count;

        /// <summary>获取已注册的动画名称集合。</summary>
        public IEnumerable<string> ClipNames => _clips.Keys;

        /// <summary>获取当前帧的源矩形；没有可播放的当前帧时为 <c>null</c>。</summary>
        public Rectangle? CurrentSource
        {
            get
            {
                if (CurrentClip is null || _frameIndex < 0 || _frameIndex >= CurrentClip.FrameCount)
                    return null;

                return CurrentClip.Frames[_frameIndex].Source;
            }
        }

        /// <summary>注册或覆盖一段动画。</summary>
        /// <param name="clip">动画。</param>
        /// <exception cref="ArgumentNullException">动画为 null。</exception>
        public void AddClip(AnimationClip clip)
        {
            if (clip is null) throw new ArgumentNullException(nameof(clip));
            _clips[clip.Name] = clip;
        }

        /// <summary>判断是否存在指定名称的动画。</summary>
        /// <param name="name">动画名称。</param>
        public bool HasClip(string name) => name is not null && _clips.ContainsKey(name);

        /// <summary>播放指定名称的动画。</summary>
        /// <param name="name">动画名称。</param>
        /// <param name="restart">
        /// 是否强制执行：为 <c>false</c> 时，若该动画正是当前动画且正在播放，则保持进度不变。
        /// </param>
        /// <returns>成功切换/继续播放返回 <c>true</c>；名称不存在返回 <c>false</c>。</returns>
        public bool Play(string name, bool restart = false)
        {
            if (name is null || !_clips.TryGetValue(name, out AnimationClip? clip))
                return false;

            if (!restart && ReferenceEquals(CurrentClip, clip) && IsPlaying)
                return true;

            CurrentClip = clip;
            _frameIndex = 0;
            _frameTime = 0f;
            IsPlaying = true;
            FinishedClip = null;
            FrameChanged = true;
            return true;
        }

        /// <summary>停止播放并回到第一帧（当前帧仍可被渲染）。</summary>
        public void Stop()
        {
            IsPlaying = false;
            _frameIndex = 0;
            _frameTime = 0f;
            FinishedClip = null;
            FrameChanged = true;
        }

        /// <summary>暂停在当前帧。</summary>
        public void Pause() => IsPlaying = false;

        /// <summary>从暂停处继续播放；尚未播放过任何动画时无效果。</summary>
        public void Resume()
        {
            if (CurrentClip is not null)
                IsPlaying = true;
        }

        /// <summary>
        /// 推进动画时间。
        /// </summary>
        /// <param name="deltaTime">距上一帧的秒数。</param>
        /// <remarks>
        /// 单次调用可以跨越任意多帧；非循环动画播放到最后一帧后停止，
        /// 并把动画名写入 <see cref="FinishedClip"/>，当前帧保持在最后一帧。
        /// </remarks>
        public void Tick(float deltaTime)
        {
            FinishedClip = null;
            FrameChanged = false;

            if (!IsPlaying || CurrentClip is null || !float.IsFinite(deltaTime) || deltaTime <= 0f || Speed <= 0f)
                return;

            AnimationClip clip = CurrentClip;
            float remaining = deltaTime * Speed;
            int guard = 0;

            while (remaining > 0f)
            {
                if (++guard > 10000)
                    break;

                AnimationFrame frame = clip.Frames[_frameIndex];
                float left = frame.Duration - _frameTime;

                if (remaining < left)
                {
                    _frameTime += remaining;
                    break;
                }

                remaining -= left;
                _frameTime = 0f;

                if (_frameIndex + 1 < clip.FrameCount)
                {
                    _frameIndex++;
                    FrameChanged = true;
                }
                else if (clip.Loop)
                {
                    _frameIndex = 0;
                    FrameChanged = true;
                }
                else
                {
                    // 非循环：停在最后一帧，本帧内不再继续推进
                    IsPlaying = false;
                    FinishedClip = clip.Name;
                    break;
                }
            }
        }
    }
}
