using System;
using System.Collections.Generic;

namespace ReluctersteSDK.Game.Ecs.Events
{
    /// <summary>
    /// 按类型组织的双缓冲事件容器。
    /// <para>系统在帧内登记事件，下一帧统一读取。读写分离保证遍历期间登记新事件是安全的，
    /// 不需要命令缓冲，也不需要快照。</para>
    /// </summary>
    /// <remarks>
    /// <para><b>交换时机</b>：<see cref="World.Update"/> 在开始处调用一次 <see cref="Swap"/>。
    /// 因此第 N 帧登记的事件在整个第 N+1 帧（Update / LateUpdate / Render）都可以读到，
    /// 所有阶段看到的是同一份缓冲。</para>
    /// <para><b>不做的事</b>：不支持委托订阅、不做异步、不做类型继承分发。
    /// 需要跨模块或跨线程的广播，请使用独立的事件总线在应用层桥接。</para>
    /// <para>本类型不是线程安全的。</para>
    /// </remarks>
    public sealed class WorldEvents
    {
        private readonly Dictionary<Type, IEventQueue> _queues = new Dictionary<Type, IEventQueue>();

        /// <summary>获取已创建的事件队列数量。</summary>
        public int QueueCount => _queues.Count;

        /// <summary>获取所有队列中待交换的事件总数。</summary>
        public int PendingCount
        {
            get
            {
                int total = 0;
                foreach (IEventQueue queue in _queues.Values)
                    total += queue.PendingCount;
                return total;
            }
        }

        /// <summary>
        /// 获取指定事件类型的队列；不存在时创建。
        /// <para>热路径可缓存返回值，避免每次登记都做类型查找。</para>
        /// </summary>
        /// <typeparam name="T">事件类型。</typeparam>
        public EventQueue<T> Queue<T>() where T : notnull
        {
            if (_queues.TryGetValue(typeof(T), out IEventQueue? existing))
                return (EventQueue<T>)existing;

            var created = new EventQueue<T>();
            _queues[typeof(T)] = created;
            return created;
        }

        /// <summary>登记一个事件。</summary>
        /// <typeparam name="T">事件类型。</typeparam>
        /// <param name="e">事件内容。</param>
        public void Raise<T>(in T e) where T : notnull => Queue<T>().Raise(in e);

        /// <summary>
        /// 读取上一批事件。
        /// <para>未登记过该类型时返回空 span，且不会创建队列。</para>
        /// <para><b>返回的 span 只在下一次 <see cref="Swap"/> 之前有效。</b></para>
        /// </summary>
        /// <typeparam name="T">事件类型。</typeparam>
        public ReadOnlySpan<T> Read<T>() where T : notnull
        {
            return _queues.TryGetValue(typeof(T), out IEventQueue? queue)
                ? ((EventQueue<T>)queue).Read
                : ReadOnlySpan<T>.Empty;
        }

        /// <summary>
        /// 读取上一批事件列表。
        /// <para>未登记过该类型时返回空列表，且不会创建队列。</para>
        /// <para>不要求 <c>ref readonly</c> 场景时用这个更省心；约束与
        /// <see cref="EventQueue{T}.ReadList"/> 相同。</para>
        /// </summary>
        /// <typeparam name="T">事件类型。</typeparam>
        public IReadOnlyList<T> ReadList<T>() where T : notnull
        {
            return _queues.TryGetValue(typeof(T), out IEventQueue? queue)
                ? ((EventQueue<T>)queue).ReadList
                : Array.Empty<T>();
        }

        /// <summary>获取指定类型尚未交换的事件数。</summary>
        /// <typeparam name="T">事件类型。</typeparam>
        public int PendingCountOf<T>() where T : notnull
        {
            return _queues.TryGetValue(typeof(T), out IEventQueue? queue) ? queue.PendingCount : 0;
        }

        /// <summary>交换所有队列的读写缓冲。由 <see cref="World.Update"/> 自动调用。</summary>
        public void Swap()
        {
            foreach (IEventQueue queue in _queues.Values)
                queue.Swap();
        }

        /// <summary>清空所有队列的读写两侧。</summary>
        public void Clear()
        {
            foreach (IEventQueue queue in _queues.Values)
                queue.Clear();
        }
    }
}
