using ReluctersteSDK.Sqlite.Migrations;

namespace ReluctersteSDK.Sqlite.Test
{
    /// <summary>
    /// <see cref="SqliteMigrationRunner"/> 的升级、幂等、失败回滚与回滚测试。
    /// </summary>
    public sealed class MigrationRunnerTests
    {
        /// <summary>创建一个顺序依赖的三步迁移：建表 → 插入 → 加列。</summary>
        private static SqliteMigration[] CreateBasicMigrations()
        {
            return new[]
            {
                TestDatabase.Migration(1, "create items", async (context, token) =>
                {
                    await context.ExecuteAsync(
                        "CREATE TABLE items (id INTEGER PRIMARY KEY, name TEXT NOT NULL);",
                        cancellationToken: token);
                }),

                TestDatabase.Migration(2, "seed items", async (context, token) =>
                {
                    await context.ExecuteAsync(
                        "INSERT INTO items (id, name) VALUES (1, 'alpha');",
                        cancellationToken: token);
                }),

                TestDatabase.Migration(3, "add score", async (context, token) =>
                {
                    await context.AddColumnAsync(
                        "items",
                        "score",
                        "INTEGER",
                        defaultValueSql: "0",
                        cancellationToken: token);
                })
            };
        }

        [Fact]
        public async Task MigrateAsync_AppliesPendingMigrationsInOrder()
        {
            await using var fixture = new TestDatabase();

            var result = await fixture.Database.Migrations.MigrateAsync(CreateBasicMigrations());

            Assert.Equal(3, result.AppliedCount);
            Assert.Equal(0, result.PreviousVersion);
            Assert.Equal(3, result.CurrentVersion);
            Assert.Equal(new long[] { 1, 2, 3 }, result.Applied.Select(m => m.Version));

            var names = await fixture.Database.QuerySqlAsync(
                "SELECT name FROM items;",
                reader => reader.GetString(0));
            Assert.Equal(new[] { "alpha" }, names);

            Assert.Contains("score", await fixture.GetColumnNamesAsync("items"));

            var applied = await fixture.Database.Migrations.GetAppliedMigrationsAsync();
            Assert.Equal(3, applied.Count);
            Assert.Equal("add score", applied[2].Description);
            Assert.True(applied.All(m => m.ExecutionMilliseconds >= 0));
        }

        [Fact]
        public async Task MigrateAsync_SecondRunSkipsAppliedMigrations()
        {
            await using var fixture = new TestDatabase();
            var migrations = CreateBasicMigrations();

            await fixture.Database.Migrations.MigrateAsync(migrations);
            var second = await fixture.Database.Migrations.MigrateAsync(migrations);

            Assert.Empty(second.Applied);
            Assert.Equal(3, second.SkippedCount);
            Assert.Equal(3, second.PreviousVersion);
            Assert.Equal(3, second.CurrentVersion);
            Assert.False(second.HasChanges);
        }

        [Fact]
        public async Task MigrateAsync_FailureRollsBackEntireMigration()
        {
            await using var fixture = new TestDatabase();

            var migrations = new[]
            {
                TestDatabase.Migration(1, "failing migration", async (context, token) =>
                {
                    await context.ExecuteAsync(
                        "CREATE TABLE transient (id INTEGER);",
                        cancellationToken: token);

                    throw new InvalidOperationException("boom");
                })
            };

            var exception = await Assert.ThrowsAsync<SqliteMigrationException>(
                () => fixture.Database.Migrations.MigrateAsync(migrations));

            Assert.Equal(1, exception.Version);
            Assert.Equal(MigrationDirection.Up, exception.Direction);
            Assert.IsType<InvalidOperationException>(exception.InnerException);

            Assert.False(await fixture.TableExistsAsync("transient"));
            Assert.Empty(await fixture.Database.Migrations.GetAppliedMigrationsAsync());
        }

        [Fact]
        public async Task MigrateAsync_DuplicateVersionsThrow()
        {
            await using var fixture = new TestDatabase();

            var migrations = new[]
            {
                TestDatabase.Migration(1, "first", (_, _) => Task.CompletedTask),
                TestDatabase.Migration(1, "second", (_, _) => Task.CompletedTask)
            };

            await Assert.ThrowsAsync<ArgumentException>(async () =>
                await fixture.Database.Migrations.MigrateAsync(migrations));
        }

        [Fact]
        public async Task MigrateAsync_OutOfOrderMigrationThrows()
        {
            await using var fixture = new TestDatabase();

            var first = TestDatabase.Migration(1, "one", async (context, token) =>
            {
                await context.ExecuteAsync("CREATE TABLE t1 (id INTEGER);", cancellationToken: token);
            });

            var third = TestDatabase.Migration(3, "three", async (context, token) =>
            {
                await context.ExecuteAsync("CREATE TABLE t3 (id INTEGER);", cancellationToken: token);
            });

            await fixture.Database.Migrations.MigrateAsync(new[] { first, third });

            var second = TestDatabase.Migration(2, "two", async (context, token) =>
            {
                await context.ExecuteAsync("CREATE TABLE t2 (id INTEGER);", cancellationToken: token);
            });

            var exception = await Assert.ThrowsAsync<InvalidOperationException>(async () =>
                await fixture.Database.Migrations.MigrateAsync(new[] { first, second, third }));

            Assert.Contains("out of order", exception.Message, StringComparison.OrdinalIgnoreCase);
            Assert.False(await fixture.TableExistsAsync("t2"));
        }

        [Fact]
        public async Task RollbackAsync_RevertsMigrationsInReverseOrder()
        {
            await using var fixture = new TestDatabase();
            var migrations = CreateReversibleMigrations();

            await fixture.Database.Migrations.MigrateAsync(migrations);
            var result = await fixture.Database.Migrations.RollbackAsync(migrations);

            Assert.Equal(3, result.RevertedCount);
            Assert.Equal(new long[] { 3, 2, 1 }, result.Reverted.Select(m => m.Version));
            Assert.Equal(3, result.PreviousVersion);
            Assert.Equal(0, result.CurrentVersion);
            Assert.False(await fixture.TableExistsAsync("items"));
            Assert.Empty(await fixture.Database.Migrations.GetAppliedMigrationsAsync());
        }

        [Fact]
        public async Task RollbackAsync_ToTargetVersionKeepsEarlierMigrations()
        {
            await using var fixture = new TestDatabase();
            var migrations = CreateReversibleMigrations();

            await fixture.Database.Migrations.MigrateAsync(migrations);
            var result = await fixture.Database.Migrations.RollbackAsync(migrations, targetVersion: 1);

            Assert.Equal(new long[] { 3, 2 }, result.Reverted.Select(m => m.Version));
            Assert.Equal(1, result.CurrentVersion);
            Assert.True(await fixture.TableExistsAsync("items"));

            var columns = await fixture.GetColumnNamesAsync("items");
            Assert.Contains("name", columns);
            Assert.DoesNotContain("score", columns);

            var applied = await fixture.Database.Migrations.GetAppliedMigrationsAsync();
            Assert.Equal(new long[] { 1 }, applied.Select(m => m.Version));
        }

        [Fact]
        public async Task RollbackAsync_MissingDownKeepsHistoryAndSchema()
        {
            await using var fixture = new TestDatabase();

            var migrations = new[]
            {
                TestDatabase.Migration(1, "create without down", async (context, token) =>
                {
                    await context.ExecuteAsync(
                        "CREATE TABLE items (id INTEGER PRIMARY KEY);",
                        cancellationToken: token);
                })
            };

            await fixture.Database.Migrations.MigrateAsync(migrations);

            var exception = await Assert.ThrowsAsync<SqliteMigrationException>(
                () => fixture.Database.Migrations.RollbackAsync(migrations));

            Assert.Equal(MigrationDirection.Down, exception.Direction);
            Assert.IsType<NotSupportedException>(exception.InnerException);
            Assert.True(await fixture.TableExistsAsync("items"));
            Assert.Single(await fixture.Database.Migrations.GetAppliedMigrationsAsync());
        }

        [Fact]
        public async Task MigrateAsync_PreCanceledTokenThrows()
        {
            await using var fixture = new TestDatabase();
            using var source = new CancellationTokenSource();
            source.Cancel();

            await Assert.ThrowsAnyAsync<OperationCanceledException>(() =>
                fixture.Database.Migrations.MigrateAsync(
                    CreateBasicMigrations(),
                    source.Token));
        }

        [Fact]
        public async Task MigrateAsync_EmptyCollectionIsNoOp()
        {
            await using var fixture = new TestDatabase();

            var result = await fixture.Database.Migrations.MigrateAsync(Array.Empty<SqliteMigration>());

            Assert.Empty(result.Applied);
            Assert.Equal(0, result.CurrentVersion);
        }

        [Fact]
        public async Task MigrateAsync_CustomHistoryTableName()
        {
            await using var fixture = new TestDatabase();
            var runner = new SqliteMigrationRunner(fixture.Database, "__my_history");

            await runner.MigrateAsync(CreateBasicMigrations());

            Assert.True(await fixture.TableExistsAsync("__my_history"));
        }

        /// <summary>创建可完整回滚的三步迁移。</summary>
        private static SqliteMigration[] CreateReversibleMigrations()
        {
            return new[]
            {
                TestDatabase.Migration(
                    1,
                    "create items",
                    async (context, token) =>
                    {
                        await context.ExecuteAsync(
                            "CREATE TABLE items (id INTEGER PRIMARY KEY, name TEXT NOT NULL);",
                            cancellationToken: token);
                    },
                    async (context, token) =>
                    {
                        await context.DropTableAsync("items", token);
                    }),

                TestDatabase.Migration(
                    2,
                    "add score",
                    async (context, token) =>
                    {
                        await context.AddColumnAsync(
                            "items",
                            "score",
                            "INTEGER",
                            defaultValueSql: "0",
                            cancellationToken: token);
                    },
                    async (context, token) =>
                    {
                        await context.DropColumnAsync("items", "score", token);
                    }),

                TestDatabase.Migration(
                    3,
                    "index name",
                    async (context, token) =>
                    {
                        await context.CreateIndexAsync(
                            "ix_items_name",
                            "items",
                            new[] { "name" },
                            cancellationToken: token);
                    },
                    async (context, token) =>
                    {
                        await context.DropIndexAsync("ix_items_name", token);
                    })
            };
        }
    }
}
