using System.Collections.Concurrent;

namespace ReluctersteSDK.RegistryKit
{
    /// <summary>
    /// 通用键值对注册表，支持对单个条目进行启用/禁用（线程安全）。
    /// 条目不可变，更新通过 ConcurrentDictionary 原子替换完成。
    /// </summary>
    public class ValueRegistry<TKey, TValue>
    {
        private sealed class Entry
        {
            public readonly TValue Value;
            public readonly bool Enabled;

            public Entry(TValue value, bool enabled)
            {
                Value = value;
                Enabled = enabled;
            }

            public Entry WithEnabled(bool enabled) => new(Value, enabled);
        }

        private readonly ConcurrentDictionary<TKey, Entry> _store;

        public ValueRegistry()
        {
            _store = new ConcurrentDictionary<TKey, Entry>();
        }

        /// <summary>
        /// 索引器：获取或注册/覆盖值（获取未找到或被禁用时返回 default）
        /// </summary>
        public TValue? this[TKey key]
        {
            get => GetValue(key);
            set => Register(key, value!);
        }

        /// <summary>
        /// 注册或覆盖一个值。若键已存在，仅更新值并保持原有启用状态；新键默认启用。
        /// </summary>
        public void Register(TKey key, TValue value)
        {
            if (key == null) throw new ArgumentNullException(nameof(key));

            _store.AddOrUpdate(
                key,
                _ => new Entry(value, true),
                (_, existing) => new Entry(value, existing.Enabled));
        }

        /// <summary>启用指定键的条目</summary>
        public bool Enable(TKey key)
        {
            if (key == null) throw new ArgumentNullException(nameof(key));
            return SetEnabled(key, true);
        }

        /// <summary>禁用指定键的条目</summary>
        public bool Disable(TKey key)
        {
            if (key == null) throw new ArgumentNullException(nameof(key));
            return SetEnabled(key, false);
        }

        private bool SetEnabled(TKey key, bool enabled)
        {
            while (_store.TryGetValue(key, out var entry))
            {
                if (entry.Enabled == enabled) return true;
                if (_store.TryUpdate(key, entry.WithEnabled(enabled), entry))
                    return true;
            }
            return false;
        }

        /// <summary>
        /// 根据键获取对应的值（忽略禁用条目）。
        /// 若不存在或被禁用，则返回 default。
        /// </summary>
        /// <param name="key">要查询的键</param>
        /// <returns>启用的值，若未找到或已禁用则返回 default</returns>
        public TValue? GetValue(TKey key)
        {
            if (key == null) throw new ArgumentNullException(nameof(key));
            return _store.TryGetValue(key, out var entry) && entry.Enabled ? entry.Value : default;
        }

        /// <summary>
        /// 根据谓词条件获取第一个匹配的已启用的值。
        /// 若未找到匹配项，则返回 default。
        /// </summary>
        /// <param name="predicate">筛选条件</param>
        /// <returns>匹配的值，未找到则返回 default</returns>
        public TValue? GetValue(Func<TValue, bool> predicate)
        {
            if (predicate == null) throw new ArgumentNullException(nameof(predicate));

            foreach (var kvp in _store)
            {
                if (kvp.Value.Enabled && predicate(kvp.Value.Value))
                {
                    return kvp.Value.Value;
                }
            }
            return default;
        }

        /// <summary>尝试获取值（忽略禁用条目）</summary>
        public bool TryGetValue(TKey key, out TValue value)
        {
            if (key == null) throw new ArgumentNullException(nameof(key));
            if (_store.TryGetValue(key, out var entry) && entry.Enabled)
            {
                value = entry.Value;
                return true;
            }
            value = default!;
            return false;
        }

        /// <summary>
        /// 重载：获取所有已启用且满足谓词条件的值列表。
        /// 若找到至少一个匹配项则返回 true，否则返回 false。
        /// </summary>
        /// <param name="predicate">筛选条件</param>
        /// <param name="values">匹配的值列表（即使无匹配也返回空列表，而非 null）</param>
        /// <returns>是否有匹配的值</returns>
        public bool TryGetValue(Func<TValue, bool> predicate, out IList<TValue> values)
        {
            if (predicate == null) throw new ArgumentNullException(nameof(predicate));

            // 获取当前快照，避免遍历时集合被修改
            var entries = _store.ToArray();
            var result = new List<TValue>(entries.Length);

            foreach (var kvp in entries)
            {
                if (kvp.Value.Enabled && predicate(kvp.Value.Value))
                    result.Add(kvp.Value.Value);
            }

            values = result;
            return result.Count > 0;
        }

        /// <summary>获取所有已启用键的集合</summary>
        public ICollection<TKey> Keys()
        {
            return _store.Where(kvp => kvp.Value.Enabled)
                         .Select(kvp => kvp.Key)
                         .ToList();
        }

        /// <summary>获取所有已启用的值的集合</summary>
        public ICollection<TValue> Values()
        {
            return _store.Where(kvp => kvp.Value.Enabled)
                         .Select(kvp => kvp.Value.Value)
                         .ToList();
        }

        /// <summary>移除指定键（无论状态）</summary>
        public bool Remove(TKey key)
        {
            if (key == null) throw new ArgumentNullException(nameof(key));
            return _store.TryRemove(key, out _);
        }

        /// <summary>清空所有注册项</summary>
        public void Clear() => _store.Clear();

        /// <summary>检查指定键是否物理存在（无视启用状态）</summary>
        public bool Exists(TKey key)
        {
            if (key == null) throw new ArgumentNullException(nameof(key));
            return _store.ContainsKey(key);
        }

        /// <summary>检查指定键是否已启用</summary>
        public bool IsEnabled(TKey key)
        {
            if (key == null) throw new ArgumentNullException(nameof(key));
            return _store.TryGetValue(key, out var entry) && entry.Enabled;
        }
    }
}
