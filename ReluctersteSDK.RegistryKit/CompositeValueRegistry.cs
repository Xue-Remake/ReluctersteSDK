using System.Collections.Concurrent;

namespace ReluctersteSDK.RegistryKit
{
    /// <summary>
    /// 复合键结构：支持任意数量、任意类型列组合构成的 Key
    /// </summary>
    public readonly struct CompositeKey : IEquatable<CompositeKey>, IRegistry
    {
        private readonly object?[] _keys;
        private readonly int _hashCode;
        /// <summary>
        /// 初始化 <see cref="CompositeKey"/> 结构的新实例。
        /// </summary>
        /// <param name="keys">组成复合键的各列元素数组</param>
        /// <exception cref="ArgumentNullException">当 <paramref name="keys"/> 为 null 时抛出</exception>
        public CompositeKey(params object?[] keys)
        {
            _keys = keys ?? throw new ArgumentNullException(nameof(keys));

            var hash = new HashCode();
            for (int i = 0; i < _keys.Length; i++)
            {
                hash.Add(_keys[i]);
            }
            _hashCode = hash.ToHashCode();
        }
        /// <summary>获取复合键包含的列数</summary>
        public int Length => _keys.Length;
        /// <summary>获取指定索引处的键列对象</summary>
        /// <param name="index">从 0 开始的列索引</param>
        public object? this[int index] => _keys[index];
        /// <summary>获取复合键内部所有列对象的只读列表视图</summary>
        public IReadOnlyList<object?> Keys => _keys;
        /// <summary>
        /// 确定当前复合键是否与另一个复合键相等（按列依次判定值相等）
        /// </summary>
        /// <param name="other">要比较的复合键</param>
        /// <returns>若两者的长度与每列的值均相等则返回 true，否则返回 false</returns>
        public bool Equals(CompositeKey other)
        {
            if (_keys.Length != other._keys.Length) return false;
            for (int i = 0; i < _keys.Length; i++)
            {
                if (!Equals(_keys[i], other._keys[i])) return false;
            }
            return true;
        }
        /// <summary>确定当前对象是否与指定对象相等</summary>
        public override bool Equals(object? obj) => obj is CompositeKey other && Equals(other);
        /// <summary>返回该复合键的哈希代码</summary>
        public override int GetHashCode() => _hashCode;
        /// <summary>返回复合键格式化的字符串表示，形式如 "[k1, k2, ...]"</summary>
        public override string ToString() => $"[{string.Join(", ", _keys)}]";

        // 隐式转换：支持数组与 C# 元组直接无缝转为 CompositeKey
        /// <summary>将对象数组隐式转换为 <see cref="CompositeKey"/></summary>
        public static implicit operator CompositeKey(object?[] keys) => new(keys);
        /// <summary>将 2 元元组隐式转换为 <see cref="CompositeKey"/></summary>
        public static implicit operator CompositeKey((object?, object?) t) => new(t.Item1, t.Item2);
        /// <summary>将 3 元元组隐式转换为 <see cref="CompositeKey"/></summary>
        public static implicit operator CompositeKey((object?, object?, object?) t) => new(t.Item1, t.Item2, t.Item3);

        /// <summary>将 4 元元组隐式转换为 <see cref="CompositeKey"/></summary>
        public static implicit operator CompositeKey((object?, object?, object?, object?) t) => new(t.Item1, t.Item2, t.Item3, t.Item4);
        /// <summary>快捷创建 CompositeKey</summary>
        public static CompositeKey From(params object?[] keys) => new(keys);
    }
    /// <summary>
    /// 通用复合键（任意多列）注册表，支持对单个条目进行启用/禁用（线程安全）。
    /// </summary>
    /// <typeparam name="TValue">存储的值类型</typeparam>
    public class CompositeValueRegistry<TValue>
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
        private readonly ConcurrentDictionary<CompositeKey, Entry> _store;
        /// <summary>
        /// 初始化 <see cref="CompositeValueRegistry{TValue}"/> 的新实例。
        /// </summary>
        public CompositeValueRegistry()
        {
            _store = new ConcurrentDictionary<CompositeKey, Entry>();
        }
        #region 索引器 Indexers
        /// <summary>
        /// 通过 CompositeKey 获取或注册/覆盖值
        /// </summary>
        public TValue? this[CompositeKey key]
        {
            get => GetValue(key);
            set => Register(key, value!);
        }
        #endregion
        #region 注册 Register (Key 在前，Value 在后)
        /// <summary>
        /// 基础注册方法：Key 在前，Value 在后
        /// </summary>
        public void Register(CompositeKey key, TValue value)
        {
            _store.AddOrUpdate(
                key,
                _ => new Entry(value, true),
                (_, existing) => new Entry(value, existing.Enabled));
        }
        /// <summary>双列 Key 注册</summary>
        public void Register<T1, T2>(T1 k1, T2 k2, TValue value)
            => Register(new CompositeKey(k1, k2), value);
        /// <summary>三列 Key 注册</summary>
        public void Register<T1, T2, T3>(T1 k1, T2 k2, T3 k3, TValue value)
            => Register(new CompositeKey(k1, k2, k3), value);
        /// <summary>四列 Key 注册</summary>
        public void Register<T1, T2, T3, T4>(T1 k1, T2 k2, T3 k3, T4 k4, TValue value)
            => Register(new CompositeKey(k1, k2, k3, k4), value);
        #endregion
        #region 启用/禁用 Enable / Disable
        /// <summary>启用指定复合键的条目</summary>
        /// <param name="key">目标复合键</param>
        /// <returns>若成功更新或条目已处于启用状态则返回 true，若条目不存在则返回 false</returns>
        public bool Enable(CompositeKey key) => SetEnabled(key, true);

        /// <summary>启用指定双列复合键的条目</summary>
        public bool Enable<T1, T2>(T1 k1, T2 k2) => SetEnabled(new CompositeKey(k1, k2), true);

        /// <summary>启用指定三列复合键的条目</summary>
        public bool Enable<T1, T2, T3>(T1 k1, T2 k2, T3 k3) => SetEnabled(new CompositeKey(k1, k2, k3), true);

        /// <summary>禁用指定复合键的条目</summary>
        /// <param name="key">目标复合键</param>
        /// <returns>若成功更新或条目已处于禁用状态则返回 true，若条目不存在则返回 false</returns>
        public bool Disable(CompositeKey key) => SetEnabled(key, false);

        /// <summary>禁用指定双列复合键的条目</summary>
        public bool Disable<T1, T2>(T1 k1, T2 k2) => SetEnabled(new CompositeKey(k1, k2), false);

        /// <summary>禁用指定三列复合键的条目</summary>
        public bool Disable<T1, T2, T3>(T1 k1, T2 k2, T3 k3) => SetEnabled(new CompositeKey(k1, k2, k3), false);

        private bool SetEnabled(CompositeKey key, bool enabled)
        {
            while (_store.TryGetValue(key, out var entry))
            {
                if (entry.Enabled == enabled) return true;
                if (_store.TryUpdate(key, entry.WithEnabled(enabled), entry))
                    return true;
            }
            return false;
        }
        #endregion
        #region 单项查询 Get / TryGet
        /// <summary>根据复合键获取对应的值（忽略禁用条目）</summary>
        public TValue? GetValue(CompositeKey key)
        {
            return _store.TryGetValue(key, out var entry) && entry.Enabled ? entry.Value : default;
        }
        /// <summary>双列 Key 查询</summary>
        public TValue? GetValue<T1, T2>(T1 k1, T2 k2) => GetValue(new CompositeKey(k1, k2));
        /// <summary>三列 Key 查询</summary>
        public TValue? GetValue<T1, T2, T3>(T1 k1, T2 k2, T3 k3) => GetValue(new CompositeKey(k1, k2, k3));
        /// <summary>根据谓词条件获取第一个匹配的已启用的值</summary>
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
        /// <summary>尝试获取值（Key 在前，out Value 在后）</summary>
        public bool TryGetValue(CompositeKey key, out TValue value)
        {
            if (_store.TryGetValue(key, out var entry) && entry.Enabled)
            {
                value = entry.Value;
                return true;
            }
            value = default!;
            return false;
        }
        /// <summary>双列 Key TryGet</summary>
        public bool TryGetValue<T1, T2>(T1 k1, T2 k2, out TValue value)
            => TryGetValue(new CompositeKey(k1, k2), out value);
        /// <summary>三列 Key TryGet</summary>
        public bool TryGetValue<T1, T2, T3>(T1 k1, T2 k2, T3 k3, out TValue value)
            => TryGetValue(new CompositeKey(k1, k2, k3), out value);
        /// <summary>获取所有已启用且满足谓词条件的值列表（out 参数版本）</summary>
        public bool TryGetValue(Func<TValue, bool> predicate, out IList<TValue> values)
        {
            values = Query(predicate);
            return values.Count > 0;
        }
        #endregion
        #region 列表查询 Query (返回 IList<TValue> 结果列表)
        /// <summary>
        /// 根据谓词条件查询所有已启用的匹配项，并直接返回结果列表。
        /// 若未找到匹配项则返回空列表（非 null）。
        /// </summary>
        /// <param name="predicate">针对 Value 的筛选条件</param>
        /// <returns>匹配的值列表</returns>
        public IList<TValue> Query(Func<TValue, bool> predicate)
        {
            if (predicate == null) throw new ArgumentNullException(nameof(predicate));
            var entries = _store.ToArray();
            var result = new List<TValue>(entries.Length);
            foreach (var kvp in entries)
            {
                if (kvp.Value.Enabled && predicate(kvp.Value.Value))
                    result.Add(kvp.Value.Value);
            }
            return result;
        }
        /// <summary>
        /// 根据谓词条件（同时包含 Key 与 Value）查询所有已启用的匹配项，并直接返回结果列表。
        /// 若未找到匹配项则返回空列表（非 null）。
        /// </summary>
        /// <param name="predicate">针对 Key 和 Value 的复合筛选条件</param>
        /// <returns>匹配的值列表</returns>
        public IList<TValue> Query(Func<CompositeKey, TValue, bool> predicate)
        {
            if (predicate == null) throw new ArgumentNullException(nameof(predicate));
            var entries = _store.ToArray();
            var result = new List<TValue>(entries.Length);
            foreach (var kvp in entries)
            {
                if (kvp.Value.Enabled && predicate(kvp.Key, kvp.Value.Value))
                    result.Add(kvp.Value.Value);
            }
            return result;
        }
        /// <summary>根据 Key 的前缀列模糊查询所有已启用的值列表</summary>
        public IList<TValue> GetValuesByPrefix(params object[] prefixKeys)
        {
            if (prefixKeys == null || prefixKeys.Length == 0)
                return Values().ToList();
            var results = new List<TValue>();
            foreach (var kvp in _store)
            {
                if (!kvp.Value.Enabled) continue;
                var compKey = kvp.Key;
                if (compKey.Length < prefixKeys.Length) continue;
                bool isMatch = true;
                for (int i = 0; i < prefixKeys.Length; i++)
                {
                    if (!Equals(compKey[i], prefixKeys[i]))
                    {
                        isMatch = false;
                        break;
                    }
                }
                if (isMatch)
                {
                    results.Add(kvp.Value.Value);
                }
            }
            return results;
        }
        #endregion
        #region 集合与状态检查 Collection & Utility
        /// <summary>获取所有已启用的复合键集合</summary>
        public ICollection<CompositeKey> Keys()
        {
            return _store.Where(kvp => kvp.Value.Enabled)
                         .Select(kvp => kvp.Key)
                         .ToList();
        }
        /// <summary>获取所有已启用的值集合</summary>
        public ICollection<TValue> Values()
        {
            return _store.Where(kvp => kvp.Value.Enabled)
                         .Select(kvp => kvp.Value.Value)
                         .ToList();
        }
        /// <summary>移除指定复合键（无论状态）</summary>
        /// <param name="key">目标复合键</param>
        /// <returns>若成功移除则返回 true，若键不存在则返回 false</returns>
        public bool Remove(CompositeKey key) => _store.TryRemove(key, out _);
        /// <summary>移除指定双列复合键（无论状态）</summary>
        public bool Remove<T1, T2>(T1 k1, T2 k2) => Remove(new CompositeKey(k1, k2));
        /// <summary>移除指定三列复合键（无论状态）</summary>
        public bool Remove<T1, T2, T3>(T1 k1, T2 k2, T3 k3) => Remove(new CompositeKey(k1, k2, k3));
        /// <summary>清空所有注册项</summary>
        public void Clear() => _store.Clear();
        /// <summary>检查指定复合键是否物理存在（无视启用状态）</summary>
        /// <param name="key">目标复合键</param>
        public bool Exists(CompositeKey key) => _store.ContainsKey(key);
        /// <summary>检查指定双列复合键是否物理存在（无视启用状态）</summary>
        public bool Exists<T1, T2>(T1 k1, T2 k2) => Exists(new CompositeKey(k1, k2));
        /// <summary>检查指定复合键是否已启用</summary>
        /// <param name="key">目标复合键</param>
        public bool IsEnabled(CompositeKey key)
        {
            return _store.TryGetValue(key, out var entry) && entry.Enabled;
        }
        /// <summary>检查指定双列复合键是否已启用</summary>
        public bool IsEnabled<T1, T2>(T1 k1, T2 k2) => IsEnabled(new CompositeKey(k1, k2));
        #endregion
    }
}
