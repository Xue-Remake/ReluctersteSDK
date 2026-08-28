using System.Linq.Expressions;
using System.Text;

namespace ReluctersteSDK.SimpleSqliteORM.RowMod
{
    public sealed class RowTable
    {
        private readonly SqliteDataBase _db;

        public string TableName { get; }

        public RowTable(SqliteDataBase db, string tableName)
        {
            _db = db;
            TableName = tableName;
        }

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

        public RowQuery Where(Expression<Func<Row, bool>> predicate) => new RowQuery(_db, TableName).Where(predicate);
        public RowQuery Select(params string[] columns) => new RowQuery(_db, TableName).Select(columns);
        public Task<List<Row>> ToListAsync() => new RowQuery(_db, TableName).ToListAsync();
        public Task<long> CountAsync() => new RowQuery(_db, TableName).CountAsync();

        public async Task<int> AddAsync(Row row)
        {
            if (row == null || row.Count == 0) return 0;

            var columns = row.Keys.Select(SqliteUtils.QuoteIdentifier);
            var paramNames = row.Keys.Select(k => "@" + k);

            var sql = $"INSERT INTO {SqliteUtils.QuoteIdentifier(TableName)} ({string.Join(", ", columns)}) VALUES ({string.Join(", ", paramNames)});";

            return await _db.ExecuteNonQueryAsync(sql, row);
        }

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
