namespace ReluctersteSDK.Game.Ecs
{
    /// <summary>世界统计快照。</summary>
    public readonly struct WorldStats
    {
        internal WorldStats(
            int entityCount,
            int entityCapacity,
            int componentStoreCount,
            int componentCount,
            int updateSystemCount,
            int lateUpdateSystemCount,
            int renderSystemCount,
            int structuralVersion,
            bool isDisposed,
            int frameCount,
            double updateMilliseconds,
            double renderMilliseconds,
            int eventQueueCount,
            int pendingEventCount)
        {
            EntityCount = entityCount;
            EntityCapacity = entityCapacity;
            ComponentStoreCount = componentStoreCount;
            ComponentCount = componentCount;
            UpdateSystemCount = updateSystemCount;
            LateUpdateSystemCount = lateUpdateSystemCount;
            RenderSystemCount = renderSystemCount;
            StructuralVersion = structuralVersion;
            IsDisposed = isDisposed;
            FrameCount = frameCount;
            UpdateMilliseconds = updateMilliseconds;
            RenderMilliseconds = renderMilliseconds;
            EventQueueCount = eventQueueCount;
            PendingEventCount = pendingEventCount;
        }

        /// <summary>存活实体数量。</summary>
        public int EntityCount { get; }

        /// <summary>已分配槽位数量。</summary>
        public int EntityCapacity { get; }

        /// <summary>组件存储类型数量。</summary>
        public int ComponentStoreCount { get; }

        /// <summary>组件实例总数。</summary>
        public int ComponentCount { get; }

        /// <summary>Update 阶段系统数量。</summary>
        public int UpdateSystemCount { get; }

        /// <summary>LateUpdate 阶段系统数量。</summary>
        public int LateUpdateSystemCount { get; }

        /// <summary>Render 阶段系统数量。</summary>
        public int RenderSystemCount { get; }

        /// <summary>当前结构版本。</summary>
        public int StructuralVersion { get; }

        /// <summary>世界是否已释放。</summary>
        public bool IsDisposed { get; }

        /// <summary>已执行的 Update 帧数。</summary>
        public int FrameCount { get; }

        /// <summary>上一帧 Update 总耗时（毫秒）。</summary>
        public double UpdateMilliseconds { get; }

        /// <summary>上一帧 Render 总耗时（毫秒）。</summary>
        public double RenderMilliseconds { get; }

        /// <summary>已创建的事件队列数量。</summary>
        public int EventQueueCount { get; }

        /// <summary>本帧已登记、尚未交换的事件总数。</summary>
        public int PendingEventCount { get; }
    }

    /// <summary>单个组件存储的统计。</summary>
    public readonly struct ComponentStoreStats
    {
        internal ComponentStoreStats(Type componentType, int count)
        {
            ComponentType = componentType;
            Count = count;
        }

        /// <summary>组件类型。</summary>
        public Type ComponentType { get; }

        /// <summary>组件实例数量。</summary>
        public int Count { get; }
    }

    /// <summary>单个系统的耗时记录。</summary>
    public readonly struct SystemTiming
    {
        internal SystemTiming(string systemName, SystemPhase phase, double milliseconds)
        {
            SystemName = systemName;
            Phase = phase;
            Milliseconds = milliseconds;
        }

        /// <summary>系统类型全名。</summary>
        public string SystemName { get; }

        /// <summary>所属阶段。</summary>
        public SystemPhase Phase { get; }

        /// <summary>本次执行耗时（毫秒）。</summary>
        public double Milliseconds { get; }
    }
}
