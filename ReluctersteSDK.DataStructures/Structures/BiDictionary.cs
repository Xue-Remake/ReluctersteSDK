using System.Collections;
using System.Text.Json.Serialization;
using ReluctersteSDK.DataStructures.Json;

namespace ReluctersteSDK.DataStructures.Structures
{
    /// <summary>
    /// 双向字典
    /// </summary>
    [JsonConverter(typeof(BiDictionaryConverterFactory))]
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
        public void Add(ValueTuple<TKey, TValue> temp)
        {
            if (temp.Item1 == null || temp.Item2 == null) throw new ArgumentNullException(nameof(temp));
            if (_forward.ContainsKey(temp.Item1))
                throw new ArgumentException($"Key '{temp.Item1}' 已经存在。");

            if (_reverse.ContainsKey(temp.Item2))
                throw new ArgumentException($"Value '{temp.Item2}' 已经存在（BiDictionary 要求 Value 也是唯一的）。");

            _forward.Add(temp.Item1, temp.Item2);
            _reverse.Add(temp.Item2, temp.Item1);
        }
        public void Add(KeyValuePair<TKey, TValue> pair)
        {
            if (pair.Key == null || pair.Value == null) throw new ArgumentNullException(nameof(pair));
            if (_forward.ContainsKey(pair.Key))
                throw new ArgumentException($"Key '{pair.Key}' 已经存在。");

            if (_reverse.ContainsKey(pair.Value))
                throw new ArgumentException($"Value '{pair.Value}' 已经存在（BiDictionary 要求 Value 也是唯一的）。");

            _forward.Add(pair.Key, pair.Value);
            _reverse.Add(pair.Value, pair.Key);
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

        #region 排序方法 (Sort)

        /// <summary>
        /// 使用默认的键比较器，按键对所有键值对进行排序。
        /// </summary>
        public void Sort() => Sort((a, b) => Comparer<TKey>.Default.Compare(a.Key, b.Key));

        /// <summary>
        /// 使用指定的比较委托对所有键值对进行排序。
        /// </summary>
        /// <param name="comparison">比较两个键值对时使用的 <see cref="Comparison{T}"/> 委托。</param>
        /// <exception cref="ArgumentNullException"><paramref name="comparison"/> 为 null。</exception>
        public void Sort(Comparison<KeyValuePair<TKey, TValue>> comparison)
        {
            if (comparison == null) throw new ArgumentNullException(nameof(comparison));

            var sorted = new List<KeyValuePair<TKey, TValue>>(_forward);
            sorted.Sort(comparison);
            RebuildForward(sorted);
        }

        /// <summary>
        /// 使用指定的比较器对所有键值对进行排序。
        /// </summary>
        /// <param name="comparer">比较键值对时使用的 <see cref="IComparer{T}"/> 实现。</param>
        /// <exception cref="ArgumentNullException"><paramref name="comparer"/> 为 null。</exception>
        public void Sort(IComparer<KeyValuePair<TKey, TValue>> comparer)
        {
            if (comparer == null) throw new ArgumentNullException(nameof(comparer));

            var sorted = new List<KeyValuePair<TKey, TValue>>(_forward);
            sorted.Sort(comparer);
            RebuildForward(sorted);
        }

        /// <summary>
        /// 根据 LINQ 键选择表达式对所有键值对进行排序。
        /// </summary>
        /// <typeparam name="TSortKey">由键选择表达式返回的排序键类型。</typeparam>
        /// <param name="keySelector">用于从每个键值对中提取排序键的表达式。</param>
        /// <param name="descending">是否按降序排序，默认为 false（升序）。</param>
        /// <exception cref="ArgumentNullException"><paramref name="keySelector"/> 为 null。</exception>
        public void Sort<TSortKey>(Func<KeyValuePair<TKey, TValue>, TSortKey> keySelector, bool descending = false)
        {
            if (keySelector == null) throw new ArgumentNullException(nameof(keySelector));

            var sorted = new List<KeyValuePair<TKey, TValue>>(_forward);
            sorted.Sort((a, b) =>
            {
                int result = Comparer<TSortKey>.Default.Compare(keySelector(a), keySelector(b));
                return descending ? -result : result;
            });
            RebuildForward(sorted);
        }

        /// <summary>
        /// 按排序后的顺序重建正向字典（键值对映射关系不变，仅调整枚举顺序）。
        /// </summary>
        private void RebuildForward(List<KeyValuePair<TKey, TValue>> sorted)
        {
            _forward.Clear();
            foreach (var kvp in sorted)
            {
                _forward.Add(kvp.Key, kvp.Value);
            }
        }

        #endregion

        public IEnumerator<KeyValuePair<TKey, TValue>> GetEnumerator() => _forward.GetEnumerator();
        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

        #endregion
    }
}
