namespace ReluctersteSDK.Sqlite.Migrations
{
    /// <summary>
    /// <see cref="SqliteMigrationContext"/> 的结构变更助手。
    /// </summary>
    /// <remarks>
    /// 这些方法只是把常见 DDL 拼成字符串；表名与列名会自动按标识符引用，
    /// 但 <c>type</c>、<c>defaultValueSql</c>、<c>whereSql</c> 是原始 SQL 片段，
    /// 只应传入代码中写死的值。
    /// </remarks>
    public sealed partial class SqliteMigrationContext
    {
        /// <summary>
        /// 按实体特性映射创建表（若不存在）。
        /// </summary>
        /// <typeparam name="T">实体类型。</typeparam>
        /// <param name="cancellationToken">取消令牌。</param>
        public Task<int> CreateTableIfNotExistsAsync<T>(
            CancellationToken cancellationToken = default)
            where T : new()
        {
            var sql = SqliteSchemaSql.BuildCreateTable(EntityMapCache.Get<T>());
            return ExecuteAsync(sql, cancellationToken: cancellationToken);
        }

        /// <summary>
        /// 向已有表添加列。
        /// </summary>
        /// <param name="tableName">表名。</param>
        /// <param name="columnName">列名。</param>
        /// <param name="type">SQLite 类型片段，例如 <c>INTEGER</c>、<c>TEXT</c>。</param>
        /// <param name="notNull">是否添加 <c>NOT NULL</c>。非空表需要同时提供默认值。</param>
        /// <param name="defaultValueSql">默认值的 SQL 文本，例如 <c>0</c>、<c>'unknown'</c>。</param>
        /// <param name="cancellationToken">取消令牌。</param>
        public Task<int> AddColumnAsync(
            string tableName,
            string columnName,
            string type,
            bool notNull = false,
            string? defaultValueSql = null,
            CancellationToken cancellationToken = default)
        {
            RequireName(tableName, nameof(tableName));
            RequireName(columnName, nameof(columnName));
            RequireName(type, nameof(type));

            var sql = $"ALTER TABLE {SqliteUtils.QuoteIdentifier(tableName)} " +
                      $"ADD COLUMN {SqliteUtils.QuoteIdentifier(columnName)} {type}";

            if (notNull)
                sql += " NOT NULL";

            if (!string.IsNullOrWhiteSpace(defaultValueSql))
                sql += $" DEFAULT {defaultValueSql}";

            sql += ";";
            return ExecuteAsync(sql, cancellationToken: cancellationToken);
        }

        /// <summary>
        /// 重命名列（需要 SQLite 3.25+）。
        /// </summary>
        /// <param name="tableName">表名。</param>
        /// <param name="oldColumnName">旧列名。</param>
        /// <param name="newColumnName">新列名。</param>
        /// <param name="cancellationToken">取消令牌。</param>
        public Task<int> RenameColumnAsync(
            string tableName,
            string oldColumnName,
            string newColumnName,
            CancellationToken cancellationToken = default)
        {
            RequireName(tableName, nameof(tableName));
            RequireName(oldColumnName, nameof(oldColumnName));
            RequireName(newColumnName, nameof(newColumnName));

            var sql = $"ALTER TABLE {SqliteUtils.QuoteIdentifier(tableName)} " +
                      $"RENAME COLUMN {SqliteUtils.QuoteIdentifier(oldColumnName)} " +
                      $"TO {SqliteUtils.QuoteIdentifier(newColumnName)};";
            return ExecuteAsync(sql, cancellationToken: cancellationToken);
        }

        /// <summary>
        /// 删除列（需要 SQLite 3.35+）。
        /// </summary>
        /// <param name="tableName">表名。</param>
        /// <param name="columnName">列名。</param>
        /// <param name="cancellationToken">取消令牌。</param>
        public Task<int> DropColumnAsync(
            string tableName,
            string columnName,
            CancellationToken cancellationToken = default)
        {
            RequireName(tableName, nameof(tableName));
            RequireName(columnName, nameof(columnName));

            var sql = $"ALTER TABLE {SqliteUtils.QuoteIdentifier(tableName)} " +
                      $"DROP COLUMN {SqliteUtils.QuoteIdentifier(columnName)};";
            return ExecuteAsync(sql, cancellationToken: cancellationToken);
        }

        /// <summary>
        /// 重命名表。
        /// </summary>
        /// <param name="oldTableName">旧表名。</param>
        /// <param name="newTableName">新表名。</param>
        /// <param name="cancellationToken">取消令牌。</param>
        public Task<int> RenameTableAsync(
            string oldTableName,
            string newTableName,
            CancellationToken cancellationToken = default)
        {
            RequireName(oldTableName, nameof(oldTableName));
            RequireName(newTableName, nameof(newTableName));

            var sql = $"ALTER TABLE {SqliteUtils.QuoteIdentifier(oldTableName)} " +
                      $"RENAME TO {SqliteUtils.QuoteIdentifier(newTableName)};";
            return ExecuteAsync(sql, cancellationToken: cancellationToken);
        }

        /// <summary>
        /// 删除表（若存在）。
        /// </summary>
        /// <param name="tableName">表名。</param>
        /// <param name="cancellationToken">取消令牌。</param>
        public Task<int> DropTableAsync(
            string tableName,
            CancellationToken cancellationToken = default)
        {
            RequireName(tableName, nameof(tableName));
            return ExecuteAsync(
                $"DROP TABLE IF EXISTS {SqliteUtils.QuoteIdentifier(tableName)};",
                cancellationToken: cancellationToken);
        }

        /// <summary>
        /// 创建索引（若不存在）。
        /// </summary>
        /// <param name="indexName">索引名。</param>
        /// <param name="tableName">表名。</param>
        /// <param name="columns">索引列，按顺序排列。</param>
        /// <param name="unique">是否创建唯一索引。</param>
        /// <param name="whereSql">可选的过滤条件（不含 <c>WHERE</c>），用于部分索引。</param>
        /// <param name="cancellationToken">取消令牌。</param>
        public Task<int> CreateIndexAsync(
            string indexName,
            string tableName,
            IEnumerable<string> columns,
            bool unique = false,
            string? whereSql = null,
            CancellationToken cancellationToken = default)
        {
            RequireName(indexName, nameof(indexName));
            RequireName(tableName, nameof(tableName));
            ArgumentNullException.ThrowIfNull(columns);

            var columnList = columns.ToList();

            if (columnList.Count == 0)
                throw new ArgumentException("At least one index column is required.", nameof(columns));

            foreach (var column in columnList)
                RequireName(column, nameof(columns));

            var columnSql = string.Join(", ", columnList.Select(SqliteUtils.QuoteIdentifier));
            var sql = $"CREATE {(unique ? "UNIQUE " : string.Empty)}INDEX IF NOT EXISTS " +
                      $"{SqliteUtils.QuoteIdentifier(indexName)} " +
                      $"ON {SqliteUtils.QuoteIdentifier(tableName)} ({columnSql})";

            if (!string.IsNullOrWhiteSpace(whereSql))
                sql += $" WHERE {whereSql}";

            sql += ";";
            return ExecuteAsync(sql, cancellationToken: cancellationToken);
        }

        /// <summary>
        /// 删除索引（若存在）。
        /// </summary>
        /// <param name="indexName">索引名。</param>
        /// <param name="cancellationToken">取消令牌。</param>
        public Task<int> DropIndexAsync(
            string indexName,
            CancellationToken cancellationToken = default)
        {
            RequireName(indexName, nameof(indexName));
            return ExecuteAsync(
                $"DROP INDEX IF EXISTS {SqliteUtils.QuoteIdentifier(indexName)};",
                cancellationToken: cancellationToken);
        }
    }
}
