using Microsoft.Data.Sqlite;
using System.Reflection;

namespace ReluctersteSDK.SimpleSqliteORM
{
    /// <summary>
    /// 提供 SQLite 相关的辅助方法（标识符引用、参数处理）。
    /// </summary>
    internal static class SqliteUtils
    {
        /// <summary>使用双引号引用标识符，并转义内部的双引号。</summary>
        public static string QuoteIdentifier(string name)
        {
            return "\"" + name.Replace("\"", "\"\"") + "\"";
        }
        /// <summary>向命令添加单个参数。</summary>
        public static void AddParameter(SqliteCommand command, string name, object? value)
        {
            if (!name.StartsWith("@"))
                name = "@" + name;
            var parameter = command.CreateParameter();
            parameter.ParameterName = name;
            parameter.Value = SqliteTypeMapper.ToDbValue(value);
            command.Parameters.Add(parameter);
        }
        /// <summary>
        /// 向命令添加多个参数。支持 <see cref="SqliteParameter"/> 集合、字典、匿名类型等。
        /// </summary>
        public static void AddParameters(SqliteCommand command, object? parameters)
        {
            if (parameters == null)
                return;
            if (parameters is IEnumerable<SqliteParameter> sqliteParameters)
            {
                foreach (var p in sqliteParameters)
                    command.Parameters.Add(p);
                return;
            }
            if (parameters is IReadOnlyDictionary<string, object?> dict)
            {
                foreach (var kv in dict)
                    AddParameter(command, kv.Key, kv.Value);
                return;
            }
            if (parameters is IEnumerable<KeyValuePair<string, object?>> pairs)
            {
                foreach (var kv in pairs)
                    AddParameter(command, kv.Key, kv.Value);
                return;
            }
            var props = parameters
                .GetType()
                .GetProperties(BindingFlags.Public | BindingFlags.Instance)
                .Where(p => p.CanRead);
            foreach (var prop in props)
            {
                AddParameter(command, prop.Name, prop.GetValue(parameters));
            }
        }
    }
}
