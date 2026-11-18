using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace ReluctersteSDK.Game.Ecs
{
    public sealed partial class World
    {
        private readonly List<SystemTiming> _systemTimings = new List<SystemTiming>();
        private int _frameCount;
        private double _updateMilliseconds;
        private double _renderMilliseconds;

        /// <summary>获取或设置是否记录系统耗时；默认关闭以避免常态开销。</summary>
        public bool StatisticsEnabled { get; set; }

        /// <summary>获取上一帧各系统的耗时记录。</summary>
        public IReadOnlyList<SystemTiming> SystemTimings => _systemTimings;

        /// <summary>获取世界统计快照。</summary>
        public WorldStats GetStats()
        {
            int componentCount = 0;
            foreach (IComponentStore store in _stores.Values)
                componentCount += store.Count;

            return new WorldStats(
                Entities.AliveCount,
                Entities.Capacity,
                _stores.Count,
                componentCount,
                CountSystems(SystemPhase.Update),
                CountSystems(SystemPhase.LateUpdate),
                CountSystems(SystemPhase.Render),
                _structuralVersion,
                _disposed,
                _frameCount,
                _updateMilliseconds,
                _renderMilliseconds,
                Events.QueueCount,
                Events.PendingCount);
        }

        /// <summary>获取各组件存储的统计。</summary>
        public ComponentStoreStats[] GetComponentStats()
        {
            var result = new ComponentStoreStats[_stores.Count];
            int index = 0;
            foreach (IComponentStore store in _stores.Values)
                result[index++] = new ComponentStoreStats(store.ComponentType, store.Count);

            return result;
        }

        internal void BeginStatisticsFrame()
        {
            if (!StatisticsEnabled)
                return;

            _systemTimings.Clear();
        }

        internal void RecordSystemTiming(ISystem system, SystemPhase phase, long startTimestamp)
        {
            if (!StatisticsEnabled)
                return;

            double milliseconds = Stopwatch.GetElapsedTime(startTimestamp).TotalMilliseconds;
            string name = system.GetType().FullName ?? system.GetType().Name;
            _systemTimings.Add(new SystemTiming(name, phase, milliseconds));
        }

        internal void EndUpdateStatistics(long startTimestamp)
        {
            if (!StatisticsEnabled)
                return;

            _updateMilliseconds = Stopwatch.GetElapsedTime(startTimestamp).TotalMilliseconds;
            _frameCount++;
        }

        internal void EndRenderStatistics(long startTimestamp)
        {
            if (!StatisticsEnabled)
                return;

            _renderMilliseconds = Stopwatch.GetElapsedTime(startTimestamp).TotalMilliseconds;
        }

        private int CountSystems(SystemPhase phase)
        {
            int count = 0;
            foreach (SystemRegistration registration in _systems)
                if (registration.Phase == phase)
                    count++;
            return count;
        }
    }
}
