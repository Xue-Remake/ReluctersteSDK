using System.Collections.Concurrent;

namespace ReluctersteSDK.RegistryKit
{
    /// <summary>
    /// 类型注册表：抽象到具体类型的映射，支持对单个映射条目启用/禁用（线程安全）。
    /// 条目不可变，更新通过 ConcurrentDictionary 原子替换完成。
    /// </summary>
    public class TypeRegistry
    {
        private sealed class MappingEntry
        {
            public readonly Type? ConcreteType;
            public readonly Func<object>? Factory;
            public readonly bool Enabled;

            public MappingEntry(Type? concreteType, Func<object>? factory, bool enabled)
            {
                ConcreteType = concreteType;
                Factory = factory;
                Enabled = enabled;
            }

            public MappingEntry WithEnabled(bool enabled) => new(ConcreteType, Factory, enabled);
        }

        private readonly ConcurrentDictionary<Type, MappingEntry> _mappings;

        public TypeRegistry()
        {
            _mappings = new ConcurrentDictionary<Type, MappingEntry>();
        }

        /// <summary>
        /// 注册类型映射。新映射默认启用；若已存在则替换具体类型并保持启用状态，
        /// 同时清除旧工厂（否则工厂优先，新注册的具体类型会被静默忽略）。
        /// </summary>
        public void Register<TAbstract, TConcrete>() where TAbstract : notnull where TConcrete : class, TAbstract
        {
            Type abstractType = typeof(TAbstract);
            _mappings.AddOrUpdate(
                abstractType,
                _ => new MappingEntry(typeof(TConcrete), null, true),
                (_, existing) => new MappingEntry(typeof(TConcrete), null, existing.Enabled));
        }

        /// <summary>
        /// 注册带工厂的类型映射。新映射默认启用；若已存在则替换工厂并保持启用状态，
        /// 同时清除旧具体类型。
        /// </summary>
        public void Register<TAbstract>(Func<TAbstract> factory) where TAbstract : notnull
        {
            if (factory == null) throw new ArgumentNullException(nameof(factory));
            Type abstractType = typeof(TAbstract);
            _mappings.AddOrUpdate(
                abstractType,
                _ => new MappingEntry(null, () => factory(), true),
                (_, existing) => new MappingEntry(null, () => factory(), existing.Enabled));
        }

        /// <summary>启用指定抽象类型的映射</summary>
        public bool Enable<TAbstract>() where TAbstract : notnull => SetEnabled(typeof(TAbstract), true);

        /// <summary>禁用指定抽象类型的映射</summary>
        public bool Disable<TAbstract>() where TAbstract : notnull => SetEnabled(typeof(TAbstract), false);

        private bool SetEnabled(Type type, bool enabled)
        {
            while (_mappings.TryGetValue(type, out var entry))
            {
                if (entry.Enabled == enabled) return true;
                if (_mappings.TryUpdate(type, entry.WithEnabled(enabled), entry))
                    return true;
            }
            return false;
        }

        /// <summary>创建实例（仅针对启用映射；每次调用都创建新实例）</summary>
        public TAbstract CreateInstance<TAbstract>() where TAbstract : notnull
        {
            Type abstractType = typeof(TAbstract);
            if (!_mappings.TryGetValue(abstractType, out var entry) || !entry.Enabled)
                throw new InvalidOperationException($"类型 {abstractType.FullName} 未启用或未注册。");

            if (entry.Factory != null)
                return (TAbstract)entry.Factory();

            if (entry.ConcreteType != null)
                return (TAbstract)Activator.CreateInstance(entry.ConcreteType)!;

            throw new InvalidOperationException($"类型 {abstractType.FullName} 无效映射。");
        }

        /// <summary>尝试创建实例</summary>
        public bool TryCreateInstance<TAbstract>(out TAbstract instance) where TAbstract : notnull
        {
            instance = default!;
            Type abstractType = typeof(TAbstract);
            if (!_mappings.TryGetValue(abstractType, out var entry) || !entry.Enabled)
                return false;

            try
            {
                if (entry.Factory != null)
                {
                    instance = (TAbstract)entry.Factory();
                    return true;
                }

                if (entry.ConcreteType != null)
                {
                    instance = (TAbstract)Activator.CreateInstance(entry.ConcreteType)!;
                    return true;
                }
            }
            catch
            {
                // 构造失败
            }

            return false;
        }

        /// <summary>获取所有已启用抽象类型的集合</summary>
        public ICollection<Type> EnabledAbstractTypes()
        {
            return _mappings.Where(kvp => kvp.Value.Enabled)
                            .Select(kvp => kvp.Key)
                            .ToList();
        }

        /// <summary>移除映射（无论状态）</summary>
        public bool Remove<TAbstract>() where TAbstract : notnull
        {
            return _mappings.TryRemove(typeof(TAbstract), out _);
        }

        /// <summary>清空所有映射</summary>
        public void Clear() => _mappings.Clear();

        /// <summary>检查抽象类型是否物理存在</summary>
        public bool Exists<TAbstract>() where TAbstract : notnull
        {
            return _mappings.ContainsKey(typeof(TAbstract));
        }

        /// <summary>检查抽象类型映射是否已启用</summary>
        public bool IsEnabled<TAbstract>() where TAbstract : notnull
        {
            return _mappings.TryGetValue(typeof(TAbstract), out var entry) && entry.Enabled;
        }
    }
}
