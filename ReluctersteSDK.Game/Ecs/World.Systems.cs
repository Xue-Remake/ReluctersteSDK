using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace ReluctersteSDK.Game.Ecs
{
    public sealed partial class World
    {
        private readonly List<SystemRegistration> _systems = new List<SystemRegistration>();
        private readonly List<ISystem> _updateSystemsView = new List<ISystem>();
        private readonly List<ISystem> _renderSystemsView = new List<ISystem>();
        private int _nextRegistrationOrder;
        private bool _started;

        /// <summary>获取 Update 阶段的系统列表（按执行顺序）。</summary>
        public IReadOnlyList<ISystem> UpdateSystems => _updateSystemsView;

        /// <summary>获取 Render 阶段的系统列表（按执行顺序）。</summary>
        public IReadOnlyList<ISystem> RenderSystems => _renderSystemsView;

        /// <summary>注册系统；同一阶段内按优先级升序执行，优先级相同按注册顺序。</summary>
        /// <param name="system">要注册的系统。</param>
        /// <param name="phase">所属阶段。</param>
        /// <param name="priority">优先级；数值小的先执行。</param>
        /// <param name="enabled">是否立即启用。</param>
        public void AddSystem(ISystem system, SystemPhase phase = SystemPhase.Update, int priority = 0, bool enabled = true)
        {
            EnsureNotDisposed();
            if (system is null) throw new ArgumentNullException(nameof(system));

            var registration = new SystemRegistration(system, phase, priority, enabled, _nextRegistrationOrder++);
            _systems.Add(registration);
            SortSystems();

            if (_started && enabled)
            {
                system.OnStart(this);
                if (phase == SystemPhase.Startup)
                    ExecuteSystem(system, 0f, SystemPhase.Startup);
            }
        }

        /// <summary>移除系统。</summary>
        /// <param name="system">要移除的系统。</param>
        /// <returns>若确实移除了系统返回 <c>true</c>。</returns>
        public bool RemoveSystem(ISystem system)
        {
            EnsureNotDisposed();
            if (system is null) throw new ArgumentNullException(nameof(system));

            bool removed = false;
            for (int i = _systems.Count - 1; i >= 0; i--)
            {
                if (!ReferenceEquals(_systems[i].System, system))
                    continue;

                _systems.RemoveAt(i);
                removed = true;
            }

            if (removed)
            {
                if (_started)
                    system.OnStop(this);
                RebuildViews();
            }

            return removed;
        }

        /// <summary>启用或禁用系统；禁用系统保留注册但不执行。</summary>
        public bool SetSystemEnabled(ISystem system, bool enabled)
        {
            EnsureNotDisposed();
            if (system is null) throw new ArgumentNullException(nameof(system));

            bool found = false;
            foreach (SystemRegistration registration in _systems)
            {
                if (ReferenceEquals(registration.System, system))
                {
                    registration.Enabled = enabled;
                    found = true;
                }
            }

            RebuildViews();
            return found;
        }

        /// <summary>修改系统优先级。</summary>
        public bool SetSystemPriority(ISystem system, int priority)
        {
            EnsureNotDisposed();
            if (system is null) throw new ArgumentNullException(nameof(system));

            bool found = false;
            foreach (SystemRegistration registration in _systems)
            {
                if (ReferenceEquals(registration.System, system))
                {
                    registration.Priority = priority;
                    found = true;
                }
            }

            if (found)
                SortSystems();

            return found;
        }

        /// <summary>判断系统是否启用。</summary>
        public bool IsSystemEnabled(ISystem system)
        {
            foreach (SystemRegistration registration in _systems)
                if (ReferenceEquals(registration.System, system))
                    return registration.Enabled;

            return false;
        }

        /// <summary>尝试获取系统的注册快照。</summary>
        public bool TryGetSystemEntry(ISystem system, out SystemEntry entry)
        {
            foreach (SystemRegistration registration in _systems)
            {
                if (ReferenceEquals(registration.System, system))
                {
                    entry = registration.ToEntry();
                    return true;
                }
            }

            entry = default;
            return false;
        }

        /// <summary>获取指定阶段的全部系统注册快照。</summary>
        /// <param name="phase">目标阶段。</param>
        public IReadOnlyList<SystemEntry> GetSystems(SystemPhase phase)
        {
            var result = new List<SystemEntry>();
            foreach (SystemRegistration registration in _systems)
            {
                if (registration.Phase == phase)
                    result.Add(registration.ToEntry());
            }

            return result;
        }

        /// <summary>启动世界：调用所有系统的 OnStart，并执行 Startup 阶段。</summary>
        public void Start()
        {
            EnsureNotDisposed();
            if (_started)
                return;

            _started = true;

            // 先通知全部系统，再执行 Startup 阶段
            SystemRegistration[] snapshot = SnapshotRegistrations();
            foreach (SystemRegistration registration in snapshot)
            {
                if (registration.Enabled)
                    registration.System.OnStart(this);
            }

            ExecutePhase(SystemPhase.Startup, 0f);
            _commands.Playback(this);
        }

        /// <summary>关闭世界：执行 Teardown 阶段并调用所有系统的 OnStop。</summary>
        public void Shutdown()
        {
            if (_disposed || !_started)
                return;

            ExecutePhase(SystemPhase.Teardown, 0f);
            _commands.Playback(this);

            SystemRegistration[] snapshot = SnapshotRegistrations();
            foreach (SystemRegistration registration in snapshot)
            {
                if (registration.Enabled)
                    registration.System.OnStop(this);
            }

            _started = false;
        }

        /// <summary>执行 Update 与 LateUpdate 阶段的全部系统；每个系统结束后立即回放命令缓冲。</summary>
        /// <param name="deltaTime">距上一帧的秒数。</param>
        public void Update(float deltaTime)
        {
            EnsureNotDisposed();
            StartIfNeeded();

            // 交换事件缓冲：本帧登记的事件在下一帧可读。
            // 放在 StartIfNeeded 之后，使 Startup 阶段登记的事件在首帧就能被读到。
            Events.Swap();

            long start = Stopwatch.GetTimestamp();
            BeginStatisticsFrame();

            ExecutePhase(SystemPhase.Update, deltaTime);
            ExecutePhase(SystemPhase.LateUpdate, deltaTime);

            // 兜底：系统之外写入的命令也要生效
            _commands.Playback(this);
            EndUpdateStatistics(start);
        }

        /// <summary>执行 Render 阶段的全部系统。</summary>
        /// <param name="deltaTime">距上一帧的秒数。</param>
        public void Render(float deltaTime)
        {
            EnsureNotDisposed();
            StartIfNeeded();

            long start = Stopwatch.GetTimestamp();
            ExecutePhase(SystemPhase.Render, deltaTime);
            _commands.Playback(this);
            EndRenderStatistics(start);
        }

        internal void ClearSystems()
        {
            _systems.Clear();
            _updateSystemsView.Clear();
            _renderSystemsView.Clear();
            _nextRegistrationOrder = 0;
            _started = false;
        }

        private void StartIfNeeded()
        {
            if (!_started)
                Start();
        }

        private void ExecutePhase(SystemPhase phase, float deltaTime)
        {
            SystemRegistration[] snapshot = SnapshotRegistrations();

            foreach (SystemRegistration registration in snapshot)
            {
                if (registration.Phase != phase || !registration.Enabled)
                    continue;

                ExecuteSystem(registration.System, deltaTime, phase);
                _commands.Playback(this);
            }
        }

        private void ExecuteSystem(ISystem system, float deltaTime, SystemPhase phase)
        {
            long start = Stopwatch.GetTimestamp();
            system.Execute(this, deltaTime);
            RecordSystemTiming(system, phase, start);
        }

        private SystemRegistration[] SnapshotRegistrations()
            => _systems.ToArray();

        private void SortSystems()
        {
            _systems.Sort(static (a, b) =>
            {
                int result = a.Phase.CompareTo(b.Phase);
                if (result != 0) return result;

                result = a.Priority.CompareTo(b.Priority);
                if (result != 0) return result;

                return a.RegistrationOrder.CompareTo(b.RegistrationOrder);
            });

            RebuildViews();
        }

        private void RebuildViews()
        {
            _updateSystemsView.Clear();
            _renderSystemsView.Clear();

            foreach (SystemRegistration registration in _systems)
            {
                if (!registration.Enabled)
                    continue;

                if (registration.Phase == SystemPhase.Update)
                    _updateSystemsView.Add(registration.System);
                else if (registration.Phase == SystemPhase.Render)
                    _renderSystemsView.Add(registration.System);
            }
        }
    }
}
