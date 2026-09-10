using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;

namespace ReluctersteSDK.Game
{
    /// <summary>
    /// 控制台游戏/模拟循环的抽象基类，提供类似 MonoGame/XNA 的游戏时钟与主循环管理。
    /// </summary>
    public abstract class ConsoleGame
    {
        private bool _isRunning;
        private bool _suppressDraw = true;
        private readonly Stopwatch _stopwatch = new();

        private TimeSpan _accumulatedElapsedTime;
        private TimeSpan _previousTicks;
        private readonly TimeSpan _maxElapsedTime = TimeSpan.FromMilliseconds(500);

        /// <summary>
        /// 获取或设置一个值，指示游戏是否使用固定时间步长更新逻辑（Fixed TimeStep）。
        /// 默认为 <see langword="true"/>。
        /// </summary>
        public bool IsFixedTimeStep { get; set; } = true;
        /// <summary>
        /// 获取或设置目标帧间隔时间（逻辑更新间隔）。
        /// 默认为 1/60 秒（约 16.67 毫秒，即 60 FPS）。
        /// </summary>
        public TimeSpan TargetElapsedTime { get; set; } = TimeSpan.FromTicks(TimeSpan.TicksPerSecond / 60);

        private readonly GameTime _gameTime = new();
        /// <summary>
        /// 初始化 <see cref="ConsoleGame"/> 的新实例。
        /// </summary>
        protected ConsoleGame()
        { }
        /// <summary>
        /// 启动游戏主循环并开始运行。
        /// </summary>
        public void Run()
        {
            _isRunning = true;
        }
    }
}
