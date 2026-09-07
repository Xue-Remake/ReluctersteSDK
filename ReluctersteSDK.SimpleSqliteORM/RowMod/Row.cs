using System.Collections;

namespace ReluctersteSDK.SimpleSqliteORM.RowMod
{
    /// <summary>
    /// 表示数据表中的一行纯数据（不关联任何实体类），不区分大小写。
    /// </summary>
    public sealed class Row : IDictionary<string, object?>, IReadOnlyDictionary<string, object?>
    {
        private readonly Dictionary<string, object?> _fields = new(StringComparer.OrdinalIgnoreCase);

        /// <summary>初始化一个空的 <see cref="Row"/> 实例。</summary>
        public Row() { }

        /// <summary>使用已有字典数据初始化 <see cref="Row"/> 实例。</summary>
        /// <param name="source">初始数据源。</param>
        public Row(IDictionary<string, object?> source)
        {
            _fields = new Dictionary<string, object?>(source, StringComparer.OrdinalIgnoreCase);
        }
        /// <summary>
        /// 获取或设置指定列的值。若列不存在则返回 <c>null</c>。
        /// </summary>
        /// <param name="columnName">列名（忽略大小写）。</param>
        /// <returns>列的值。</returns>
        public object? this[string columnName]
        {
            get => _fields.TryGetValue(columnName, out var val) ? val : null;
            set => _fields[columnName] = value;
        }

        /// <summary>
        /// 获取转换为指定强类型的列值。
        /// </summary>
        public T? Get<T>(string columnName)
        {
            var value = this[columnName];
            if (value == null || value == DBNull.Value)
                return default;

            return (T)SqliteTypeMapper.FromDbValue(value, typeof(T))!;
        }

        /// <inheritdoc />
        public ICollection<string> Keys => _fields.Keys;
        /// <inheritdoc />
        public ICollection<object?> Values => _fields.Values;
        /// <inheritdoc />
        public int Count => _fields.Count;
        /// <inheritdoc />
        public bool IsReadOnly => false;

        IEnumerable<string> IReadOnlyDictionary<string, object?>.Keys => _fields.Keys;
        IEnumerable<object?> IReadOnlyDictionary<string, object?>.Values => _fields.Values;

        /// <inheritdoc />
        public void Add(string key, object? value) => _fields.Add(key, value);
        /// <inheritdoc />
        public void Add(KeyValuePair<string, object?> item) => _fields.Add(item.Key, item.Value);
        /// <inheritdoc />
        public void Clear() => _fields.Clear();
        /// <inheritdoc />
        public bool Contains(KeyValuePair<string, object?> item) => ((ICollection<KeyValuePair<string, object?>>)_fields).Contains(item);
        /// <inheritdoc />
        public bool ContainsKey(string key) => _fields.ContainsKey(key);
        /// <inheritdoc />
        public void CopyTo(KeyValuePair<string, object?>[] array, int arrayIndex) => ((ICollection<KeyValuePair<string, object?>>)_fields).CopyTo(array, arrayIndex);
        /// <inheritdoc />
        public bool Remove(string key) => _fields.Remove(key);
        /// <inheritdoc />
        public bool Remove(KeyValuePair<string, object?> item) => ((ICollection<KeyValuePair<string, object?>>)_fields).Remove(item);
        /// <inheritdoc />
        public bool TryGetValue(string key, out object? value) => _fields.TryGetValue(key, out value);
        /// <inheritdoc />
        public IEnumerator<KeyValuePair<string, object?>> GetEnumerator() => _fields.GetEnumerator();
        IEnumerator IEnumerable.GetEnumerator() => _fields.GetEnumerator();
    }
}
