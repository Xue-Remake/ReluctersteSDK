using ReluctersteSDK.SimpleSqliteORM;
using System.Collections;

namespace RowMod
{
    /// <summary>
    /// 表示数据表中的一行纯数据（不关联任何实体类）。
    /// </summary>
    public sealed class Row : IDictionary<string, object?>, IReadOnlyDictionary<string, object?>
    {
        private readonly Dictionary<string, object?> _fields = new(StringComparer.OrdinalIgnoreCase);

        public Row() { }

        public Row(IDictionary<string, object?> source)
        {
            _fields = new Dictionary<string, object?>(source, StringComparer.OrdinalIgnoreCase);
        }

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

        public ICollection<string> Keys => _fields.Keys;
        public ICollection<object?> Values => _fields.Values;
        public int Count => _fields.Count;
        public bool IsReadOnly => false;

        IEnumerable<string> IReadOnlyDictionary<string, object?>.Keys => _fields.Keys;
        IEnumerable<object?> IReadOnlyDictionary<string, object?>.Values => _fields.Values;

        public void Add(string key, object? value) => _fields.Add(key, value);
        public void Add(KeyValuePair<string, object?> item) => _fields.Add(item.Key, item.Value);
        public void Clear() => _fields.Clear();
        public bool Contains(KeyValuePair<string, object?> item) => ((ICollection<KeyValuePair<string, object?>>)_fields).Contains(item);
        public bool ContainsKey(string key) => _fields.ContainsKey(key);
        public void CopyTo(KeyValuePair<string, object?>[] array, int arrayIndex) => ((ICollection<KeyValuePair<string, object?>>)_fields).CopyTo(array, arrayIndex);
        public bool Remove(string key) => _fields.Remove(key);
        public bool Remove(KeyValuePair<string, object?> item) => ((ICollection<KeyValuePair<string, object?>>)_fields).Remove(item);
        public bool TryGetValue(string key, out object? value) => _fields.TryGetValue(key, out value);
        public IEnumerator<KeyValuePair<string, object?>> GetEnumerator() => _fields.GetEnumerator();
        IEnumerator IEnumerable.GetEnumerator() => _fields.GetEnumerator();
    }
}
