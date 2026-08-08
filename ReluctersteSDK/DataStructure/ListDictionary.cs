using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;

namespace ReluctersteSDK.DataStructure
{
    /// <summary>
    /// 表示一个基于 <see cref="List{T}"/> 的、线程安全的键值对集合。
    /// 该类不继承 <see cref="IDictionary{TKey, TValue}"/>，专为需要通过索引（Index）顺序访问键值对的场景设计。
    /// 所有的读取和写入操作均通过 <see cref="ReaderWriterLockSlim"/> 保证线程安全。
    /// </summary>
    /// <typeparam name="TKey">字典中键的类型。</typeparam>
    /// <typeparam name="TValue">字典中值的类型。</typeparam>
    public class ListDictionary<TKey, TValue> : IEnumerable<KeyValuePair<TKey, TValue>>, IDisposable
    {
        private readonly List<KeyValuePair<TKey, TValue>> _list;
        private readonly ReaderWriterLockSlim _lock;
        private readonly IEqualityComparer<TKey> _keyComparer;

        /// <summary>
        /// 初始化 <see cref="ListDictionary{TKey, TValue}"/> 类的新实例，使用默认的键相等比较器。
        /// </summary>
        public ListDictionary() : this(EqualityComparer<TKey>.Default) { }

        /// <summary>
        /// 初始化 <see cref="ListDictionary{TKey, TValue}"/> 类的新实例，并指定用于比较键的相等比较器。
        /// </summary>
        /// <param name="keyComparer">用于比较键的 <see cref="IEqualityComparer{TKey}"/> 接口实现。</param>
        public ListDictionary(IEqualityComparer<TKey> keyComparer)
        {
            _list = new List<KeyValuePair<TKey, TValue>>();
            _lock = new ReaderWriterLockSlim(LockRecursionPolicy.NoRecursion);
            _keyComparer = keyComparer ?? EqualityComparer<TKey>.Default;
        }

        #region 索引器与基本属性

        /// <summary>
        /// 获取或设置指定索引处的键值对。
        /// </summary>
        /// <param name="index">要获得或设置的元素的从零开始的索引。</param>
        /// <returns>指定索引处的键值对。</returns>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="index"/> 小于 0 或大于等于集合数量。</exception>
        /// <exception cref="ArgumentException">在设置新值时，如果新键已存在于其他索引处，则引发此异常。</exception>
        public KeyValuePair<TKey, TValue> this[int index]
        {
            get
            {
                _lock.EnterReadLock();
                try
                {
                    return _list[index];
                }
                finally
                {
                    _lock.ExitReadLock();
                }
            }
            set
            {
                _lock.EnterWriteLock();
                try
                {
                    // 如果修改了Key，需要确保新的Key没有在其他位置重复
                    if (!_keyComparer.Equals(_list[index].Key, value.Key))
                    {
                        int existingIndex = FindIndex(value.Key);
                        if (existingIndex >= 0 && existingIndex != index)
                        {
                            throw new ArgumentException($"An item with the same key has already been added. Key: {value.Key}");
                        }
                    }
                    _list[index] = value;
                }
                finally
                {
                    _lock.ExitWriteLock();
                }
            }
        }

        /// <summary>
        /// 获取包含在集合中的键/值对的数目。
        /// </summary>
        public int Count
        {
            get
            {
                _lock.EnterReadLock();
                try { return _list.Count; }
                finally { _lock.ExitReadLock(); }
            }
        }

        #endregion

        #region 基于 Key 的查询与操作 (替代原 dict[key])

        /// <summary>
        /// 获取与指定的键相关联的值。
        /// </summary>
        /// <param name="key">要获取的值的键。</param>
        /// <returns>与指定的键相关联的值。</returns>
        /// <exception cref="ArgumentNullException"><paramref name="key"/> 为 null。</exception>
        /// <exception cref="KeyNotFoundException">字典中不存在指定的键。</exception>
        public TValue GetValue(TKey key)
        {
            _lock.EnterReadLock();
            try
            {
                var index = FindIndex(key);
                if (index >= 0) return _list[index].Value;
                throw new KeyNotFoundException($"Key '{key}' was not found.");
            }
            finally
            {
                _lock.ExitReadLock();
            }
        }

        /// <summary>
        /// 尝试获取与指定的键相关联的值。
        /// </summary>
        /// <param name="key">要获取其值的键。</param>
        /// <param name="value">当此方法返回值时，如果找到该键，便会返回与指定的键相关联的值；否则返回默认值。</param>
        /// <returns>如果包含具有指定键的元素，则为 true；否则为 false。</returns>
        /// <exception cref="ArgumentNullException"><paramref name="key"/> 为 null。</exception>
        public bool TryGetValue(TKey key, out TValue value)
        {
            _lock.EnterReadLock();
            try
            {
                var index = FindIndex(key);
                if (index >= 0)
                {
                    value = _list[index].Value;
                    return true;
                }
                value = default;
                return false;
            }
            finally
            {
                _lock.ExitReadLock();
            }
        }

        /// <summary>
        /// 设置与指定的键相关联的值。如果键存在，则更新值；如果键不存在，则追加到列表末尾。
        /// </summary>
        /// <param name="key">要设置的键。</param>
        /// <param name="value">要设置的值。</param>
        /// <exception cref="ArgumentNullException"><paramref name="key"/> 为 null。</exception>
        public void SetValue(TKey key, TValue value)
        {
            _lock.EnterWriteLock();
            try
            {
                var index = FindIndex(key);
                if (index >= 0)
                {
                    _list[index] = new KeyValuePair<TKey, TValue>(key, value);
                }
                else
                {
                    _list.Add(new KeyValuePair<TKey, TValue>(key, value));
                }
            }
            finally
            {
                _lock.ExitWriteLock();
            }
        }

        /// <summary>
        /// 确定是否包含具有指定键的元素。
        /// </summary>
        /// <param name="key">要在集合中定位的键。</param>
        /// <returns>如果包含具有指定键的元素，则为 true；否则为 false。</returns>
        /// <exception cref="ArgumentNullException"><paramref name="key"/> 为 null。</exception>
        public bool ContainsKey(TKey key)
        {
            _lock.EnterReadLock();
            try { return FindIndex(key) >= 0; }
            finally { _lock.ExitReadLock(); }
        }

        #endregion

        #region 列表标准操作 (Add, Remove, Clear)

        /// <summary>
        /// 将带有指定的键和值的元素追加到集合末尾。
        /// </summary>
        /// <param name="key">要添加的元素的键。</param>
        /// <param name="value">要添加的元素的值。</param>
        /// <exception cref="ArgumentNullException"><paramref name="key"/> 为 null。</exception>
        /// <exception cref="ArgumentException">集合中已存在具有相同键的元素。</exception>
        public void Add(TKey key, TValue value)
        {
            _lock.EnterWriteLock();
            try
            {
                if (FindIndex(key) >= 0)
                    throw new ArgumentException($"An item with the same key has already been added. Key: {key}");

                _list.Add(new KeyValuePair<TKey, TValue>(key, value));
            }
            finally
            {
                _lock.ExitWriteLock();
            }
        }

        /// <summary>
        /// 从集合中移除带有指定键的元素。
        /// </summary>
        /// <param name="key">要移除的元素的键。</param>
        /// <returns>如果成功找到并移除该元素，则为 true；否则为 false。</returns>
        /// <exception cref="ArgumentNullException"><paramref name="key"/> 为 null。</exception>
        public bool Remove(TKey key)
        {
            _lock.EnterWriteLock();
            try
            {
                var index = FindIndex(key);
                if (index >= 0)
                {
                    _list.RemoveAt(index);
                    return true;
                }
                return false;
            }
            finally
            {
                _lock.ExitWriteLock();
            }
        }

        /// <summary>
        /// 移除指定索引处的元素。
        /// </summary>
        /// <param name="index">要移除的元素的从零开始的索引。</param>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="index"/> 小于 0 或大于等于集合数量。</exception>
        public void RemoveAt(int index)
        {
            _lock.EnterWriteLock();
            try
            {
                _list.RemoveAt(index);
            }
            finally
            {
                _lock.ExitWriteLock();
            }
        }

        /// <summary>
        /// 从集合中移除所有键和值。
        /// </summary>
        public void Clear()
        {
            _lock.EnterWriteLock();
            try { _list.Clear(); }
            finally { _lock.ExitWriteLock(); }
        }

        /// <summary>
        /// 在内部列表中查找指定键的索引（O(N) 复杂度）。
        /// 注意：调用此方法前必须已获取读锁或写锁。
        /// </summary>
        private int FindIndex(TKey key)
        {
            if (key == null) throw new ArgumentNullException(nameof(key));
            for (int i = 0; i < _list.Count; i++)
            {
                if (_keyComparer.Equals(_list[i].Key, key))
                    return i;
            }
            return -1;
        }

        #endregion

        #region 支持 LINQ 参数 / 委托的方法重载

        /// <summary>
        /// 根据指定的谓词条件批量移除集合中的元素。
        /// </summary>
        /// <param name="predicate">用于定义要移除的元素需满足的条件的委托。</param>
        /// <returns>从集合中移除的元素数。</returns>
        /// <exception cref="ArgumentNullException"><paramref name="predicate"/> 为 null。</exception>
        public int RemoveWhere(Func<KeyValuePair<TKey, TValue>, bool> predicate)
        {
            if (predicate == null) throw new ArgumentNullException(nameof(predicate));

            _lock.EnterWriteLock();
            try
            {
                return _list.RemoveAll(new Predicate<KeyValuePair<TKey, TValue>>(predicate));
            }
            finally
            {
                _lock.ExitWriteLock();
            }
        }

        /// <summary>
        /// 查找满足指定谓词条件的第一个键值对。
        /// </summary>
        /// <param name="predicate">用于测试每个元素是否满足条件的委托。</param>
        /// <returns>如果找到，则返回满足条件的第一个键值对；否则返回 null。</returns>
        /// <exception cref="ArgumentNullException"><paramref name="predicate"/> 为 null。</exception>
        public KeyValuePair<TKey, TValue>? FirstOrDefault(Func<KeyValuePair<TKey, TValue>, bool> predicate)
        {
            if (predicate == null) throw new ArgumentNullException(nameof(predicate));

            _lock.EnterReadLock();
            try
            {
                foreach (var item in _list)
                {
                    if (predicate(item)) return item;
                }
                return null;
            }
            finally
            {
                _lock.ExitReadLock();
            }
        }

        /// <summary>
        /// 筛选满足指定谓词条件的键值对集合。（返回的是评估结果的快照）
        /// </summary>
        /// <param name="predicate">用于测试每个元素是否满足条件的委托。</param>
        /// <returns>一个包含满足条件的元素的 <see cref="IEnumerable{T}"/>。</returns>
        /// <exception cref="ArgumentNullException"><paramref name="predicate"/> 为 null。</exception>
        public IEnumerable<KeyValuePair<TKey, TValue>> Where(Func<KeyValuePair<TKey, TValue>, bool> predicate)
        {
            if (predicate == null) throw new ArgumentNullException(nameof(predicate));

            _lock.EnterReadLock();
            try
            {
                return _list.Where(predicate).ToList();
            }
            finally
            {
                _lock.ExitReadLock();
            }
        }

        /// <summary>
        /// 确定集合中是否包含满足指定谓词条件的任何键值对。
        /// </summary>
        /// <param name="predicate">用于测试每个元素是否满足条件的委托。</param>
        /// <returns>如果源序列中的任何元素都通过指定谓词中的测试，则为 true；否则为 false。</returns>
        /// <exception cref="ArgumentNullException"><paramref name="predicate"/> 为 null。</exception>
        public bool Any(Func<KeyValuePair<TKey, TValue>, bool> predicate)
        {
            if (predicate == null) throw new ArgumentNullException(nameof(predicate));

            _lock.EnterReadLock();
            try
            {
                return _list.Any(predicate);
            }
            finally
            {
                _lock.ExitReadLock();
            }
        }

        #endregion

        #region IEnumerable 实现

        /// <summary>
        /// 返回一个循环访问集合的枚举器。
        /// 为保证线程安全，此枚举器基于调用此方法时集合内元素的快照创建。
        /// </summary>
        /// <returns>用于集合快照的 <see cref="IEnumerator{T}"/>。</returns>
        public IEnumerator<KeyValuePair<TKey, TValue>> GetEnumerator()
        {
            List<KeyValuePair<TKey, TValue>> snapshot;
            _lock.EnterReadLock();
            try
            {
                snapshot = _list.ToList();
            }
            finally
            {
                _lock.ExitReadLock();
            }

            return snapshot.GetEnumerator();
        }

        /// <summary>
        /// 返回一个循环访问集合的枚举器。
        /// </summary>
        /// <returns>一个可用于循环访问集合的 <see cref="IEnumerator"/>。</returns>
        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

        #endregion

        /// <summary>
        /// 释放由 <see cref="ListDictionary{TKey, TValue}"/> 使用的所有资源。
        /// </summary>
        public void Dispose()
        {
            _lock?.Dispose();
        }
    }
}
