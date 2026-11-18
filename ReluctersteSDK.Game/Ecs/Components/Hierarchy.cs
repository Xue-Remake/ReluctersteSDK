using System.Collections.Generic;

namespace ReluctersteSDK.Game.Ecs
{
    /// <summary>父节点引用组件。</summary>
    public struct Parent
    {
        /// <summary>父实体句柄。</summary>
        public Entity Value;
    }

    /// <summary>
    /// 子节点集合组件。
    /// <para>因为需要可变列表，所以是引用类型；<see cref="ComponentStore{T}"/> 以 <c>notnull</c> 约束支持它。</para>
    /// </summary>
    public sealed class Children
    {
        private readonly List<Entity> _items = new List<Entity>();

        /// <summary>子实体句柄只读列表（顺序无关紧要，由使用方按需排序）。</summary>
        public IReadOnlyList<Entity> Items => _items;

        /// <summary>获取子节点数量。</summary>
        public int Count => _items.Count;

        internal void AddInternal(Entity entity) => _items.Add(entity);

        internal bool RemoveInternal(Entity entity) => _items.Remove(entity);

        internal bool ContainsInternal(Entity entity) => _items.Contains(entity);

        internal void ClearInternal() => _items.Clear();
    }

    /// <summary>世界变换组件：由 <c>TransformSystem</c> 每帧写入的结果。</summary>
    public struct WorldTransform
    {
        /// <summary>世界矩阵。</summary>
        public System.Numerics.Matrix3x2 Value;
    }
}
