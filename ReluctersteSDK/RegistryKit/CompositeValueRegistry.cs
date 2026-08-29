using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Text;

namespace ReluctersteSDK.RegistryKit
{
    /// <summary>
    /// 复合键结构：支持任意数量、任意类型列组合构成的 Key
    /// </summary>
    public readonly struct CompositeKey : IEquatable<CompositeKey>
    {
        private readonly object?[] _keys;
        private readonly int _hashCode;
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
        public int Length => _keys.Length;
        public object? this[int index] => _keys[index];
        public IReadOnlyList<object?> Keys => _keys;
        public bool Equals(CompositeKey other)
        {
            if (_keys.Length != other._keys.Length) return false;
            for (int i = 0; i < _keys.Length; i++)
            {
                if (!Equals(_keys[i], other._keys[i])) return false;
            }
            return true;
        }
        public override bool Equals(object? obj) => obj is CompositeKey other && Equals(other);
        public override int GetHashCode() => _hashCode;
        public override string ToString() => $"[{string.Join(", ", _keys)}]";
        // 隐式转换：支持数组与 C# 元组直接无缝转为 CompositeKey
        public static implicit operator CompositeKey(object?[] keys) => new(keys);
        public static implicit operator CompositeKey((object?, object?) t) => new(t.Item1, t.Item2);
        public static implicit operator CompositeKey((object?, object?, object?) t) => new(t.Item1, t.Item2, t.Item3);
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
        public CompositeValueRegistry()
        {
            _store = new ConcurrentDictionary<CompositeKey, Entry>();
        }
        #region 索引器 Indexers (Key 在前)
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
        // --- 常用多列快捷重载 (Key 前, Value 后) ---
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
        public bool Enable(CompositeKey key) => SetEnabled(key, true);
        public bool Enable<T1, T2>(T1 k1, T2 k2) => SetEnabled(new CompositeKey(k1, k2), true);
        public bool Enable<T1, T2, T3>(T1 k1, T2 k2, T3 k3) => SetEnabled(new CompositeKey(k1, k2, k3), true);
        public bool Disable(CompositeKey key) => SetEnabled(key, false);
        public bool Disable<T1, T2>(T1 k1, T2 k2) => SetEnabled(new CompositeKey(k1, k2), false);
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
        #region 查询与获取 Get / TryGet
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
        /// <summary>尝试获取值（ Key 在前，out Value 在后）</summary>
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
        /// <summary>获取所有已启用且满足谓词条件的值列表</summary>
        public bool TryGetValue(Func<TValue, bool> predicate, out IList<TValue> values)
        {
            if (predicate == null) throw new ArgumentNullException(nameof(predicate));
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
        /// <summary>根据 Key 的前缀列模糊获取已启用的值</summary>
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
        public ICollection<CompositeKey> Keys()
        {
            return _store.Where(kvp => kvp.Value.Enabled)
                         .Select(kvp => kvp.Key)
                         .ToList();
        }
        public ICollection<TValue> Values()
        {
            return _store.Where(kvp => kvp.Value.Enabled)
                         .Select(kvp => kvp.Value.Value)
                         .ToList();
        }
        public bool Remove(CompositeKey key) => _store.TryRemove(key, out _);
        public bool Remove<T1, T2>(T1 k1, T2 k2) => Remove(new CompositeKey(k1, k2));
        public bool Remove<T1, T2, T3>(T1 k1, T2 k2, T3 k3) => Remove(new CompositeKey(k1, k2, k3));
        public void Clear() => _store.Clear();
        public bool Exists(CompositeKey key) => _store.ContainsKey(key);
        public bool Exists<T1, T2>(T1 k1, T2 k2) => Exists(new CompositeKey(k1, k2));
        public bool IsEnabled(CompositeKey key)
        {
            return _store.TryGetValue(key, out var entry) && entry.Enabled;
        }
        public bool IsEnabled<T1, T2>(T1 k1, T2 k2) => IsEnabled(new CompositeKey(k1, k2));
        #endregion
    }
}
