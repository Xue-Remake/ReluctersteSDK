using System.Collections.Generic;
using System.Runtime.InteropServices;

namespace ReluctersteSDK.Game.Ecs.Events
{
    /// <summary>
    /// 单一类型的双缓冲事件队列。
    /// <para>写入落在写缓冲，读取看读缓冲，两者是两个独立的列表对象，
    /// 因此在遍历读取时继续登记新事件是安全的。</para>
    /// </summary>
    /// <remarks>
    /// <para>语义：第 N 帧登记的事件，在第 N+1 帧的 <see cref="Read"/> 中可见。
    /// 交换由 <see cref="WorldEvents.Swap"/> 统一触发。</para>
    /// <para>本类型不是线程安全的；所有操作应在游戏主线程上进行。</para>
    /// </remarks>
    /// <typeparam name="T">事件类型。建议使用 <c>readonly record struct</c> 以避免分配。</typeparam>
    public sealed class EventQueue<T> : IEventQueue where T : notnull
    {
        private List<T> _write = new List<T>();
        private List<T> _read = new List<T>();

        /// <summary>获取本帧已登记、尚未交换的事件条数。</summary>
        public int PendingCount => _write.Count;

        /// <summary>获取活跃队列数（固定为 1，用于统计一致性）。</summary>
        int IEventQueue.PendingCount => _write.Count;

        /// <summary>
        /// 获取上一帧登记的事件视图。
        /// <para><b>返回的 span 只在下一次 <see cref="Swap"/> 之前有效</b>；
        /// 不要把它保存到字段或跨帧使用。</para>
        /// </summary>
        public ReadOnlySpan<T> Read => CollectionsMarshal.AsSpan(_read);

        /// <summary>
        /// 获取上一帧登记的事件列表。
        /// <para>与 <see cref="Read"/> 相同的零分配，但返回的是可安全持有引用的只读视图，
        /// 不能用于 <c>ref readonly</c> 遍历。</para>
        /// <para><b>读缓冲由两个列表交替复用</b>：下一次 <see cref="Swap"/> 后，
        /// 你持有的旧引用会被清空（不会抛异常，但内容消失）。
        /// 需要跨帧保存请自行复制。</para>
        /// </summary>
        public IReadOnlyList<T> ReadList => _read;

        /// <summary>登记一个事件。事件会在下一次交换后对读取方可见。</summary>
        /// <param name="e">事件内容。</param>
        public void Raise(in T e) => _write.Add(e);

        /// <summary>交换读写缓冲：本帧登记的事件变为可读，上一批事件被丢弃并复用。</summary>
        public void Swap()
        {
            (_read, _write) = (_write, _read);
            _write.Clear();
        }

        /// <summary>清空读写两侧的全部事件。</summary>
        public void Clear()
        {
            _write.Clear();
            _read.Clear();
        }

        void IEventQueue.Swap() => Swap();

        void IEventQueue.Clear() => Clear();
    }

    /// <summary>
    /// 非泛型接口，供 <see cref="WorldEvents"/> 批量执行交换与清空。
    /// </summary>
    internal interface IEventQueue
    {
        int PendingCount { get; }

        void Swap();

        void Clear();
    }
}
