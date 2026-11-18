using System.Diagnostics;
using Microsoft.Data.Sqlite;

namespace ReluctersteSDK.Sqlite.Migrations
{
    /// <summary>
    /// 版本化迁移执行器：按版本号升序执行未应用的迁移，并把结果记录到历史表。
    /// </summary>
    /// <remarks>
    /// <para>
    /// 执行期间持有数据库的写锁，因此与普通写操作互斥；每个迁移在独立事务中执行，
    /// 失败时整个迁移回滚且不会写入历史表。
    /// </para>
    /// <para>
    /// 默认历史表名为 <c>__schema_migrations</c>，可通过构造函数修改。
    /// 已有迁移的版本号不要修改，也不要插入版本号小于当前最大值的“补丁”迁移
    /// （执行器会拒绝乱序迁移）。
    /// </para>
    /// </remarks>
    public sealed partial class SqliteMigrationRunner
    {
        private readonly SqliteDataBase _database;

        /// <summary>
        /// 创建迁移执行器。通常直接使用 <see cref="SqliteDataBase.Migrations"/>。
        /// </summary>
        /// <param name="database">目标数据库。</param>
        /// <param name="historyTableName">迁移历史表名，默认 <c>__schema_migrations</c>。</param>
        public SqliteMigrationRunner(
            SqliteDataBase database,
            string historyTableName = "__schema_migrations")
        {
            _database = database ?? throw new ArgumentNullException(nameof(database));

            if (string.IsNullOrWhiteSpace(historyTableName))
                throw new ArgumentException("History table name cannot be null or empty.", nameof(historyTableName));

            HistoryTableName = historyTableName;
        }

        /// <summary>获取迁移历史表名。</summary>
        public string HistoryTableName { get; }

        /// <summary>
        /// 读取已应用的迁移，按版本升序排列。
        /// </summary>
        /// <param name="cancellationToken">取消令牌。</param>
        public Task<IReadOnlyList<AppliedMigration>> GetAppliedMigrationsAsync(
            CancellationToken cancellationToken = default)
        {
            return _database.ExecuteWithWriteLockAsync(
                async (connection, token) =>
                {
                    await EnsureHistoryTableAsync(connection, token).ConfigureAwait(false);
                    var rows = await ReadAppliedMigrationsAsync(connection, token).ConfigureAwait(false);
                    return (IReadOnlyList<AppliedMigration>)rows;
                },
                cancellationToken);
        }

        /// <summary>
        /// 执行所有尚未应用的迁移。
        /// </summary>
        /// <param name="migrations">迁移集合；内部会按版本号排序并校验重复。</param>
        /// <param name="cancellationToken">取消令牌。</param>
        /// <returns>本次执行结果。</returns>
        /// <exception cref="ArgumentException">迁移为空、版本重复或说明为空。</exception>
        /// <exception cref="ArgumentOutOfRangeException">版本号小于等于 0。</exception>
        /// <exception cref="InvalidOperationException">出现版本号小于当前最大已应用版本的乱序迁移。</exception>
        /// <exception cref="SqliteMigrationException">某个迁移执行失败（已回滚）。</exception>
        public Task<MigrationResult> MigrateAsync(
            IEnumerable<SqliteMigration> migrations,
            CancellationToken cancellationToken = default)
        {
            var ordered = Normalize(migrations);
            return _database.ExecuteWithWriteLockAsync(
                (connection, token) => MigrateCoreAsync(connection, ordered, token),
                cancellationToken);
        }

        /// <summary>
        /// 回滚版本号大于 <paramref name="targetVersion"/> 的迁移（按版本降序）。
        /// </summary>
        /// <param name="migrations">迁移集合，必须包含待回滚迁移的定义。</param>
        /// <param name="targetVersion">回滚到的目标版本；0 表示全部回滚。</param>
        /// <param name="cancellationToken">取消令牌。</param>
        /// <returns>本次回滚结果。</returns>
        /// <exception cref="InvalidOperationException">待回滚迁移没有提供定义或未实现 DownAsync。</exception>
        /// <exception cref="SqliteMigrationException">某个回滚执行失败（已回滚）。</exception>
        public Task<MigrationResult> RollbackAsync(
            IEnumerable<SqliteMigration> migrations,
            long targetVersion = 0,
            CancellationToken cancellationToken = default)
        {
            if (targetVersion < 0)
                throw new ArgumentOutOfRangeException(nameof(targetVersion), "Target version cannot be negative.");

            var ordered = Normalize(migrations);
            return _database.ExecuteWithWriteLockAsync(
                (connection, token) => RollbackCoreAsync(connection, ordered, targetVersion, token),
                cancellationToken);
        }

        /// <summary>在已打开的连接上执行升级流程。</summary>
        private async Task<MigrationResult> MigrateCoreAsync(
            SqliteConnection connection,
            IReadOnlyList<SqliteMigration> migrations,
            CancellationToken cancellationToken)
        {
            await EnsureHistoryTableAsync(connection, cancellationToken).ConfigureAwait(false);

            var applied = await ReadAppliedMigrationsAsync(connection, cancellationToken).ConfigureAwait(false);
            var appliedVersions = new HashSet<long>(applied.Select(x => x.Version));
            var previousVersion = applied.Count == 0 ? 0 : applied.Max(x => x.Version);

            var pending = migrations
                .Where(m => !appliedVersions.Contains(m.Version))
                .ToList();

            var appliedNow = new List<AppliedMigration>(pending.Count);
            var currentVersion = previousVersion;

            foreach (var migration in pending)
            {
                if (migration.Version < previousVersion)
                {
                    throw new InvalidOperationException(
                        $"Migration {migration.Version} ({migration.Description}) is out of order: " +
                        $"the database is already at version {previousVersion}. " +
                        "Out-of-order migrations are not supported; assign a version greater than " +
                        "the current maximum.");
                }

                var record = await ApplyOneAsync(connection, migration, cancellationToken)
                    .ConfigureAwait(false);
                appliedNow.Add(record);
                currentVersion = migration.Version;
            }

            return new MigrationResult(
                appliedNow,
                Array.Empty<AppliedMigration>(),
                skippedCount: migrations.Count - pending.Count,
                previousVersion,
                currentVersion);
        }

        /// <summary>在一个事务中执行单个迁移并写入历史记录。</summary>
        private async Task<AppliedMigration> ApplyOneAsync(
            SqliteConnection connection,
            SqliteMigration migration,
            CancellationToken cancellationToken)
        {
            var stopwatch = Stopwatch.StartNew();
            await using var transaction = (SqliteTransaction)await connection
                .BeginTransactionAsync(cancellationToken)
                .ConfigureAwait(false);

            try
            {
                var context = new SqliteMigrationContext(
                    connection,
                    transaction,
                    _database.DatabasePath);

                await migration.UpAsync(context, cancellationToken).ConfigureAwait(false);
                stopwatch.Stop();

                var appliedAt = DateTimeOffset.UtcNow;
                await InsertHistoryAsync(
                    connection,
                    transaction,
                    migration,
                    appliedAt,
                    stopwatch.ElapsedMilliseconds,
                    cancellationToken).ConfigureAwait(false);

                await transaction.CommitAsync(cancellationToken).ConfigureAwait(false);

                return new AppliedMigration(
                    migration.Version,
                    migration.Description,
                    appliedAt,
                    stopwatch.ElapsedMilliseconds);
            }
            catch (OperationCanceledException)
            {
                await SafeRollbackAsync(transaction).ConfigureAwait(false);
                throw;
            }
            catch (Exception exception)
            {
                await SafeRollbackAsync(transaction).ConfigureAwait(false);
                throw new SqliteMigrationException(
                    migration.Version,
                    migration.Description,
                    MigrationDirection.Up,
                    exception);
            }
        }

        /// <summary>在已打开的连接上执行回滚流程。</summary>
        private async Task<MigrationResult> RollbackCoreAsync(
            SqliteConnection connection,
            IReadOnlyList<SqliteMigration> migrations,
            long targetVersion,
            CancellationToken cancellationToken)
        {
            await EnsureHistoryTableAsync(connection, cancellationToken).ConfigureAwait(false);

            var applied = await ReadAppliedMigrationsAsync(connection, cancellationToken).ConfigureAwait(false);
            var previousVersion = applied.Count == 0 ? 0 : applied.Max(x => x.Version);
            var definitions = migrations.ToDictionary(m => m.Version);
            var remaining = applied.ToList();
            var reverted = new List<AppliedMigration>();

            foreach (var record in applied.OrderByDescending(x => x.Version))
            {
                if (record.Version <= targetVersion)
                    break;

                if (!definitions.TryGetValue(record.Version, out var migration))
                {
                    throw new InvalidOperationException(
                        $"Cannot roll back migration {record.Version} ({record.Description}): " +
                        "its definition was not supplied to RollbackAsync.");
                }

                await RevertOneAsync(connection, migration, cancellationToken).ConfigureAwait(false);
                reverted.Add(record);
                remaining.Remove(record);
            }

            var currentVersion = remaining.Count == 0 ? 0 : remaining.Max(x => x.Version);

            return new MigrationResult(
                Array.Empty<AppliedMigration>(),
                reverted,
                skippedCount: 0,
                previousVersion,
                currentVersion);
        }

        /// <summary>在一个事务中执行单个回滚并删除历史记录。</summary>
        private async Task RevertOneAsync(
            SqliteConnection connection,
            SqliteMigration migration,
            CancellationToken cancellationToken)
        {
            await using var transaction = (SqliteTransaction)await connection
                .BeginTransactionAsync(cancellationToken)
                .ConfigureAwait(false);

            try
            {
                var context = new SqliteMigrationContext(
                    connection,
                    transaction,
                    _database.DatabasePath);

                await migration.DownAsync(context, cancellationToken).ConfigureAwait(false);
                await DeleteHistoryAsync(connection, transaction, migration.Version, cancellationToken)
                    .ConfigureAwait(false);
                await transaction.CommitAsync(cancellationToken).ConfigureAwait(false);
            }
            catch (OperationCanceledException)
            {
                await SafeRollbackAsync(transaction).ConfigureAwait(false);
                throw;
            }
            catch (Exception exception)
            {
                await SafeRollbackAsync(transaction).ConfigureAwait(false);
                throw new SqliteMigrationException(
                    migration.Version,
                    migration.Description,
                    MigrationDirection.Down,
                    exception);
            }
        }

        /// <summary>校验并排序迁移集合。</summary>
        private static List<SqliteMigration> Normalize(IEnumerable<SqliteMigration> migrations)
        {
            ArgumentNullException.ThrowIfNull(migrations);

            var list = migrations.ToList();
            var versions = new HashSet<long>();

            foreach (var migration in list)
            {
                if (migration is null)
                    throw new ArgumentException("Migration collection cannot contain null.", nameof(migrations));

                if (migration.Version <= 0)
                {
                    throw new ArgumentOutOfRangeException(
                        nameof(migrations),
                        $"Migration version must be greater than 0, but was {migration.Version}.");
                }

                if (string.IsNullOrWhiteSpace(migration.Description))
                {
                    throw new ArgumentException(
                        $"Migration {migration.Version} must have a non-empty description.",
                        nameof(migrations));
                }

                if (!versions.Add(migration.Version))
                {
                    throw new ArgumentException(
                        $"Duplicate migration version: {migration.Version}.",
                        nameof(migrations));
                }
            }

            list.Sort((left, right) => left.Version.CompareTo(right.Version));
            return list;
        }

        /// <summary>回滚失败时尽力清理事务，不覆盖原始异常。</summary>
        private static async Task SafeRollbackAsync(SqliteTransaction transaction)
        {
            try
            {
                await transaction.RollbackAsync(CancellationToken.None).ConfigureAwait(false);
            }
            catch
            {
                // 事务可能已经提交或回滚；这里只做尽力清理。
            }
        }
    }
}
