using ReluctersteSDK.Config.Interfaces;

namespace ReluctersteSDK.Config.Stores
{
    /// <summary>
    /// 内存配置存储。主要用于单元测试，以及"先跑起来、后接持久化"的场景。
    /// </summary>
    /// <typeparam name="T">配置类型。</typeparam>
    public sealed class InMemoryConfigStore<T> : IConfigStore<T> where T : class, IConfig, new()
    {
        private readonly Func<T, object?> _keySelector;
        private readonly Dictionary<string, T> _items = new(StringComparer.Ordinal);
        private readonly object _sync = new();

        /// <summary>
        /// 创建内存存储。
        /// </summary>
        /// <param name="keySelector">从配置实例取出主键；复合主键可返回元组或数组。</param>
        public InMemoryConfigStore(Func<T, object?> keySelector)
        {
            _keySelector = keySelector ?? throw new ArgumentNullException(nameof(keySelector));
        }

        /// <inheritdoc />
        public Task<T?> GetAsync(IReadOnlyList<object?> keys, CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(keys);
            cancellationToken.ThrowIfCancellationRequested();

            lock (_sync)
            {
                return Task.FromResult(_items.TryGetValue(ConfigKey.From(keys), out T? value) ? value : null);
            }
        }

        /// <inheritdoc />
        public Task<IReadOnlyList<T>> QueryAsync(
            ConfigQuery<T>? query = null,
            CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();

            List<T> result;
            lock (_sync)
            {
                IEnumerable<T> source = _items.Values;
                if (query?.Predicate is not null)
                    source = source.Where(query.Predicate);
                if (query?.Limit is int limit)
                    source = source.Take(limit);
                result = source.ToList();
            }
            return Task.FromResult<IReadOnlyList<T>>(result);
        }

        /// <inheritdoc />
        public Task<ConfigSaveResult> SaveAsync(
            IReadOnlyList<T> items,
            SaveMode mode,
            CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(items);
            cancellationToken.ThrowIfCancellationRequested();

            if (items.Count == 0)
                return Task.FromResult(ConfigSaveResult.Ok(0));

            lock (_sync)
            {
                // 先整体校验，避免 Insert 模式中途失败留下部分写入。
                if (mode == SaveMode.Insert)
                {
                    foreach (T item in items)
                    {
                        string key = ConfigKey.From(_keySelector(item));
                        if (_items.ContainsKey(key))
                        {
                            return Task.FromResult(ConfigSaveResult.Fail(
                                new InvalidOperationException($"配置项已存在：{key}")));
                        }
                    }
                }

                foreach (T item in items)
                {
                    _items[ConfigKey.From(_keySelector(item))] = item;
                }
            }

            return Task.FromResult(ConfigSaveResult.Ok(items.Count));
        }
    }
}
