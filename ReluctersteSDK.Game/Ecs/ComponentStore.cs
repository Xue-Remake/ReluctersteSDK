using System;
using System.Collections.Generic;

namespace ReluctersteSDK.Game.Ecs
{
    /// <summary>
    /// 基于稀疏集（Sparse Set）的单类型组件存储。
    /// <para>
    /// 结构：<c>_sparse[entityIndex]</c> 指向 dense 下标；dense 区由
    /// <c>_entities[i]</c> 与 <c>_components[i]</c> 平行存放，遍历时内存连续。
    /// 增删均为 O(1)，删除使用 swap-remove。
    /// </para>
    /// </summary>
    /// <typeparam name="T">组件类型；struct 走连续内存，class 存引用。</typeparam>
    public sealed class ComponentStore<T> : IComponentStore where T : notnull
    {
        private const int InitialCapacity = 16;

        private int[] _sparse;
        private Entity[] _entities;
        private T[] _components;
        private int _count;

        /// <summary>由 <see cref="World"/> 注入的实体存活校验；独立使用时为 null。</summary>
        internal Func<Entity, bool>? EntityValidator;

        /// <summary>组件新增后的回调；由 <see cref="World"/> 接线。</summary>
        internal Action<Entity>? ComponentAdded;

        /// <summary>组件移除后的回调；由 <see cref="World"/> 接线。</summary>
        internal Action<Entity>? ComponentRemoved;

        /// <summary>结构变化回调；由 <see cref="World"/> 接线。</summary>
        internal Action? StructureChanged;

        /// <summary>初始化一个空的组件存储。</summary>
        public ComponentStore()
        {
            _sparse = Array.Empty<int>();
            _entities = Array.Empty<Entity>();
            _components = Array.Empty<T>();
        }

        /// <inheritdoc />
        public Type ComponentType => typeof(T);

        /// <inheritdoc />
        public int Count => _count;

        /// <summary>获取 dense 区中的实体句柄视图（只读）。</summary>
        public ReadOnlySpan<Entity> Entities => _entities.AsSpan(0, _count);

        /// <summary>获取 dense 区中的组件数据视图（可写）。</summary>
        public Span<T> Components => _components.AsSpan(0, _count);

        /// <summary>为实体添加或覆盖组件。</summary>
        /// <param name="entity">目标实体。</param>
        /// <param name="component">组件数据。</param>
        /// <returns>组件是新建时返回 <c>true</c>；覆盖已有组件返回 <c>false</c>。</returns>
        /// <exception cref="ArgumentException">实体句柄为空或已失效。</exception>
        /// <exception cref="ArgumentNullException">组件数据为 null。</exception>
        public bool Add(Entity entity, in T component)
        {
            if (entity.IsNull)
                throw new ArgumentException("不能为无效实体添加组件。", nameof(entity));

            if (EntityValidator is not null && !EntityValidator(entity))
                throw new ArgumentException($"实体 {entity} 不存在或已被销毁。", nameof(entity));

            // 引用类型组件（如 Children）不允许为 null，否则后续读取会到处空引用
            if (component is null)
                throw new ArgumentNullException(nameof(component), "组件数据不能为 null。");

            EnsureSparseCapacity(entity.Index);

            int dense = _sparse[entity.Index];
            bool isNew = dense < 0;
            if (isNew)
            {
                if (_count == _components.Length)
                    GrowDense();

                dense = _count++;
                _sparse[entity.Index] = dense;
            }

            _entities[dense] = entity;
            _components[dense] = component;

            if (isNew)
            {
                ComponentAdded?.Invoke(entity);
                StructureChanged?.Invoke();
            }

            return isNew;
        }

        /// <summary>获取实体组件的引用；不存在时抛出异常。</summary>
        /// <param name="entity">目标实体。</param>
        /// <exception cref="KeyNotFoundException">实体没有该组件。</exception>
        public ref T Get(Entity entity)
        {
            if (!TryGetDense(entity.Index, out int dense))
                throw new KeyNotFoundException($"实体 {entity} 上不存在组件 {typeof(T).Name}。");

            return ref _components[dense];
        }

        /// <summary>尝试获取实体组件。</summary>
        /// <param name="entity">目标实体。</param>
        /// <param name="component">取出的组件；不存在时为 <c>default</c>。</param>
        /// <returns>是否成功取得组件。</returns>
        public bool TryGet(Entity entity, out T component)
        {
            if (TryGetDense(entity.Index, out int dense))
            {
                component = _components[dense];
                return true;
            }

            component = default!;
            return false;
        }

        /// <summary>按实体句柄移除组件。</summary>
        /// <param name="entity">目标实体。</param>
        /// <returns>若确实移除了组件返回 <c>true</c>。</returns>
        public bool Remove(Entity entity)
        {
            if (EntityValidator is not null && !EntityValidator(entity))
                return false;

            bool removed = RemoveDense(entity.Index);
            if (removed)
            {
                ComponentRemoved?.Invoke(entity);
                StructureChanged?.Invoke();
            }

            return removed;
        }

        /// <inheritdoc />
        public bool Remove(int entityIndex) => RemoveDense(entityIndex);

        /// <inheritdoc />
        public bool Contains(int entityIndex) => TryGetDense(entityIndex, out _);

        /// <summary>查询实体槽位在 dense 区中的下标。</summary>
        /// <param name="entityIndex">实体槽位索引。</param>
        /// <param name="denseIndex">dense 下标；未持有组件时为 -1。</param>
        /// <returns>是否持有组件。</returns>
        public bool TryGetDense(int entityIndex, out int denseIndex)
        {
            if ((uint)entityIndex < (uint)_sparse.Length)
            {
                denseIndex = _sparse[entityIndex];
                return denseIndex >= 0;
            }

            denseIndex = -1;
            return false;
        }

        /// <inheritdoc />
        public void Clear()
        {
            if (_count > 0)
            {
                Array.Clear(_components, 0, _count);
                Array.Clear(_entities, 0, _count);
            }

            Array.Fill(_sparse, -1);
            _count = 0;
        }

        private bool RemoveDense(int entityIndex)
        {
            if ((uint)entityIndex >= (uint)_sparse.Length)
                return false;

            int dense = _sparse[entityIndex];
            if (dense < 0)
                return false;

            int last = _count - 1;
            if (dense != last)
            {
                // swap-remove：把末尾元素搬到被删位置，并修正其 sparse 索引
                _components[dense] = _components[last];
                _entities[dense] = _entities[last];
                _sparse[_entities[dense].Index] = dense;
            }

            _components[last] = default!;
            _entities[last] = default;
            _sparse[entityIndex] = -1;
            _count--;
            return true;
        }

        private void GrowDense()
        {
            int size = _components.Length == 0 ? InitialCapacity : _components.Length * 2;
            Array.Resize(ref _components, size);
            Array.Resize(ref _entities, size);
        }

        private void EnsureSparseCapacity(int entityIndex)
        {
            if (entityIndex < _sparse.Length)
                return;

            int size = _sparse.Length == 0 ? InitialCapacity : _sparse.Length;
            while (size <= entityIndex)
                size *= 2;

            var sparse = new int[size];
            Array.Fill(sparse, -1);
            Array.Copy(_sparse, sparse, _sparse.Length);
            _sparse = sparse;
        }
    }
}
