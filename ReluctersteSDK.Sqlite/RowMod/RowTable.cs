using System.Linq.Expressions;
using System.Text;

namespace ReluctersteSDK.Sqlite.RowMod
{
    /// <summary>
    /// 表示一个基于纯行（<see cref="Row"/>）模式的数据表操作器。
    /// </summary>
    public sealed class RowTable
    {
        private readonly SqliteDataBase _db;
        /// <summary>获取当前操作的数据表名称。</summary>
        public string TableName { get; }
        /// <summary>
        /// 初始化 <see cref="RowTable"/> 实例。
        /// </summary>
        /// <param name="db">底层 SQLite 数据库操作实例。</param>
        /// <param name="tableName">数据表名称。</param>
        public RowTable(SqliteDataBase db, string tableName)
        {
            _db = db;
            TableName = tableName;
        }
        /// <summary>
        /// 异步确保数据表存在；若表不存在则创建该表。
        /// </summary>
        /// <param name="columns">列定义元组数组（列名, 列定义/数据类型，例如 ("Id", "INTEGER PRIMARY KEY AUTOINCREMENT")）。</param>
        public async Task CreateTableIfNotExistsAsync(params (string ColumnName, string ColumnType)[] columns)
        {
            var sb = new StringBuilder();
            sb.Append("CREATE TABLE IF NOT EXISTS ")
              .Append(SqliteUtils.QuoteIdentifier(TableName))
              .Append(" (");

            var cols = columns.Select(c => $"{SqliteUtils.QuoteIdentifier(c.ColumnName)} {c.ColumnType}");
            sb.Append(string.Join(", ", cols)).Append(");");

            await _db.ExecuteNonQueryAsync(sb.ToString());
        }
        /// <summary>
        /// 创建带有过滤条件的查询操作。
        /// </summary>
        /// <param name="predicate">过滤条件表达式。</param>
        /// <returns>查询构建器 <see cref="RowQuery"/>。</returns>
        public RowQuery Where(Expression<Func<Row, bool>> predicate) => new RowQuery(_db, TableName).Where(predicate);
        /// <summary>
        /// 创建指定投影列的查询操作。
        /// </summary>
        /// <param name="columns">要检索的列名。</param>
        /// <returns>查询构建器 <see cref="RowQuery"/>。</returns>
        public RowQuery Select(params string[] columns) => new RowQuery(_db, TableName).Select(columns);
        /// <summary>
        /// 异步获取全表的全部数据行。
        /// </summary>
        /// <returns>包含全表所有行的数据列表。</returns>
        public Task<List<Row>> ToListAsync() => new RowQuery(_db, TableName).ToListAsync();
        /// <summary>
        /// 异步获取全表的总记录数。
        /// </summary>
        /// <returns>表中的数据总行数。</returns>
        public Task<long> CountAsync() => new RowQuery(_db, TableName).CountAsync();
        /// <summary>
        /// 异步向表中插入单行数据。
        /// </summary>
        /// <param name="row">待插入的行数据。</param>
        /// <returns>受影响的行数。</returns>
        public async Task<int> AddAsync(Row row)
        {
            if (row == null || row.Count == 0) return 0;

            var columns = row.Keys.Select(SqliteUtils.QuoteIdentifier);
            var paramNames = row.Keys.Select(k => "@" + k);

            var sql = $"INSERT INTO {SqliteUtils.QuoteIdentifier(TableName)} ({string.Join(", ", columns)}) VALUES ({string.Join(", ", paramNames)});";

            return await _db.ExecuteNonQueryAsync(sql, row);
        }
        /// <summary>
        /// 在单个事务中异步批量插入多行数据。
        /// </summary>
        /// <param name="rows">待插入的行集合。</param>
        public async Task AddRangeAsync(IEnumerable<Row> rows)
        {
            var list = rows.ToList();
            if (list.Count == 0) return;

            await _db.ExecuteInTransactionAsync(async (conn, trans) =>
            {
                foreach (var row in list)
                {
                    var columns = row.Keys.Select(SqliteUtils.QuoteIdentifier);
                    var paramNames = row.Keys.Select(k => "@" + k);
                    var sql = $"INSERT INTO {SqliteUtils.QuoteIdentifier(TableName)} ({string.Join(", ", columns)}) VALUES ({string.Join(", ", paramNames)});";

                    await using var cmd = conn.CreateCommand();
                    cmd.Transaction = trans;
                    cmd.CommandText = sql;
                    SqliteUtils.AddParameters(cmd, row);
                    await cmd.ExecuteNonQueryAsync();
                }
            });
        }
    }
}
