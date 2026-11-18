using ReluctersteSDK.Config.Interfaces;
using ReluctersteSDK.Config.Stores;

namespace ReluctersteSDK.Config.Hosting
{
    /// <summary>
    /// 配置节注册表：各模块把自己的配置类型与存储注册进来，
    /// 上层按类型取用，不需要集中枚举全部配置。
    /// </summary>
    /// <remarks>
    /// 注册阶段不是线程安全的；读写在锁内完成，注册完成后可并发读取。
    /// 通过本注册表拿到的存储会在保存成功时触发 <see cref="Changed"/>。
    /// 若绕过注册表直接使用原始存储，则不会触发事件。
    /// </remarks>
    public sealed class ConfigRegistry
    {
        private readonly object _sync = new();
        private readonly Dictionary<Type, object> _stores = new();
        private readonly Dictionary<string, Type> _sections = new(StringComparer.Ordinal);

        /// <summary>
        /// 某个配置节保存成功后触发。
        /// </summary>
        public event EventHandler<ConfigChangedEventArgs>? Changed;

        /// <summary>
        /// 已注册的配置节名。
        /// </summary>
        public IReadOnlyCollection<string> SectionNames
        {
            get
            {
                lock (_sync)
                {
                    return _sections.Keys.ToArray();
                }
            }
        }

        /// <summary>
        /// 注册一个配置节。
        /// </summary>
        /// <typeparam name="T">配置类型。</typeparam>
        /// <param name="store">该配置的存储。</param>
        /// <param name="sectionName">配置节名；为 null 时使用类型名。</param>
        /// <exception cref="InvalidOperationException">类型或节名已注册时抛出。</exception>
        public void Register<T>(IConfigStore<T> store, string? sectionName = null)
            where T : class, IConfig, new()
        {
            ArgumentNullException.ThrowIfNull(store);

            string name = string.IsNullOrWhiteSpace(sectionName) ? typeof(T).Name : sectionName;

            lock (_sync)
            {
                if (_stores.ContainsKey(typeof(T)))
                    throw new InvalidOperationException($"配置类型 {typeof(T).FullName} 已注册。");
                if (_sections.ContainsKey(name))
                    throw new InvalidOperationException($"配置节名“{name}”已被占用。");

                _stores[typeof(T)] = new NotifyingConfigStore<T>(store, name, RaiseChanged);
                _sections[name] = typeof(T);
            }
        }

        /// <summary>
        /// 尝试取出已注册的配置存储。
        /// </summary>
        /// <typeparam name="T">配置类型。</typeparam>
        /// <param name="store">取出的存储。</param>
        /// <returns>已注册返回 true。</returns>
        public bool TryGetStore<T>(out IConfigStore<T> store) where T : class, IConfig, new()
        {
            lock (_sync)
            {
                if (_stores.TryGetValue(typeof(T), out object? value))
                {
                    store = (IConfigStore<T>)value;
                    return true;
                }
            }

            store = null!;
            return false;
        }

        /// <summary>
        /// 取出已注册的配置存储。
        /// </summary>
        /// <typeparam name="T">配置类型。</typeparam>
        /// <exception cref="InvalidOperationException">未注册时抛出。</exception>
        public IConfigStore<T> GetStore<T>() where T : class, IConfig, new()
        {
            return TryGetStore<T>(out IConfigStore<T> store)
                ? store
                : throw new InvalidOperationException($"配置类型 {typeof(T).FullName} 尚未注册。");
        }

        /// <summary>
        /// 按配置节名反查配置类型。
        /// </summary>
        /// <param name="sectionName">配置节名。</param>
        /// <param name="configType">对应的配置类型。</param>
        public bool TryGetConfigType(string sectionName, out Type configType)
        {
            ArgumentNullException.ThrowIfNull(sectionName);

            lock (_sync)
            {
                if (_sections.TryGetValue(sectionName, out Type? type))
                {
                    configType = type;
                    return true;
                }
            }

            configType = typeof(object);
            return false;
        }

        private void RaiseChanged(ConfigChangedEventArgs args) => Changed?.Invoke(this, args);
    }

    /// <summary>
    /// 在保存成功后转发变更事件的存储装饰器。
    /// </summary>
    internal sealed class NotifyingConfigStore<T> : IConfigStore<T> where T : class, IConfig, new()
    {
        private readonly IConfigStore<T> _inner;
        private readonly string _section;
        private readonly Action<ConfigChangedEventArgs> _notify;

        internal NotifyingConfigStore(IConfigStore<T> inner, string section, Action<ConfigChangedEventArgs> notify)
        {
            _inner = inner;
            _section = section;
            _notify = notify;
        }

        public Task<T?> GetAsync(IReadOnlyList<object?> keys, CancellationToken cancellationToken = default)
            => _inner.GetAsync(keys, cancellationToken);

        public Task<IReadOnlyList<T>> QueryAsync(
            ConfigQuery<T>? query = null,
            CancellationToken cancellationToken = default)
            => _inner.QueryAsync(query, cancellationToken);

        public async Task<ConfigSaveResult> SaveAsync(
            IReadOnlyList<T> items,
            SaveMode mode,
            CancellationToken cancellationToken = default)
        {
            ConfigSaveResult result = await _inner.SaveAsync(items, mode, cancellationToken).ConfigureAwait(false);

            if (result.Success && result.Written > 0)
                _notify(new ConfigChangedEventArgs(_section, typeof(T), result.Written, mode));

            return result;
        }
    }
}
