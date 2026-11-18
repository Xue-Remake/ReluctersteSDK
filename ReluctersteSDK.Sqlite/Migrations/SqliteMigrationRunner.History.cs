using System.Globalization;
using Microsoft.Data.Sqlite;

namespace ReluctersteSDK.Sqlite.Migrations
{
    /// <summary>
    /// <see cref="SqliteMigrationRunner"/> 的历史表读写部分。
    /// </summary>
    public sealed partial class SqliteMigrationRunner
    {
        /// <summary>创建历史表（若不存在）。</summary>
        private async Task EnsureHistoryTableAsync(
            SqliteConnection connection,
            CancellationToken cancellationToken)
        {
            await using var command = connection.CreateCommand();
            command.CommandText =
                $"CREATE TABLE IF NOT EXISTS {QuoteHistoryTable()} (" +
                "\"version\" INTEGER NOT NULL PRIMARY KEY, " +
                "\"description\" TEXT NOT NULL, " +
                "\"applied_at\" TEXT NOT NULL, " +
                "\"execution_ms\" INTEGER NOT NULL);";

            await command.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);
        }

        /// <summary>读取全部历史记录，按版本升序。</summary>
        private async Task<List<AppliedMigration>> ReadAppliedMigrationsAsync(
            SqliteConnection connection,
            CancellationToken cancellationToken)
        {
            var rows = new List<AppliedMigration>();

            await using var command = connection.CreateCommand();
            command.CommandText =
                $"SELECT \"version\", \"description\", \"applied_at\", \"execution_ms\" " +
                $"FROM {QuoteHistoryTable()} ORDER BY \"version\";";

            await using var reader = await command.ExecuteReaderAsync(cancellationToken)
                .ConfigureAwait(false);

            while (await reader.ReadAsync(cancellationToken).ConfigureAwait(false))
            {
                rows.Add(new AppliedMigration(
                    version: reader.GetInt64(0),
                    description: reader.GetString(1),
                    appliedAt: ParseAppliedAt(reader.GetString(2)),
                    executionMilliseconds: reader.GetInt64(3)));
            }

            return rows;
        }

        /// <summary>在迁移事务中写入一条历史记录。</summary>
        private async Task InsertHistoryAsync(
            SqliteConnection connection,
            SqliteTransaction transaction,
            SqliteMigration migration,
            DateTimeOffset appliedAt,
            long executionMilliseconds,
            CancellationToken cancellationToken)
        {
            await using var command = connection.CreateCommand();
            command.Transaction = transaction;
            command.CommandText =
                $"INSERT INTO {QuoteHistoryTable()} " +
                "(\"version\", \"description\", \"applied_at\", \"execution_ms\") " +
                "VALUES (@version, @description, @applied_at, @execution_ms);";

            SqliteUtils.AddParameters(command, new
            {
                version = migration.Version,
                description = migration.Description,
                applied_at = appliedAt.ToString("O", CultureInfo.InvariantCulture),
                execution_ms = executionMilliseconds
            });

            await command.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);
        }

        /// <summary>在回滚事务中删除一条历史记录。</summary>
        private async Task DeleteHistoryAsync(
            SqliteConnection connection,
            SqliteTransaction transaction,
            long version,
            CancellationToken cancellationToken)
        {
            await using var command = connection.CreateCommand();
            command.Transaction = transaction;
            command.CommandText =
                $"DELETE FROM {QuoteHistoryTable()} WHERE \"version\" = @version;";

            SqliteUtils.AddParameters(command, new { version });
            await command.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);
        }

        /// <summary>引用历史表名。</summary>
        private string QuoteHistoryTable() => SqliteUtils.QuoteIdentifier(HistoryTableName);

        /// <summary>解析历史表中的时间；损坏数据回退到最小值。</summary>
        private static DateTimeOffset ParseAppliedAt(string value)
        {
            return DateTimeOffset.TryParse(
                value,
                CultureInfo.InvariantCulture,
                DateTimeStyles.RoundtripKind,
                out var parsed)
                ? parsed
                : DateTimeOffset.MinValue;
        }
    }
}
