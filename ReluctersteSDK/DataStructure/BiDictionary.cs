using System;
using System.Collections.Generic;
using System.Text;

namespace ReluctersteSDK.DataStructure
{
    /// <summary>
    /// 双向字典
    /// </summary>
    public class BiDictionary<TKey, TValue> : IEnumerable<KeyValuePair<TKey, TValue>>
        where TKey : notnull
        where TValue : notnull
    {
        private readonly Dictionary<TKey, TValue> _forward;
        private readonly Dictionary<TValue, TKey> _reverse;

        #region 构造函数

        /// <summary>
        /// 无参构造函数
        /// </summary>
        public BiDictionary() : this(null, null, null) { }

        /// <summary>
        /// 支持自定义比较器的构造函数
        /// </summary>
        public BiDictionary(IEqualityComparer<TKey>? keyComparer, IEqualityComparer<TValue>? valueComparer)
            : this(null, keyComparer, valueComparer) { }

        /// <summary>
        /// 直接使用已有的 KeyValuePair 集合构造双向字典
        /// </summary>
        public BiDictionary(IEnumerable<KeyValuePair<TKey, TValue>> collection,
                            IEqualityComparer<TKey>? keyComparer = null,
                            IEqualityComparer<TValue>? valueComparer = null)
        {
            _forward = new Dictionary<TKey, TValue>(keyComparer);
            _reverse = new Dictionary<TValue, TKey>(valueComparer);

            if (collection != null)
            {
                foreach (var kvp in collection)
                {
                    Add(kvp.Key, kvp.Value);
                }
            }
        }

        /// <summary>
        /// 从现有的普通 Dictionary 构造双向字典
        /// </summary>
        public BiDictionary(IDictionary<TKey, TValue> dictionary,
                            IEqualityComparer<TKey>? keyComparer = null,
                            IEqualityComparer<TValue>? valueComparer = null)
            : this((IEnumerable<KeyValuePair<TKey, TValue>>)dictionary, keyComparer, valueComparer)
        {
        }

        #endregion

        #region 常用属性与方法

        public int Count => _forward.Count;
        public IEnumerable<TKey> Keys => _forward.Keys;
        public IEnumerable<TValue> Values => _reverse.Keys;

        public TValue this[TKey key]
        {
            get => _forward[key];
            set => AddOrUpdate(key, value);
        }

        public void Add(TKey key, TValue value)
        {
            if (key == null) throw new ArgumentNullException(nameof(key));
            if (value == null) throw new ArgumentNullException(nameof(value));

            if (_forward.ContainsKey(key))
                throw new ArgumentException($"Key '{key}' 已经存在。");

            if (_reverse.ContainsKey(value))
                throw new ArgumentException($"Value '{value}' 已经存在（BiDictionary 要求 Value 也是唯一的）。");

            _forward.Add(key, value);
            _reverse.Add(value, key);
        }

        public void AddOrUpdate(TKey key, TValue value)
        {
            if (key == null) throw new ArgumentNullException(nameof(key));
            if (value == null) throw new ArgumentNullException(nameof(value));

            if (_forward.TryGetValue(key, out TValue? existingValue))
            {
                _reverse.Remove(existingValue);
            }

            if (_reverse.TryGetValue(value, out TKey? existingKey))
            {
                _forward.Remove(existingKey);
            }

            _forward[key] = value;
            _reverse[value] = key;
        }

        public bool TryGetValue(TKey key, out TValue value) => _forward.TryGetValue(key, out value!);
        public bool TryGetKey(TValue value, out TKey key) => _reverse.TryGetValue(value, out key!);

        public TValue GetValue(TKey key) => _forward[key];
        public TKey GetKey(TValue value) => _reverse[value];

        public bool ContainsKey(TKey key) => _forward.ContainsKey(key);
        public bool ContainsValue(TValue value) => _reverse.ContainsKey(value);

        public bool RemoveByKey(TKey key)
        {
            if (_forward.TryGetValue(key, out TValue? value))
            {
                _forward.Remove(key);
                _reverse.Remove(value);
                return true;
            }
            return false;
        }

        public bool RemoveByValue(TValue value)
        {
            if (_reverse.TryGetValue(value, out TKey? key))
            {
                _reverse.Remove(value);
                _forward.Remove(key);
                return true;
            }
            return false;
        }

        public void Clear()
        {
            _forward.Clear();
            _reverse.Clear();
        }

        public IEnumerator<KeyValuePair<TKey, TValue>> GetEnumerator() => _forward.GetEnumerator();
        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

        #endregion
    }
}
