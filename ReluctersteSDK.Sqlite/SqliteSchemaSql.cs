using System.Text;

namespace ReluctersteSDK.Sqlite
{
    /// <summary>
    /// 根据实体映射生成表结构 SQL。
    /// </summary>
    /// <remarks>
    /// 特性映射的建表与迁移上下文共用这里的生成结果，
    /// 避免两处各写一套 <c>CREATE TABLE</c>。
    /// </remarks>
    internal static class SqliteSchemaSql
    {
        /// <summary>
        /// 生成 <c>CREATE TABLE IF NOT EXISTS</c> 语句。
        /// </summary>
        /// <param name="map">实体映射。</param>
        public static string BuildCreateTable(EntityMap map)
        {
            var sb = new StringBuilder();
            sb.Append("CREATE TABLE IF NOT EXISTS ");
            sb.Append(SqliteUtils.QuoteIdentifier(map.TableName));
            sb.AppendLine(" (");
            for (var i = 0; i < map.Columns.Count; i++)
            {
                var col = map.Columns[i];
                sb.Append("    ");
                sb.Append(SqliteUtils.QuoteIdentifier(col.ColumnName));
                sb.Append(' ');
                sb.Append(col.SqliteType);
                if (col.IsKey || !col.IsNullable)
                    sb.Append(" NOT NULL");
                if (i < map.Columns.Count - 1 || map.Keys.Count > 0)
                    sb.Append(',');
                sb.AppendLine();
            }
            if (map.Keys.Count > 0)
            {
                sb.Append("    PRIMARY KEY (");
                sb.Append(string.Join(", ", map.Keys.Select(k => SqliteUtils.QuoteIdentifier(k.ColumnName))));
                sb.AppendLine(")");
            }
            sb.Append(");");
            return sb.ToString();
        }
    }
}
