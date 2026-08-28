using Microsoft.Data.Sqlite;

namespace ReluctersteSDK.SimpleSqliteORM.RowMod
{
    /// <summary>
    /// 为原有的 SqliteDataBase 提供 RowModeORM 所需的底层执行扩展方法。
    /// </summary>
    internal static class SqliteDataBaseExtensions
    {
        public static async Task<List<T>> ExecuteQueryAsync<T>(
            this SqliteDataBase db,
            string sql,
            IEnumerable<SqliteParameter>? parameters,
            Func<SqliteDataReader, T> map)
        {
            await db.EnsureInitializedAsync();

            // 利用 ConnectionString 建立只读/读取连接
            var builder = new SqliteConnectionStringBuilder
            {
                DataSource = db.DatabasePath,
                Cache = SqliteCacheMode.Shared,
                Mode = SqliteOpenMode.ReadWriteCreate
            };

            await using var conn = new SqliteConnection(builder.ToString());
            await conn.OpenAsync();

            await using var cmd = conn.CreateCommand();
            cmd.CommandText = sql;

            if (parameters != null)
            {
                foreach (var p in parameters)
                {
                    cmd.Parameters.Add(p);
                }
            }

            await using var reader = await cmd.ExecuteReaderAsync();
            var list = new List<T>();
            while (await reader.ReadAsync())
            {
                list.Add(map(reader));
            }
            return list;
        }

        public static async Task<object?> ExecuteScalarAsync(
            this SqliteDataBase db,
            string sql,
            IEnumerable<SqliteParameter>? parameters)
        {
            await db.EnsureInitializedAsync();

            var builder = new SqliteConnectionStringBuilder
            {
                DataSource = db.DatabasePath,
                Cache = SqliteCacheMode.Shared,
                Mode = SqliteOpenMode.ReadWriteCreate
            };

            await using var conn = new SqliteConnection(builder.ToString());
            await conn.OpenAsync();

            await using var cmd = conn.CreateCommand();
            cmd.CommandText = sql;

            if (parameters != null)
            {
                foreach (var p in parameters)
                {
                    cmd.Parameters.Add(p);
                }
            }

            return await cmd.ExecuteScalarAsync();
        }

        public static async Task<int> ExecuteNonQueryAsync(
            this SqliteDataBase db,
            string sql,
            IDictionary<string, object?>? parameters = null)
        {
            await db.EnsureInitializedAsync();

            var builder = new SqliteConnectionStringBuilder
            {
                DataSource = db.DatabasePath,
                Cache = SqliteCacheMode.Shared,
                Mode = SqliteOpenMode.ReadWriteCreate
            };

            await using var conn = new SqliteConnection(builder.ToString());
            await conn.OpenAsync();

            await using var cmd = conn.CreateCommand();
            cmd.CommandText = sql;

            if (parameters != null)
            {
                SqliteUtils.AddParameters(cmd, parameters);
            }

            return await cmd.ExecuteNonQueryAsync();
        }

        public static async Task ExecuteInTransactionAsync(
            this SqliteDataBase db,
            Func<SqliteConnection, SqliteTransaction, Task> action)
        {
            await db.EnsureInitializedAsync();

            var builder = new SqliteConnectionStringBuilder
            {
                DataSource = db.DatabasePath,
                Cache = SqliteCacheMode.Shared,
                Mode = SqliteOpenMode.ReadWriteCreate
            };

            await using var conn = new SqliteConnection(builder.ToString());
            await conn.OpenAsync();

            await using var trans = (SqliteTransaction)await conn.BeginTransactionAsync();
            try
            {
                await action(conn, trans);
                await trans.CommitAsync();
            }
            catch
            {
                await trans.RollbackAsync();
                throw;
            }
        }
    }
}
