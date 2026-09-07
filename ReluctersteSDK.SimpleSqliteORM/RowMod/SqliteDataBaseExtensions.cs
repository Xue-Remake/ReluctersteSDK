using Microsoft.Data.Sqlite;

namespace ReluctersteSDK.SimpleSqliteORM.RowMod
{
    /// <summary>
    /// 为原有的 SqliteDataBase 提供 RowModeORM 所需的底层执行扩展方法。
    /// </summary>
    internal static class SqliteDataBaseExtensions
    {
        /// <summary>
        /// 异步执行查询 SQL 语句并将结果集映射为对象列表。
        /// </summary>
        /// <typeparam name="T">映射目标类型。</typeparam>
        /// <param name="db">数据库实例。</param>
        /// <param name="sql">要执行的 SQL 查询语句。</param>
        /// <param name="parameters">SQL 参数列表，可为 <c>null</c>。</param>
        /// <param name="map">将每行数据读取器映射为对象的委托函数。</param>
        /// <returns>包含映射结果的对象列表。</returns>
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

        /// <summary>
        /// 异步执行查询 SQL 语句并返回第一行第一列的标量值。
        /// </summary>
        /// <param name="db">数据库实例。</param>
        /// <param name="sql">要执行的 SQL 查询语句。</param>
        /// <param name="parameters">SQL 参数列表，可为 <c>null</c>。</param>
        /// <returns>查询结果的第一行第一列值；若结果集为空则返回 <c>null</c>。</returns>
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
        /// <summary>
        /// 异步执行非查询 SQL 语句（如 INSERT、UPDATE、DELETE、DDL 等）。
        /// </summary>
        /// <param name="db">数据库实例。</param>
        /// <param name="sql">要执行的 SQL 语句。</param>
        /// <param name="parameters">参数字典，可为 <c>null</c>。</param>
        /// <returns>受影响的行数。</returns>
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
        /// <summary>
        /// 在独立的事务中异步执行一组数据库操作。
        /// </summary>
        /// <param name="db">数据库实例。</param>
        /// <param name="action">包含连接与事务参数的执行委托，发生异常时会自动回滚。</param>
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
