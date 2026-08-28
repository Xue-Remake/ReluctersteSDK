namespace ReluctersteSDK.SimpleSqliteORM.RowMod
{
    public class RowDbContext : IAsyncDisposable
    {
        public SqliteDataBase Database { get; }

        public RowDbContext(SqliteDataBase db)
        {
            Database = db;
        }

        public RowTable Table(string tableName) => new RowTable(Database, tableName);
        public RowTable this[string tableName] => Table(tableName);

        public ValueTask DisposeAsync() => Database.DisposeAsync();
    }
}
