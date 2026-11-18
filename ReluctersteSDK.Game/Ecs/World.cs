using System;
using System.Collections.Generic;
using ReluctersteSDK.Game.Ecs.Events;

namespace ReluctersteSDK.Game.Ecs
{
    /// <summary>
    /// ECS 世界：持有实体、组件存储、系统列表与命令缓冲。
    /// <para>所有结构性修改（创建/销毁实体、增删组件）都会让 <see cref="StructuralVersion"/> 自增，
    /// 遍历期间若发生结构性修改会立即抛出异常。</para>
    /// </summary>
    public sealed partial class World : IDisposable
    {
        private readonly Dictionary<Type, IComponentStore> _stores = new Dictionary<Type, IComponentStore>();
        private readonly Dictionary<Type, object> _singletons = new Dictionary<Type, object>();
        private readonly CommandBuffer _commands = new CommandBuffer();

        private int _structuralVersion;
        private int _hierarchyVersion;
        private bool _disposed;

        /// <summary>获取实体分配器。</summary>
        public EntityManager Entities { get; } = new EntityManager();

        /// <summary>获取当前存活实体数量。</summary>
        public int EntityCount => Entities.AliveCount;

        /// <summary>获取结构性修改版本号（创建/销毁实体、增删组件时自增）。</summary>
        public int StructuralVersion => _structuralVersion;

        /// <summary>获取层级修改版本号（父子关系变化时自增）。</summary>
        public int HierarchyVersion => _hierarchyVersion;

        /// <summary>获取延迟命令缓冲。</summary>
        public CommandBuffer Commands => _commands;

        /// <summary>
        /// 获取双缓冲事件容器。
        /// <para>系统在帧内登记事件，下一帧统一读取；<see cref="Update"/> 在开始处自动交换缓冲。</para>
        /// </summary>
        public WorldEvents Events { get; } = new WorldEvents();

        /// <summary>获取世界是否已释放。</summary>
        public bool IsDisposed => _disposed;

        /// <summary>实体创建后触发。</summary>
        public event Action<Entity>? EntityCreated;

        /// <summary>实体销毁后触发。</summary>
        public event Action<Entity>? EntityDestroyed;

        /// <summary>组件新增后触发（覆盖不触发）。</summary>
        public event Action<ComponentEventArgs>? ComponentAdded;

        /// <summary>组件移除后触发。</summary>
        public event Action<ComponentEventArgs>? ComponentRemoved;

        /// <summary>世界被 <see cref="Clear"/> 清空后触发。</summary>
        public event Action? WorldCleared;

        #region 实体

        /// <summary>创建一个新实体。</summary>
        public Entity Create()
        {
            EnsureNotDisposed();
            Entity entity = Entities.Create();
            _structuralVersion++;
            EntityCreated?.Invoke(entity);
            return entity;
        }

        /// <summary>
        /// 销毁实体，并级联清理它在所有组件存储中的数据。
        /// <para>层级一致性：自身从父节点的子列表中摘除；其子节点**保留**并升级为根（不级联销毁）。</para>
        /// </summary>
        /// <param name="entity">目标实体。</param>
        public void Destroy(Entity entity)
        {
            EnsureNotDisposed();
            if (!Entities.IsAlive(entity))
                return;

            // 1. 从父节点的 Children 中摘除自己
            if (Has<Parent>(entity) && TryGet<Parent>(entity, out Parent parent)
                && Entities.IsAlive(parent.Value)
                && TryGet<Children>(parent.Value, out Children siblings) && siblings is not null)
            {
                siblings.RemoveInternal(entity);
            }

            // 2. 子节点升级为根
            if (Has<Children>(entity) && TryGet<Children>(entity, out Children children) && children is not null)
            {
                foreach (Entity child in children.Items)
                {
                    if (Entities.IsAlive(child))
                        Remove<Parent>(child);
                }

                children.ClearInternal();
            }

            // 3. 逐存储清理并触发移除事件（先取快照，允许事件处理器安全地新增存储）
            IComponentStore[] stores = new IComponentStore[_stores.Count];
            _stores.Values.CopyTo(stores, 0);

            foreach (IComponentStore store in stores)
            {
                if (!store.Contains(entity.Index))
                    continue;

                ComponentRemoved?.Invoke(new ComponentEventArgs(entity, store.ComponentType));
                store.Remove(entity.Index);
            }

            Entities.Destroy(entity);
            _structuralVersion++;
            _hierarchyVersion++;
            EntityDestroyed?.Invoke(entity);
        }

        /// <summary>判断实体是否仍然存活。</summary>
        /// <param name="entity">目标实体。</param>
        public bool IsAlive(Entity entity) => Entities.IsAlive(entity);

        #endregion

        #region 组件

        /// <summary>获取（必要时创建）指定组件类型的存储。</summary>
        /// <typeparam name="T">组件类型。</typeparam>
        public ComponentStore<T> Store<T>() where T : notnull
        {
            EnsureNotDisposed();
            Type type = typeof(T);
            if (_stores.TryGetValue(type, out IComponentStore? existing))
                return (ComponentStore<T>)existing;

            var store = new ComponentStore<T>
            {
                EntityValidator = Entities.IsAlive,
                ComponentAdded = entity => ComponentAdded?.Invoke(new ComponentEventArgs(entity, typeof(T))),
                ComponentRemoved = entity => ComponentRemoved?.Invoke(new ComponentEventArgs(entity, typeof(T))),
                StructureChanged = () => _structuralVersion++
            };

            _stores.Add(type, store);
            return store;
        }

        /// <summary>为实体添加或覆盖组件。</summary>
        /// <typeparam name="T">组件类型。</typeparam>
        /// <param name="entity">目标实体。</param>
        /// <param name="component">组件数据。</param>
        /// <exception cref="ArgumentException">实体不存在或已销毁。</exception>
        public void Add<T>(Entity entity, in T component) where T : notnull
        {
            EnsureNotDisposed();
            Store<T>().Add(entity, component);
        }

        /// <summary>获取实体组件的引用；不存在时抛出异常。</summary>
        /// <typeparam name="T">组件类型。</typeparam>
        /// <param name="entity">目标实体。</param>
        /// <exception cref="ArgumentException">实体不存在或已销毁。</exception>
        /// <exception cref="KeyNotFoundException">实体没有该组件。</exception>
        public ref T Get<T>(Entity entity) where T : notnull
        {
            EnsureNotDisposed();
            EnsureAlive(entity);
            return ref Store<T>().Get(entity);
        }

        /// <summary>尝试获取实体组件。</summary>
        /// <typeparam name="T">组件类型。</typeparam>
        /// <param name="entity">目标实体。</param>
        /// <param name="component">取出的组件。</param>
        public bool TryGet<T>(Entity entity, out T component) where T : notnull
        {
            EnsureNotDisposed();
            if (!Entities.IsAlive(entity))
            {
                component = default!;
                return false;
            }

            return Store<T>().TryGet(entity, out component);
        }

        /// <summary>判断实体是否持有指定组件。</summary>
        /// <typeparam name="T">组件类型。</typeparam>
        /// <param name="entity">目标实体。</param>
        public bool Has<T>(Entity entity) where T : notnull
        {
            if (_disposed || !Entities.IsAlive(entity))
                return false;

            return _stores.TryGetValue(typeof(T), out IComponentStore? store) && store.Contains(entity.Index);
        }

        /// <summary>移除实体上的指定组件。</summary>
        /// <typeparam name="T">组件类型。</typeparam>
        /// <param name="entity">目标实体。</param>
        /// <returns>若确实移除了组件返回 <c>true</c>。</returns>
        public bool Remove<T>(Entity entity) where T : notnull
        {
            EnsureNotDisposed();
            if (!Entities.IsAlive(entity))
                return false;

            return _stores.TryGetValue(typeof(T), out IComponentStore? store)
                && ((ComponentStore<T>)store).Remove(entity);
        }

        #endregion

        #region 查询

        /// <summary>创建单组件查询。</summary>
        public Query<T1> Query<T1>() where T1 : notnull => new Query<T1>(this, Store<T1>());

        /// <summary>创建双组件查询（取交集）。</summary>
        public Query<T1, T2> Query<T1, T2>() where T1 : notnull where T2 : notnull
            => new Query<T1, T2>(this, Store<T1>(), Store<T2>());

        /// <summary>创建三组件查询（取交集）。</summary>
        public Query<T1, T2, T3> Query<T1, T2, T3>()
            where T1 : notnull where T2 : notnull where T3 : notnull
            => new Query<T1, T2, T3>(this, Store<T1>(), Store<T2>(), Store<T3>());

        /// <summary>创建四组件查询（取交集）。</summary>
        public Query<T1, T2, T3, T4> Query<T1, T2, T3, T4>()
            where T1 : notnull where T2 : notnull where T3 : notnull where T4 : notnull
            => new Query<T1, T2, T3, T4>(this, Store<T1>(), Store<T2>(), Store<T3>(), Store<T4>());

        #endregion

        #region 单例组件

        /// <summary>设置一个单例组件；覆盖已有值不会自增结构版本。</summary>
        /// <typeparam name="T">单例类型。</typeparam>
        /// <param name="value">值。</param>
        public void SetSingleton<T>(in T value) where T : notnull
        {
            EnsureNotDisposed();
            Type type = typeof(T);
            if (_singletons.TryGetValue(type, out object? box))
            {
                ((SingletonBox<T>)box).Value = value;
                return;
            }

            _singletons.Add(type, new SingletonBox<T> { Value = value });
            _structuralVersion++;
        }

        /// <summary>获取单例组件的引用；不存在时抛出异常。</summary>
        /// <typeparam name="T">单例类型。</typeparam>
        public ref T GetSingleton<T>() where T : notnull
        {
            EnsureNotDisposed();
            if (_singletons.TryGetValue(typeof(T), out object? box))
                return ref ((SingletonBox<T>)box).Value;

            throw new KeyNotFoundException($"世界中没有单例组件 {typeof(T).Name}。");
        }

        /// <summary>尝试获取单例组件。</summary>
        public bool TryGetSingleton<T>(out T value) where T : notnull
        {
            if (!_disposed && _singletons.TryGetValue(typeof(T), out object? box))
            {
                value = ((SingletonBox<T>)box).Value;
                return true;
            }

            value = default!;
            return false;
        }

        /// <summary>判断是否存在指定类型的单例组件。</summary>
        public bool HasSingleton<T>() where T : notnull => !_disposed && _singletons.ContainsKey(typeof(T));

        /// <summary>移除指定类型的单例组件。</summary>
        public bool RemoveSingleton<T>() where T : notnull
        {
            EnsureNotDisposed();
            if (!_singletons.Remove(typeof(T)))
                return false;

            _structuralVersion++;
            return true;
        }

        #endregion

        /// <summary>清空整个世界（实体、组件、系统、命令与单例）。</summary>
        public void Clear()
        {
            EnsureNotDisposed();
            ClearCore();
            WorldCleared?.Invoke();
        }

        /// <summary>释放世界；释放后除 <see cref="Dispose"/> 与 <see cref="IsDisposed"/> 外的公开操作会抛异常。</summary>
        public void Dispose()
        {
            if (_disposed)
                return;

            Shutdown();
            ClearCore();

            EntityCreated = null;
            EntityDestroyed = null;
            ComponentAdded = null;
            ComponentRemoved = null;
            WorldCleared = null;

            _disposed = true;
        }

        internal void BumpHierarchyVersion() => _hierarchyVersion++;

        internal Entity RestoreEntity(int index, uint generation)
        {
            EnsureNotDisposed();
            Entity entity = Entities.Restore(index, generation);
            _structuralVersion++;
            EntityCreated?.Invoke(entity);
            return entity;
        }

        internal void ClearForRestore()
        {
            _stores.Clear();
            _singletons.Clear();
            _commands.Clear();
            Entities.Clear();
            _structuralVersion++;
            _hierarchyVersion++;
        }

        private void ClearCore()
        {
            _stores.Clear();
            _singletons.Clear();
            _commands.Clear();
            Events.Clear();
            ClearSystems();
            Entities.Clear();
            _structuralVersion++;
            _hierarchyVersion++;
        }

        private void EnsureNotDisposed()
        {
            if (_disposed)
                throw new ObjectDisposedException(nameof(World));
        }

        private void EnsureAlive(Entity entity)
        {
            if (!Entities.IsAlive(entity))
                throw new ArgumentException($"实体 {entity} 不存在或已被销毁。", nameof(entity));
        }

        private sealed class SingletonBox<T> where T : notnull
        {
            public T Value = default!;
        }
    }
}
