using System;
using System.Collections.Generic;
using System.Numerics;
using ReluctersteSDK.Game.Ecs;
using ReluctersteSDK.Math.Vectors;

namespace ReluctersteSDK.Game.Systems
{
    /// <summary>
    /// 世界变换计算系统：按拓扑序把 <see cref="Transform"/> 逐层累乘成 <see cref="WorldTransform"/>。
    /// <para>
    /// 支持脏标记与上一帧快照比对；只有局部变换、父节点或层级发生变化时才重算。
    /// 执行顺序要求：在位移/旋转等逻辑系统之后、渲染系统之前。
    /// </para>
    /// </summary>
    public sealed class TransformSystem : ISystem
    {
        private struct TransformSnapshot
        {
            public CartesianVector2D LocalPosition;
            public float LocalRotation;
            public CartesianVector2D LocalScale;
            public Entity Parent;
            public bool HasParent;
            public bool Valid;
        }

        private readonly List<Entity> _roots = new List<Entity>();
        private readonly List<Entity> _order = new List<Entity>();
        private readonly Stack<Entity> _stack = new Stack<Entity>();
        private readonly HashSet<int> _visited = new HashSet<int>();
        private readonly Dictionary<int, TransformSnapshot> _snapshots = new Dictionary<int, TransformSnapshot>();
        private readonly HashSet<int> _dirtyThisFrame = new HashSet<int>();

        private int _lastStructuralVersion = -1;
        private int _lastHierarchyVersion = -1;
        private bool _forceFullRebuild = true;

        /// <summary>强制下一帧重建全部拓扑与世界矩阵。</summary>
        public void FullRebuild()
        {
            _forceFullRebuild = true;
            _snapshots.Clear();
            _lastStructuralVersion = -1;
            _lastHierarchyVersion = -1;
        }

        /// <inheritdoc />
        public void Execute(World world, float deltaTime)
        {
            if (world is null) throw new ArgumentNullException(nameof(world));

            ComponentStore<Transform> transforms = world.Store<Transform>();

            bool topologyChanged = _forceFullRebuild
                || _lastStructuralVersion != world.StructuralVersion
                || _lastHierarchyVersion != world.HierarchyVersion;

            if (topologyChanged)
                RebuildOrder(world, transforms);

            _dirtyThisFrame.Clear();

            for (int i = 0; i < _order.Count; i++)
            {
                Entity entity = _order[i];
                if (!world.IsAlive(entity) || !transforms.TryGetDense(entity.Index, out int dense))
                    continue;

                Transform transform = transforms.Components[dense];

                bool hasParent = world.TryGet<Parent>(entity, out Parent parent)
                    && world.IsAlive(parent.Value)
                    && world.Has<Transform>(parent.Value)
                    && parent.Value != entity;

                bool hasSnapshot = _snapshots.TryGetValue(entity.Index, out TransformSnapshot snapshot);
                bool localChanged = !hasSnapshot
                    || snapshot.LocalPosition != transform.LocalPosition
                    || snapshot.LocalRotation != transform.LocalRotation
                    || snapshot.LocalScale != transform.LocalScale;
                bool parentChanged = !hasSnapshot
                    || snapshot.HasParent != hasParent
                    || (hasParent && snapshot.Parent != parent.Value);

                bool parentDirty = hasParent && _dirtyThisFrame.Contains(parent.Value.Index);
                bool needsUpdate = _forceFullRebuild
                    || transform.IsDirty
                    || !hasSnapshot
                    || localChanged
                    || parentChanged
                    || parentDirty
                    || !world.Has<WorldTransform>(entity);

                if (needsUpdate)
                {
                    Matrix3x2 local = transform.ToMatrix();

                    if (hasParent
                        && world.TryGet<WorldTransform>(parent.Value, out WorldTransform parentWorld))
                    {
                        local = local * parentWorld.Value;
                    }

                    world.Add(entity, new WorldTransform { Value = local });
                    transforms.Components[dense].IsDirty = false;
                    _dirtyThisFrame.Add(entity.Index);

                    _snapshots[entity.Index] = new TransformSnapshot
                    {
                        LocalPosition = transform.LocalPosition,
                        LocalRotation = transform.LocalRotation,
                        LocalScale = transform.LocalScale,
                        Parent = hasParent ? parent.Value : Entity.Null,
                        HasParent = hasParent,
                        Valid = true
                    };
                }
            }

            if (topologyChanged)
                RemoveDeadSnapshots(world);

            _lastStructuralVersion = world.StructuralVersion;
            _lastHierarchyVersion = world.HierarchyVersion;
            _forceFullRebuild = false;
        }

        private void RebuildOrder(World world, ComponentStore<Transform> transforms)
        {
            _roots.Clear();
            _order.Clear();
            _stack.Clear();
            _visited.Clear();

            ReadOnlySpan<Entity> entities = transforms.Entities;

            // 1. 收集根：有 Transform，且父节点不存在 / 已销毁 / 没有 Transform / 自环。
            for (int i = 0; i < entities.Length; i++)
            {
                Entity entity = entities[i];
                bool hasUsableParent =
                    world.TryGet<Parent>(entity, out Parent parent)
                    && world.IsAlive(parent.Value)
                    && world.Has<Transform>(parent.Value)
                    && parent.Value != entity;

                if (!hasUsableParent)
                    _roots.Add(entity);
            }

            // 2. 深度优先，得到"父先于子"的拓扑序；只压入带 Transform 的子节点。
            for (int i = _roots.Count - 1; i >= 0; i--)
                _stack.Push(_roots[i]);

            while (_stack.Count > 0)
            {
                Entity entity = _stack.Pop();
                if (!_visited.Add(entity.Index))
                    continue;

                _order.Add(entity);

                if (world.TryGet<Children>(entity, out Children children) && children is not null)
                {
                    IReadOnlyList<Entity> items = children.Items;
                    for (int i = items.Count - 1; i >= 0; i--)
                    {
                        Entity child = items[i];
                        if (world.IsAlive(child) && world.Has<Transform>(child))
                            _stack.Push(child);
                    }
                }
            }

            // 3. 兜底：异常状态下（例如已存在的环）未被访问到的实体也补进队列
            for (int i = 0; i < entities.Length; i++)
            {
                Entity entity = entities[i];
                if (_visited.Add(entity.Index))
                    _order.Add(entity);
            }
        }

        private void RemoveDeadSnapshots(World world)
        {
            if (_snapshots.Count == 0)
                return;

            _aliveIndices.Clear();
            foreach (Entity entity in world.Entities.GetAliveEntities())
                _aliveIndices.Add(entity.Index);

            _stale.Clear();
            foreach (int index in _snapshots.Keys)
            {
                if (!_aliveIndices.Contains(index))
                    _stale.Add(index);
            }

            foreach (int index in _stale)
                _snapshots.Remove(index);
        }

        private readonly HashSet<int> _aliveIndices = new HashSet<int>();
        private readonly List<int> _stale = new List<int>();
    }
}
