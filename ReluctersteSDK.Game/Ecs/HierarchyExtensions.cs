using System;
using System.Collections.Generic;

namespace ReluctersteSDK.Game.Ecs
{
    /// <summary>
    /// 父子层级的操作入口。
    /// <para>
    /// 必须通过这里维护层级：<see cref="Parent"/> 与 <see cref="Children"/> 是双向数据，
    /// 手工只改一侧会导致两边不一致。
    /// </para>
    /// </summary>
    public static class HierarchyExtensions
    {
        private const int CycleGuardLimit = 4096;

        /// <summary>把 <paramref name="child"/> 挂到 <paramref name="parent"/> 下。</summary>
        /// <param name="world">目标世界。</param>
        /// <param name="child">子实体。</param>
        /// <param name="parent">父实体。</param>
        /// <returns>成功建立父子关系返回 <c>true</c>；实体失效、自环或成环时返回 <c>false</c>。</returns>
        public static bool SetParent(this World world, Entity child, Entity parent)
        {
            if (world is null) throw new ArgumentNullException(nameof(world));
            if (!world.IsAlive(child) || !world.IsAlive(parent)) return false;
            if (child == parent) return false;

            // 若 parent 的祖先链中出现 child，则连接会成环
            if (IsInAncestorChain(world, parent, child)) return false;

            // 先解除旧父节点
            if (world.TryGet<Parent>(child, out Parent old) && world.IsAlive(old.Value)
                && world.TryGet<Children>(old.Value, out Children oldSiblings) && oldSiblings is not null)
            {
                oldSiblings.RemoveInternal(child);
            }

            world.Add(child, new Parent { Value = parent });

            if (!world.TryGet<Children>(parent, out Children siblings) || siblings is null)
            {
                siblings = new Children();
                world.Add(parent, siblings);
            }

            if (!siblings.ContainsInternal(child))
                siblings.AddInternal(child);

            world.BumpHierarchyVersion();
            MarkTransformDirty(world, child);
            return true;
        }

        /// <summary>解除子实体的父关系，使其成为根节点。</summary>
        /// <param name="world">目标世界。</param>
        /// <param name="child">子实体。</param>
        /// <returns>确实解除了父关系返回 <c>true</c>。</returns>
        public static bool RemoveParent(this World world, Entity child)
        {
            if (world is null) throw new ArgumentNullException(nameof(world));
            if (!world.TryGet<Parent>(child, out Parent parent)) return false;

            if (world.IsAlive(parent.Value)
                && world.TryGet<Children>(parent.Value, out Children siblings) && siblings is not null)
            {
                siblings.RemoveInternal(child);
            }

            world.Remove<Parent>(child);
            world.BumpHierarchyVersion();
            MarkTransformDirty(world, child);
            return true;
        }

        /// <summary>获取父实体；没有父节点时返回 <see cref="Entity.Null"/>。</summary>
        /// <param name="world">目标世界。</param>
        /// <param name="child">子实体。</param>
        public static Entity GetParent(this World world, Entity child)
        {
            if (world is null) throw new ArgumentNullException(nameof(world));
            return world.TryGet<Parent>(child, out Parent parent) ? parent.Value : Entity.Null;
        }

        /// <summary>获取子实体列表；没有子节点时返回空列表。</summary>
        /// <param name="world">目标世界。</param>
        /// <param name="parent">父实体。</param>
        public static IReadOnlyList<Entity> GetChildren(this World world, Entity parent)
        {
            if (world is null) throw new ArgumentNullException(nameof(world));
            return world.TryGet<Children>(parent, out Children children) && children is not null
                ? children.Items
                : Array.Empty<Entity>();
        }

        /// <summary>判断 <paramref name="node"/> 是否位于 <paramref name="candidate"/> 的祖先链上。</summary>
        /// <param name="world">目标世界。</param>
        /// <param name="node">起点实体。</param>
        /// <param name="candidate">待查找的祖先实体。</param>
        public static bool IsInAncestorChain(this World world, Entity node, Entity candidate)
        {
            Entity current = node;
            int guard = 0;

            while (world.TryGet<Parent>(current, out Parent parent) && world.IsAlive(parent.Value))
            {
                if (parent.Value == candidate) return true;
                current = parent.Value;

                if (++guard > CycleGuardLimit)
                    return true;   // 已存在异常环，保守拒绝本次连接
            }

            return false;
        }

        private static void MarkTransformDirty(World world, Entity entity)
        {
            if (world.Has<Transform>(entity))
                world.Get<Transform>(entity).IsDirty = true;
        }
    }
}
