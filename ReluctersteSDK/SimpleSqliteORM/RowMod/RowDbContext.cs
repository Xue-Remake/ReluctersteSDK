namespace ReluctersteSDK.SimpleSqliteORM.RowMod
{
    /// <summary>
    /// 提供纯行模式（Row-based）的数据库上下文，无需预先定义实体类即可操作数据表。
    /// </summary>
    public class RowDbContext : IAsyncDisposable
    {
        /// <summary>
        /// 获取关联的底层 SQLite 数据库操作实例。
        /// </summary>
        public SqliteDataBase Database { get; }
        /// <summary>
        /// 初始化 <see cref="RowDbContext"/> 实例。
        /// </summary>
        /// <param name="db">底层 SQLite 数据库操作实例。</param>
        public RowDbContext(SqliteDataBase db)
        {
            Database = db;
        }
        /// <summary>
        /// 获取指定名称的数据表操作对象。
        /// </summary>
        /// <param name="tableName">表名。</param>
        /// <returns>数据表操作对象 <see cref="RowTable"/>。</returns>
        public RowTable Table(string tableName) => new RowTable(Database, tableName);
        /// <summary>
        /// 通过索引器获取指定名称的数据表操作对象。
        /// </summary>
        /// <param name="tableName">表名。</param>
        /// <returns>数据表操作对象 <see cref="RowTable"/>。</returns>
        public RowTable this[string tableName] => Table(tableName);

        /// <inheritdoc />
        public ValueTask DisposeAsync() => Database.DisposeAsync();
    }
}
