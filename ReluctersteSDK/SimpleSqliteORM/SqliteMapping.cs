using System.Globalization;

namespace ReluctersteSDK.SimpleSqliteORM
{
    /// <summary>
    /// 提供 .NET 类型与 SQLite 类型之间的映射转换。
    /// </summary>
    internal static class SqliteTypeMapper
    {
        /// <summary>将 .NET 类型映射为 SQLite 类型名称。</summary>
        public static string ToSqliteType(Type type)
        {
            type = Nullable.GetUnderlyingType(type) ?? type;
            if (type.IsEnum)
                return "INTEGER";
            if (type == typeof(byte) ||
                type == typeof(sbyte) ||
                type == typeof(short) ||
                type == typeof(ushort) ||
                type == typeof(int) ||
                type == typeof(uint) ||
                type == typeof(long) ||
                type == typeof(ulong) ||
                type == typeof(bool))
            {
                return "INTEGER";
            }
            if (type == typeof(float) ||
                type == typeof(double) ||
                type == typeof(decimal))
            {
                return "REAL";
            }
            if (type == typeof(string) ||
                type == typeof(DateTime) ||
                type == typeof(DateTimeOffset) ||
                type == typeof(Guid))
            {
                return "TEXT";
            }
            if (type == typeof(byte[]))
            {
                return "BLOB";
            }
            throw new NotSupportedException($"Unsupported property type: {type.FullName}");
        }
        /// <summary>将 .NET 值转换为可存储到数据库的值。</summary>
        public static object? ToDbValue(object? value)
        {
            if (value == null)
                return DBNull.Value;
            var type = Nullable.GetUnderlyingType(value.GetType()) ?? value.GetType();
            if (type.IsEnum)
                return Convert.ToInt64(value, CultureInfo.InvariantCulture);
            if (type == typeof(bool))
                return (bool)value ? 1 : 0;
            if (type == typeof(DateTime))
                return ((DateTime)value).ToString("O", CultureInfo.InvariantCulture);
            if (type == typeof(DateTimeOffset))
                return ((DateTimeOffset)value).ToString("O", CultureInfo.InvariantCulture);
            if (type == typeof(Guid))
                return value.ToString();
            return value;
        }
        /// <summary>将数据库值转换为 .NET 类型值。</summary>
        public static object? FromDbValue(object dbValue, Type targetType)
        {
            if (dbValue == DBNull.Value)
            {
                if (!targetType.IsValueType || Nullable.GetUnderlyingType(targetType) != null)
                    return null;
                return Activator.CreateInstance(targetType);
            }
            var actualType = Nullable.GetUnderlyingType(targetType) ?? targetType;
            if (actualType.IsEnum)
            {
                var number = Convert.ToInt64(dbValue, CultureInfo.InvariantCulture);
                return Enum.ToObject(actualType, number);
            }
            if (actualType == typeof(bool))
                return Convert.ToInt64(dbValue, CultureInfo.InvariantCulture) != 0;
            if (actualType == typeof(DateTime))
                return DateTime.Parse(dbValue.ToString()!, null, DateTimeStyles.RoundtripKind);
            if (actualType == typeof(DateTimeOffset))
                return DateTimeOffset.Parse(dbValue.ToString()!, null, DateTimeStyles.RoundtripKind);
            if (actualType == typeof(Guid))
                return Guid.Parse(dbValue.ToString()!);
            if (actualType == typeof(decimal))
                return Convert.ToDecimal(dbValue, CultureInfo.InvariantCulture);
            if (actualType == typeof(byte[]))
                return (byte[])dbValue;
            return Convert.ChangeType(dbValue, actualType, CultureInfo.InvariantCulture);
        }
    }
}
