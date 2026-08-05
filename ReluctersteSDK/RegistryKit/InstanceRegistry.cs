using System.Collections.Concurrent;

namespace ReluctersteSDK.RegistryKit
{
    /// <summary>
    /// 实例注册表：按类型单例/工厂注册，支持对单个类型条目启用/禁用（线程安全）。
    /// 条目采用不可变类设计：任何更新都会整体替换条目，通过 ConcurrentDictionary
    /// 的原子操作发布，读路径无需加锁，也不存在字段可见性问题。
    /// </summary>
    public class InstanceRegistry
    {
        private sealed class TypeEntry
        {
            public readonly object? Instance;
            public readonly Lazy<object>? LazyFactory;
            public readonly bool Enabled;

            public TypeEntry(object? instance, Lazy<object>? lazyFactory, bool enabled)
            {
                Instance = instance;
                LazyFactory = lazyFactory;
                Enabled = enabled;
            }

            public TypeEntry WithEnabled(bool enabled) => new(Instance, LazyFactory, enabled);
        }

        private readonly ConcurrentDictionary<Type, TypeEntry> _entries;

        public InstanceRegistry()
        {
            _entries = new ConcurrentDictionary<Type, TypeEntry>();
        }

        /// <summary>
        /// 注册已创建的实例。新类型默认启用；若已存在则覆盖实例并保持启用状态，
        /// 同时清除旧工厂（否则新实例会被旧工厂路径静默忽略）。
        /// </summary>
        public void RegisterInstance<T>(T instance) where T : notnull
        {
            if (instance == null) throw new ArgumentNullException(nameof(instance));
            Type type = typeof(T);

            _entries.AddOrUpdate(
                type,
                _ => new TypeEntry(instance, null, true),
                (_, existing) => new TypeEntry(instance, null, existing.Enabled));
        }

        /// <summary>
        /// 注册工厂（懒加载单例：首次解析时创建并缓存）。
        /// 新类型默认启用；若已存在则覆盖工厂并保持启用状态，
        /// 同时清除旧实例（否则新工厂永远不会被调用）。
        /// </summary>
        public void RegisterFactory<T>(Func<T> factory) where T : notnull
        {
            if (factory == null) throw new ArgumentNullException(nameof(factory));
            Type type = typeof(T);

            // PublicationOnly：并发解析时工厂只会被采用一次成功结果；
            // 工厂抛出的异常不会被缓存，下次解析可重试。
            var lazyFactory = new Lazy<object>(() => factory(), LazyThreadSafetyMode.PublicationOnly);

            _entries.AddOrUpdate(
                type,
                _ => new TypeEntry(null, lazyFactory, true),
                (_, existing) => new TypeEntry(null, lazyFactory, existing.Enabled));
        }

        /// <summary>启用指定类型的解析</summary>
        public bool Enable<T>() where T : notnull => SetEnabled(typeof(T), true);

        /// <summary>禁用指定类型的解析（已有的缓存实例不会被清除，但 Resolve 会失败）</summary>
        public bool Disable<T>() where T : notnull => SetEnabled(typeof(T), false);

        private bool SetEnabled(Type type, bool enabled)
        {
            // CAS 循环：原子替换条目，避免"启用操作把已被移除的条目复活"
            while (_entries.TryGetValue(type, out var entry))
            {
                if (entry.Enabled == enabled) return true;
                if (_entries.TryUpdate(type, entry.WithEnabled(enabled), entry))
                    return true;
            }
            return false;
        }

        /// <summary>解析实例（仅返回启用类型）</summary>
        public T Resolve<T>() where T : notnull
        {
            Type type = typeof(T);
            if (!_entries.TryGetValue(type, out var entry) || !entry.Enabled)
                throw new InvalidOperationException($"类型 {type.FullName} 未启用或未注册。");

            if (entry.Instance is T instance)
                return instance;

            if (entry.LazyFactory != null)
                return (T)entry.LazyFactory.Value;

            throw new InvalidOperationException($"类型 {type.FullName} 缺少实例或工厂。");
        }

        /// <summary>尝试解析</summary>
        public bool TryResolve<T>(out T instance) where T : notnull
        {
            instance = default!;
            Type type = typeof(T);
            if (!_entries.TryGetValue(type, out var entry) || !entry.Enabled)
                return false;

            if (entry.Instance is T cached)
            {
                instance = cached;
                return true;
            }

            if (entry.LazyFactory != null)
            {
                instance = (T)entry.LazyFactory.Value;
                return true;
            }

            return false;
        }

        /// <summary>获取所有已启用类型的集合</summary>
        public ICollection<Type> EnabledTypes()
        {
            return _entries.Where(kvp => kvp.Value.Enabled)
                           .Select(kvp => kvp.Key)
                           .ToList();
        }

        /// <summary>移除某个类型的注册（无论状态）</summary>
        public bool Remove<T>() where T : notnull
        {
            return _entries.TryRemove(typeof(T), out _);
        }

        /// <summary>清空所有注册</summary>
        public void Clear() => _entries.Clear();

        /// <summary>检查类型是否物理存在</summary>
        public bool Exists<T>() where T : notnull
        {
            return _entries.ContainsKey(typeof(T));
        }

        /// <summary>检查类型是否已启用</summary>
        public bool IsEnabled<T>() where T : notnull
        {
            return _entries.TryGetValue(typeof(T), out var entry) && entry.Enabled;
        }
    }
}
