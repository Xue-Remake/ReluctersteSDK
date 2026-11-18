using ReluctersteSDK.Sqlite.Migrations;

namespace ReluctersteSDK.Sqlite.Test
{
    /// <summary>
    /// 每个测试独立的临时数据库；释放时删除整个目录。
    /// </summary>
    internal sealed class TestDatabase : IAsyncDisposable
    {
        public TestDatabase()
        {
            Folder = Path.Combine(
                Path.GetTempPath(),
                "ReluctersteSDK.Sqlite.Test",
                Guid.NewGuid().ToString("N"));

            Database = new SqliteDataBase(Folder, "test.db");
        }

        /// <summary>获取临时目录。</summary>
        public string Folder { get; }

        /// <summary>获取数据库实例。</summary>
        public SqliteDataBase Database { get; }

        public async ValueTask DisposeAsync()
        {
            await Database.DisposeAsync();

            try
            {
                if (Directory.Exists(Folder))
                    Directory.Delete(Folder, recursive: true);
            }
            catch
            {
                // 测试清理失败不应掩盖断言结果。
            }
        }

        /// <summary>构造一个委托形式的迁移。</summary>
        public static SqliteMigration Migration(
            long version,
            string description,
            Func<SqliteMigrationContext, CancellationToken, Task> up,
            Func<SqliteMigrationContext, CancellationToken, Task>? down = null)
        {
            return SqliteMigration.Create(version, description, up, down);
        }

        /// <summary>判断表是否存在。</summary>
        public async Task<bool> TableExistsAsync(string tableName)
        {
            var value = await Database.ExecuteScalarAsync(
                "SELECT COUNT(*) FROM sqlite_master WHERE type = 'table' AND name = @name;",
                new { name = tableName });

            return Convert.ToInt64(value) > 0;
        }

        /// <summary>读取表的列名。</summary>
        public async Task<List<string>> GetColumnNamesAsync(string tableName)
        {
            var quoted = "\"" + tableName.Replace("\"", "\"\"") + "\"";
            return await Database.QuerySqlAsync(
                $"PRAGMA table_info({quoted});",
                reader => reader.GetString(1));
        }
    }
}
