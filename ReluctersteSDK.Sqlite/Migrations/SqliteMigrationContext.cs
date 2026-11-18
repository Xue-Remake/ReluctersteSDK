using System.Globalization;
using Microsoft.Data.Sqlite;

namespace ReluctersteSDK.Sqlite.Migrations
{
    /// <summary>
    /// 迁移上下文：绑定到当前迁移的连接与事务，提供 SQL 执行、查询与结构探测。
    /// </summary>
    /// <remarks>
    /// <para>
    /// 上下文由 <see cref="SqliteMigrationRunner"/> 创建，生命周期只覆盖一次
    /// <c>UpAsync</c> / <c>DownAsync</c> 调用；不要把它保存到字段中跨迁移使用。
    /// </para>
    /// <para>
    /// 所有命令都自动挂到当前事务；抛出异常时整个迁移回滚。
    /// </para>
    /// </remarks>
    public sealed partial class SqliteMigrationContext
    {
        internal SqliteMigrationContext(
            SqliteConnection connection,
            SqliteTransaction transaction,
            string databasePath)
        {
            Connection = connection;
            Transaction = transaction;
            DatabasePath = databasePath;
        }

        /// <summary>获取当前迁移使用的连接。</summary>
        public SqliteConnection Connection { get; }

        /// <summary>获取当前迁移所在的事务。</summary>
        public SqliteTransaction Transaction { get; }

        /// <summary>获取数据库文件完整路径。</summary>
        public string DatabasePath { get; }

        /// <summary>
        /// 执行非查询 SQL（DDL 或 DML）。
        /// </summary>
        /// <param name="sql">SQL 语句。只应包含代码中写死的结构，值请走参数。</param>
        /// <param name="parameters">参数对象，支持匿名类型、字典或参数集合。</param>
        /// <param name="cancellationToken">取消令牌。</param>
        /// <returns>受影响的行数。</returns>
        public async Task<int> ExecuteAsync(
            string sql,
            object? parameters = null,
            CancellationToken cancellationToken = default)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(sql);
            await using var command = Connection.CreateCommand();
            command.Transaction = Transaction;
            command.CommandText = sql;
            SqliteUtils.AddParameters(command, parameters);
            return await command.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);
        }

        /// <summary>
        /// 执行标量查询，返回第一行第一列。
        /// </summary>
        /// <param name="sql">查询语句。</param>
        /// <param name="parameters">参数对象。</param>
        /// <param name="cancellationToken">取消令牌。</param>
        public async Task<object?> ExecuteScalarAsync(
            string sql,
            object? parameters = null,
            CancellationToken cancellationToken = default)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(sql);
            await using var command = Connection.CreateCommand();
            command.Transaction = Transaction;
            command.CommandText = sql;
            SqliteUtils.AddParameters(command, parameters);
            return await command.ExecuteScalarAsync(cancellationToken).ConfigureAwait(false);
        }

        /// <summary>
        /// 执行查询并用投影函数转换每一行。
        /// </summary>
        /// <typeparam name="TResult">结果类型。</typeparam>
        /// <param name="sql">查询语句。</param>
        /// <param name="projector">行投影委托。</param>
        /// <param name="parameters">参数对象。</param>
        /// <param name="cancellationToken">取消令牌。</param>
        public async Task<List<TResult>> QueryAsync<TResult>(
            string sql,
            Func<SqliteDataReader, TResult> projector,
            object? parameters = null,
            CancellationToken cancellationToken = default)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(sql);
            ArgumentNullException.ThrowIfNull(projector);
            await using var command = Connection.CreateCommand();
            command.Transaction = Transaction;
            command.CommandText = sql;
            SqliteUtils.AddParameters(command, parameters);
            var list = new List<TResult>();
            await using var reader = await command.ExecuteReaderAsync(cancellationToken).ConfigureAwait(false);
            while (await reader.ReadAsync(cancellationToken).ConfigureAwait(false))
            {
                list.Add(projector(reader));
            }
            return list;
        }

        /// <summary>
        /// 判断表是否存在（不区分大小写）。
        /// </summary>
        /// <param name="tableName">表名。</param>
        /// <param name="cancellationToken">取消令牌。</param>
        public async Task<bool> TableExistsAsync(
            string tableName,
            CancellationToken cancellationToken = default)
        {
            RequireName(tableName, nameof(tableName));
            var value = await ExecuteScalarAsync(
                "SELECT COUNT(*) FROM sqlite_master " +
                "WHERE type = 'table' AND name = @name COLLATE NOCASE;",
                new { name = tableName },
                cancellationToken).ConfigureAwait(false);
            return Convert.ToInt64(value, CultureInfo.InvariantCulture) > 0;
        }

        /// <summary>
        /// 判断表中是否存在指定列（不区分大小写）。
        /// </summary>
        /// <param name="tableName">表名。</param>
        /// <param name="columnName">列名。</param>
        /// <param name="cancellationToken">取消令牌。</param>
        public async Task<bool> ColumnExistsAsync(
            string tableName,
            string columnName,
            CancellationToken cancellationToken = default)
        {
            RequireName(columnName, nameof(columnName));
            var columns = await GetColumnsAsync(tableName, cancellationToken).ConfigureAwait(false);
            return columns.Any(c =>
                string.Equals(c.Name, columnName, StringComparison.OrdinalIgnoreCase));
        }

        /// <summary>
        /// 读取表的所有列信息；表不存在时返回空列表。
        /// </summary>
        /// <param name="tableName">表名。</param>
        /// <param name="cancellationToken">取消令牌。</param>
        public async Task<IReadOnlyList<SqliteColumnInfo>> GetColumnsAsync(
            string tableName,
            CancellationToken cancellationToken = default)
        {
            RequireName(tableName, nameof(tableName));
            var columns = new List<SqliteColumnInfo>();

            await using var command = Connection.CreateCommand();
            command.Transaction = Transaction;

            // PRAGMA 不支持参数绑定，表名必须先按标识符规则引用。
            command.CommandText = $"PRAGMA table_info({SqliteUtils.QuoteIdentifier(tableName)});";

            await using var reader = await command.ExecuteReaderAsync(cancellationToken).ConfigureAwait(false);
            while (await reader.ReadAsync(cancellationToken).ConfigureAwait(false))
            {
                columns.Add(new SqliteColumnInfo(
                    name: reader.GetString(1),
                    declaredType: reader.GetString(2),
                    isNotNull: reader.GetInt64(3) != 0,
                    defaultValue: reader.IsDBNull(4) ? null : reader.GetString(4),
                    primaryKeyOrder: (int)reader.GetInt64(5)));
            }

            return columns;
        }

        /// <summary>校验表名 / 列名非空。</summary>
        private static void RequireName(string value, string parameterName)
        {
            if (string.IsNullOrWhiteSpace(value))
                throw new ArgumentException("Identifier cannot be null or empty.", parameterName);
        }
    }
}
