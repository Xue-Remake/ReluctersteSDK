namespace ReluctersteSDK.Sqlite.Migrations
{
    /// <summary>
    /// 一条已执行的迁移记录。
    /// </summary>
    public sealed class AppliedMigration
    {
        internal AppliedMigration(
            long version,
            string description,
            DateTimeOffset appliedAt,
            long executionMilliseconds)
        {
            Version = version;
            Description = description;
            AppliedAt = appliedAt;
            ExecutionMilliseconds = executionMilliseconds;
        }

        /// <summary>获取迁移版本号。</summary>
        public long Version { get; }

        /// <summary>获取迁移说明。</summary>
        public string Description { get; }

        /// <summary>获取迁移执行完成的时间（UTC）。</summary>
        public DateTimeOffset AppliedAt { get; }

        /// <summary>获取迁移执行耗时（毫秒，不含事务提交）。</summary>
        public long ExecutionMilliseconds { get; }
    }

    /// <summary>
    /// 一次迁移或回滚操作的结果。
    /// </summary>
    public sealed class MigrationResult
    {
        internal MigrationResult(
            IReadOnlyList<AppliedMigration> applied,
            IReadOnlyList<AppliedMigration> reverted,
            int skippedCount,
            long previousVersion,
            long currentVersion)
        {
            Applied = applied;
            Reverted = reverted;
            SkippedCount = skippedCount;
            PreviousVersion = previousVersion;
            CurrentVersion = currentVersion;
        }

        /// <summary>获取本次实际执行的升级迁移。</summary>
        public IReadOnlyList<AppliedMigration> Applied { get; }

        /// <summary>获取本次实际执行的回滚迁移。</summary>
        public IReadOnlyList<AppliedMigration> Reverted { get; }

        /// <summary>获取本次执行的升级迁移数量。</summary>
        public int AppliedCount => Applied.Count;

        /// <summary>获取本次执行的回滚迁移数量。</summary>
        public int RevertedCount => Reverted.Count;

        /// <summary>获取本次因已执行而跳过的迁移数量。</summary>
        public int SkippedCount { get; }

        /// <summary>获取操作前的数据库版本（最大已执行版本，空库为 0）。</summary>
        public long PreviousVersion { get; }

        /// <summary>获取操作后的数据库版本。</summary>
        public long CurrentVersion { get; }

        /// <summary>获取本次是否实际执行了迁移或回滚。</summary>
        public bool HasChanges => Applied.Count > 0 || Reverted.Count > 0;
    }
}
