using System;
using System.Collections.Generic;

namespace ReluctersteSDK.Game.Ecs
{
    /// <summary>带组件引用的单组件遍历委托。</summary>
    public delegate void RefAction<T1>(Entity entity, ref T1 c1) where T1 : notnull;

    /// <summary>带组件引用的双组件遍历委托。</summary>
    public delegate void RefAction<T1, T2>(Entity entity, ref T1 c1, ref T2 c2)
        where T1 : notnull
        where T2 : notnull;

    /// <summary>带组件引用的三组件遍历委托。</summary>
    public delegate void RefAction<T1, T2, T3>(Entity entity, ref T1 c1, ref T2 c2, ref T3 c3)
        where T1 : notnull
        where T2 : notnull
        where T3 : notnull;

    /// <summary>带组件引用的四组件遍历委托。</summary>
    public delegate void RefAction<T1, T2, T3, T4>(Entity entity, ref T1 c1, ref T2 c2, ref T3 c3, ref T4 c4)
        where T1 : notnull
        where T2 : notnull
        where T3 : notnull
        where T4 : notnull;

    /// <summary>遍历安全校验。</summary>
    internal static class QueryGuard
    {
        public static void Ensure(World world, int version)
        {
            if (world.StructuralVersion != version)
            {
                throw new InvalidOperationException(
                    "遍历过程中发生了结构性修改（创建/销毁实体，或增删组件）。" +
                    "请改用 World.Commands 延迟执行，或先调用 ToEntityArray() 取快照。");
            }
        }

        public static bool IsExcluded(Entity entity, IComponentStore[]? excluded)
        {
            if (excluded is null)
                return false;

            for (int i = 0; i < excluded.Length; i++)
                if (excluded[i].Contains(entity.Index))
                    return true;

            return false;
        }

        public static IComponentStore[] Append(IComponentStore[]? existing, IComponentStore store)
        {
            if (existing is null || existing.Length == 0)
                return new[] { store };

            var result = new IComponentStore[existing.Length + 1];
            Array.Copy(existing, result, existing.Length);
            result[^1] = store;
            return result;
        }
    }

    /// <summary>单组件查询。</summary>
    /// <typeparam name="T1">组件类型。</typeparam>
    public readonly struct Query<T1> where T1 : notnull
    {
        private readonly World _world;
        private readonly ComponentStore<T1> _store1;
        private readonly IComponentStore[]? _excluded;

        internal Query(World world, ComponentStore<T1> store1, IComponentStore[]? excluded = null)
        {
            _world = world;
            _store1 = store1;
            _excluded = excluded;
        }

        /// <summary>排除同时持有指定组件的实体。</summary>
        public Query<T1> Without<TExclude>() where TExclude : notnull
            => new Query<T1>(_world, _store1, QueryGuard.Append(_excluded, _world.Store<TExclude>()));

        /// <summary>获取匹配的实体数量。</summary>
        public int Count
        {
            get
            {
                int count = 0;
                ReadOnlySpan<Entity> entities = _store1.Entities;
                for (int i = 0; i < entities.Length; i++)
                    if (!QueryGuard.IsExcluded(entities[i], _excluded))
                        count++;
                return count;
            }
        }

        /// <summary>遍历所有持有该组件的实体。</summary>
        /// <param name="action">回调；组件以引用形式传入，可直接修改。</param>
        /// <exception cref="InvalidOperationException">回调中发生了结构性修改。</exception>
        public void ForEach(RefAction<T1> action)
        {
            if (action is null) throw new ArgumentNullException(nameof(action));

            int version = _world.StructuralVersion;
            ReadOnlySpan<Entity> entities = _store1.Entities;
            Span<T1> components = _store1.Components;

            for (int i = 0; i < entities.Length; i++)
            {
                Entity entity = entities[i];
                if (QueryGuard.IsExcluded(entity, _excluded))
                    continue;

                QueryGuard.Ensure(_world, version);
                action(entity, ref components[i]);
                QueryGuard.Ensure(_world, version);
            }
        }

        /// <summary>获取匹配实体的快照数组。</summary>
        public Entity[] ToEntityArray()
        {
            ReadOnlySpan<Entity> entities = _store1.Entities;
            var result = new Entity[Count];
            int n = 0;

            for (int i = 0; i < entities.Length; i++)
            {
                if (!QueryGuard.IsExcluded(entities[i], _excluded))
                    result[n++] = entities[i];
            }

            return result;
        }
    }

    /// <summary>双组件查询（取交集）。</summary>
    public readonly struct Query<T1, T2> where T1 : notnull where T2 : notnull
    {
        private readonly World _world;
        private readonly ComponentStore<T1> _store1;
        private readonly ComponentStore<T2> _store2;
        private readonly IComponentStore[]? _excluded;

        internal Query(World world, ComponentStore<T1> store1, ComponentStore<T2> store2, IComponentStore[]? excluded = null)
        {
            _world = world;
            _store1 = store1;
            _store2 = store2;
            _excluded = excluded;
        }

        /// <summary>排除同时持有指定组件的实体。</summary>
        public Query<T1, T2> Without<TExclude>() where TExclude : notnull
            => new Query<T1, T2>(_world, _store1, _store2, QueryGuard.Append(_excluded, _world.Store<TExclude>()));

        /// <summary>获取匹配的实体数量（每次调用现算）。</summary>
        public int Count
        {
            get
            {
                int count = 0;
                if (_store1.Count <= _store2.Count)
                {
                    ReadOnlySpan<Entity> entities = _store1.Entities;
                    for (int i = 0; i < entities.Length; i++)
                    {
                        Entity entity = entities[i];
                        if (_store2.TryGetDense(entity.Index, out _) && !QueryGuard.IsExcluded(entity, _excluded))
                            count++;
                    }
                }
                else
                {
                    ReadOnlySpan<Entity> entities = _store2.Entities;
                    for (int i = 0; i < entities.Length; i++)
                    {
                        Entity entity = entities[i];
                        if (_store1.TryGetDense(entity.Index, out _) && !QueryGuard.IsExcluded(entity, _excluded))
                            count++;
                    }
                }

                return count;
            }
        }

        /// <summary>遍历同时持有两种组件的实体。</summary>
        /// <param name="action">回调；两个组件均以引用形式传入。</param>
        /// <exception cref="InvalidOperationException">回调中发生了结构性修改。</exception>
        public void ForEach(RefAction<T1, T2> action)
        {
            if (action is null) throw new ArgumentNullException(nameof(action));

            int version = _world.StructuralVersion;
            Span<T1> c1 = _store1.Components;
            Span<T2> c2 = _store2.Components;

            if (_store1.Count <= _store2.Count)
            {
                ReadOnlySpan<Entity> entities = _store1.Entities;
                for (int i = 0; i < entities.Length; i++)
                {
                    Entity entity = entities[i];
                    if (!_store2.TryGetDense(entity.Index, out int d2) || QueryGuard.IsExcluded(entity, _excluded))
                        continue;

                    QueryGuard.Ensure(_world, version);
                    action(entity, ref c1[i], ref c2[d2]);
                    QueryGuard.Ensure(_world, version);
                }
            }
            else
            {
                ReadOnlySpan<Entity> entities = _store2.Entities;
                for (int i = 0; i < entities.Length; i++)
                {
                    Entity entity = entities[i];
                    if (!_store1.TryGetDense(entity.Index, out int d1) || QueryGuard.IsExcluded(entity, _excluded))
                        continue;

                    QueryGuard.Ensure(_world, version);
                    action(entity, ref c1[d1], ref c2[i]);
                    QueryGuard.Ensure(_world, version);
                }
            }
        }

        /// <summary>获取匹配实体的快照数组。</summary>
        public Entity[] ToEntityArray()
        {
            ReadOnlySpan<Entity> entities = _store1.Entities;
            var result = new Entity[Count];
            int n = 0;

            for (int i = 0; i < entities.Length; i++)
            {
                Entity entity = entities[i];
                if (_store2.TryGetDense(entity.Index, out _) && !QueryGuard.IsExcluded(entity, _excluded))
                    result[n++] = entity;
            }

            return result;
        }
    }

    /// <summary>三组件查询（取交集）。</summary>
    public readonly struct Query<T1, T2, T3>
        where T1 : notnull where T2 : notnull where T3 : notnull
    {
        private readonly World _world;
        private readonly ComponentStore<T1> _store1;
        private readonly ComponentStore<T2> _store2;
        private readonly ComponentStore<T3> _store3;
        private readonly IComponentStore[]? _excluded;

        internal Query(
            World world,
            ComponentStore<T1> store1,
            ComponentStore<T2> store2,
            ComponentStore<T3> store3,
            IComponentStore[]? excluded = null)
        {
            _world = world;
            _store1 = store1;
            _store2 = store2;
            _store3 = store3;
            _excluded = excluded;
        }

        /// <summary>排除同时持有指定组件的实体。</summary>
        public Query<T1, T2, T3> Without<TExclude>() where TExclude : notnull
            => new Query<T1, T2, T3>(_world, _store1, _store2, _store3, QueryGuard.Append(_excluded, _world.Store<TExclude>()));

        /// <summary>获取匹配的实体数量。</summary>
        public int Count
        {
            get
            {
                int count = 0;
                ForEachCore((Entity entity, ref T1 _, ref T2 _, ref T3 _) => count++);
                return count;
            }
        }

        /// <summary>遍历同时持有三种组件的实体。</summary>
        public void ForEach(RefAction<T1, T2, T3> action)
        {
            if (action is null) throw new ArgumentNullException(nameof(action));
            ForEachCore(action);
        }

        private void ForEachCore(RefAction<T1, T2, T3> action)
        {
            int version = _world.StructuralVersion;
            Span<T1> c1 = _store1.Components;
            Span<T2> c2 = _store2.Components;
            Span<T3> c3 = _store3.Components;

            // 选 Count 最小的存储作为驱动
            if (_store1.Count <= _store2.Count && _store1.Count <= _store3.Count)
            {
                ReadOnlySpan<Entity> entities = _store1.Entities;
                for (int i = 0; i < entities.Length; i++)
                {
                    Entity entity = entities[i];
                    if (!_store2.TryGetDense(entity.Index, out int d2) ||
                        !_store3.TryGetDense(entity.Index, out int d3) ||
                        QueryGuard.IsExcluded(entity, _excluded))
                        continue;

                    QueryGuard.Ensure(_world, version);
                    action(entity, ref c1[i], ref c2[d2], ref c3[d3]);
                    QueryGuard.Ensure(_world, version);
                }
            }
            else if (_store2.Count <= _store3.Count)
            {
                ReadOnlySpan<Entity> entities = _store2.Entities;
                for (int i = 0; i < entities.Length; i++)
                {
                    Entity entity = entities[i];
                    if (!_store1.TryGetDense(entity.Index, out int d1) ||
                        !_store3.TryGetDense(entity.Index, out int d3) ||
                        QueryGuard.IsExcluded(entity, _excluded))
                        continue;

                    QueryGuard.Ensure(_world, version);
                    action(entity, ref c1[d1], ref c2[i], ref c3[d3]);
                    QueryGuard.Ensure(_world, version);
                }
            }
            else
            {
                ReadOnlySpan<Entity> entities = _store3.Entities;
                for (int i = 0; i < entities.Length; i++)
                {
                    Entity entity = entities[i];
                    if (!_store1.TryGetDense(entity.Index, out int d1) ||
                        !_store2.TryGetDense(entity.Index, out int d2) ||
                        QueryGuard.IsExcluded(entity, _excluded))
                        continue;

                    QueryGuard.Ensure(_world, version);
                    action(entity, ref c1[d1], ref c2[d2], ref c3[i]);
                    QueryGuard.Ensure(_world, version);
                }
            }
        }

        /// <summary>获取匹配实体的快照数组。</summary>
        public Entity[] ToEntityArray()
        {
            var result = new List<Entity>(Count);
            ForEachCore((Entity entity, ref T1 _, ref T2 _, ref T3 _) => result.Add(entity));
            return result.ToArray();
        }
    }

    /// <summary>四组件查询（取交集）。</summary>
    public readonly struct Query<T1, T2, T3, T4>
        where T1 : notnull where T2 : notnull where T3 : notnull where T4 : notnull
    {
        private readonly World _world;
        private readonly ComponentStore<T1> _store1;
        private readonly ComponentStore<T2> _store2;
        private readonly ComponentStore<T3> _store3;
        private readonly ComponentStore<T4> _store4;
        private readonly IComponentStore[]? _excluded;

        internal Query(
            World world,
            ComponentStore<T1> store1,
            ComponentStore<T2> store2,
            ComponentStore<T3> store3,
            ComponentStore<T4> store4,
            IComponentStore[]? excluded = null)
        {
            _world = world;
            _store1 = store1;
            _store2 = store2;
            _store3 = store3;
            _store4 = store4;
            _excluded = excluded;
        }

        /// <summary>排除同时持有指定组件的实体。</summary>
        public Query<T1, T2, T3, T4> Without<TExclude>() where TExclude : notnull
            => new Query<T1, T2, T3, T4>(
                _world, _store1, _store2, _store3, _store4,
                QueryGuard.Append(_excluded, _world.Store<TExclude>()));

        /// <summary>获取匹配的实体数量。</summary>
        public int Count
        {
            get
            {
                int count = 0;
                ForEachCore((Entity entity, ref T1 _, ref T2 _, ref T3 _, ref T4 _) => count++);
                return count;
            }
        }

        /// <summary>遍历同时持有四种组件的实体。</summary>
        public void ForEach(RefAction<T1, T2, T3, T4> action)
        {
            if (action is null) throw new ArgumentNullException(nameof(action));
            ForEachCore(action);
        }

        private void ForEachCore(RefAction<T1, T2, T3, T4> action)
        {
            int version = _world.StructuralVersion;
            Span<T1> c1 = _store1.Components;
            Span<T2> c2 = _store2.Components;
            Span<T3> c3 = _store3.Components;
            Span<T4> c4 = _store4.Components;

            if (_store1.Count <= _store2.Count && _store1.Count <= _store3.Count && _store1.Count <= _store4.Count)
            {
                ReadOnlySpan<Entity> entities = _store1.Entities;
                for (int i = 0; i < entities.Length; i++)
                {
                    Entity entity = entities[i];
                    if (!_store2.TryGetDense(entity.Index, out int d2) ||
                        !_store3.TryGetDense(entity.Index, out int d3) ||
                        !_store4.TryGetDense(entity.Index, out int d4) ||
                        QueryGuard.IsExcluded(entity, _excluded))
                        continue;

                    QueryGuard.Ensure(_world, version);
                    action(entity, ref c1[i], ref c2[d2], ref c3[d3], ref c4[d4]);
                    QueryGuard.Ensure(_world, version);
                }
            }
            else if (_store2.Count <= _store3.Count && _store2.Count <= _store4.Count)
            {
                ReadOnlySpan<Entity> entities = _store2.Entities;
                for (int i = 0; i < entities.Length; i++)
                {
                    Entity entity = entities[i];
                    if (!_store1.TryGetDense(entity.Index, out int d1) ||
                        !_store3.TryGetDense(entity.Index, out int d3) ||
                        !_store4.TryGetDense(entity.Index, out int d4) ||
                        QueryGuard.IsExcluded(entity, _excluded))
                        continue;

                    QueryGuard.Ensure(_world, version);
                    action(entity, ref c1[d1], ref c2[i], ref c3[d3], ref c4[d4]);
                    QueryGuard.Ensure(_world, version);
                }
            }
            else if (_store3.Count <= _store4.Count)
            {
                ReadOnlySpan<Entity> entities = _store3.Entities;
                for (int i = 0; i < entities.Length; i++)
                {
                    Entity entity = entities[i];
                    if (!_store1.TryGetDense(entity.Index, out int d1) ||
                        !_store2.TryGetDense(entity.Index, out int d2) ||
                        !_store4.TryGetDense(entity.Index, out int d4) ||
                        QueryGuard.IsExcluded(entity, _excluded))
                        continue;

                    QueryGuard.Ensure(_world, version);
                    action(entity, ref c1[d1], ref c2[d2], ref c3[i], ref c4[d4]);
                    QueryGuard.Ensure(_world, version);
                }
            }
            else
            {
                ReadOnlySpan<Entity> entities = _store4.Entities;
                for (int i = 0; i < entities.Length; i++)
                {
                    Entity entity = entities[i];
                    if (!_store1.TryGetDense(entity.Index, out int d1) ||
                        !_store2.TryGetDense(entity.Index, out int d2) ||
                        !_store3.TryGetDense(entity.Index, out int d3) ||
                        QueryGuard.IsExcluded(entity, _excluded))
                        continue;

                    QueryGuard.Ensure(_world, version);
                    action(entity, ref c1[d1], ref c2[d2], ref c3[d3], ref c4[i]);
                    QueryGuard.Ensure(_world, version);
                }
            }
        }

        /// <summary>获取匹配实体的快照数组。</summary>
        public Entity[] ToEntityArray()
        {
            var result = new List<Entity>(Count);
            ForEachCore((Entity entity, ref T1 _, ref T2 _, ref T3 _, ref T4 _) => result.Add(entity));
            return result.ToArray();
        }
    }
}
