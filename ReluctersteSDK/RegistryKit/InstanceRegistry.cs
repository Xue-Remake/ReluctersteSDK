using System.Collections.Concurrent;

namespace ReluctersteSDK.RegistryKit
{
    /// <summary>
    /// 实例注册表：按类型单例/工厂注册，支持对单个类型条目启用/禁用（线程安全）。
    /// </summary>
    public class InstanceRegistry
    {
        private class TypeEntry
        {
            public object Instance;
            public Func<object> Factory;
            public volatile bool Enabled;

            public TypeEntry(object instance, bool enabled = true)
            {
                Instance = instance;
                Enabled = enabled;
            }

            public TypeEntry(Func<object> factory, bool enabled = true)
            {
                Factory = factory;
                Enabled = enabled;
            }
        }

        private readonly ConcurrentDictionary<Type, TypeEntry> _entries;

        public InstanceRegistry()
        {
            _entries = new ConcurrentDictionary<Type, TypeEntry>();
        }

        /// <summary>注册已创建的实例。新类型默认启用；若已存在则覆盖实例并保持启用状态。</summary>
        public void RegisterInstance<T>(T instance) where T : class
        {
            if (instance == null) throw new ArgumentNullException(nameof(instance));
            Type type = typeof(T);

            _entries.AddOrUpdate(
                type,
                _ => new TypeEntry(instance, true),
                (_, existing) =>
                {
                    existing.Instance = instance;
                    return existing;
                });
        }

        /// <summary>注册工厂。新类型默认启用；若已存在则覆盖工厂并保持启用状态。</summary>
        public void RegisterFactory<T>(Func<T> factory) where T : class
        {
            if (factory == null) throw new ArgumentNullException(nameof(factory));
            Type type = typeof(T);

            _entries.AddOrUpdate(
                type,
                _ => new TypeEntry(() => factory(), true),
                (_, existing) =>
                {
                    existing.Factory = () => factory();
                    return existing;
                });
        }

        /// <summary>启用指定类型的解析</summary>
        public bool Enable<T>() where T : class
        {
            return SetEnabled(typeof(T), true);
        }

        /// <summary>禁用指定类型的解析（已有的缓存实例不会被清除，但 Resolve 会失败）</summary>
        public bool Disable<T>() where T : class
        {
            return SetEnabled(typeof(T), false);
        }

        private bool SetEnabled(Type type, bool enabled)
        {
            if (_entries.TryGetValue(type, out var entry))
            {
                entry.Enabled = enabled;
                return true;
            }
            return false;
        }

        /// <summary>解析实例（仅返回启用类型）</summary>
        public T Resolve<T>() where T : class
        {
            Type type = typeof(T);
            if (!_entries.TryGetValue(type, out var entry) || !entry.Enabled)
                throw new InvalidOperationException($"类型 {type.FullName} 未启用或未注册。");

            if (entry.Instance != null)
                return (T)entry.Instance;

            if (entry.Factory != null)
            {
                lock (entry)
                {
                    if (entry.Instance != null)
                        return (T)entry.Instance;

                    // 再次检查启用状态，防止在等待锁期间被禁用
                    if (!entry.Enabled)
                        throw new InvalidOperationException($"类型 {type.FullName} 在解析过程中被禁用。");

                    T newInstance = (T)entry.Factory();
                    entry.Instance = newInstance;
                    return newInstance;
                }
            }

            throw new InvalidOperationException($"类型 {type.FullName} 缺少实例或工厂。");
        }

        /// <summary>尝试解析</summary>
        public bool TryResolve<T>(out T instance) where T : class
        {
            instance = default;
            Type type = typeof(T);
            if (!_entries.TryGetValue(type, out var entry) || !entry.Enabled)
                return false;

            if (entry.Instance != null)
            {
                instance = (T)entry.Instance;
                return true;
            }

            if (entry.Factory != null)
            {
                lock (entry)
                {
                    if (entry.Instance != null)
                    {
                        instance = (T)entry.Instance;
                        return true;
                    }

                    if (!entry.Enabled)
                        return false;

                    T newInstance = (T)entry.Factory();
                    entry.Instance = newInstance;
                    instance = newInstance;
                    return true;
                }
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
        public bool Remove<T>() where T : class
        {
            return _entries.TryRemove(typeof(T), out _);
        }

        /// <summary>清空所有注册</summary>
        public void Clear() => _entries.Clear();

        /// <summary>检查类型是否物理存在</summary>
        public bool Exists<T>() where T : class
        {
            return _entries.ContainsKey(typeof(T));
        }

        /// <summary>检查类型是否已启用</summary>
        public bool IsEnabled<T>() where T : class
        {
            return _entries.TryGetValue(typeof(T), out var entry) && entry.Enabled;
        }
    }
}
