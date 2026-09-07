using System.Collections.Concurrent;
using System.Reflection;

namespace ReluctersteSDK.SimpleSqliteORM
{
    /// <summary>
    /// 表示实体类型到数据库表的映射信息。
    /// </summary>
    internal sealed class EntityMap
    {
        /// <summary>实体类型。</summary>
        public required Type EntityType { get; init; }
        /// <summary>数据库表名。</summary>
        public required string TableName { get; init; }
        /// <summary>所有列映射的列表。</summary>
        public required IReadOnlyList<ColumnMap> Columns { get; init; }
        /// <summary>获取主键列映射。</summary>
        public IReadOnlyList<ColumnMap> Keys => Columns.Where(x => x.IsKey).ToArray();
        /// <summary>获取非主键列映射。</summary>
        public IReadOnlyList<ColumnMap> NormalColumns => Columns.Where(x => !x.IsKey).ToArray();
    }
    /// <summary>
    /// 表示实体属性到数据库列的映射信息。
    /// </summary>
    internal sealed class ColumnMap
    {
        /// <summary>对应的属性信息。</summary>
        public required PropertyInfo Property { get; init; }
        /// <summary>列名。</summary>
        public required string ColumnName { get; init; }
        /// <summary>是否为键（主键）。</summary>
        public required bool IsKey { get; init; }
        /// <summary>SQLite 数据类型（如 INTEGER, TEXT）。</summary>
        public required string SqliteType { get; init; }
        /// <summary>是否可为空。</summary>
        public required bool IsNullable { get; init; }
    }
    /// <summary>
    /// 提供实体映射的缓存，避免重复构建。
    /// </summary>
    internal static class EntityMapCache
    {
        private static readonly ConcurrentDictionary<Type, EntityMap> Cache = new();
        /// <summary>获取指定类型的映射（泛型版本）。</summary>
        public static EntityMap Get<T>()
        {
            return Get(typeof(T));
        }
        /// <summary>获取指定类型的映射。</summary>
        public static EntityMap Get(Type type)
        {
            return Cache.GetOrAdd(type, Build);
        }
        /// <summary>根据类型构建映射信息。</summary>
        private static EntityMap Build(Type type)
        {
            var tableAttr = type.GetCustomAttribute<DbTableAttribute>();
            var tableName = string.IsNullOrWhiteSpace(tableAttr?.Name)
                ? type.Name
                : tableAttr!.Name!;
            var props = type
                .GetProperties(BindingFlags.Public | BindingFlags.Instance)
                .Where(p => p.CanRead && p.CanWrite)
                .Where(p => p.GetCustomAttribute<DbIgnoreAttribute>() == null)
                .ToArray();
            var columns = new List<ColumnMap>();
            foreach (var prop in props)
            {
                var keyAttr = prop.GetCustomAttribute<DbKeyAttribute>();
                var columnAttr = prop.GetCustomAttribute<DbColumnAttribute>();
                var isKey = keyAttr != null;
                /*
                 * 规则：
                 * 1. [DbKey] 属性一定会进入数据库
                 * 2. [DbColumn] 属性会进入数据库
                 * 3. 没有 [DbKey] / [DbColumn] / [DbIgnore] 的属性默认不进入数据库
                 */
                if (!isKey && columnAttr == null)
                    continue;
                var columnName =
                    !string.IsNullOrWhiteSpace(columnAttr?.Name) ? columnAttr!.Name! :
                    !string.IsNullOrWhiteSpace(keyAttr?.Name) ? keyAttr!.Name! :
                    prop.Name;
                var propType = Nullable.GetUnderlyingType(prop.PropertyType) ?? prop.PropertyType;
                columns.Add(new ColumnMap
                {
                    Property = prop,
                    ColumnName = columnName,
                    IsKey = isKey,
                    SqliteType = SqliteTypeMapper.ToSqliteType(propType),
                    IsNullable = IsNullable(prop)
                });
            }
            if (columns.Count == 0)
            {
                throw new InvalidOperationException(
                    $"Type {type.FullName} does not contain any [DbKey] or [DbColumn] property.");
            }
            return new EntityMap
            {
                EntityType = type,
                TableName = tableName,
                Columns = columns
            };
        }
        /// <summary>
        /// 判断属性是否可为空。
        /// <para>值类型若为 <see cref="Nullable{T}"/> 返回 <c>true</c>，否则返回 <c>false</c>；</para>
        /// <para>引用类型一律返回 <c>true</c>。</para>
        /// </summary>
        private static bool IsNullable(PropertyInfo property)
        {
            var type = property.PropertyType;
            if (!type.IsValueType)
                return true;
            return Nullable.GetUnderlyingType(type) != null;
        }
    }
}
