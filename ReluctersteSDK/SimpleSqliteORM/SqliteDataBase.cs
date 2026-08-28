using Microsoft.Data.Sqlite;
using ReluctersteSDK.PathHelper;
using ReluctersteSDK.PathHelper.Tools;
using System.Text;

namespace ReluctersteSDK.SimpleSqliteORM
{
    /// <summary>
    /// 表示一个 SQLite 数据库连接和操作的封装，支持异步 I/O 和 WAL 模式。
    /// </summary>
    public sealed class SqliteDataBase : IAsyncDisposable
    {
        private readonly string _folderPath;
        private readonly string _databaseName;
        private readonly bool _enableWal;
        private readonly string _connectionString;
        private readonly SemaphoreSlim _writeLock = new(1, 1);
        private readonly object _initLock = new();
        private Task? _initTask;
        /// <summary>获取数据库文件的完整路径。</summary>
        public string DatabasePath { get; }
        /// <summary>
        /// 初始化 <see cref="SqliteDataBase"/> 实例。
        /// </summary>
        /// <param name="folderPath">数据库文件所在的文件夹路径。</param>
        /// <param name="databaseName">数据库文件名。</param>
        /// <param name="enableWal">是否启用 WAL 模式（默认启用）。</param>
        /// <exception cref="ArgumentException">参数为 null 或空白时抛出。</exception>
        public SqliteDataBase(
            string folderPath,
            string databaseName,
            bool enableWal = true)
        {
            if (string.IsNullOrWhiteSpace(folderPath))
                throw new ArgumentException("Folder path cannot be null or empty.", nameof(folderPath));
            if (string.IsNullOrWhiteSpace(databaseName))
                throw new ArgumentException("Database name cannot be null or empty.", nameof(databaseName));
            _folderPath = folderPath;
            _databaseName = databaseName;
            _enableWal = enableWal;
            Directory.CreateDirectory(_folderPath);
            DatabasePath = Path.Combine(_folderPath, _databaseName);
            var builder = new SqliteConnectionStringBuilder
            {
                DataSource = DatabasePath,
                Cache = SqliteCacheMode.Shared,
                Mode = SqliteOpenMode.ReadWriteCreate
            };
            _connectionString = builder.ToString();
        }
        /// <summary>
        /// 初始化 <see cref="SqliteDataBase"/> 实例。路径参数为FdPath类型的重载
        /// </summary>
        /// <param name="folderPath">数据库文件所在的文件夹路径（FdPath类型）。</param>
        /// <param name="databaseName">数据库文件名。</param>
        /// <param name="enableWal">是否启用 WAL 模式（默认启用）。</param>
        /// <exception cref="ArgumentException">参数为 null 或空白时抛出。</exception>
        public SqliteDataBase(FdPath folderPath, string databaseName, bool enableWal = true)
        {
            var fdPath = PathAnalyzer.Analysis(folderPath)?.PathStr;
            if (string.IsNullOrWhiteSpace(fdPath))
                throw new ArgumentException("Folder path cannot be null or empty.", nameof(fdPath));
            if (string.IsNullOrWhiteSpace(databaseName))
                throw new ArgumentException("Database name cannot be null or empty.", nameof(databaseName));
            _folderPath = fdPath;
            _databaseName = databaseName;
            _enableWal = enableWal;
            Directory.CreateDirectory(_folderPath);
            DatabasePath = Path.Combine(_folderPath, _databaseName);
            var builder = new SqliteConnectionStringBuilder
            {
                DataSource = DatabasePath,
                Cache = SqliteCacheMode.Shared,
                Mode = SqliteOpenMode.ReadWriteCreate
            };
            _connectionString = builder.ToString();
        }
        /// <inheritdoc />
        public async ValueTask DisposeAsync()
        {
            await _writeLock.WaitAsync();
            try
            {
                _writeLock.Dispose();
            }
            catch
            {
                // ignored
            }
        }
        /// <summary>
        /// 确保数据库已初始化（创建文件夹、设置 PRAGMA）。
        /// </summary>
        /// <remarks>该方法可被多次调用，实际初始化只执行一次。</remarks>
        public async Task EnsureInitializedAsync()
        {
            if (_initTask != null)
            {
                await _initTask.ConfigureAwait(false);
                return;
            }
            lock (_initLock)
            {
                _initTask ??= InitializeCoreAsync();
            }
            await _initTask.ConfigureAwait(false);
        }
        private async Task InitializeCoreAsync()
        {
            await using var connection = new SqliteConnection(_connectionString);
            await connection.OpenAsync().ConfigureAwait(false);
            await ExecutePragmaAsync(connection, "PRAGMA busy_timeout = 5000;").ConfigureAwait(false);
            await ExecutePragmaAsync(connection, "PRAGMA foreign_keys = ON;").ConfigureAwait(false);
            if (_enableWal)
            {
                await ExecutePragmaAsync(connection, "PRAGMA journal_mode = WAL;").ConfigureAwait(false);
                await ExecutePragmaAsync(connection, "PRAGMA synchronous = NORMAL;").ConfigureAwait(false);
            }
        }
        private async Task<SqliteConnection> OpenConnectionAsync()
        {
            await EnsureInitializedAsync().ConfigureAwait(false);
            var connection = new SqliteConnection(_connectionString);
            await connection.OpenAsync().ConfigureAwait(false);
            await ExecutePragmaAsync(connection, "PRAGMA busy_timeout = 5000;").ConfigureAwait(false);
            await ExecutePragmaAsync(connection, "PRAGMA foreign_keys = ON;").ConfigureAwait(false);
            return connection;
        }
        private static async Task ExecutePragmaAsync(SqliteConnection connection, string sql)
        {
            await using var command = connection.CreateCommand();
            command.CommandText = sql;
            await command.ExecuteNonQueryAsync().ConfigureAwait(false);
        }
        /// <summary>
        /// 确保实体对应的表存在（若不存在则创建）。
        /// </summary>
        /// <typeparam name="T">实体类型。</typeparam>
        /// <exception cref="InvalidOperationException">当实体没有定义任何列映射时抛出。</exception>
        public async Task EnsureTableAsync<T>()
            where T : new()
        {
            var map = EntityMapCache.Get<T>();
            var sql = BuildCreateTableSql(map);
            await _writeLock.WaitAsync().ConfigureAwait(false);
            try
            {
                await using var connection = await OpenConnectionAsync().ConfigureAwait(false);
                await using var command = connection.CreateCommand();
                command.CommandText = sql;
                await command.ExecuteNonQueryAsync().ConfigureAwait(false);
            }
            finally
            {
                _writeLock.Release();
            }
        }
        private static string BuildCreateTableSql(EntityMap map)
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
        /// <summary>
        /// 插入一条实体记录。
        /// </summary>
        /// <typeparam name="T">实体类型。</typeparam>
        /// <param name="entity">要插入的实体对象。</param>
        /// <returns>受影响的行数（通常为 1）。</returns>
        /// <exception cref="ArgumentNullException">entity 为 null。</exception>
        public async Task<int> InsertAsync<T>(T entity)
        {
            ArgumentNullException.ThrowIfNull(entity);
            var map = EntityMapCache.Get<T>();
            var columns = map.Columns;
            var columnSql = string.Join(", ", columns.Select(c => SqliteUtils.QuoteIdentifier(c.ColumnName)));
            var valueSql = string.Join(", ", columns.Select((_, i) => $"@p{i}"));
            var sql =
                $"INSERT INTO {SqliteUtils.QuoteIdentifier(map.TableName)} " +
                $"({columnSql}) VALUES ({valueSql});";
            return await ExecuteWriteEntityAsync(entity, map, columns, sql).ConfigureAwait(false);
        }
        /// <summary>
        /// 插入或更新一条实体记录（基于主键冲突）。
        /// </summary>
        /// <typeparam name="T">实体类型。</typeparam>
        /// <param name="entity">实体对象。</param>
        /// <returns>受影响的行数。</returns>
        /// <exception cref="ArgumentNullException">entity 为 null。</exception>
        /// <exception cref="InvalidOperationException">实体未定义任何 [DbKey] 属性。</exception>
        public async Task<int> UpsertAsync<T>(T entity)
        {
            ArgumentNullException.ThrowIfNull(entity);
            var map = EntityMapCache.Get<T>();
            if (map.Keys.Count == 0)
                throw new InvalidOperationException($"Type {typeof(T).FullName} does not define any [DbKey].");
            var columns = map.Columns;
            var columnSql = string.Join(", ", columns.Select(c => SqliteUtils.QuoteIdentifier(c.ColumnName)));
            var valueSql = string.Join(", ", columns.Select((_, i) => $"@p{i}"));
            var updateColumns = map.Columns.Where(c => !c.IsKey).ToArray();
            string sql;
            if (updateColumns.Length == 0)
            {
                sql =
                    $"INSERT OR IGNORE INTO {SqliteUtils.QuoteIdentifier(map.TableName)} " +
                    $"({columnSql}) VALUES ({valueSql});";
            }
            else
            {
                var conflictSql = string.Join(", ", map.Keys.Select(k => SqliteUtils.QuoteIdentifier(k.ColumnName)));
                var setSql = string.Join(", ", updateColumns.Select(c =>
                    $"{SqliteUtils.QuoteIdentifier(c.ColumnName)} = excluded.{SqliteUtils.QuoteIdentifier(c.ColumnName)}"));
                sql =
                    $"INSERT INTO {SqliteUtils.QuoteIdentifier(map.TableName)} " +
                    $"({columnSql}) VALUES ({valueSql}) " +
                    $"ON CONFLICT ({conflictSql}) DO UPDATE SET {setSql};";
            }
            return await ExecuteWriteEntityAsync(entity, map, columns, sql).ConfigureAwait(false);
        }
        /// <summary>
        /// 根据主键更新实体的非键列。
        /// </summary>
        /// <typeparam name="T">实体类型。</typeparam>
        /// <param name="entity">包含新值的实体对象（主键用于定位）。</param>
        /// <returns>受影响的行数。</returns>
        /// <exception cref="ArgumentNullException">entity 为 null。</exception>
        /// <exception cref="InvalidOperationException">实体未定义任何 [DbKey] 属性，或没有非键列可更新。</exception>
        public async Task<int> UpdateAsync<T>(T entity)
        {
            ArgumentNullException.ThrowIfNull(entity);
            var map = EntityMapCache.Get<T>();
            if (map.Keys.Count == 0)
                throw new InvalidOperationException($"Type {typeof(T).FullName} does not define any [DbKey].");
            var normalColumns = map.NormalColumns.ToArray();
            if (normalColumns.Length == 0)
                return 0;
            var setSql = string.Join(", ", normalColumns.Select((c, i) =>
                $"{SqliteUtils.QuoteIdentifier(c.ColumnName)} = @p{i}"));
            var whereSql = string.Join(" AND ", map.Keys.Select((c, i) =>
                $"{SqliteUtils.QuoteIdentifier(c.ColumnName)} = @k{i}"));
            var sql =
                $"UPDATE {SqliteUtils.QuoteIdentifier(map.TableName)} " +
                $"SET {setSql} WHERE {whereSql};";
            await _writeLock.WaitAsync().ConfigureAwait(false);
            try
            {
                await using var connection = await OpenConnectionAsync().ConfigureAwait(false);
                await using var command = connection.CreateCommand();
                command.CommandText = sql;
                for (var i = 0; i < normalColumns.Length; i++)
                {
                    var value = normalColumns[i].Property.GetValue(entity);
                    SqliteUtils.AddParameter(command, $"p{i}", value);
                }
                for (var i = 0; i < map.Keys.Count; i++)
                {
                    var value = map.Keys[i].Property.GetValue(entity);
                    SqliteUtils.AddParameter(command, $"k{i}", value);
                }
                return await command.ExecuteNonQueryAsync().ConfigureAwait(false);
            }
            finally
            {
                _writeLock.Release();
            }
        }
        /// <summary>
        /// 根据实体对象的主键删除对应记录。
        /// </summary>
        /// <typeparam name="T">实体类型。</typeparam>
        /// <param name="entity">包含主键值的实体对象。</param>
        /// <returns>受影响的行数。</returns>
        /// <exception cref="ArgumentNullException">entity 为 null。</exception>
        /// <exception cref="InvalidOperationException">实体未定义任何 [DbKey] 属性。</exception>
        public async Task<int> DeleteAsync<T>(T entity)
        {
            ArgumentNullException.ThrowIfNull(entity);
            var map = EntityMapCache.Get<T>();
            if (map.Keys.Count == 0)
                throw new InvalidOperationException($"Type {typeof(T).FullName} does not define any [DbKey].");
            var whereSql = string.Join(" AND ", map.Keys.Select((c, i) =>
                $"{SqliteUtils.QuoteIdentifier(c.ColumnName)} = @k{i}"));
            var sql =
                $"DELETE FROM {SqliteUtils.QuoteIdentifier(map.TableName)} " +
                $"WHERE {whereSql};";
            await _writeLock.WaitAsync().ConfigureAwait(false);
            try
            {
                await using var connection = await OpenConnectionAsync().ConfigureAwait(false);
                await using var command = connection.CreateCommand();
                command.CommandText = sql;
                for (var i = 0; i < map.Keys.Count; i++)
                {
                    var value = map.Keys[i].Property.GetValue(entity);
                    SqliteUtils.AddParameter(command, $"k{i}", value);
                }
                return await command.ExecuteNonQueryAsync().ConfigureAwait(false);
            }
            finally
            {
                _writeLock.Release();
            }
        }
        /// <summary>
        /// 根据主键值列表删除记录。
        /// </summary>
        /// <typeparam name="T">实体类型。</typeparam>
        /// <param name="keys">主键值，顺序必须与实体 [DbKey] 声明的顺序一致。</param>
        /// <returns>受影响的行数。</returns>
        /// <exception cref="InvalidOperationException">实体未定义任何 [DbKey] 属性。</exception>
        /// <exception cref="ArgumentException">提供的键值数量与主键数量不匹配。</exception>
        public async Task<int> DeleteByKeyAsync<T>(params object?[] keys)
            where T : new()
        {
            var map = EntityMapCache.Get<T>();
            if (map.Keys.Count == 0)
                throw new InvalidOperationException($"Type {typeof(T).FullName} does not define any [DbKey].");
            if (keys.Length != map.Keys.Count)
                throw new ArgumentException($"Key count mismatch. Expected {map.Keys.Count}, actual {keys.Length}.");
            var whereSql = string.Join(" AND ", map.Keys.Select((c, i) =>
                $"{SqliteUtils.QuoteIdentifier(c.ColumnName)} = @k{i}"));
            var sql =
                $"DELETE FROM {SqliteUtils.QuoteIdentifier(map.TableName)} " +
                $"WHERE {whereSql};";
            await _writeLock.WaitAsync().ConfigureAwait(false);
            try
            {
                await using var connection = await OpenConnectionAsync().ConfigureAwait(false);
                await using var command = connection.CreateCommand();
                command.CommandText = sql;
                for (var i = 0; i < keys.Length; i++)
                    SqliteUtils.AddParameter(command, $"k{i}", keys[i]);
                return await command.ExecuteNonQueryAsync().ConfigureAwait(false);
            }
            finally
            {
                _writeLock.Release();
            }
        }
        private async Task<int> ExecuteWriteEntityAsync<T>(
            T entity,
            EntityMap map,
            IReadOnlyList<ColumnMap> columns,
            string sql)
        {
            await _writeLock.WaitAsync().ConfigureAwait(false);
            try
            {
                await using var connection = await OpenConnectionAsync().ConfigureAwait(false);
                await using var command = connection.CreateCommand();
                command.CommandText = sql;
                for (var i = 0; i < columns.Count; i++)
                {
                    var value = columns[i].Property.GetValue(entity);
                    SqliteUtils.AddParameter(command, $"p{i}", value);
                }
                return await command.ExecuteNonQueryAsync().ConfigureAwait(false);
            }
            finally
            {
                _writeLock.Release();
            }
        }
        /// <summary>
        /// 根据主键值获取单个实体。
        /// </summary>
        /// <typeparam name="T">实体类型。</typeparam>
        /// <param name="keys">主键值，顺序与 [DbKey] 声明顺序一致。</param>
        /// <returns>实体对象，若未找到则返回 null。</returns>
        /// <exception cref="InvalidOperationException">实体未定义任何 [DbKey] 属性。</exception>
        /// <exception cref="ArgumentException">提供的键值数量与主键数量不匹配。</exception>
        public async Task<T?> GetByKeyAsync<T>(params object?[] keys)
            where T : new()
        {
            var map = EntityMapCache.Get<T>();
            if (map.Keys.Count == 0)
                throw new InvalidOperationException($"Type {typeof(T).FullName} does not define any [DbKey].");
            if (keys.Length != map.Keys.Count)
                throw new ArgumentException($"Key count mismatch. Expected {map.Keys.Count}, actual {keys.Length}.");
            var whereSql = string.Join(" AND ", map.Keys.Select((c, i) =>
                $"{SqliteUtils.QuoteIdentifier(c.ColumnName)} = @k{i}"));
            var parameters = keys
                .Select((value, index) => new KeyValuePair<string, object?>($"k{index}", value))
                .ToArray();
            var result = await QueryAsync<T>(
                where: whereSql,
                parameters: parameters,
                limit: 1).ConfigureAwait(false);
            return result.FirstOrDefault();
        }
        /// <summary>
        /// 执行查询，返回实体列表。
        /// </summary>
        /// <typeparam name="T">实体类型。</typeparam>
        /// <param name="where">WHERE 子句（可包含 "WHERE" 前缀，也可不带）。</param>
        /// <param name="parameters">参数对象（匿名类型、字典、或 KeyValuePair 集合）。</param>
        /// <param name="orderBy">ORDER BY 子句（不含 "ORDER BY" 关键字）。</param>
        /// <param name="limit">限制返回行数。</param>
        /// <returns>实体列表。</returns>
        public async Task<List<T>> QueryAsync<T>(
            string? where = null,
            object? parameters = null,
            string? orderBy = null,
            int? limit = null)
            where T : new()
        {
            var map = EntityMapCache.Get<T>();
            var selectSql = string.Join(", ", map.Columns.Select(c =>
                $"{SqliteUtils.QuoteIdentifier(c.ColumnName)} AS {SqliteUtils.QuoteIdentifier(c.Property.Name)}"));
            var sql = new StringBuilder();
            sql.Append("SELECT ");
            sql.Append(selectSql);
            sql.Append(" FROM ");
            sql.Append(SqliteUtils.QuoteIdentifier(map.TableName));
            if (!string.IsNullOrWhiteSpace(where))
            {
                sql.Append(' ');
                if (where.TrimStart().StartsWith("WHERE", StringComparison.OrdinalIgnoreCase))
                    sql.Append(where);
                else
                    sql.Append("WHERE ").Append(where);
            }
            if (!string.IsNullOrWhiteSpace(orderBy))
            {
                sql.Append(" ORDER BY ");
                sql.Append(orderBy);
            }
            if (limit.HasValue)
            {
                sql.Append(" LIMIT ");
                sql.Append(limit.Value);
            }
            sql.Append(';');
            await using var connection = await OpenConnectionAsync().ConfigureAwait(false);
            await using var command = connection.CreateCommand();
            command.CommandText = sql.ToString();
            SqliteUtils.AddParameters(command, parameters);
            var list = new List<T>();
            await using var reader = await command.ExecuteReaderAsync().ConfigureAwait(false);
            while (await reader.ReadAsync().ConfigureAwait(false))
            {
                list.Add(ReadEntity<T>(reader, map));
            }
            return list;
        }
        private static T ReadEntity<T>(SqliteDataReader reader, EntityMap map)
            where T : new()
        {
            var entity = new T();
            foreach (var column in map.Columns)
            {
                var ordinal = reader.GetOrdinal(column.Property.Name);
                var dbValue = reader.GetValue(ordinal);
                var value = SqliteTypeMapper.FromDbValue(dbValue, column.Property.PropertyType);
                column.Property.SetValue(entity, value);
            }
            return entity;
        }
        /// <summary>
        /// 读取指定实体对应的表中的所有数据。
        /// </summary>
        /// <typeparam name="T">实体类型。</typeparam>
        /// <returns>实体列表。</returns>
        public async Task<List<T>> GetAllAsync<T>()
            where T : new()
        {
            // 确保表存在（幂等操作）
            await EnsureTableAsync<T>().ConfigureAwait(false);
            // 查询所有记录
            return await QueryAsync<T>().ConfigureAwait(false);
        }
        /// <summary>
        /// 执行自定义 SQL 查询，并通过投影函数将每一行转换为结果对象。
        /// </summary>
        /// <typeparam name="TResult">结果类型。</typeparam>
        /// <param name="sql">完整的 SQL 查询语句。</param>
        /// <param name="projector">将 <see cref="SqliteDataReader"/> 转换为 <typeparamref name="TResult"/> 的委托。</param>
        /// <param name="parameters">参数对象。</param>
        /// <returns>结果列表。</returns>
        /// <exception cref="ArgumentException">sql 为 null 或空白。</exception>
        /// <exception cref="ArgumentNullException">projector 为 null。</exception>
        public async Task<List<TResult>> QuerySqlAsync<TResult>(
            string sql,
            Func<SqliteDataReader, TResult> projector,
            object? parameters = null)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(sql);
            ArgumentNullException.ThrowIfNull(projector);
            await using var connection = await OpenConnectionAsync().ConfigureAwait(false);
            await using var command = connection.CreateCommand();
            command.CommandText = sql;
            SqliteUtils.AddParameters(command, parameters);
            var list = new List<TResult>();
            await using var reader = await command.ExecuteReaderAsync().ConfigureAwait(false);
            while (await reader.ReadAsync().ConfigureAwait(false))
            {
                list.Add(projector(reader));
            }
            return list;
        }
        /// <summary>
        /// 执行非查询 SQL（如 INSERT、UPDATE、DELETE）。
        /// </summary>
        /// <param name="sql">SQL 语句。</param>
        /// <param name="parameters">参数对象。</param>
        /// <returns>受影响的行数。</returns>
        /// <exception cref="ArgumentException">sql 为 null 或空白。</exception>
        public async Task<int> ExecuteAsync(string sql, object? parameters = null)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(sql);
            await _writeLock.WaitAsync().ConfigureAwait(false);
            try
            {
                await using var connection = await OpenConnectionAsync().ConfigureAwait(false);
                await using var command = connection.CreateCommand();
                command.CommandText = sql;
                SqliteUtils.AddParameters(command, parameters);
                return await command.ExecuteNonQueryAsync().ConfigureAwait(false);
            }
            finally
            {
                _writeLock.Release();
            }
        }
        /// <summary>
        /// 执行标量查询，返回结果的第一行第一列。
        /// </summary>
        /// <param name="sql">SQL 语句。</param>
        /// <param name="parameters">参数对象。</param>
        /// <returns>标量值，可能为 null。</returns>
        /// <exception cref="ArgumentException">sql 为 null 或空白。</exception>
        public async Task<object?> ExecuteScalarAsync(string sql, object? parameters = null)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(sql);
            await using var connection = await OpenConnectionAsync().ConfigureAwait(false);
            await using var command = connection.CreateCommand();
            command.CommandText = sql;
            SqliteUtils.AddParameters(command, parameters);
            return await command.ExecuteScalarAsync().ConfigureAwait(false);
        }
    }
}
