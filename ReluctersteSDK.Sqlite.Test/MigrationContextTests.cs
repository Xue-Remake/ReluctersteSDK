using ReluctersteSDK.Sqlite.Migrations;

namespace ReluctersteSDK.Sqlite.Test
{
    /// <summary>
    /// <see cref="SqliteMigrationContext"/> 的 DDL 助手与结构探测测试。
    /// </summary>
    public sealed class MigrationContextTests
    {
        [Fact]
        public async Task Context_IntrospectionAndSchemaHelpers()
        {
            await using var fixture = new TestDatabase();

            var tableExists = false;
            var columnExists = false;
            var missingColumnExists = true;
            IReadOnlyList<SqliteColumnInfo> columns = Array.Empty<SqliteColumnInfo>();

            var migrations = new[]
            {
                TestDatabase.Migration(1, "schema helpers", async (context, token) =>
                {
                    await context.CreateTableIfNotExistsAsync<ItemEntity>(token);
                    await context.AddColumnAsync(
                        "items",
                        "level",
                        "INTEGER",
                        defaultValueSql: "1",
                        cancellationToken: token);
                    await context.CreateIndexAsync(
                        "ix_items_name",
                        "items",
                        new[] { "name" },
                        cancellationToken: token);
                    await context.CreateIndexAsync(
                        "ux_items_name",
                        "items",
                        new[] { "name" },
                        unique: true,
                        cancellationToken: token);

                    tableExists = await context.TableExistsAsync("items", token);
                    columnExists = await context.ColumnExistsAsync("items", "level", token);
                    missingColumnExists = await context.ColumnExistsAsync("items", "missing", token);
                    columns = await context.GetColumnsAsync("items", token);
                })
            };

            await fixture.Database.Migrations.MigrateAsync(migrations);

            Assert.True(tableExists);
            Assert.True(columnExists);
            Assert.False(missingColumnExists);
            Assert.Contains(columns, c => c.Name == "id" && c.PrimaryKeyOrder == 1);
            Assert.Contains(columns, c => c.Name == "level" && c.DefaultValue == "1");

            var indexCount = await fixture.Database.ExecuteScalarAsync(
                "SELECT COUNT(*) FROM sqlite_master WHERE type = 'index' AND name = 'ix_items_name';");
            Assert.Equal(1L, Convert.ToInt64(indexCount));

            var uniqueSql = await fixture.Database.ExecuteScalarAsync(
                "SELECT sql FROM sqlite_master WHERE type = 'index' AND name = 'ux_items_name';");
            Assert.Contains("UNIQUE", uniqueSql!.ToString(), StringComparison.OrdinalIgnoreCase);
        }

        [Fact]
        public async Task Context_CreateTableIfNotExists_WorksWithAttributeMapping()
        {
            await using var fixture = new TestDatabase();

            var migrations = new[]
            {
                TestDatabase.Migration(1, "create items from entity", async (context, token) =>
                {
                    await context.CreateTableIfNotExistsAsync<ItemEntity>(token);
                })
            };

            await fixture.Database.Migrations.MigrateAsync(migrations);

            await fixture.Database.InsertAsync(new ItemEntity
            {
                Id = 1,
                Name = "alpha",
                Score = 7
            });

            var item = await fixture.Database.GetByKeyAsync<ItemEntity>(1);

            Assert.NotNull(item);
            Assert.Equal("alpha", item!.Name);
            Assert.Equal(7, item.Score);
        }

        [Fact]
        public async Task Context_GetColumns_ReturnsMetadata()
        {
            await using var fixture = new TestDatabase();

            IReadOnlyList<SqliteColumnInfo> columns = Array.Empty<SqliteColumnInfo>();

            var migrations = new[]
            {
                TestDatabase.Migration(1, "create metadata table", async (context, token) =>
                {
                    await context.ExecuteAsync(
                        "CREATE TABLE meta (" +
                        "\"id\" INTEGER NOT NULL PRIMARY KEY, " +
                        "\"name\" TEXT NOT NULL DEFAULT 'x', " +
                        "\"note\" TEXT);",
                        cancellationToken: token);

                    columns = await context.GetColumnsAsync("meta", token);
                })
            };

            await fixture.Database.Migrations.MigrateAsync(migrations);

            var id = Assert.Single(columns, c => c.Name == "id");
            Assert.True(id.IsNotNull);
            Assert.Equal(1, id.PrimaryKeyOrder);

            var name = Assert.Single(columns, c => c.Name == "name");
            Assert.True(name.IsNotNull);
            Assert.Equal("'x'", name.DefaultValue);

            var note = Assert.Single(columns, c => c.Name == "note");
            Assert.False(note.IsNotNull);
            Assert.Null(note.DefaultValue);
        }

        [Fact]
        public async Task Context_RenameAndDropColumn()
        {
            await using var fixture = new TestDatabase();

            var migrations = new[]
            {
                TestDatabase.Migration(1, "alter columns", async (context, token) =>
                {
                    await context.ExecuteAsync(
                        "CREATE TABLE items (id INTEGER, old_name TEXT, obsolete TEXT);",
                        cancellationToken: token);

                    await context.RenameColumnAsync("items", "old_name", "name", token);
                    await context.DropColumnAsync("items", "obsolete", token);
                })
            };

            await fixture.Database.Migrations.MigrateAsync(migrations);

            var columns = await fixture.GetColumnNamesAsync("items");

            Assert.Equal(new[] { "id", "name" }, columns);
        }

        [Fact]
        public async Task Context_ExecuteWithParametersAndProjection()
        {
            await using var fixture = new TestDatabase();

            var migrations = new[]
            {
                TestDatabase.Migration(1, "seed rows", async (context, token) =>
                {
                    await context.ExecuteAsync(
                        "CREATE TABLE items (id INTEGER PRIMARY KEY, name TEXT NOT NULL);",
                        cancellationToken: token);

                    await context.ExecuteAsync(
                        "INSERT INTO items (id, name) VALUES (@id, @name);",
                        new { id = 1, name = "alpha" },
                        token);

                    await context.ExecuteAsync(
                        "INSERT INTO items (id, name) VALUES (@id, @name);",
                        new { id = 2, name = "beta" },
                        token);
                })
            };

            await fixture.Database.Migrations.MigrateAsync(migrations);

            var count = await fixture.Database.ExecuteScalarAsync("SELECT COUNT(*) FROM items;");
            Assert.Equal(2L, Convert.ToInt64(count));

            var rows = await fixture.Database.QuerySqlAsync(
                "SELECT id, name FROM items ORDER BY id;",
                reader => (Id: reader.GetInt64(0), Name: reader.GetString(1)));

            Assert.Equal(new[] { (1L, "alpha"), (2L, "beta") }, rows);
        }

        [Fact]
        public async Task Context_RenameTable()
        {
            await using var fixture = new TestDatabase();

            var migrations = new[]
            {
                TestDatabase.Migration(1, "rename table", async (context, token) =>
                {
                    await context.ExecuteAsync(
                        "CREATE TABLE old_items (id INTEGER PRIMARY KEY);",
                        cancellationToken: token);
                    await context.RenameTableAsync("old_items", "items", token);
                })
            };

            await fixture.Database.Migrations.MigrateAsync(migrations);

            Assert.True(await fixture.TableExistsAsync("items"));
            Assert.False(await fixture.TableExistsAsync("old_items"));
        }
    }
}
